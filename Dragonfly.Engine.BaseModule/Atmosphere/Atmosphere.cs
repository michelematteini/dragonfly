using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Represents the state and the rendering parametrization of a spherical atmosphere.
    /// </summary>
    public class CompAtmosphere : Component
    {
        public TiledFloat3 Location;
        public float MaxDensityRadius;
        public float ZeroDensityRadius;
        public float MieDirectionalFactor;
        public SubTextureReference OpticalDistAtlasRegion;
        public SubTextureReference IrradianceAtlasRegion;
        private CompLightDirectional lightSource;
        private CompValue<Float3> lightBaseColor;

        public static readonly Float3 RgbWavelengthsInv4;
        public static readonly  Float3 RgbCalibration;
        public const float RayleighScatteringConst = 5e-7f;
        public const float MieScatteringConst = 1E-07f;
        public const float GlobalIntensity = 0.20f;
        public const float IrradianceScatteringConst = 20e-6f;
        public const float IrradianceIntensity = 0.25f / GlobalIntensity;

        static CompAtmosphere()
        {
            RgbWavelengthsInv4 = 1.0f / new Float3(0.650f, 0.520f, 0.450f);
            RgbWavelengthsInv4 *= RgbWavelengthsInv4;
            RgbWavelengthsInv4 *= RgbWavelengthsInv4;
            RgbCalibration = new Float3(1.053f, 1.000f, 1.300f) * GlobalIntensity;
        }

        public CompAtmosphere(Component parent, TiledFloat3 location, float radius) : base(parent)
        {
            Location = location;
            MaxDensityRadius = radius;
            HalfDensityHeight = radius * 0.001f;
            ZeroDensityRadius = radius + (float)Math.Log(1e6) / HeightDensityCoeff; // stops when density is 1 / 1e6 of the max
            MieDirectionalFactor = -0.75f;
        }

        /// <summary>
        /// Height below which 50% of the total atmosphere mass is located
        /// </summary>
        public float HalfDensityHeight;

        public float HeightDensityCoeff => (float)Math.Log(2.0) / HalfDensityHeight;

        public LookupTable<Byte4> LightColorLUT { get; internal set; }

        public Float3 LightIntensity => LightSource.Intensity.GetValue() * (lightBaseColor.GetValue()) * RgbCalibration;

        public Float3 LightColor => lightBaseColor.GetValue();

        public CompLightDirectional LightSource 
        {
            get { return lightSource; }
            set
            {
                lightSource = value;
                lightBaseColor = lightSource.LightColor.Clone(lightSource);
            }
        }

        public Float4 OpticalDistLutScaleOffset
        {
            get 
            {
                if (OpticalDistAtlasRegion == null)
                    return new Float4(1.0f, 1.0f, 0.0f, 0.0f);
                Float2 texelSize = 1.0f / (Float2)OpticalDistAtlasRegion.ParentLayout.Resolution;
                return new Float4(OpticalDistAtlasRegion.Area.Max - OpticalDistAtlasRegion.Area.Min - texelSize, OpticalDistAtlasRegion.Area.Min + 0.5f * texelSize);
            }
        }

        public Float4 IrradianceLutScaleOffset
        {
            get
            {

                if (IrradianceAtlasRegion == null)
                    return new Float4(1.0f, 1.0f, 0.0f, 0.0f);
                Float2 texelSize = 1.0f / (Float2)IrradianceAtlasRegion.ParentLayout.Resolution;
                return new Float4(IrradianceAtlasRegion.Area.Max - IrradianceAtlasRegion.Area.Min - texelSize, IrradianceAtlasRegion.Area.Min + 0.5f * texelSize);
            }
        }

    }
}
