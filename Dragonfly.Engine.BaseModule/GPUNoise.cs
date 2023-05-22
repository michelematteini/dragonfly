using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Helper class for noise usage in shaders.
    /// </summary>
    public static class GPUNoise
    {
        public static int PeriodToOctave(float periodMeters)
        {
            return -(int)FMath.Ceil(FMath.Log2(periodMeters));
        }

        public static float OctaveToPeriod(int octave)
        {
            return FMath.Exp2(-octave);
        }

        public static int MaxSolvableOctave(float worldSize, int resolution)
        {
            return PeriodToOctave(4.0f * worldSize / resolution);
        }

        public static Float2 SeedToNoiseOffset(int seed)
        {
            uint seedBits = unchecked((uint)seed);
            seedBits = RandomEx.HashUint(seedBits); // shuffle
            int seedx = (int)(seedBits & (uint)0xffff) - 32768;
            int seedy = (int)(seedBits >> 16) - 32768;
            return new Float2(seedx, seedy);
        }

        /// <summary>
        /// A noise distribution, usable in shader.
        /// </summary>
        public struct Distribution
        {
            public int StartOctave, EndOctave;
            public float StartAmplitude, AmplitudeMul;

            public void SetToShader(string paramName, Shader s)
            {
                if (!IsValid)
                {
                    s.SetParam(paramName, new Float4(0.0f, 0.0f, 0.0f, 1.0f));
                    return;
                }
                
                s.SetParam(paramName, new Float4(StartAmplitude, AmplitudeMul, StartOctave, EndOctave));
            }

            public bool IsValid
            {
                get
                {
                    return StartOctave <= EndOctave;
                }
            }

            public float MaxValue
            {
                get
                {
                    if (!IsValid)
                        return 0;
                    if (AmplitudeMul == 1.0)
                        return StartAmplitude * (EndOctave - StartOctave + 1);
                    return StartAmplitude * (1.0f - FMath.Pow(AmplitudeMul, EndOctave - StartOctave + 1)) / (1.0f - AmplitudeMul);
                }
            }

            /// <summary>
            /// Update the StartAmplitude field so that this distribution fits the [-1; 1] interval when evaluated.
            /// </summary>
            public void Normalize()
            {
                StartAmplitude = 1.0f;
                StartAmplitude /= MaxValue;
            }

            public float GetOctaveAmplitude(int octave)
            {
                float a = StartAmplitude;
                for (; octave > StartOctave; octave--)
                    a *= AmplitudeMul;
                return a;
            }

        }
    }
}
