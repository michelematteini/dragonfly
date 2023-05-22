using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// An helper component that setup a screen-space render pass with the given material.
    /// </summary>
    public class CompScreenPass : Component
    {
        private CompMesh screenMesh;

        public CompScreenPass(Component parent, string configClass, CompRenderBuffer renderBuffer) : base(parent)
        {
            Pass = new CompRenderPass(this, configClass, renderBuffer);
            Pass.MaterialFilters.Add(new MaterialClassFilter(MaterialClassFilterType.Include, configClass));
            Pass.Camera = new CompCamIdentity(Pass);
            screenMesh = BaseMod.CreateScreenMesh(Pass);
        }

        public CompScreenPass(Component parent, string configClass, Int2 resolution) : this(parent, configClass, null)
        {
            Pass.RenderBuffer = new CompRenderBuffer(this, SurfaceFormat.Color, resolution.X, resolution.Y);
            Pass.RenderToTexture = true;
        }

        public CompScreenPass(Component parent, string configClass, Int2 resolution, CompMaterial imageProcMaterial) 
            : this(parent, configClass, resolution)
        {
            Material = imageProcMaterial.DisplayIn(Pass);
        }

        public CompRenderPass Pass { get; private set; }

        public CompMaterial Material
        {
            get 
            {
                return screenMesh.GetFirstMaterialOfClass(Pass.MainClass);
            }
            set 
            {
                screenMesh.RemoveMaterialsOfClass(Pass.MainClass);
                if (value != null)
                    screenMesh.Materials.Add(value.DisplayIn(Pass));
            }
        }

        /// <summary>
        /// Creates and returns a picture control that display the result of this pass in the specified UI container.
        /// </summary>
        public CompUiCtrlPicture DisplayIn(CompUiContainer uiContainer)
        {
            CompMtlImgCopy screenPassPicture = new CompMtlImgCopy(this);
            screenPassPicture.Image.SetSource(new RenderTargetRef(Pass.RenderBuffer));
            return new CompUiCtrlPicture(uiContainer, screenPassPicture);
        }

        /// <summary>
        /// Returns a reference to the output render target of this pass.
        /// </summary>
        /// <returns></returns>
        public RenderTargetRef GetOutputRef()
        {
            return new RenderTargetRef(Pass.RenderBuffer);
        }
    }
}
