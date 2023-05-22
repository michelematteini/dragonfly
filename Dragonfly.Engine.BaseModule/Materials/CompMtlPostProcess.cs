using Dragonfly.BaseModule.Atmosphere;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// An image processing material that group all the post processing applied to the final render output. The source rendering should be in RGBE hdr format.
    /// </summary>
    public class CompMtlPostProcess: CompMtlImage
    {
        public CompMtlPostProcess(Component parent) : base(parent)
        {
            ExposureMul = 1.0f;
            ColorEncoding = OuputColorEncoding.Linear;
            Fog = new MtlModExpFog(this);
            Flares = new MtlModSSFlare(this);
            Atmosphere = new MtlModSSAtmosphere(this);
            DepthBufferEnable = false;
            DepthBufferWriteEnable = false;
            BlendMode = BlendMode.Opaque;
            DitheringEnabled = true;
            UpdateEachFrame = true;
        }

        public override string EffectName { get { return "PostProcessPass"; } }

        #region PP parameters

        public float ExposureMul { get; set; }

        public float ExposureValue
        {
            get
            {
                return -(float)System.Math.Log(ExposureMul, 2);
            }
            set
            {
                ExposureMul = (float)System.Math.Pow(2, -value);
            }
        }

        public OuputColorEncoding ColorEncoding { get; set; }

        public MtlModExpFog Fog { get; private set; }

        public MtlModSSFlare Flares { get; private set; }

        public MtlModSSAtmosphere Atmosphere { get; private set; }

        private bool ditheringEnabled;
        public bool DitheringEnabled
        {
            get { return ditheringEnabled; }
            set
            {
                ditheringEnabled = value;
                SetVariantValue("ditheringEnabled", value);
            }
        }

        #endregion

        protected override void UpdateParams()
        {
            BaseMod baseMod = Context.GetModule<BaseMod>();
            Shader.SetParam("rgbeInput", Image);
            Shader.SetParam("exposure", ExposureMul);
            Shader.SetParam("tonemappingType", (float)ColorEncoding);
            if (!baseMod.DepthPrepass.RenderBuffer.LoadingRequired)
                Shader.SetParam("depthInput", baseMod.DepthPrepass.GetTarget());
            Shader.SetParam("invCameraMatrix", (baseMod.MainPass.Camera.GetTransform().Value * baseMod.MainPass.Camera.GetValue()).Invert());
            Shader.SetParam("cameraPos", baseMod.MainPass.Camera.LocalPosition);
        }

    }

    public enum OuputColorEncoding
    {
        Linear = 0,
        Reinhard = 1,
        LogLuv = 2,
        RGBE = 3
    }

}
