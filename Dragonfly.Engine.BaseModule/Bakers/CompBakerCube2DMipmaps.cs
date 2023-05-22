using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Given an input cube2D buffer, bake the specified mipmaps on it. The operation starts immediately.
    /// </summary>
    public class CompBakerCube2DMipmaps : Component
    {
        /// <summary>
        /// Create a new cubemap mipmap baker.
        /// </summary>
        /// <param name="mipmapCount">The number of mipmaps to be generated.</param>
        public CompBakerCube2DMipmaps(Component parent, CompRenderBuffer inOutBuffer, int mipmapCount) : base(parent)
        {
            int cubemapEdgeSize = inOutBuffer.Width / 4;
            mipmapCount = CubeMapHelper.ValidateMipmapCount(cubemapEdgeSize, mipmapCount);

            // render mipmaps with bilinear filtering
            List<CompScreenPass> renderPasses = new List<CompScreenPass>();
            {
                Int2 mipmapResolution = inOutBuffer.Resolution / 2;
                for (int i = 0; i < mipmapCount; i++, mipmapResolution /= 2)
                {
                    // mipmap rendering
                    CompScreenPass renderPass = new CompScreenPass(this, Name + ID + "_Cube2DMip" + (i + 1), mipmapResolution);
                    CompMtlCube2DHdrMipmap mipmapMat = new CompMtlCube2DHdrMipmap(renderPass);
                    renderPass.Material = mipmapMat;

                    // select the correct source
                    if (i == 0)
                        mipmapMat.Image.SetSource(new RenderTargetRef(inOutBuffer));
                    else
                    {
                        mipmapMat.Image.SetSource(renderPasses[i - 1].GetOutputRef());
                        renderPass.Pass.RequiredPasses.Add(renderPasses[i - 1].Pass);
                    }

                    renderPasses.Add(renderPass);
                }
            }

            // copy mipmaps to the final atlas
            List<CompScreenPass> copyPasses = new List<CompScreenPass>();
            {
                Float2 mipStartLocation = (Float2)0.5f;
                for (int i = 0; i < mipmapCount; i++)
                {
                    CompScreenPass mipmapPass = renderPasses[i];

                    // mipmap insertion into the cubemap
                    CompScreenPass curMipCopyPass = new CompScreenPass(mipmapPass, Name + ID + "_Cube2DMipCopy" + (i + 1), inOutBuffer);
                    curMipCopyPass.Pass.Camera.Viewport = new AARect(mipStartLocation.X, mipStartLocation.Y, 1, 1);
                    CompMtlImgCopy mipCopyMat = new CompMtlImgCopy(curMipCopyPass);
                    mipCopyMat.Image.SetSource(mipmapPass.GetOutputRef());
                    curMipCopyPass.Material = mipCopyMat;
                    curMipCopyPass.Pass.RequiredPasses.Add(mipmapPass.Pass);
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

    }
}
