using Dragonfly.Engine.Core;

namespace Dragonfly.BaseModule
{
    internal class CompMtlAtmosphereLightColorLUT : CompMaterial
    {
        private CompTextureRef opticalDistLut;

        public CompMtlAtmosphereLightColorLUT(Component parent, CompAtmosphere atmosphere, CompTextureRef opticalDistLut) : base(parent)
        {
            Atmosphere = new MtlModAtmosphere(this, atmosphere);
            this.opticalDistLut = opticalDistLut;
        }

        public MtlModAtmosphere Atmosphere { get; private set; }

        public override string EffectName => "AtmosphereLightColorLUT";

        protected override void UpdateParams()
        {
            Shader.SetParam("opticalDistLut", opticalDistLut);
        }
    }
}
