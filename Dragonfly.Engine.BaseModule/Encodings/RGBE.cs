using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// RGB + exponent codec.
    /// </summary>
    public static class RGBE
    {
        // pre-calculated exponent multipliers
        private static float[] exponent; 

        static RGBE()
        {
            // pre-calculate an exponents to multiplier map for rgbe decoding
            exponent = new float[256];
            for (int i = 0; i < 256; i++)
                exponent[i] = FMath.Exp2(i - 127);
        }

        public static readonly HdrColorEncoder Encoder = (float[] srcBuffer, int srcStart, int srcEnd, byte[] destBuffer, int destStart) =>
        {
            for (int i = srcStart; i < srcEnd; i += 3)
            {
                byte e = (byte)((int)FMath.Ceil(FMath.Log2(Math.Max(srcBuffer[i + 0], Math.Max(srcBuffer[i + 1], srcBuffer[i + 2])))) + 127);
                destBuffer[destStart++] = (srcBuffer[i + 2] / exponent[e]).ToByte();
                destBuffer[destStart++] = (srcBuffer[i + 1] / exponent[e]).ToByte();
                destBuffer[destStart++] = (srcBuffer[i + 0] / exponent[e]).ToByte();
                destBuffer[destStart++] = e;
            }
        };

        public static readonly HdrColorDecoder Decoder = (byte[] srcBuffer, int srcStart, int srcEnd, float[] destBuffer, int destStart) =>
        {
            for (int i = srcStart; i < srcEnd; i += 4)
            {
                float mul = exponent[srcBuffer[i + 3]];
                destBuffer[destStart++] = srcBuffer[i + 2].ToFloat() * mul;
                destBuffer[destStart++] = srcBuffer[i + 1].ToFloat() * mul;
                destBuffer[destStart++] = srcBuffer[i + 0].ToFloat() * mul;
            }
        };

    }
}
