using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Manage allocations of on a 2d atlas texture as a a quad tree.
    /// Only power of two textures and allocations are supported.
    /// Also allocates the needed render target and pass to render to it.
    /// </summary>
    public class TextureAtlas
    {
        private CompMesh screenMesh;

        public TextureAtlas(Component parent, string name, string renderPassClass, SurfaceFormat format, AtlasLayout layout, string shaderTemplate = null)
        {
            RenderBuffer = new CompRenderBuffer(parent, format, layout.Resolution.Width, layout.Resolution.Height);
            Pass = new CompRenderPass(parent, name, RenderBuffer);
            Pass.MainClass = renderPassClass;
            Pass.OverrideShaderTemplate = shaderTemplate;
            Layout = layout;
            Texture = new CompTextureRef(parent);
            Texture.SetSource(new RenderTargetRef(RenderBuffer, 0), TexRefFlags.None);
        }

        public CompRenderPass Pass { get; private set; }

        public CompRenderBuffer RenderBuffer { get; private set; }

        public CompTextureRef Texture { get; private set; }

        public AtlasLayout Layout { get; private set; }

        /// <summary>
        /// Prepare a mesh that will be used to render screen-space quads in this atlas.
        /// </summary>
        public void SetupForScreenSpaceRendering()
        {
            screenMesh = BaseMod.CreateScreenMesh(Pass);
        }

        public CompCamera AddScreenRenderingCamera(SubTextureReference atlasRegion, CompMaterial material)
        {
            CompCamera screenCam = new CompCamIdentity(Pass);
            screenCam.Viewport = atlasRegion.Area;
            material.VisibleOnlyForCamera = screenCam;
            screenMesh.Materials.Add(material.DisplayIn(Pass));
            Pass.CameraList.Add(screenCam);
            return screenCam;
        }
    }
}
