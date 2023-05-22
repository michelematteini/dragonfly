using System;
using System.Collections.Generic;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System.Threading.Tasks;
using System.Linq;

namespace Dragonfly.Engine.Core
{
    public partial class CompRenderPass : Component, ICompAllocator, ICompUpdatable
    {
        /// <summary>
        /// When the specified number of draw call in a single pass is reached, additional command lists are created to parallelize work submition.
        /// </summary>
        private const int MaxDrawCallsPerCmdList = 2048;

        /// <summary>
        /// Maximum number of command lists created by a single pass.
        /// </summary>
        private const int MaxCmdListsPerPass = 3;

        class RenderThread : SlimParallel.ITaskBody
        {
            public CommandList CmdList;
            public SortedLinkedList<CompMaterial> MaterialList;
            public int StartCamera;
            public int EndCamera;
            public CompRenderPass Pass;
            internal int StartMaterial;
            internal int EndMaterial;

            public void Execute()
            {
                Pass.FillCommandList(CmdList, MaterialList, StartCamera, EndCamera, StartMaterial, EndMaterial);
                CmdList.QueueExecution();
            }
        }

        private ArrayRange<Float4x4> instanceList;
        private List<RenderThread> renderThreads;
        private int statsFrameID;
        private object statsLock;

        public CompRenderPass(Component parent, string name, CompRenderBuffer renderBuffer) : base(parent)
        {
            Name = name;
            RenderBuffer = renderBuffer;
            RenderToTexture = renderBuffer != null;
            ClearFlags = ClearFlags.ClearTargets | ClearFlags.ClearDepth;
            RequiredPasses = new List<CompRenderPass>();
            CameraList = new List<CompCamera>();
            MaterialFilters = new List<MaterialClassFilter>();
            instanceList = new ArrayRange<Float4x4>(8192);
            renderThreads = new List<RenderThread>();
            statsLock = new object();
            statsFrameID = -1;
            DebugColor = Color.LightBlue;
            ClearValue = Float4.UnitW;
        }

        public CompRenderPass(Component parent, string name) : this(parent, name, null) { }

        public List<MaterialClassFilter> MaterialFilters { get; private set; }

        public UpdateType NeededUpdates => UpdateType.ResourceLoaded;

        public void Update(UpdateType updateType)
        {
            if (renderThreads.Count == 0)
                return;

            // update required cmd lists before rendering
            renderThreads[0].CmdList.RequiredLists.Clear();
            for (int i = 0; i < RequiredPasses.Count; i++)
            {
                if (RequiredPasses[i].CanBeRendered)
                    renderThreads[0].CmdList.RequiredLists.Add(RequiredPasses[i].renderThreads.Last().CmdList);
            }
        }

        /// <summary>
        /// Manage the class name of the first material inclusion filter. If no classes are available, one is added on set.
        /// </summary>
        public string MainClass
        {
            get
            {
                return MaterialFilters.Find(f => f.Type == MaterialClassFilterType.Include).ClassName;
            }
            set
            {
                int filterID = MaterialFilters.FindIndex(f => f.Type == MaterialClassFilterType.Include);
                MaterialClassFilter filterValue = new MaterialClassFilter(MaterialClassFilterType.Include, value);

                if (filterID < 0) MaterialFilters.Add(filterValue); // add an inlcude filter if missing
                else MaterialFilters[filterID] = filterValue; // update filter value if found
            }
        }

        public Float4 ClearValue { get; set; }

        public ClearFlags ClearFlags { get; set; }

        public CompRenderBuffer RenderBuffer { get; set; }

        public Int2 Resolution
        {
            get
            {
                if (RenderBuffer == null)
                    return Context.Scene.Resolution;

                return RenderBuffer.Resolution;
            }
        }

        /// <summary>
        /// Get or sets whether this pass should render to texture.
        /// </summary>
        public bool RenderToTexture { get; set; }

        /// <summary>
        /// Returns true if with the current configuration, rendering to texture is supported.
        /// </summary>
        public bool CanRenderToTexture
        {
            get
            {
                return RenderBuffer != null;
            }
        }

        /// <summary>
        /// Gets or sets a camera for this view. If this camera is left empty, the default scene camera will be used.
        /// </summary>
        public CompCamera Camera
        {
            get { return CameraList.Count == 0 ? null : CameraList[0]; }
            set
            {
                CameraList.Clear();
                if (value != null) CameraList.Add(value);
            }
        }

