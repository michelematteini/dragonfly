using Dragonfly.BaseModule.Atmosphere;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Module for PBR materials that manages indirect lighting sources
    /// </summary>
    public class MtlModIndirectLighting : MaterialModule
    {
        /// <summary>
        /// Shader variant used to select how the radiance is sampled in the shader.
        /// </summary>
        private enum RadianceModes
        {
            NoRadiance = 0,
            RadianceMap,
            AtmosphericRadiance
        }

        /// <summary>
        /// Defines the source of radiance infomation to be used with the parent material.
        /// </summary>
        public enum IndirectLightSource
        {
            Disabled = 0,
            DefaultRadiance,
            AtmosphericRadiance
        }
        
        private CompIndirectLightManager indirectLightManager;
        private MtlModAtmosphere atmosphereMod;
        private CompMaterial.Param<IndirectLightSource> indirectLightSource;

        public MtlModIndirectLighting(CompMaterial parentMaterial) : base(parentMaterial)
        {
            indirectLightManager = Context.Scene.Root.GetFirstChild<CompIndirectLightManager>();
            atmosphereMod = new MtlModAtmosphere(parentMaterial, null);
            atmosphereMod.UseDynamicPosition = true;
            indirectLightSource = MakeParam(IndirectLightSource.Disabled);
            UseDefaultRadiance();
        }

        private RadianceModes SourceToRadianceMode(IndirectLightSource source)
        {
            switch (source)
            {
                case IndirectLightSource.Disabled:
                    return RadianceModes.NoRadiance;
                case IndirectLightSource.DefaultRadiance:
                    return RadianceModes.RadianceMap;
                case IndirectLightSource.AtmosphericRadiance:
                    return RadianceModes.AtmosphericRadiance;
                default:
                    return RadianceModes.NoRadiance;
            }
        }

        public void Disable()
        {
            switch (indirectLightSource.Value)
            {
                case IndirectLightSource.DefaultRadiance:
                    MonitoredParams.Remove(indirectLightManager.DefaultBackgroundRadiance);
                    break;

                case IndirectLightSource.AtmosphericRadiance:
                    atmosphereMod.Atmosphere = null;
                    atmosphereMod = null;
                    break;
            }

            Source = IndirectLightSource.Disabled;
        }

        /// <summary>
        /// Use the default radiance source provided externally.
        /// </summary>
        public void UseDefaultRadiance()
        {
            Disable();
            Source = IndirectLightSource.DefaultRadiance;
            MonitoredParams.Add(indirectLightManager.DefaultBackgroundRadiance);
        }

        public void UseRadianceFromAtmosphere(CompAtmosphere atmosphere)
        {
            Disable();
            Source = IndirectLightSource.AtmosphericRadiance;
            atmosphereMod.Atmosphere = atmosphere;
        }


        public IndirectLightSource Source
        {
            get 
            { 
                return indirectLightSource; 
            }
            private set
            {
                Material.SetVariantValue("radianceMode", SourceToRadianceMode(value).ToString());
                indirectLightSource.Value = value;
            }

        }

        protected override void UpdateAdditionalParams(Shader s)
        {
            switch (indirectLightSource.Value)
            {
                case IndirectLightSource.DefaultRadiance:
                    s.SetParam("radianceMap", indirectLightManager.DefaultBackgroundRadiance);
                    break;
                case IndirectLightSource.AtmosphericRadiance:
                    {
                        CompAtmoBakingManager atmoLuts = Context.Scene.Root.GetFirstChild<CompAtmoBakingManager>();
                        s.SetParam("radianceMap", atmoLuts.IrradianceAtlas);
                    }
                    break;
            }
        }
    }
}
