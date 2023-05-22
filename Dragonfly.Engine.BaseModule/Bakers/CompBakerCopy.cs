using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A baker that just copy the input texture to an output buffers.
    /// </summary>
    public class CompBakerCopy : Component
    {
        private CompRenderBuffer destBuffer;

        public CompBakerCopy(Component parent, Int2 resolution) : base(parent)
        {
            destBuffer = new CompRenderBuffer(parent, Graphics.SurfaceFormat.Color, resolution.X, resolution.Y);
            InitializeBaker();
        }

        public CompBakerCopy(Component parent, CompRenderBuffer destBuffer) : base(parent)
        {
            this.destBuffer = destBuffer;
            InitializeBaker();
        }

        private void InitializeBaker()
        {
            // create texture copy pass
            CompScreenPass copyPass = new CompScreenPass(this, Name + ID + "CopyPass", destBuffer);
            CompMtlImgCopy copyMat = new CompMtlImgCopy(copyPass);
            InputImage = copyMat.Image;
            copyPass.Material = copyMat;

            // create baker
            Baker = new CompBaker(this, copyPass.Pass, null, new CompEvent(this, () => InputImage.LoadedChanged));
        }

        public CompBaker Baker { get; private set; }

        public CompTextureRef InputImage { get; private set; }
    }
}
