using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System.Collections.Generic;
using System;

namespace Dragonfly.BaseModule
{
    internal class CompShadowAtlas : Component
    {
        private static readonly ShadowState NO_SHADOWS = new ShadowState(null, null, 0);
        private Dictionary<CompLight, ShadowState> smStates;
        private BaseModShadowParams settings;
        private float[] csmSplitDepths;
        private ShadowmapPacker shadowPacking;
        private CompTaskScheduler.ITask shadowPackingTask;

        internal CompShadowAtlas(Component parent, CompRenderPass requiredBy) : base(parent)
        {
            BaseMod baseMod = Context.GetModule<BaseMod>();
            settings = baseMod.Settings.Shadows;
            smStates = new Dictionary<CompLight, ShadowState>();

            ShadowAtlas = new TextureAtlas(this, "ShadowAtlas", baseMod.Settings.MaterialClasses.Solid, Graphics.SurfaceFormat.Half, new AtlasLayoutQuadTree(settings.AtlasResolution), baseMod.Settings.ShaderTemplates.ShadowMaps);
            ShadowAtlas.Pass.ClearValue = Color.Black.ToFloat4(); // this will be the farthest z since its inverted in shader
            requiredBy.RequiredPasses.Add(ShadowAtlas.Pass);

            shadowPacking = new ShadowmapPacker(settings);
            shadowPackingTask = GetComponent<CompTaskScheduler>().CreateTask("GenerateShadowAllocationMap", shadowPacking.GenerateAllocationMap, settings.QualityDistributionRefreshSeconds);
        }

        internal TextureAtlas ShadowAtlas { get; private set; }

        internal void Update()
        {
            RemoveInactiveLightShadows();
            UdpateShadowRenderStates();

            if (shadowPackingTask.State == CompTaskScheduler.TaskState.Completed)
            {
                UpdateShadowMapAllocations();
                shadowPackingTask.Reset();
            }

            if (shadowPackingTask.State == CompTaskScheduler.TaskState.Idle)
            {
                shadowPacking.InitializeAllocationParams(ShadowAtlas.Layout.Resolution, GetComponent<CompLightTableManager>().GetReferenceCamera(), GetComponents<CompLight>());
                shadowPackingTask.QueueExecution();
            }

            UpdateShadowCameras();

            // commits atlas to shaders
            Context.Scene.Globals.SetParam("shadowAtlas", ShadowAtlas.Texture);
        }

        List<CompLight> inactiveLights = new List<CompLight>();
        private void RemoveInactiveLightShadows()
        {
            inactiveLights.Clear();

            // search for the no longer available lights
            foreach (CompLight l in smStates.Keys)
            {
                if (l.Disposed || !l.Active || !l.CastShadow)
                    inactiveLights.Add(l);
            }

            // remove their state
            foreach (CompLight l in inactiveLights)
            {
                smStates[l].Delete();
                smStates.Remove(l);
            }
        }

        private void UdpateShadowRenderStates()
        {
            foreach (ShadowState s in smStates.Values)
            {
                if (s.QueuedForRender)
                    s.Rendered = true;

                s.QueuedForRender = !s.IsStatic || !s.Rendered; // static shadows are rendered only once
            }
        }

