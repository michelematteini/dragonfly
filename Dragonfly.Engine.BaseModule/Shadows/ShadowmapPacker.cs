using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Used to computing the optimal shadowmap resolution for a set of lights and a given budget.
    /// </summary>
    internal class ShadowmapPacker
    {
        public struct Allocation
        {
            public int Resolution;
            public bool IsStatic;

            public override string ToString() { return (IsStatic ? "Static" : "Dynamic") + ", " + Resolution; }
        }

        private Int2 atlasResolution;
        private CompCamera viewCamera;
        private List<CompLight> lightList;
        private BaseModShadowParams settings;
        private float coverageToResPower; // the power applied to the coveraged to convert it to resolution.
        private float shadowQualityDistribution; // affects how fast shadowmaps decrease in resolution with lower coverages. Higher values allows to fit more lower-quality shadowmaps in the atlas.

        public ShadowmapPacker(BaseModShadowParams settings)
        {
            Allocations = new Dictionary<CompLight, Allocation>();
            lightList = new List<CompLight>();
            this.settings = settings;
            coverageToResPower = 0.5f;
            shadowQualityDistribution = 1.0f / settings.MaxDynamicShadowMaps;

        }

        public void InitializeAllocationParams(Int2 atlasResolution, CompCamera viewCamera, IReadOnlyList<CompLight> lightList)
        {
            this.atlasResolution = atlasResolution;
            this.viewCamera = viewCamera;
            this.lightList.Clear();
            foreach (CompLight l in lightList)
                this.lightList.Add(l);
        }

        /// <summary>
        /// The last computed shadowmap allocation scheme
        /// </summary>
        public Dictionary<CompLight, Allocation> Allocations { get; private set; }

        /// <summary>
        /// Try to fit the most light shadows in the shadow map atlas. 
        /// <para/> Returns the required sm reolution for each light and the update model.
        /// </summary>
        public void GenerateAllocationMap()
        {
            List<Pair<CompLight, float>> smLights = GetSortedShadowCastingLightsAndCoverage();
            int atlasPixCount = atlasResolution.X * atlasResolution.Y, freeSmPixels = 0;
            int atlasPixShortageCount = 0;

            // iteratively find a balanced allocation scheme
            for (int i = 0; i < 10; i++)
            {
                // adjust coverage LOD power based on current  atlas usage           
                if (atlasPixShortageCount > 0)
                    DecreaseLOD();
                else if (freeSmPixels > atlasPixCount / 2)
                    IncreaseLOD();
                else if (i > 0)
                    break; // correct scheme found!

                // try to fit all shadows with the specified lod
                Allocations.Clear();
                atlasPixShortageCount = 0;
                freeSmPixels = atlasPixCount;
                int dynamicShadowsLeft = settings.MaxDynamicShadowMaps;
                for (int li = 0; li < smLights.Count; li++)
                {
                    Allocation alloc;
                    alloc.Resolution = CoverageToSMResolution(smLights[li].Second, li);
                    int requiredShadowMaps = GetRequiredShadowMaps(smLights[li].First);
                    int requiredPix = alloc.Resolution * alloc.Resolution * requiredShadowMaps;

                    // if this shadow won't fit the atlas, skip it but track the ammount of unavailable extra pixels that would be required.
                    if (freeSmPixels < requiredPix)
                    {
                        atlasPixShortageCount += requiredPix;
                        continue;
                    }

                    // decide whether to make this shadow static or dynamic
                    alloc.IsStatic = dynamicShadowsLeft < requiredShadowMaps;
                    if (!alloc.IsStatic) dynamicShadowsLeft -= requiredShadowMaps;

                    // add allocation
                    Allocations.Add(smLights[li].First, alloc);
                    freeSmPixels -= requiredPix;
                }
            }
        }

        private int GetRequiredShadowMaps(CompLight l)
        {
            if (l is CompLightDirectional)
                return settings.CascadeCount;
            else if (l is CompLightPoint)
                return 6;

            return 1;
        }

        private int CoverageToSMResolution(float coverage, int priorityIndex)
        {
            int coverageRes = ((int)(settings.MaxShadowMapResolution * Math.Pow(coverage, coverageToResPower))).RoundPower2();
            int resDivider = Math.Max(1, (priorityIndex * shadowQualityDistribution).FloorPower2());
            return Math.Max(coverageRes / resDivider, settings.MinShadowMapResolution);
        }


        /// <summary>
        /// Increase shadow LOD by increasing thier coverage-based resolution distribution.
        /// </summary>
        public void DecreaseLOD()
        {
            shadowQualityDistribution = (shadowQualityDistribution * 2).Clamp(1.0f / settings.MaxDynamicShadowMaps, 32.0f);
        }

        /// <summary>
        /// Decrease shadow LOD by decreasing thier coverage-based resolution distribution.
        /// </summary>
        public void IncreaseLOD()
        {
            shadowQualityDistribution = (shadowQualityDistribution / 2).Clamp(1.0f / settings.MaxDynamicShadowMaps, 32.0f);
        }

        private List<Pair<CompLight, float>> GetSortedShadowCastingLightsAndCoverage()
        {
            List<Pair<CompLight, float>> shadowCastingLights = new List<Pair<CompLight, float>>();

            // camera view space approximation
            Float4x4 cameraMatrix = viewCamera.GetTransform().Value * viewCamera.GetValue();
            Float3 cameraPos = viewCamera.GetTransform().Value.Origin, cameraDir = viewCamera.Direction;
            Sphere shadowBoundaries = new Sphere(cameraPos, settings.MaxShadowDistance);

            foreach (CompLight l in lightList)
            {
                if (!l.CastShadow) continue;

                float coverage = 1.0f; // light is everywhere, maximum priority

                if (l.HasPosition)
                {
                    AABox lightBox = l.GetBoundingBox();

                    if (!shadowBoundaries.Intersects(lightBox))
                        continue; // the shadow would be too far away, skip

                    // rotate box in front of the camera (ignore where the camera is pointing, would make shadows pop in and out too often)
                    Float3 dirToLight = (lightBox.Center - cameraPos).Normal();
                    Float4x4 inFrontRotation = Float4x4.Translation(-cameraPos) * Float4x4.Rotation(dirToLight, cameraDir) * Float4x4.Translation(cameraPos);
                    lightBox *= inFrontRotation * cameraMatrix;

                    // calculate coverage
                    AARect screenCoverageRect = AARect.Bounding(lightBox.Min.XY.Clamp(-1, 1), lightBox.Max.XY.Clamp(-1, 1));
                    coverage = screenCoverageRect.Width * screenCoverageRect.Height * 0.25f;
                    coverage = coverage.Clamp(0, 0.99999f); // priority always lower than directional lights when sorting.
                }

                shadowCastingLights.Add(new Pair<CompLight, float>(l, coverage));
            }

            shadowCastingLights.Sort((t1, t2) => Math.Sign(t2.Second - t1.Second));
            return shadowCastingLights;
        }
    }
}
