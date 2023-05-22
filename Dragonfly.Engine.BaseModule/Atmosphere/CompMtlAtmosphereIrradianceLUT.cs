using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Material that renders the irradiance cache into a lut, usable for atmospheric scattering and indirect lighting.
    /// </summary>
    public class CompMtlAtmosphereIrradianceLUT : CompMaterial
    {
        private CompTextureRef irradianceCache;

        public CompMtlAtmosphereIrradianceLUT(Component parent, RenderTargetRef irradianceCache) : base(parent)
        {
            this.irradianceCache = new CompTextureRef(this, Color.Black);
            this.irradianceCache.SetSource(irradianceCache);
        }

        public override string EffectName => "AtmosphereIrradianceLUT";

        protected override void UpdateParams()
        {
            Shader.SetParam("irradianceCache", irradianceCache);
        }
    }
}
