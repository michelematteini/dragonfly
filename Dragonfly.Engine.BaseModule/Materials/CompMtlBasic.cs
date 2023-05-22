using System;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A simple material that can either be filled with a solid color or with a texture.
    /// If Both are specified, they will be multiplied.
    /// </summary>
    public class CompMtlBasic : CompMaterial
    {
        public CompMtlBasic(Component owner, CompTextureRef textureRef) : base(owner)
        {
            Initialize(Graphics.Math.Color.White.ToFloat3(), textureRef);
        }

        public CompMtlBasic(Component owner, Float3 color, string texturePath) : base(owner)
        {
            Initialize(color, new CompTextureRef(this, Graphics.Math.Color.White));
            if (!string.IsNullOrEmpty(texturePath)) ColorTexture.SetSource(texturePath);
        }

        public void Initialize(Float3 initialColor, CompTextureRef textureRef)
        {
            Color = MakeParam(initialColor);
            ColorTexture = textureRef;
            BlendMode = BlendMode.AlphaBlend;
            AlphaMasking = new MtlModAlphaMasking(this, ColorTexture);
            CullMode = Context.GetModule<BaseMod>().Settings.DefaultCullMode;

            MonitoredParams.Add(ColorTexture);
        }

        public CompMtlBasic(Component owner, Float3 color) : this(owner, color, null) { }

        public CompMtlBasic(Component owner, string texturePath) : this(owner, Graphics.Math.Color.White.ToFloat3(), texturePath) { }

        public Param<Float3> Color { get; private set; }

        public CompTextureRef ColorTexture { get; private set; }

        public MtlModAlphaMasking AlphaMasking { get; private set; }

        public override string EffectName
        {
            get { return "BasicMaterial"; }
        }

        protected override void UpdateParams()
        {
            Shader.SetParam("color", Color.Value);
            Shader.SetParam("colorTexture", ColorTexture);
        }

        public class Factory : MaterialFactory
        {
            protected override CompMaterial CreateMaterialFromDescr(MaterialDescription matDescr, Component parent)
            {
                CompMtlBasic m = new CompMtlBasic(parent, Graphics.Math.Color.White.ToFloat3());
                m.Color.Value = matDescr.Albedo;
                m.AlphaMasking.Enabled.Value = matDescr.ClipTransparency && matDescr.UseTransparencyFromAlbedo;
                m.BlendMode = BlendMode.Opaque;

                if (matDescr.HasAlbedoMap)
                    m.ColorTexture.SetSource(matDescr.AlbedoMapPath);

                return m;
            }
        }

    }
}