        public List<CompCamera> CameraList { get; private set; }

        /// <summary>
        /// After this call, the current pass camera list will be the same as the specified pass. 
        /// Any change to the camera list of these two passes will be visible from the other.
        /// All the camera components currently set to this pass will not be released.
        /// </summary>
        /// <param name="otherPass"></param>
        public void LinkCameraListWith(CompRenderPass otherPass)
        {
            CameraList.Clear();
            CameraList = otherPass.CameraList;
        }

        public void UnlinkCameraList()
        {
            CameraList = new List<CompCamera>();
        }

        public int ActiveCameraCount
        {
            get
            {
                int activeCameraCount = 0;
                for (int i = 0; i < CameraList.Count; i++)
                    if (CameraList[i].Active)
                        activeCameraCount++;

                return activeCameraCount;
            }
        }

        /// <summary>
        /// Other render passes that should be rendered before this instance.
        /// <para/> These entries will only be user for pass sorting, and will not cause additional rendering or override the Active flag if diasabled for the listed passes.
        /// </summary>
        public List<CompRenderPass> RequiredPasses { get; internal set; }

        /// <summary>
        /// If set to a valid value, the specified template will be used to draw materials. 
        /// If a template is not available for a particular material, this will not be drawn.
        /// </summary>
        public string OverrideShaderTemplate { get; set; }

        /// <summary>
        /// Get or sets an array of indices that will be used from the RenderBuffer during rendering.
        /// If this value is set to null (default), all the surfaces from the RenderBuffer will be used.
        /// </summary>
        public int[] ActiveBufferIndices { get; set; }

        /// <summary>
        /// Return the render currently used by this render pass, given its index. ActiveBufferIndices are taken into accountif available.
        /// </summary>
        public RenderTarget GetTarget(int index = 0)
        {
            if (ActiveBufferIndices != null)
                return RenderBuffer[ActiveBufferIndices[index]];
            else
                return RenderBuffer[index];
        }

        public void LoadGraphicResources(EngineResourceAllocator g)
        {
            // create and add a new render thread for this pass
            RenderThread rt = new RenderThread();
            rt.Pass = this;
            rt.CmdList = g.CreateCommandList();
            rt.CmdList.ResourceName = Name + ID + "_CMDLIST" + renderThreads.Count;
            if (renderThreads.Count > 0)
                rt.CmdList.RequiredLists.Add(renderThreads.Last().CmdList);
            renderThreads.Add(rt);
        }

        public void ReleaseGraphicResources()
        {
            foreach(RenderThread rt in renderThreads)
            {
                rt.CmdList.Release();
            }
            renderThreads.Clear();
        }

        public bool LoadingRequired
        {
            get
            {
                return renderThreads.Count == 0 || (Stats.ProcessedDrawableCount / renderThreads.Count > MaxDrawCallsPerCmdList) && renderThreads.Count < MaxCmdListsPerPass;
            }
        }

        internal bool CanBeRendered
        {
            get
            {
                return !LoadingRequired && !Disposed && Active && Ready && ActiveCameraCount > 0;
            }
        }

        public void Render()
        {
            SortedLinkedList<CompMaterial> materialList = Context.Scene.Components.QueryMaterials(MaterialFilters);
            int activeCameraCount = ActiveCameraCount;

            if (activeCameraCount > 1)
            {
                // split load on cameras
                int camPerList = (activeCameraCount + renderThreads.Count - 1) / renderThreads.Count;

                for (int i = 0; i < renderThreads.Count; i++)
                {
                    RenderThread rt = renderThreads[i];
                    rt.StartCamera = i == 0 ? 0 : renderThreads[i - 1].EndCamera;
                    rt.EndCamera = rt.StartCamera;
                    for (int rtCamCount = 0; rtCamCount < camPerList && rt.EndCamera < CameraList.Count; rt.EndCamera++)
                        rtCamCount += CameraList[rt.EndCamera].Active.ToInt();
                    rt.MaterialList = materialList;
                    rt.StartMaterial = 0;
                    rt.EndMaterial = materialList.Count;
                    rt.CmdList.StartRecording();
                    SlimParallel.RunAsync(rt);
                }
            }
            else
            {
                // split load on materials
                int materialsPerList = (materialList.Count + renderThreads.Count - 1) / renderThreads.Count;

                for (int i = 0; i < renderThreads.Count; i++)
                {
                    RenderThread rt = renderThreads[i];
                    rt.StartCamera = 0;
                    rt.EndCamera = CameraList.Count;
                    rt.StartMaterial = i * materialsPerList;
                    rt.EndMaterial = Math.Min(materialList.Count, rt.StartMaterial + materialsPerList);
                    rt.MaterialList = materialList;
                    rt.CmdList.StartRecording();
                    SlimParallel.RunAsync(rt);
                }

            }
        }

