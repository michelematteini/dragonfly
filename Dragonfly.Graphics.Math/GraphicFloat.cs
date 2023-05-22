using System;

namespace Dragonfly.Graphics.Math
{
    public static class GraphicFloat
    {
        private const float ONE_OVER_510 = 0.00196078431f;

        public static int ToByteInt(this float value)
        {
            return value < 0f ? 0 : (value > 1.0f ? 255 : (int)((value + ONE_OVER_510) * 255f));
        }

        public static byte ToByte(this float value)
        {
            return value < 0f ? byte.MinValue : (value > 1.0f ? byte.MaxValue : (byte)((value + ONE_OVER_510) * 255f));
        }

        public static float ToFloat(this byte value)
        {
            return (float)value / 255f;
        }
		
		public static float ToRadians(this float value)
		{
            return FMath.ToRadians(value);
		}

		public static float ToDegree(this float value)
		{
            return FMath.ToDegree(value);
		}

        public static float Saturate(this float value)
        {         
            return FMath.Saturate(value);
        }

        public static float Frac(this float value)
        {
            return FMath.Frac(value);
        }

        public static float Floor(this float value)
        {
            return FMath.Floor(value);
        }

        public static float Clamp(this float value, float min, float max)
        {
            return FMath.Clamp(value, min, max);
        }

        private static float EPSILON = 0.000001f;
        public static bool IsAlmostZero(this float value)
        {
            return System.Math.Abs(value) < EPSILON;
        }

        public static bool AlmostEquals(this float reference, float value)
        {
            return IsAlmostZero(reference - value);
        }

        public static bool IsBetween(this float value, float includedMin, float includedMax)
        {
            return value >= includedMin && value <= includedMax;
        }

        public static float ToInterpolator(this float value, InterpolationType intType)
        {
            switch (intType)
            {
                case InterpolationType.Linear: default:
                    return value;
                case InterpolationType.Cubic:
                    {
                        float v2 = value * value;
                        return 3 * v2 - 2 * v2 * value;
                    }
                case InterpolationType.Square:
                    return value * value;
                case InterpolationType.Root:
                    return (float)System.Math.Sqrt(value);
            }
        }

        public static float Lerp(this float value, float other, float interpolationAmmount)
        {
            return FMath.Lerp(value, other, interpolationAmmount);
        }

        public static float ToFloat(this bool value)
        {
            return value ? 1.0f : 0.0f;
        }
    }


    public enum InterpolationType
    {
        Linear = 0,
        Cubic,
        Square,
        Root
    }



}
