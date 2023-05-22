using Dragonfly.BaseModule.Atmosphere;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
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
        /// <summary>
        /// Max number of atmospheres that can be displayed at the same time.
        /// </summary>
        public const int MAX_DISPLAYED_COUNT = 32;

        public static readonly Float3 RgbWavelengthsInv4;
        public static readonly Float3 RgbCalibration;
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

        public TiledFloat3 Location;
        public float MaxDensityRadius;
        public float ZeroDensityRadius;
        public float MieDirectionalFactor;
        public SubTextureReference OpticalDistAtlasRegion;
        public SubTextureReference IrradianceAtlasRegion;


        private CompLightDirectional lightSource;
        private CompValue<Float3> lightBaseColor;
        private CompMesh lightFilteringMesh;
        private bool visible;

        public CompAtmosphere(Component parent, TiledFloat3 location, float radius) : base(parent)
        {
            Location = location;
            MaxDensityRadius = radius;
            HalfDensityHeight = radius * 0.001f;
            ZeroDensityRadius = radius + (float)Math.Log(1e6) / HeightDensityCoeff; // stops when density is 1 / 1e6 of the max
            MieDirectionalFactor = -0.75f;
            LightColorGpuLUT = new CompTextureRef(this, Color.White);

            // create a mesh that will render atmospheric light filtering in the directional light filter pass
            {
                CompMtlAtmosphereLightFilter lightFilterMat = new CompMtlAtmosphereLightFilter(this, this);
                lightFilterMat.CullMode = CullMode.Clockwise;
                lightFilteringMesh = new CompMesh(this, lightFilterMat.OfClass(Context.GetModule<BaseMod>().Settings.MaterialClasses.DirectionalLightFilter));
                Primitives.Spheroid(lightFilteringMesh.AsObject3D(), Float3.Zero, 2.0f * 1.1f * (Float3)ZeroDensityRadius, 256);
            }

            // setup global components for managing atmospheres
            {
                // add the atmosphere baking manager, if it does not exist
                if (GetComponent<CompAtmoBakingManager>() == null)
                    new CompAtmoBakingManager(Context.Scene.Root);

                // add the atmosphere table, if it does not exist
                if (GetComponent<CompAtmosphereTable>() == null)
                    new CompAtmosphereTable(Context.Scene.Root);
            }
        }

        /// <summary>
        /// Height below which 50% of the total atmosphere mass is located
        /// </summary>
        public float HalfDensityHeight;

        public float HeightDensityCoeff => (float)Math.Log(2.0) / HalfDensityHeight;

        public LookupTable<Byte4> LightColorLUT { get; internal set; }

        public Float3 LightIntensity => LightSource.Intensity.GetValue() * (lightBaseColor.GetValue()) * RgbCalibration;

        public Float3 LightColor => lightBaseColor.GetValue();

        public CompTextureRef LightColorGpuLUT { get; set; }

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

        public bool Visible
        {
            get 
            {
                return visible;
            }
            set
            {
                if (visible != value)
                {
                    if (value)
                    {
                        GetComponent<CompAtmosphereTable>().InstanceList.Add(this);
                        GetComponent<CompAtmoBakingManager>().StartBakingAtmosphere(this);
                    }
                    else
                    {
                        GetComponent<CompAtmosphereTable>().InstanceList.Remove(this);
                    }
                }
                visible = value;
            }
        }

        /// <summary>
        /// Calc a blend factor that will be used from shader to decide whether to use the actual wordPosition 
        /// or one projected to the planet surface as reference to ray-tracing scattering and other effects.
        /// This will blend away low tessellation and zbuffer precision artifacts in the distance
        /// </summary>
        public float CalcWorldPosBlend(TiledFloat3 viewPos)
        {
            float relativeDistance = ((viewPos - Location).ToFloat3() / ZeroDensityRadius).Length;
            return FMath.Smoothstep(1.08f, 1.2f, relativeDistance);
        }


    }
}
