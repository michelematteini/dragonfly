using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Given an input cube2D with mipmaps, apply a GGX filter in-place with roughness distributed uniformly between mipmaps.
    /// </summary>
    public class CompBakerCube2DGGX : Component
    {
        /// <summary>
        /// Create a new cubemap GGX mipmap baker.
        /// </summary>
        /// <param name="mipmapCount">The number of mipmaps to be generated.</param>
        public CompBakerCube2DGGX(Component parent, CompRenderBuffer inOutBuffer, int mipmapCount) : base(parent)
        {
            int cubemapEdgeSize = inOutBuffer.Width / 4;
            mipmapCount = CubeMapHelper.ValidateMipmapCount(cubemapEdgeSize, mipmapCount);

            // render mipmaps with GGX filtering
            List<CompScreenPass> screenMipPasses = new List<CompScreenPass>();
            List<CompRenderPass> mipPasses = new List<CompRenderPass>();
            {
                Int2 mipmapResolution = inOutBuffer.Resolution / 2;
                RenderTargetRef srcCube2D = new RenderTargetRef(inOutBuffer);
                for (int i = 0; i < mipmapCount; i++, mipmapResolution /= 2)
                {
                    // mipmap rendering
                    CompScreenPass renderPass = new CompScreenPass(this, Name + ID + "_Cube2DMip" + (i + 1), mipmapResolution);
                    CompMtlCube2DHdrMipmap mipmapMat = new CompMtlCube2DHdrMipmap(renderPass, CompMtlCube2DHdrMipmap.FilterType.GGX);
                    mipmapMat.Image.SetSource(srcCube2D);
                    mipmapMat.Roughness.Value = MipmapToMatRoughness(i + 1, mipmapCount);
                    renderPass.Material = mipmapMat;

                    screenMipPasses.Add(renderPass);
                    mipPasses.Add(renderPass.Pass);
                }
            }

            // copy mipmaps to the src cube2d
            List<CompScreenPass> copyPasses = new List<CompScreenPass>();
            {
                Float2 mipStartLocation = (Float2)0.5f;
                for (int i = 0; i < mipmapCount; i++)
                {
                    CompScreenPass mipmapPass = screenMipPasses[i];

                    // mipmap insertion into the cubemap
                    CompScreenPass curMipCopyPass = new CompScreenPass(mipmapPass, Name + ID + "_Cube2DMipCopy" + (i + 1), inOutBuffer);
                    curMipCopyPass.Pass.Camera.Viewport = new AARect(mipStartLocation.X, mipStartLocation.Y, 1, 1);
                    CompMtlImgCopy mipCopyMat = new CompMtlImgCopy(curMipCopyPass);
                    mipCopyMat.Image.SetSource(mipmapPass.GetOutputRef());
                    curMipCopyPass.Material = mipCopyMat;
                    curMipCopyPass.Pass.RequiredPasses.AddRange(mipPasses); // wait for all the mip to read the cube before modifying it
                    if (i > 0)
                        curMipCopyPass.Pass.RequiredPasses.Add(copyPasses.Last().Pass);

                    copyPasses.Add(curMipCopyPass);
                    mipStartLocation = mipStartLocation + curMipCopyPass.Pass.Camera.Viewport.Size * 0.5f;
                }
            }

            // create the base baker
            Baker = new CompBaker(this, copyPasses.Last().Pass, null, new CompEvent(this, () => true));
        }

        public CompBaker Baker { get; private set; }

        private const float MIP_ROUGHNESS_GAMMA  = 1.0f;

        /// <summary>
        /// Converts the mipmap index to the prefiltered material roughness stored in the environment map.
        /// Must be the inverse on the one in Physical.dfx.
        /// </summary>
        private float MipmapToMatRoughness(int lod, int maxLod)
        {
            float x = (float)lod / maxLod;
            return FMath.Gamma(x, MIP_ROUGHNESS_GAMMA);
        }
    }
}
