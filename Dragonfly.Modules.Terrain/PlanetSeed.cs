﻿using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;

namespace Dragonfly.Terrain
{
    public struct PlanetSeed
    {
        private static readonly Random rnd = new Random();

        public static PlanetSeed Random()
        {
            return new PlanetSeed() { Value = rnd.NextUlong() };
        }

        public static PlanetSeed RandomWithRadius(float approximateRadius)
        {
            PlanetSeed seed = Random();
            ulong encodedRadius = (ulong)FMath.Ceil(FMath.Log2(approximateRadius * 0.001f));
            seed.Value = seed.Value & 0x0fffffffffffffff + encodedRadius << 60; // override radius part
            return seed;
        }

        public static PlanetSeed ExplicitWithRadius(float approximateRadius, int seedValue)
        {
            PlanetSeed seed = new PlanetSeed() { Value = unchecked((uint)seedValue) };
            ulong encodedRadius = (ulong)FMath.Ceil(FMath.Log2(approximateRadius * 0.001f));
            seed.Value = (seed.Value & (ulong)0x0fff_ffff_ffff_ffff) + (encodedRadius << 60); // override radius part
            return seed;
        }

        public ulong Value;
        
        /// <summary>
        /// Returns the radius range that the planet generated by this seed will be in
        /// </summary>
        public Range<float> RadiusRange
        {
            get
            {
                Range<float> radiusRange = new Range<float>(0, FMath.Exp2((int)(Value >> 60)) * 1000.0f);
                radiusRange.From = 0.5f * radiusRange.To;
                return radiusRange;
            }
        }

        private int GetIntSeed()
        {
            return unchecked((int)Value);
        }



        public void ApplyTo(ref PlanetParams planetParams, CompFractalDataSource source)
        {
            // prepare the procedural seed (LS 4 bytes)
            int seed = GetIntSeed();
            source.ProceduralParams.Seed = seed;
            FRandom frnd = new FRandom(seed);

            // randomize radius
            Range<float> radiusRange = RadiusRange;
            planetParams.Radius = frnd.NextFloat(radiusRange.From, radiusRange.To);

            // generate height profile params
            float maxHeight = frnd.NextFloat(1000.0f, 1000.0f + Math.Min(20000.0f, 0.1f * planetParams.Radius));
            source.ProceduralParams.ContinentAvgHeightMeters = maxHeight * FMath.GammaInterp(0.01f, 0.2f, 25.0f, frnd.NextFloat());
            source.ProceduralParams.PeaksMaxHeightMeters = maxHeight;
            source.ProceduralParams.OceanMaxDepthMeters = maxHeight * frnd.NextFloat(0.2f, 0.8f);
            source.ProceduralParams.OceanAvgDepthMeters = source.ProceduralParams.OceanMaxDepthMeters * FMath.GammaInterp(0.01f, 0.5f, 25.0f, frnd.NextFloat());

            // generate continent scales
            float avgContinentSize = (0.4f * planetParams.Radius).Clamp(250_000.0f, 10_000_000.0f);
            source.ProceduralParams.ContinentMaxSizeMeters = frnd.NextFloat(0.2f * avgContinentSize, 2.0f * avgContinentSize);
            source.ProceduralParams.OceanPercent = frnd.NextFloat(0, 1);

            // generate terrain height features
            source.ProceduralParams.FeaturesMaxSizeMeters = frnd.NextFloat(maxHeight * 0.08f, maxHeight * 0.4f);
            source.ProceduralParams.FeaturesCliffMaxHeightMeters = FMath.GammaInterp(2.0f, 8.0f, 3.0f, frnd.NextFloat());
            source.ProceduralParams.FeaturesCliffMinHeightMeters = source.ProceduralParams.FeaturesCliffMaxHeightMeters * FMath.Lerp(0.1f, 0.3f, frnd.NextFloat());
            source.ProceduralParams.FeaturesErosionPercent = frnd.NextFloat(0.2f, 0.7f);
            source.ProceduralParams.OceanDepthVariance = FMath.GammaInterp(0.08f, 0.7f, 25.0f, frnd.NextFloat());
            source.ProceduralParams.PeaksPercent = FMath.GammaInterp(0.05f, 0.8f, 5.0f, frnd.NextFloat());

            // randomize albedo (for now a fixed table)
            source.ProceduralParams.AlbedoLUT.SetSource("textures/terrain/lut/terrainAlbedo1.png");
        }

    }
}