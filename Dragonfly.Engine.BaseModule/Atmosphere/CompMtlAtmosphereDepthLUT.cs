
using Dragonfly.Engine.Core;

namespace Dragonfly.BaseModule
{
    internal class CompMtlAtmosphereDepthLUT : CompMaterial
    {
        public MtlModAtmosphere AtmosphereMod { get; private set; }

        public CompMtlAtmosphereDepthLUT(Component parent, CompAtmosphere atmosphere) : base(parent)
        {
            AtmosphereMod = new MtlModAtmosphere(this, atmosphere);
        }

        public override string EffectName => "AtmosphereDepthLUT";

        protected override void UpdateParams()
        {
            
        }
    }
}
