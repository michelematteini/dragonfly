using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Material used to bake a lut (lightAngle x atmoHeight) divided in 3 quadrants, with atmosphere irradiance samples at 3 different zenith angle bands.
    /// </summary>
    public class CompMtlAtmosphereIrradianceLUTCache : CompMaterial
    {
        private CompTextureRef opticalDistLut;
        private CompTextureRef emptyIrradiance;

        public CompMtlAtmosphereIrradianceLUTCache(Component parent, CompAtmosphere atmosphere, CompTextureRef opticalDistLuts) : base(parent)
        {
            Atmosphere = new MtlModAtmosphere(this, atmosphere);
            opticalDistLut = opticalDistLuts;
            emptyIrradiance = new CompTextureRef(this, Color.Black);
        }

        public MtlModAtmosphere Atmosphere { get; private set; }

        public override string EffectName => "AtmosphereIrradianceLUTCache";

        protected override void UpdateParams()
        {
            Shader.SetParam("opticalDistLut", opticalDistLut);
            Shader.SetParam("irradianceLut", emptyIrradiance);
        }
    }
}
