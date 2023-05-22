
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    public class MtlModExpFog : MaterialModule
    {
        private bool fogEnabled;
        private BaseMod baseMod;

        public MtlModExpFog(CompMaterial parentMaterial) : base(parentMaterial)
        {
            Color = MakeParam(Float3.One);
            Multiplier = MakeParam(0.0f);
            GradientCoeff = MakeParam(0.001f);
            GradientDir = MakeParam(Float3.UnitY);
            GroundLevel = MakeParam(TiledFloat.Zero);
            baseMod = Context.GetModule<BaseMod>();
        }

        public bool Enabled
        {
            get
            {
                return fogEnabled;
            }
            set
            {
                fogEnabled = value;
                Material.SetVariantValue("fogEnabled", value);
            }
        }

        public CompMaterial.Param<Float3> Color { get; private set; }

        public CompMaterial.Param<float> Multiplier { get; private set; }

        public Float3 GradientDir { get; set; }

        public TiledFloat GroundLevel { get; set; }

        public float GradientCoeff { get; set; }

        protected override void UpdateAdditionalParams(Shader s)
        {
            if (fogEnabled)
            {
                TiledFloat groundOffset = new TiledFloat3(Float3.Zero, baseMod.CurWorldTile).Dot(GradientDir);
                s.SetParam("fogColor", Color);
                s.SetParam("fogExponent", Multiplier);
                s.SetParam("fogGradDir", GradientDir);
                s.SetParam("fogGradExponent", GradientCoeff);
                s.SetParam("fogGroundLevel", (GroundLevel - groundOffset).ToFloat());
            }
        }
    }

}
