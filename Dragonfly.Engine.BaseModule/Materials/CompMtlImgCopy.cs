using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Draw the input image. A transformation matrix can be optionally specified  and will be applied to the color from the image.
    /// </summary>
    public class CompMtlImgCopy : CompMtlImageProc
    {
        public CompMtlImgCopy(Component parent, bool alphaBlendingEnabled = false) : base(parent, ImgProcessingType.Copy)
        {
            ColorTransform = MakeParam(Float4x4.Identity);
            AlphaBlending = alphaBlendingEnabled;
            DepthBufferEnable = false;
            DepthBufferWriteEnable = false;
            BlendMode = alphaBlendingEnabled ? BlendMode.AlphaBlend : BlendMode.Opaque;
        }

        public bool AlphaBlending
        {
            get { return BlendMode == BlendMode.AlphaBlend; }
            set { BlendMode = value ? BlendMode.AlphaBlend : BlendMode.Opaque; }
        }

        public Param<Float4x4> ColorTransform { get; private set; }

        protected override void UpdateParams()
        {
            Shader.SetParam("filteredTex", Image);
            Shader.SetParam("inputMatrix1", ColorTransform);
        }

    }

 
}
