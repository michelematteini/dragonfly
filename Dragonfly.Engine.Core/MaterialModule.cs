using Dragonfly.Graphics.Resources;
using System;
using System.Collections.Generic;

namespace Dragonfly.Engine.Core
{
    /// <summary>
    /// Contains a set of material parameters that can be used by different materials.
    /// </summary>
    public abstract class MaterialModule
    {
        protected CompMaterial Material;

        public MaterialModule(CompMaterial parentMaterial)
        {
            Material = parentMaterial;
            Material.AddModule(this);
        }

        protected CompMaterial.Param<T> MakeParam<T>(T value)
        {
            return new CompMaterial.Param<T>(Material, value);
        }

        protected IList<Component> MonitoredParams { get { return Material.MonitoredParams; } }

        protected EngineContext Context { get { return Material.Context; } }

        protected internal abstract void UpdateAdditionalParams(Shader s);

    }
}
