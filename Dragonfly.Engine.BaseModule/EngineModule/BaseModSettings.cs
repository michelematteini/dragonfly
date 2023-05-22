using Dragonfly.Graphics;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Contains all the settings used to initialize the Base Module components.
    /// </summary>
    public class BaseModSettings
    {
        public static BaseModSettings Default
        {
            get
            {
                BaseModSettings settings = new BaseModSettings();
                settings.Shadows = new BaseModShadowParams(4096);
                settings.UI = new BaseModUiSettings();
                settings.MaterialClasses = new BaseModMaterialClasses();
                settings.ShaderTemplates = new BaseModShaderTemplates();
                settings.GlobalAlphaTestTHR = 0.5f;
                settings.LightsClipIntensity = 0.50f;
                settings.DefaultCullMode = CullMode.CounterClockwise;
                return settings;
            }
        }

        public BaseModShadowParams Shadows { get; private set; }

        public BaseModUiSettings UI { get; private set; }

        public BaseModMaterialClasses MaterialClasses { get; private set; }

        public BaseModShaderTemplates ShaderTemplates { get; private set; }

        /// <summary>
        /// Global alpha test threashold that should be the same across all rendering pipeline stages.
        /// </summary>
        public float GlobalAlphaTestTHR { get; set; }

        /// <summary>
        /// Intensity at which lights are clipped (considered as not visible / contributing to the scene).
        /// </summary>
        public float LightsClipIntensity { get; set; }

        /// <summary>
        /// Triangle orientation that is considered facing backward and not to be drawn by default.
        /// </summary>
        public CullMode DefaultCullMode { get; set; }

    }


}
