using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// List of pass class names used by the base module.
    /// </summary>
    public class BaseModMaterialClasses
    {
        public BaseModMaterialClasses()
        {
            Solid = "Solid";
            UI = "UiMaterial";
            DirectionalLightFilter = "DirLightFilter";
        }

        public string Solid { get; set; }

        public string DirectionalLightFilter { get; set; }

        public string UI { get; set; }
    }

}

