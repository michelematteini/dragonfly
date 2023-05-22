using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.Terrain
{
    public class FractalDataSourceParams
    {
        const int MAX_DISTR_RANGE = 12; // maximum range filled by a noise layer
        public const int MaxOctave = 12; // max world octave displayed on the terrain

        /// <summary>
        /// A random seed to generate the terrain from.
        /// </summary>
        public int Seed;
        /// <summary>
        /// Ocean average depth in meters.
        /// </summary>
        public float OceanAvgDepthMeters;
        /// <summary>
        /// Ocean Max depth in meters.
        /// </summary>
        public float OceanMaxDepthMeters;
        /// <summary>
        /// Percent of the terrain covered in water (i.e. below 0 height), range [0; 1]
        /// </summary>
        public float OceanPercent;
        /// <summary>
        /// How varied the ocean depth is. Range (0; 1], where 0 is a flat ocean, an 1 an almost uniform heights distribution.
        /// </summary>
        public float OceanDepthVariance;
        /// <summary>
        /// Average height of the lands above the ocean level, in meters.
        /// </summary>
        public float ContinentAvgHeightMeters;
        /// <summary>
        /// Maximum possible height of the terrain in meters.
        /// </summary>
        public float PeaksMaxHeightMeters;
        /// <summary>
        /// Maximum indicative maximum size for the generated landmasses.
        /// </summary>
        public float ContinentMaxSizeMeters;
        /// <summary>
        /// Distribution exponent which changes the ammount of land covered in mountains. Range [0; 1] where 0 means almost no mountains and 1 almost all land is mountains.
        /// </summary>
        public float PeaksPercent;
        /// <summary>
        /// The size of the largest possible detail on the terrain in meters (e.g. the linear size of the biggest mountain).
        /// </summary>
        public float FeaturesMaxSizeMeters;
        /// <summary>
        /// The size of the smallest possible details on the terrain in meters. Smaller values makes for an hightly detailed terrain, at a gpu baking performance cost.
        /// </summary>
        public float FeaturesMinSizeMeters;
        /// <summary>
        /// Define the minimum height of detail cliffs over the landscape
        /// </summary>
        public float FeaturesCliffMinHeightMeters;
        /// <summary>
        /// The maximum height of detail cliffs over the landscape.
        /// </summary>
        public float FeaturesCliffMaxHeightMeters;

        /// <summary>
        /// A LUT to sample the terrain base albedo from. Y = terrain height, the mapping between rows and heights is non-linear and affected by AlbedoLUTCompression.
        /// X <= 0.5 contains all albedo variations for that elevation, X > 0.5 all the variations for cliffs at that elevation.
        /// X = 0 contains special data for that elevation: R= SlopeTHR, G= Noise type blend
        /// </summary>
        public CompTextureRef AlbedoLUT { get; private set; }
        /// <summary>
        /// Lower values use more lut data near the 0 height level. Valid values are >= 1.
        /// </summary>
        public float AlbedoLUTCompression;

        /// <summary>
        /// Defines the percent of the landscape height that can be eroded away around features.
        /// A value of 0 generate a completely smooth landscape, 1 will allow any height to be eroded to the water level, creating deep valley and fractures.
        /// </summary>
        public float FeaturesErosionPercent;

        public FractalDataSourceParams(Component parent)
        {
            OceanAvgDepthMeters = 3500.0f;
            OceanMaxDepthMeters = 10000.0f;
            OceanPercent = 0.5f;
            OceanDepthVariance = 0.10f;
            ContinentAvgHeightMeters = 200.0f;
            PeaksMaxHeightMeters = 9000.0f;
            ContinentMaxSizeMeters = 200_000.0f;
            PeaksPercent = 0.2f;
            Seed = 0;
            FeaturesMaxSizeMeters = 10_000.0f;
            FeaturesMinSizeMeters = 0.05f;
            FeaturesErosionPercent = 0.5f;
            FeaturesCliffMinHeightMeters = 0.50f;
            FeaturesCliffMaxHeightMeters = 5.0f;

            AlbedoLUT = new CompTextureRef(parent);
            AlbedoLUTCompression = 200.0f;
        }

        public void Validate()
        {
            OceanMaxDepthMeters = System.Math.Max(OceanAvgDepthMeters + 1, OceanMaxDepthMeters);
            PeaksMaxHeightMeters = System.Math.Max(ContinentAvgHeightMeters + 1, PeaksMaxHeightMeters);
            FeaturesMaxSizeMeters = System.Math.Min(FeaturesMaxSizeMeters, 0.5f * ContinentMaxSizeMeters);
            FeaturesMinSizeMeters = System.Math.Min(FeaturesMinSizeMeters, 0.5f * FeaturesMaxSizeMeters);
        }

        public int GetContinentEndOctave()
        {
            GPUNoise.Distribution baseNoise;
            CalcHeightNoiseDistributions(MaxOctave, out baseNoise, out _, out _);
            return baseNoise.EndOctave;
        }

        public int GetFirstDetailEndOctave()
        {
            GPUNoise.Distribution detail1;
            CalcHeightNoiseDistributions(MaxOctave, out _, out detail1, out _);
            return detail1.EndOctave;
        }

        private void CalcHeightNoiseDistributions(int maxOctave, out GPUNoise.Distribution baseDistr, out GPUNoise.Distribution detailDistr1, out GPUNoise.Distribution detailDistr2)
        {
            baseDistr = new GPUNoise.Distribution();
            detailDistr1 = new GPUNoise.Distribution();
            detailDistr2 = new GPUNoise.Distribution();

            // calc octave ranges for each noise type
            baseDistr.StartOctave = GPUNoise.PeriodToOctave(ContinentMaxSizeMeters);
            detailDistr1.StartOctave = GPUNoise.PeriodToOctave(FeaturesMaxSizeMeters);
            baseDistr.EndOctave = detailDistr1.StartOctave - 1;
            detailDistr2.EndOctave = GPUNoise.PeriodToOctave(FeaturesMinSizeMeters);
            int detailOctavesCount = detailDistr2.EndOctave - detailDistr1.StartOctave;

            detailDistr1.EndOctave = detailDistr1.StartOctave + detailOctavesCount / 2;
            detailDistr2.StartOctave = detailDistr1.EndOctave + 1;

            // calc amplitudes
            baseDistr.AmplitudeMul = 0.50f;
            baseDistr.Normalize();
            detailDistr1.AmplitudeMul = 0.45f;
            detailDistr1.Normalize();
            detailDistr2.AmplitudeMul = 0.50f;
            detailDistr2.Normalize();

            // clamp noise range
            baseDistr.EndOctave = System.Math.Min(baseDistr.EndOctave, baseDistr.StartOctave + MAX_DISTR_RANGE);
            detailDistr1.EndOctave = System.Math.Min(detailDistr1.EndOctave, detailDistr1.StartOctave + MAX_DISTR_RANGE);
            detailDistr2.EndOctave = System.Math.Min(detailDistr2.EndOctave, detailDistr2.StartOctave + MAX_DISTR_RANGE);

            // clamp final octaves
            baseDistr.EndOctave = System.Math.Min(baseDistr.EndOctave, maxOctave);
            detailDistr1.EndOctave = System.Math.Min(detailDistr1.EndOctave, maxOctave);
            detailDistr2.EndOctave = System.Math.Min(detailDistr2.EndOctave, maxOctave);
            if (detailDistr1.StartOctave > maxOctave)
            {
                // detailDistr1 first octave is always preserved, since it greatly affect the final albedo and would otherwise create a popping effect.
                detailDistr1.EndOctave = System.Math.Max(detailDistr1.StartOctave, System.Math.Min(detailDistr1.EndOctave, maxOctave));
            }
        }

        public void UpdateShader(Shader s, int maxOctave)
        {
            Validate();

            GPUNoise.Distribution baseNoise, detailNoise1, detailNoise2;
            CalcHeightNoiseDistributions(maxOctave, out baseNoise, out detailNoise1, out detailNoise2);
            baseNoise.SetToShader("baseDistr", s); // base terra distr
            detailNoise1.SetToShader("detailDistr1", s); // detail 1 
            detailNoise2.SetToShader("detailDistr2", s); // detail 2

            // terra distribution params
            Float4 K = new Float4(
                (OceanMaxDepthMeters - OceanAvgDepthMeters) / (OceanMaxDepthMeters + ContinentAvgHeightMeters), // ocean average depth percent
                OceanDepthVariance, // depth variation of the ocean floor
                PeaksPercent, // peak percent (how much land is covered in mountains)
                OceanPercent // percent of the land below sea level
            );
            s.SetParam("baseDistrK", K);

            // terra scaling params
            Float4 M = new Float4(
                OceanMaxDepthMeters / (OceanMaxDepthMeters + ContinentAvgHeightMeters), // ocean level percent 
                OceanMaxDepthMeters + ContinentAvgHeightMeters,
                PeaksMaxHeightMeters - ContinentAvgHeightMeters,
                0
            );
            s.SetParam("baseDistrM", M);

            // noise distribution to modulate base noise peaks
            GPUNoise.Distribution peaksModulationDistr = new GPUNoise.Distribution();
            peaksModulationDistr.EndOctave = baseNoise.EndOctave;
            peaksModulationDistr.StartOctave = baseNoise.EndOctave - 1;
            peaksModulationDistr.AmplitudeMul = 0.5f;
            peaksModulationDistr.Normalize();
            peaksModulationDistr.SetToShader("peaksModulationDistr", s);

            // additional noise params
            s.SetParam("noiseSeed", GPUNoise.SeedToNoiseOffset(Seed)); // noise seed
            s.SetParam("erosionPercent", FeaturesErosionPercent);
            s.SetParam("cliffHeightMinMax", new Float2(FeaturesCliffMinHeightMeters, FeaturesCliffMaxHeightMeters));

            // albedoBlendDistr: tea-noise distribution used to add noise to the sampled albedo from the LUT (pre-baked)
            GPUNoise.Distribution aBlendNoise = new GPUNoise.Distribution();
            aBlendNoise.AmplitudeMul = 1.0f;
            aBlendNoise.EndOctave = GPUNoise.PeriodToOctave(FeaturesMinSizeMeters) + 1;
            aBlendNoise.StartOctave = aBlendNoise.EndOctave - MAX_DISTR_RANGE;
            float octaveBlendDist = ((aBlendNoise.EndOctave - maxOctave) * 0.2f).Saturate();
            s.SetParam("albedoBlendSharpness", (1.0f - octaveBlendDist * octaveBlendDist).Clamp(0.2f/*retain a bit of sharpness*/, 1.0f));
            aBlendNoise.EndOctave = System.Math.Min(aBlendNoise.EndOctave, maxOctave);
            aBlendNoise.Normalize();
            aBlendNoise.SetToShader("albedoBlendDistr", s);

            // albedo LUT
            s.SetParam("albedoLut", AlbedoLUT);
            s.SetParam("albedoLutK", AlbedoLUTCompression);

            // albedoVarDistr: used for various pre-baked noises to variate among different albedo values for a give altitute
            GPUNoise.Distribution aVarNoise = new GPUNoise.Distribution();
            aVarNoise.AmplitudeMul = 0.5f;
            aVarNoise.StartOctave = baseNoise.StartOctave + 3;
            aVarNoise.EndOctave = System.Math.Min(maxOctave, aVarNoise.StartOctave + MAX_DISTR_RANGE); 
            aVarNoise.Normalize();
            aVarNoise.SetToShader("albedoVarDistr", s);

            // normal attenuation
            s.SetParam("detailNormalMul", FMath.Exp2(maxOctave - detailNoise1.EndOctave).Saturate());
        }
    }
}
