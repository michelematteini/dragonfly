using System;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A material used to render sprite text.
    /// </summary>
    public class CompMtlText : CompMaterial
    {
        public CompMtlText(Component owner, string fontAtlasTexture) : base(owner)
        {
            FontTexture = new CompTextureRef(owner, Color.Black);
            FontTexture.SetSource(fontAtlasTexture);
            MonitoredParams.Add(FontTexture);
            RenderMode = MakeParam(TextRenderMode.Normal);
            BlendMode = BlendMode.AlphaBlend;
            DepthBufferEnable = false;
            DepthBufferWriteEnable = false;
            CullMode = Graphics.CullMode.None;
        }

        public CompTextureRef FontTexture { get; protected set; }

        public Param<TextRenderMode> RenderMode { get; private set; }

        public override string EffectName
        {
            get { return "TextMaterial"; }
        }

        protected override void UpdateParams()
        {
            Shader.SetParam("textRenderMode", (float)(int)RenderMode.Value);

            if (FontTexture.Available)
            {
                Shader.SetParam("fontTexture", FontTexture);
                Shader.SetParam("texSpriteSizes", (Float2)FontTexture.Resolution); 
            }
        }
    }

    public enum TextRenderMode
    {
        Normal = 0,
        Sharp = 1,
        Crisp = 2,
        NoAA = 3,
    }

}
