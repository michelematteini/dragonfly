using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    public class MtlModAtmosphere : MaterialModule
    {
        public CompAtmosphere Atmosphere { get; set; }

        /// <summary>
        /// If set, all atmosphere dynamic parameters (e.g. location) are taken from the atmosphere list when available. 
        /// This is slower but avoids having to update parameters each frame for static materials.
        /// </summary>
        public bool UseDynamicPosition { get; set; }

        public MtlModAtmosphere(CompMaterial parentMaterial, CompAtmosphere atmosphere) : base(parentMaterial)
        {
            this.Atmosphere = atmosphere;
        }

        protected override void UpdateAdditionalParams(Shader s)
        {
            if (Atmosphere == null)
                return; // no atmosphere, needed when this module is optional

            int atmoListIndex = -1;
            CompAtmosphereTable atmoTable = null;
            if (UseDynamicPosition)
            {
                atmoTable = Context.Scene.Root.GetFirstChild<CompAtmosphereTable>();
                atmoListIndex = atmoTable == null ? -1 : atmoTable.InstanceList.IndexOf(Atmosphere);
            }

            s.SetParam("atmoListIndex", atmoListIndex);
            if (atmoListIndex >= 0)
            {
                s.SetParam("atmoParamsList", atmoTable.Buffer);
            }
            else
            {
                TiledFloat3 viewPosition = Context.GetModule<BaseMod>().MainPass.Camera.Position;
                s.SetParam("atmoLocation", Atmosphere.Location.ToFloat3(viewPosition.Tile));
                s.SetParam("atmoWorldPosFlattenBlend", Atmosphere.CalcWorldPosBlend(viewPosition));
            }
            s.SetParam("atmoMaxDensityRadius", Atmosphere.MaxDensityRadius);
            s.SetParam("atmoZeroDensityRadius", Atmosphere.ZeroDensityRadius);
            s.SetParam("atmoHeightDensityCoeff", Atmosphere.HeightDensityCoeff);
            s.SetParam("atmoLightDir", Atmosphere.LightSource.Direction);
            s.SetParam("atmoMieDirFactor", Atmosphere.MieDirectionalFactor);
            s.SetParam("atmoLightIntensity", Atmosphere.LightIntensity);
            s.SetParam("atmoOpticalDistLutScaleOffset", Atmosphere.OpticalDistLutScaleOffset);
            s.SetParam("atmoIrradianceLutScaleOffset", Atmosphere.IrradianceLutScaleOffset);
            s.SetParam("atmoRgbWavelengthsInv4", CompAtmosphere.RgbWavelengthsInv4);
            s.SetParam("atmoRayleighScatteringConst", CompAtmosphere.RayleighScatteringConst);
            s.SetParam("atmoMieScatteringConst", CompAtmosphere.MieScatteringConst);
            s.SetParam("atmoIrradianceScatteringConst", CompAtmosphere.IrradianceScatteringConst);
            s.SetParam("atmoMaxDensityRadius", Atmosphere.MaxDensityRadius);
        }
    }
}
