using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    internal class CompMtlAtmosphereLightFilter : CompMaterial
    {
        private BaseMod baseMod;

        public CompMtlAtmosphereLightFilter(Component parent, CompAtmosphere atmosphere) : base(parent)
        {
            baseMod = Context.GetModule<BaseMod>();
            AtmosphereModule = new MtlModAtmosphere(this, atmosphere);
            CullMode = baseMod.Settings.DefaultCullMode == CullMode.Clockwise ? CullMode.CounterClockwise : CullMode.Clockwise;
            DepthBufferEnable = false;
            DepthBufferWriteEnable = false;
            // MonitoredParams.Add(atmosphere.LightColorGpuLUT);
            UpdateEachFrame = true;
            LightColorOffset = 0.04f;
        }

        public override string EffectName => "AtmosphereLightFilter";

        /// <summary>
        /// Offsets the light color sample from the lut to adjust the shadowed area.
        /// Can be used to compensate for above sea-level occlusion, not taken into account during lut computation.
        /// </summary>
        public float LightColorOffset { get; set; }

        public override bool Ready 
        {
            get
            {
                bool isLightSourceFiltered = GetComponent<CompDirectionalLightFilterPass>().CurrentLight == AtmosphereModule.Atmosphere.LightSource;
                return base.Ready && isLightSourceFiltered;
            }
            protected set
            {
                base.Ready = value;
            }
        }

        public MtlModAtmosphere AtmosphereModule { get; private set; }

        protected override void UpdateParams()
        {
            RenderTarget depthTarget = baseMod.DepthPrepass.GetTarget();
            if (depthTarget != null)
                Shader.SetParam("depthInput", baseMod.DepthPrepass.GetTarget());
            Shader.SetParam("atmosphereLightColorLUT", AtmosphereModule.Atmosphere.LightColorGpuLUT);
            Shader.SetParam("lightColorOffset", LightColorOffset);
        }
    }
}
