using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A generic baker that render to a custom buffer with the given material.
    /// </summary>
    public class CompBakerScreenSpace : Component
    {
        private CompScreenPass copyPass;

        public CompBakerScreenSpace(Component parent, Int2 resolution, SurfaceFormat[] bufferSurfaces, CompMaterial material = null) : base(parent)
        {
            CompRenderBuffer destBuffer = new CompRenderBuffer(this, bufferSurfaces, resolution.X, resolution.Y);
            Initialize(parent, destBuffer, material);
        }

        public CompBakerScreenSpace(Component parent, CompRenderBuffer destBuffer, CompMaterial material = null) : base(parent)
        {
            Initialize(parent, destBuffer, material);
        }

        private void Initialize(Component parent, CompRenderBuffer destBuffer, CompMaterial material)
        {
            // create texture copy pass
            copyPass = new CompScreenPass(this, "ScreenBakePass" + ID, destBuffer);
            copyPass.Material = material;

            // create baker
            Baker = new CompBaker(this, copyPass.Pass, null, new CompEvent(this, CanStartBaking));
        }

        /// <summary>
        /// Material used for baking. No baking will occur when this is set to null.
        /// </summary>
        public CompMaterial Material
        {
            get
            {
                return copyPass.Material;
            }
            set
            {
                copyPass.Material = value;
            }
        }

        private bool CanStartBaking()
        {
            return Material != null && Material.Ready;
        }

        public CompBaker Baker { get; private set; }
    }
}
