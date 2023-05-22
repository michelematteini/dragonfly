using System;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Reordered rgb components coded.
    /// </summary>
    public static class BGRA
    {
        /// <summary>
        /// Encode the input buffer  from rgb to bgr, in-place operation can be performed using the same buffer for both source and destination.
        /// </summary>
        public static readonly ColorEncoder Encoder = (byte[] srcBuffer, int srcStart, int srcEnd, byte[] destBuffer, int destStart) =>
        {
            for (int i = srcStart; i < srcEnd; i += 4)
            {
                byte srcR = srcBuffer[i]; // support for in-place operations
                destBuffer[destStart++] = srcBuffer[i + 2];
                destBuffer[destStart++] = srcBuffer[i + 1];
                destBuffer[destStart++] = srcR;
                destBuffer[destStart++] = srcBuffer[i + 3];
            }
        };
    }
}