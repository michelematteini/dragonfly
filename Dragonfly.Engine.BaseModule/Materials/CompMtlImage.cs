using Dragonfly.Engine.Core;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Base class for a material that is used to process an image.
    /// </summary>
    public abstract class CompMtlImage : CompMaterial
    {
        public CompMtlImage(Component parent) : base(parent)
        {
            Image = new CompTextureRef(this);
            MonitoredParams.Add(Image);
            CullMode = Graphics.CullMode.None;
        }

        /// <summary>
        /// The image to be processed by this material.
        /// </summary>
        public CompTextureRef Image { get; private set; }

    }
}