        private void UpdateShadowMapAllocations()
        {
            // compute an updated allocation table
            Dictionary<CompLight, ShadowmapPacker.Allocation> smAllocations = shadowPacking.Allocations;

            // update allocations 1 - already existing ones that are unchanged or decrease in size
            CompLight[] allocatedSMs = new CompLight[smStates.Count];
            smStates.Keys.CopyTo(allocatedSMs, 0);
            for (int i = 0; i < allocatedSMs.Length; i++)
            {
                CompLight l = allocatedSMs[i];
                ShadowState curSmState = smStates[l];
                ShadowmapPacker.Allocation newAllocation;

                if (!smAllocations.TryGetValue(l, out newAllocation))
                {
                    // no longer required, delete
                    smStates[l].Delete();
                    smStates.Remove(l);
                }
                else if (newAllocation.Resolution == curSmState.Resolution)
                {
                    // light allocation unchanged, remove from the allocation list
                    curSmState.IsStatic = newAllocation.IsStatic;
                    smAllocations.Remove(l);
                }
                else if (newAllocation.Resolution < curSmState.Resolution)
                {
                    // required size decreased
                    curSmState.ReleaseShadowMaps();
                    if (!curSmState.TryAllocShadowMaps(newAllocation.Resolution))
                        throw new Exception("Failed shadowMap allocation!"); // wtf ? cannot allocate LESS space? 

                    curSmState.IsStatic = newAllocation.IsStatic;
                    curSmState.Rendered = false;
                    curSmState.QueuedForRender = true;
                    smAllocations.Remove(l);
                }
            }

            // update allocations 2 - new shadows and shadows that need more resolution
            foreach (CompLight l in smAllocations.Keys)
            {
                ShadowmapPacker.Allocation newAllocation = smAllocations[l];
                ShadowState curSmState;

                if (!smStates.TryGetValue(l, out curSmState))
                {
                    // new allocation
                    if (TryAllocLightSM(l, newAllocation.Resolution))
                    {
                        curSmState = smStates[l];
                        curSmState.IsStatic = newAllocation.IsStatic;
                    }
                    // else: not enough space, this can be caused by a miscalculated allocation table, skip this light
                }
                else
                {
                    // update resolution
                    int prevResolution = curSmState.Resolution;
                    curSmState.ReleaseShadowMaps();

                    if (!curSmState.TryAllocShadowMaps(newAllocation.Resolution))
                    {
                        // not enough space, this can be caused by a miscalculated allocation table, restore previous size
                        if (!curSmState.TryAllocShadowMaps(prevResolution))
                            throw new Exception("Failed shadowMap allocation!"); // this can only mean a bug in atlas allocations.
                    }

                    curSmState.IsStatic = newAllocation.IsStatic;
                    curSmState.Rendered = false;
                    curSmState.QueuedForRender = true;
                }
            }
        }

        private bool TryAllocLightSM(CompLight l, int resolution)
        {
            if (l is CompLightDirectional dl)
                return TryAllocDirectionalLightSM(dl, resolution);
            else if (l is CompLightSpot sl)
                return TryAllocSpotLightSM(sl, resolution);
            else if (l is CompLightPoint pl)
                return TryAllocPointLightSM(pl, resolution);
            else
                return false;
        }

        private bool TryAllocDirectionalLightSM(CompLightDirectional l, int resolution)
        {
            ShadowState shadowState = new ShadowState(l, ShadowAtlas, settings.CascadeCount);

            // alloc all cascades textures
            if (!shadowState.TryAllocShadowMaps(resolution))
                return false; // not enough texture space available

            // create a shadow camera for each cascade
            CompCamCascade previousSlice = null;
            for (int i = 0; i < settings.CascadeCount; i++)
            {
                shadowState.CameraTransforms[i] = new CompTransformStack(this);
                CompCamCascade shadowCamera = new CompCamCascade(shadowState.CameraTransforms[i]);
                shadowCamera.DistanceFromPoints.Set(settings.MaxOccluderDistance);
                shadowCamera.PreviousSlice = previousSlice;
                previousSlice = shadowCamera;

                shadowState.CameraList[i] = shadowCamera; // add it the shadow state
            }

            shadowState.QueuedForRender = true;
            smStates[l] = shadowState;
            return true;
        }

        private bool TryAllocSpotLightSM(CompLightSpot l, int resolution)
        {
            ShadowState shadowState = new ShadowState(l, ShadowAtlas, 1);

            // alloc a subtexture
            if (!shadowState.TryAllocShadowMaps(resolution))
                return false; // not enough texture space available

            // create the shadowmap camera
            shadowState.CameraTransforms[0] = new CompTransformStack(this);
            CompCamPerspective shadowCamera = new CompCamPerspective(shadowState.CameraTransforms[0]);
            shadowCamera.AutoAspectRatio = false;
            shadowCamera.AspectRatio.Set(1.0f);
            shadowCamera.NearPlane = 0.1f;
            shadowState.CameraList[0] = shadowCamera; // add it the shadow state

            shadowState.QueuedForRender = true;
            smStates[l] = shadowState;
            return true;
        }

