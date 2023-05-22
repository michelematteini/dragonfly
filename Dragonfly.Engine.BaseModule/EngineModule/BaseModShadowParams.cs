using System;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class BaseModShadowParams
    {
        public enum CascadeMode
        {
            /// <summary>
            /// Shadowmap LODs are distributed along the visible frustum.
            /// </summary>
            FrustumSlicing,
            /// <summary>
            /// Shadowmap LODs are build around the view position. 
            /// Better if a lot of non-realtime slices need to be used since it avoid cache miss on camera rotations and movements.
            /// </summary>
            PositionCentered
        }

        public BaseModShadowParams(int atlasResolution)
        {
            AtlasResolution = atlasResolution.CeilPower2();
            MaxOccluderDistance = 4000.0f;
            CascadeCount = 5;
            CascadePerFrameCount = 4;
            MaxShadowDistance = 5000.0f;
            CascadedShadowsLambda = 7.0f;
            MaxShadowMapResolution = AtlasResolution / 4;
            MinShadowMapResolution = AtlasResolution / 64;
            MaxDynamicShadowMaps = 16;
            QualityDistributionRefreshSeconds = 0.5f;
            DefaultDepthBias = -1.000f;
            DefaultNormalBias = -0.500f;
            DefaultQuantizationBias = -2.000f;
        }

        public int AtlasResolution { get; set; }

        public float MaxOccluderDistance { get; set; }

        public float MaxShadowDistance { get; set; }

        /// <summary>
        /// Number of shadow map splits rendered for each directional light
        /// </summary>
        public int CascadeCount { get; set; }

        /// <summary>
        /// Number of shadow map splits updated each frame for directional lights
        /// </summary>
        public int CascadePerFrameCount { get; set; }

        public float CascadedShadowsLambda { get; set; }

        public int MaxShadowMapResolution { get; set; }

        public int MinShadowMapResolution { get; set; }

        /// <summary>
        /// Specify after how many seconds the shadow quality allocated to each light is updated.
        /// </summary>
        public float QualityDistributionRefreshSeconds { get; set; }

        /// <summary>
        /// Max number of Dynamic shadow maps that can be active toghether.
        /// </summary>
        public int MaxDynamicShadowMaps { get; set; }

        /// <summary>
        /// How cascades for directional lights are positioned.
        /// </summary>
        public CascadeMode CascadedShadowsMode { get; set; }

        /// <summary>
        /// Default depth bias applied during the shadow pass, lower values reduce overall aliasing. 
        /// Modifying this value won't affect existing materials, it's just a default for new materials.
        /// </summary>
        public float DefaultDepthBias { get; set; }

        /// <summary>
        /// Default normal bias applied during the shadow pass, lower values reduce aliasing to surfaces that are parallel to the light direction. 
        /// The value represents an offset in texels of the shadowmap.
        /// Modifying this value won't affect existing materials, it's just a default for new materials.
        /// </summary>
        public float DefaultNormalBias { get; set; }

        /// <summary>
        /// Default bias applied during the shadow pass to compensate shadowmaps low resolution. 
        /// A value of x will compensate quantization for depth slopes of maximum atan(-x) degrees.
        /// Modifying this value won't affect existing materials, it's just a default for new materials.
        /// </summary>
        public float DefaultQuantizationBias { get; set; }
    }
}
