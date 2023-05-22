using Dragonfly.BaseModule.Atmosphere;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    public class MtlModSSAtmosphere : MaterialModule
    {
        public MtlModSSAtmosphere(CompMaterial parentMaterial) : base(parentMaterial)
        {
        }
        
        protected override void UpdateAdditionalParams(Shader s)
        {
            CompAtmosphereTable atmoTable = Context.Scene.Root.GetFirstChild<CompAtmosphereTable>();
            int atmoCount = atmoTable != null ? atmoTable.InstanceList.Count : 0;
            s.SetParam("ssaCount", atmoCount);

            if (atmoCount > 0)
            {
                s.SetParam("ssaParams", atmoTable.Buffer);
                CompAtmoBakingManager bakingManager = Context.Scene.Root.GetFirstChild<CompAtmoBakingManager>();
                s.SetParam("ssaLutAtlas", bakingManager.OpticalDistAtlas);
                s.SetParam("ssaIrradianceLutAtlas", bakingManager.IrradianceAtlas);
                s.SetParam("rgbWavelengthsInv4", CompAtmosphere.RgbWavelengthsInv4);
                s.SetParam("rayleighScatteringConst", CompAtmosphere.RayleighScatteringConst);
                s.SetParam("mieScatteringConst", CompAtmosphere.MieScatteringConst);
                s.SetParam("irradianceScatteringConst", CompAtmosphere.IrradianceScatteringConst);
                s.SetParam("irradianceIntensity", CompAtmosphere.IrradianceIntensity);
            }
        }

    }
}