        /// <summary>
        /// Fill the command list with the draw call the render the specified materials.
        /// </summary>
        internal void FillCommandList(CommandList cmdList, SortedLinkedList<CompMaterial> materialList, int startCamera, int endCamera, int startMaterial, int endMaterial)
        {
            bool templateOverrideEnabled = !string.IsNullOrEmpty(OverrideShaderTemplate);
            int templateOverrideHash = templateOverrideEnabled ? OverrideShaderTemplate.GetHashCode() : 0;

            RenderStats partialPassStats = new RenderStats();
#if VERBOSE
            Context.Scene.Log.WriteLine("Rendering pass: " + ToString());
#endif

#if TRACING
            Context.Scene.Graphics.StartTracedSection(cmdList, DebugColor, Name);
#endif
            cmdList.ResetRenderTargets();

            // setup render targets
            if (RenderToTexture && CanRenderToTexture)
            {
                if (ActiveBufferIndices != null && ActiveBufferIndices.Length > 0)
                {
                    for (int i = 0; i < ActiveBufferIndices.Length; i++)
                        cmdList.SetRenderTarget(RenderBuffer[ActiveBufferIndices[i]], i);
                }
                else
                {
                    for (int i = 0; i < RenderBuffer.SurfaceCount; i++)
                        cmdList.SetRenderTarget(RenderBuffer[i], i);
                }
            }

            // draw each camera in this pass
            for (int camID = startCamera; camID < endCamera; camID++)
            {
                CompCamera camera = CameraList[camID];
                if (!camera.Active) continue;
#if TRACING
                Context.Scene.Graphics.StartTracedSection(cmdList, Color.Purple, camera.Name);
#endif
                RenderStats cameraStats = new RenderStats();

                cmdList.SetViewport(camera.Viewport);
                if (startMaterial == 0 && ClearFlags != ClearFlags.None)
                    cmdList.ClearSurfaces(ClearValue, ClearFlags);

                TiledFloat4x4 cameraTransform = camera.GetTransform();
                Float4x4 cameraMatrix = cameraTransform.Value * camera.GetValue();
                Float4x4 cameraInverse = cameraMatrix.Invert();
                cmdList.SetParam("CAMERA_MATRIX", cameraMatrix);
                cmdList.SetParam("CAMERA_INVERSE", cameraInverse);
                cmdList.SetParam("CAMERA_POS", camera.LocalPosition);
                cmdList.SetParam("CAMERA_DIR", camera.Direction);
                cmdList.SetParam("CAMERA_UP",  camera.UpDirection);
                cmdList.SetParam("PIX_SIZE", 1.0f / (Float2)Resolution / camera.Viewport.Size);
                // DEPRECATED: avoiding using the world tile in shader is possible, making full world-coordinate CPU-only.
                // This make it possible to change or updated them in the future.
                //cmdList.SetParam("WORLD_TILE", cameraTransform.Tile);

                IVolume cameraVolume = camera.Volume;

                // process each material
                int materialID = -1;
                foreach(CompMaterial m in materialList)
                {
                    materialID++;
                    cameraStats.ProcessedDrawableCount++; // pre-count as 1 drawable even if this material is skipped

                    // limit the range of rendered materials
                    if (materialID < startMaterial)
                        continue; // skip materials up to the first one to be rendered
                    else if (materialID >= endMaterial)
                        break; // stop at the last material

                    StartTracedSection(Color.Orange, m.Name);

                    if (!m.Ready // not ready, skip
                        || (templateOverrideEnabled && !m.IsTemplateAvailable(templateOverrideHash)) // do not implement the template required by this pass
                        || (m.VisibleOnlyForCamera != null && m.VisibleOnlyForCamera.ID != camera.ID) // should not be rendered for this camera
                        )
                    {
                        EndTracedSection();
                        continue;
                    }

                    bool materialUpdated = false;

                    // draw each drawable using the current material
                    int drawableCount = m.UsedBy.Count;
                    cameraStats.ProcessedDrawableCount += drawableCount - 1;
                    for (int drawableID = 0; drawableID < drawableCount; drawableID++)
                    {
                        CompDrawable d = m.UsedBy[drawableID];
                        StartTracedSection(Color.Green, d.Name);

                        if (!d.Active || !d.Ready)
                        {
                            EndTracedSection();
                            continue; // drawable not ready or active
                        }

                        StartTracedSection(Color.Red, "Visibility");

                        // test for visibility
                        bool isInstanced = d.Instances.Count > 0;
                        Float4x4 worldMatrix = d.GetTransform().ToFloat4x4(cameraTransform.Tile);
                        if (d.IsBounded)
                        {
                            if (isInstanced)
                            {
                                instanceList.Count = 0;
                                AABox bb = d.GetBoundingBox();
                                Float4x4 instWorld;
                                for (int i = 0; i < d.Instances.Count; i++)
                                {
                                    instWorld = d.Instances[i] * worldMatrix;
                                    if (cameraVolume.Intersects(bb * instWorld))
                                        instanceList.Add(d.Instances[i]);
                                }

                                if (instanceList.Count == 0)
                                {
                                    EndTracedSection();
                                    EndTracedSection();
                                    continue; // drawable not visible
                                }
                            }
                            else
                            {
                                if (!cameraVolume.Intersects(d.GetBoundingBox() * worldMatrix))
                                {
                                    EndTracedSection();
                                    EndTracedSection();
                                    continue; // drawable not visible
                                }
                            }
                        }

                        EndTracedSection();

                        // update transform matrices                 
                        cmdList.SetParam("WORLD_MATRIX", worldMatrix);
                        Float3x3 nrmMatrix = ((Float3x3)worldMatrix).Invert().Transpose();
                        cmdList.SetParam("NRM_WORLD_MATRIX", nrmMatrix);

                        // update the parent material before the first drawable uses it
                        if (!materialUpdated)
                        {
                            cmdList.SetShader(m.Shaders[templateOverrideEnabled ? templateOverrideHash : m.DefaultTemplateHash]);
                            materialUpdated = true;
                        }

                        // setup geometry
                        VertexBuffer vb = d.GetVertexBuffer();
                        cmdList.SetVertices(vb);
                        IndexBuffer ib = d.GetIndexBuffer();
                        cmdList.SetIndices(ib);

                        // draw
                        if (isInstanced)
                            cmdList.DrawIndexedInstanced(instanceList);
                        else
                            cmdList.DrawIndexed();

                        // update stats
                        cameraStats.PolygonCount += (isInstanced ? instanceList.Count : 1) * (ib == null ? vb.VertexCount : ib.IndexCount) / 3;
                        cameraStats.DrawCallCount++;

                        EndTracedSection();
                    } // foreach drawable
           
                    EndTracedSection();
                } // foreach material

                // merge and update camera stats
                partialPassStats += cameraStats;
                lock (camera.StatsLock)
                {
                    camera.Stats = cameraStats + (camera.StatsFrameID < Context.Time.FrameIndex ? new RenderStats() : camera.Stats);
                    camera.StatsFrameID = Context.Time.FrameIndex;
                }

#if TRACING
                Context.Scene.Graphics.EndTracedSection(cmdList); // end camera section
#endif
            
            } // end foreach camera

            // update stats
            lock (statsLock)
            {
                Stats = partialPassStats + (statsFrameID < Context.Time.FrameIndex ? new RenderStats() : Stats);
                statsFrameID = Context.Time.FrameIndex;
            }

#if TRACING
            Context.Scene.Graphics.EndTracedSection(cmdList);
#endif
        }

        public RenderStats Stats { get; private set; }
        /// <summary>
        /// A color that will be used for debug purposes to mark this pass (e.g. frame captures).
        /// </summary>
        public Byte4 DebugColor { get; set; }

        public override string ToString()
        {
            return string.Format("Pass: {0}, Target: {1}", Name, RenderBuffer != null ? RenderBuffer.ToString() : "[ Screen ]");           
        }
    }


}
