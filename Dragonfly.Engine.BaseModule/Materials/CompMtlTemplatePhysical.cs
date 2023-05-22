using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Template for a photorealistic material using physical parameters to simulare real surfaces.
    /// </summary>
    public abstract class CompMtlTemplatePhysical : CompMaterial
    {
        private CompTextureRef brdfLUT;

        protected CompMtlTemplatePhysical(Component parent) : base(parent)
        {
            brdfLUT = new CompTextureRef(this, new Byte4(0, 255, 0, 0));
            brdfLUT.SetSource("textures/ggx_brdf.png");
            MonitoredParams.Add(brdfLUT);
        }

        protected override void UpdateParams()
        {
            Shader.SetParam("brdf_lut", brdfLUT);
        }

    }

}
