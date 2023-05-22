using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Decode an array of color data.
    /// </summary>
    public delegate void HdrColorDecoder(byte[] srcBuffer, int srcStart, int srcEnd, float[] destBuffer, int destStart);

    /// <summary>
    /// Decode an array of color data
    /// </summary>
    public delegate void ColorDecoder(byte[] srcBuffer, int srcStart, int srcEnd, byte[] destBuffer, int destStart);

    /// <summary>
    /// Encode an array of color data.
    /// </summary>
    public delegate void HdrColorEncoder(float[] srcBuffer, int srcStart, int srcEnd, byte[] destBuffer, int destStart);

    /// <summary>
    /// Encode an array of color data.
    /// </summary>
    /// <param name="srcBuffer">The color data, in BGRA order (interleaved).</param>
    /// <param name="srcStart">The index from which to start encoding the srcBuffer.</param>
    /// <param name="destBuffer">The buffer where to write the encoded result.</param>
    /// <param name="destStart">The index at which to start writing.</param>
    public delegate void ColorEncoder(byte[] srcBuffer, int srcStart, int srcEnd, byte[] destBuffer, int destStart);


    public static class ColorEncoding
    {
        private static byte[] pixelBuffer1 = new byte[4];
        private static float[] hdrPixelBuffer = new float[3];

        public static Byte4 EncodeHdr(Float3 color, HdrColorEncoder encoder)
        {
            hdrPixelBuffer[0] = color.R;
            hdrPixelBuffer[1] = color.G;
            hdrPixelBuffer[2] = color.B;
            encoder(hdrPixelBuffer, 0, 3, pixelBuffer1, 0);
            return new Byte4(pixelBuffer1[3], pixelBuffer1[2], pixelBuffer1[1], pixelBuffer1[0]);
        }

        public static Float3 DecodeHdr(Byte4 color, HdrColorDecoder decoder)
        {
            pixelBuffer1[0] = color.B;
            pixelBuffer1[1] = color.G;
            pixelBuffer1[2] = color.R;
            pixelBuffer1[3] = color.A;
            decoder(pixelBuffer1, 0, 3, hdrPixelBuffer, 0);
            return new Float3(hdrPixelBuffer[0], hdrPixelBuffer[1], hdrPixelBuffer[2]);
        }
    }
}