        private bool TryAllocPointLightSM(CompLightPoint l, int resolution)
        {
            ShadowState shadowState = new ShadowState(l, ShadowAtlas, 6);

            // alloc all subtextures
            if (!shadowState.TryAllocShadowMaps(resolution))
                return false; // not enough texture space available

            // create the cube shadowmap cameras
            CubeMapHelper.CreateFaceCameras(this, shadowState.CameraList, shadowState.CameraTransforms);

            shadowState.QueuedForRender = true;
            smStates[l] = shadowState;
            return true;
        }

        private void UpdateShadowCameras()
        {
            CompCamera viewCamera = GetComponent<CompLightTableManager>().GetReferenceCamera();
            ViewFrustum viewFrustum = viewCamera.ViewFrustum;
            Int3 viewTile = viewCamera.GetTransform().Tile;

            // calculate frustum split points for directional lights cascades
            {
                // initialize split point arrays
                if (csmSplitDepths == null || csmSplitDepths.Length != (settings.CascadeCount + 1))
                    csmSplitDepths = new float[settings.CascadeCount + 1];

                // calc split depths for each slice
                float maxDepth = System.Math.Min(settings.MaxShadowDistance, viewFrustum.Depth);
                for (int i = 0; i <= settings.CascadeCount; i++)
                    csmSplitDepths[i] = FMath.ExpInterp(0.0f, maxDepth, settings.CascadedShadowsLambda, (float)i / settings.CascadeCount);
            }

            // update shadow cameras
            foreach (CompLight l in smStates.Keys)
            {
                ShadowState shadowState = smStates[l];

                if (shadowState.IsStatic && shadowState.Rendered)
                    continue;

                if (l is CompLightDirectional dl)
                {
                    Float3 lightDir = dl.Direction;

                    for (int i = 0; i < shadowState.CameraList.Length; i++)
                    {
                        CompCamCascade curCamera = shadowState.CameraList[i] as CompCamCascade;
                        curCamera.Active = true;

                        // alternate the rendering of the most far away shadow maps
                        if (shadowState.Rendered && i > settings.CascadePerFrameCount - 2)
                            curCamera.Active = (Context.Time.FrameIndex + i) % (settings.CascadeCount - settings.CascadePerFrameCount + 1) == 0;

                        if (!curCamera.Active)
                            continue;

                        curCamera.LightDirection.Set(lightDir);
                        curCamera.ViewDirection = viewCamera.Direction;
                        curCamera.ViewTile = viewTile;
                        curCamera.Viewport = shadowState.ShadowMaps[i].Area;
                        curCamera.SnappingResolution = shadowState.Resolution;
                        if (settings.CascadedShadowsMode == BaseModShadowParams.CascadeMode.FrustumSlicing)
                            curCamera.UpdateView(viewFrustum, csmSplitDepths[i], csmSplitDepths[i + 1]);
                        else if (settings.CascadedShadowsMode == BaseModShadowParams.CascadeMode.PositionCentered)
                            curCamera.UpdateView(new Sphere(viewCamera.LocalPosition, csmSplitDepths[i + 1]));
                    }
                }
                else if (l is CompLightSpot sl)
                {
                    Float3 lightDir = sl.Direction;
                    Float3 lightPos = sl.Position;

                    CompTransformStack curTransform = shadowState.CameraTransforms[0];
                    curTransform.Set(Float4x4.LookAt(lightPos, lightDir, Float3.NotParallelAxis(lightDir, Float3.UnitY)));

                    CompCamPerspective curCamera = shadowState.CameraList[0] as CompCamPerspective;
                    curCamera.FarPlane = System.Math.Min(settings.MaxOccluderDistance, sl.GetClippingDistance());
                    curCamera.NearPlane = GetLightPreferredNear(curCamera.FarPlane);
                    curCamera.FOV.Set(sl.OuterConeAngleRadians);
                    curCamera.Viewport = shadowState.ShadowMaps[0].Area;
                }
                else if (l is CompLightPoint pl)
                {
                    Float3 lightPos = pl.Position;
                    float plFar = System.Math.Min(settings.MaxOccluderDistance, pl.GetClippingDistance());
                    float plNear = GetLightPreferredNear(plFar);
                    CubeMapHelper.SetFaceCamerasPosition(shadowState.CameraTransforms, lightPos);

                    for (int i = 0; i < shadowState.CameraList.Length; i++)
                    {
                        CompCamPerspective faceCamera = shadowState.CameraList[i] as CompCamPerspective;
                        faceCamera.FarPlane = plFar;
                        faceCamera.NearPlane = plNear;
                        faceCamera.Viewport = shadowState.ShadowMaps[i].Area;
                    }
                }
            }
        }

