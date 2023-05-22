using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    public class MtlModShadowMapBiasing : MaterialModule
    {
        public MtlModShadowMapBiasing(CompMaterial parentMaterial) : base(parentMaterial)
        {
            BaseModShadowParams settings = Context.GetModule<BaseMod>().Settings.Shadows;
            DepthBias = MakeParam<float>(settings.DefaultDepthBias);
            NormalBias = MakeParam<float>(settings.DefaultNormalBias);
            QuantizationBias = MakeParam<float>(settings.DefaultQuantizationBias);
        }

        public CompMaterial.Param<float> DepthBias { get; private set; }
        
        public CompMaterial.Param<float> NormalBias { get; private set; }

        public CompMaterial.Param<float> QuantizationBias { get; private set; }

        protected override void UpdateAdditionalParams(Shader s)
        {
            s.SetParam("depthBiases", new Float3(DepthBias, NormalBias, QuantizationBias));
        }
    }
}
