using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Given an input environment map in equirect format, bake a Cubemap2D to a texture.
    /// </summary>
    public class CompBakerEquirectToCube2D : Component
    {
        /// <summary>
        /// Create a new cubemap baker that start rendering the cubemap as soon as its InputEnviromentMap property is assigned to a valid texure.
        /// </summary>
        /// <param name="cubemapEdgeSize">The edge size in pixel of the cubemap to be generated. The total surface of the result will be 4 cubemapEdgeSize wide and 2 cubemapEdgeSize tall. This value will be ceiled to a power of 2.</param>
        /// <param name="rotationRadiants">An horizontal rotation to be applied to the input image.</param>
        /// <param name="exposureMul">An exposure multiplier to be applied to the input image.</param>
        public CompBakerEquirectToCube2D(Component parent, int cubemapEdgeSize, float rotationRadiants, float exposureMul) : base(parent)
        {
            cubemapEdgeSize = cubemapEdgeSize.CeilPower2();

            // setup equirect to cube2D pass
            CompRenderBuffer cubemapBuffer = new CompRenderBuffer(this, Graphics.SurfaceFormat.Color, cubemapEdgeSize * 4, cubemapEdgeSize * 2);
            CompScreenPass equirectToCube2DPass = new CompScreenPass(this, Name + ID + "_EquirectToCube", cubemapBuffer);
            CompMtlEquirectToCube2D equirectToCubeMat = new CompMtlEquirectToCube2D(equirectToCube2DPass);
            equirectToCubeMat.HorizontalRotation.Value = rotationRadiants;
            equirectToCubeMat.ExposureMultiplier.Value = exposureMul;
            InputEnviromentMap = equirectToCubeMat.Image;
            equirectToCube2DPass.Material = equirectToCubeMat;
            CompRenderPass finalPass = equirectToCube2DPass.Pass;

            // create the base baker
            Baker = new CompBaker(this, finalPass, null, new CompEvent(this, () => InputEnviromentMap.Loaded));
        }

        public CompBaker Baker { get; private set; }

        /// <summary>
        /// The source enviroment map, the baking will start as soon as this reference becomes available.
        /// </summary>
        public CompTextureRef InputEnviromentMap { get; private set; }

    }
}
