using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// List of Shader templates used for various rendering passes.
    /// </summary>
    public class BaseModShaderTemplates
    {
        public BaseModShaderTemplates()
        {
            DepthPrePass = "DepthPrePass";
            ShadowMaps = "ShadowMap";
        }

        public string DepthPrePass { get; private set; }

        public string ShadowMaps { get; private set; }
    }
}