        private float GetLightPreferredNear(float farPlane)
        {
            return (farPlane * 0.0002f).Clamp(0.005f, 1.0f);
        }

        #region Shadowmap light table

        Float4[] smRecordCache = new Float4[LightTable.SM_STRUCT_SIZE4];
        internal void FillShadowmapTable(LightTable lightTable)
        {
            Int3 worldTile = Context.GetModule<BaseMod>().CurWorldTile;

            foreach (ShadowState s in smStates.Values)
            {
                // flag lights shadows that have not be rendered (or will not be rendered this frame) as empty
                if (!s.Rendered && !s.QueuedForRender)
                {
                    s.LightTableIndex = -1;
                    continue;
                }

                smRecordCache[5].XYZ = s.Position;
                s.LightTableIndex = lightTable.ShadowMapCount;

                for (int i = 0; i < s.ShadowMaps.Length; i++)
                {
                    Float4x4 shadowProj = s.CameraList[i].GetTransform().Rebase(worldTile).Value * s.CameraList[i].GetValue();
                    smRecordCache[0] = shadowProj.GetColumn(0);
                    smRecordCache[1] = shadowProj.GetColumn(1);
                    smRecordCache[2] = shadowProj.GetColumn(2);
                    smRecordCache[3] = shadowProj.GetColumn(3);
                    smRecordCache[4].XY = s.ShadowMaps[i].Area.Size;
                    smRecordCache[4].ZW = s.ShadowMaps[i].Area.Min;
                    smRecordCache[5].W = CalcBlurRadius(s, i);
                    Float4x4 shadowProjInverse = shadowProj.Invert();
                    smRecordCache[6] = shadowProjInverse.GetColumn(0);
                    smRecordCache[7] = shadowProjInverse.GetColumn(1);
                    smRecordCache[8] = shadowProjInverse.GetColumn(2);
                    smRecordCache[9] = shadowProjInverse.GetColumn(3);
                    lightTable.AddSerializedShadowState(smRecordCache);
                }
            }
        }

        private float CalcBlurRadius(ShadowState s, int smIndex)
        {
            float blurRadius = 0;

            if (s.ParentLight is CompLightDirectional dirLight)
            {
                // retrieve csm slice
                CompCamCascade csmSlice = s.CameraList[smIndex] as CompCamCascade;
                blurRadius = (dirLight.RadiusMeters.GetValue() / dirLight.AvgDistanceMeters) * csmSlice.DistanceFromPoints.GetValue() / csmSlice.SliceSize;
            }
            else if (s.ParentLight is CompLightSpot spotLight)
            {
                // retrieve the light perspective camera
                CompCamPerspective lightCam = s.CameraList[smIndex] as CompCamPerspective;
                blurRadius = spotLight.Radius.GetValue() * (lightCam.FarPlane - lightCam.NearPlane) / ((float)System.Math.Tan(lightCam.FOV.GetValue() / 2.0f) * lightCam.FarPlane * lightCam.NearPlane);
            }
            else if (s.ParentLight is CompLightPoint pointLight)
            {
                // retrieve the light perspective camera
                CompCamPerspective lightCam = s.CameraList[smIndex] as CompCamPerspective;
                blurRadius = pointLight.Radius.GetValue() * (lightCam.FarPlane - lightCam.NearPlane) / ((float)System.Math.Tan(lightCam.FOV.GetValue() / 2.0f) * lightCam.FarPlane * lightCam.NearPlane);
            }

            return blurRadius;
        }

        internal float GetFirstShadowmapIndex(CompLight l)
        {
            ShadowState shadowState;
            if (!l.CastShadow || !smStates.TryGetValue(l, out shadowState))
                return -1;

            return shadowState.LightTableIndex;
        }

        internal float GetShadowmapsCount(CompLight l)
        {
            ShadowState shadowState;
            if (!l.CastShadow || !smStates.TryGetValue(l, out shadowState))
                return 0;

            return shadowState.ShadowMaps.Length;
        }

        #endregion
    }

}