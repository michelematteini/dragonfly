using System;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public static class SRGB
    {
        private static float encodePow = 1.0f / 2.2f;
        private static float decodePow = 2.2f;

        public static readonly HdrColorEncoder Encoder = (float[] srcBuffer, int srcStart, int srcEnd, byte[] destBuffer, int destStart) =>
        {
            for (int i = srcStart; i < srcEnd; i+=3)
            {
                destBuffer[destStart++] = FMath.Pow(srcBuffer[i + 2], encodePow).ToByte();
                destBuffer[destStart++] = FMath.Pow(srcBuffer[i + 1], encodePow).ToByte();
                destBuffer[destStart++] = FMath.Pow(srcBuffer[i + 0], encodePow).ToByte();
                destBuffer[destStart++] = 255;
            }
        };

        public static Float3 Encode(Float3 linearColor)
        {
            return Float3.Pow(linearColor, encodePow);
        }

        public static Float3 Decode(Float3 srgbColor)
        {
            return Float3.Pow(srgbColor, decodePow);
        }
    }
}
