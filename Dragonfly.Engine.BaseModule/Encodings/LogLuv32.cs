using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public static class LogLuv32
    {
        private static Float3x3 rbgToLuvTransform = new Float3x3(
            0.2209f, 0.3390f, 0.4184f,
            0.1138f, 0.6780f, 0.7319f,
            0.0102f, 0.1130f, 0.2969f
        );

        public static readonly HdrColorEncoder Encoder = (float[] srcBuffer, int srcStart, int srcEnd, byte[] destBuffer, int destStart) => 
        {
            for (int i = srcStart; i < srcEnd; i += 3)
            {
                Byte4 logLuv = Encode(new Float3(srcBuffer[i], srcBuffer[i + 1], srcBuffer[i + 2])).ToByte4();
                destBuffer[destStart++] = logLuv.B;
                destBuffer[destStart++] = logLuv.G;
                destBuffer[destStart++] = logLuv.R;
                destBuffer[destStart++] = logLuv.A;
            }
        };

        public static Float4 Encode(Float3 vRGB)
        {
            Float4 vResult = Float4.Zero;
            Float3 Xp_Y_XYZp = vRGB * rbgToLuvTransform;
            Xp_Y_XYZp = Float3.Max(Xp_Y_XYZp, (Float3)0.000001f);
            vResult.XY = Xp_Y_XYZp.XY / Xp_Y_XYZp.Z;
            float Le = 2 * (float)Math.Log(Xp_Y_XYZp.Y, 2.0) + 127;
            vResult.W = FMath.Frac(Le);
            vResult.Z = (Le - FMath.Floor(vResult.W * 255.0f) / 255.0f) / 255.0f;
            return vResult;
        }

    }
}
