using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Math
{
    /// <summary>
    /// Color conversion, constants and utilities. 
    /// </summary>
    public static class Color
    {
        #region Colors

        public static readonly Byte4 White       = new Byte4(255, 255, 255, 255);
        public static readonly Byte4 Black       = new Byte4(255,   0,   0,   0);
        public static readonly Byte4 Gray        = new Byte4(255, 127, 127, 127);
        public static readonly Byte4 Red         = new Byte4(255, 255,   0,   0);
        public static readonly Byte4 Orange      = new Byte4(255, 255, 100,   0);
        public static readonly Byte4 Yellow      = new Byte4(255, 255, 255,   0);
        public static readonly Byte4 Green       = new Byte4(255,   0, 255,   0);
        public static readonly Byte4 Blue        = new Byte4(255,   0,   0, 255);
        public static readonly Byte4 LightBlue   = new Byte4(255,   0, 127, 255);
        public static readonly Byte4 Magenta     = new Byte4(255, 255,   0, 255);
        public static readonly Byte4 DarkGreen   = new Byte4(255,   0, 127,   0);
        public static readonly Byte4 Purple      = new Byte4(255, 127,  0,  255);
        public static readonly Byte4 Cyan        = new Byte4(255,   0, 255, 255);
        public static readonly Byte4 TransparentWhite = new Byte4(  0, 255, 255, 255);
        public static readonly Byte4 TransparentBlack = new Byte4(  0, 0, 0, 0);

        #endregion

        #region Conversions

        private const float eps = 0.0000001f;

        public static Float3 Hsv2Rgb(Float3 c)
        {
            Float3 rgb = (((c.X * 6.0f + new Float3(0.0f, 4.0f, 2.0f)).Mod(6.0f) - 3.0f).Abs() - 1.0f).Saturate();
            return c.Z * Float3.One.Lerp(rgb, c.Y);
        }

        public static Float3 Hsl2Rgb(Float3 c)
        {
            Float3 rgb = (((c.X * 6.0f + new Float3(0.0f, 4.0f, 2.0f)).Mod(6.0f) - 3.0f).Abs() - 1.0f).Saturate();
            return c.Z + c.Y * (rgb - 0.5f) * (1.0f - FMath.Abs(2.0f * c.Z - 1.0f));
        }

        public static Float3 Rgb2Hsv(Float3 c)
        {
            Float4 k = new Float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
            Float4 p = Float4.Lerp(new Float4(c.Z, c.Y, k.W, k.Z), new Float4(c.Y, c.Z, k.X, k.Y), (c.Z < c.Y) ? 1.0f : 0.0f);
            Float4 q = Float4.Lerp(new Float4(p.X, p.Y, p.W, c.X), new Float4(c.X, p.Y, p.Z, p.X), (p.X < c.X) ? 1.0f : 0.0f);
            float d = q.X - System.Math.Min(q.W, q.Y);
            return new Float3(FMath.Abs(q.Z + (q.W - q.Y) / (6.0f * d + eps)), d / (q.X + eps), q.X);
        }

        public static Float3 Rgb2Hsl(Float3 col)
        {
            float minc = System.Math.Min(col.R, System.Math.Min(col.G, col.B));
            float maxc = System.Math.Max(col.R, System.Math.Max(col.G, col.B));
            Float3 mask = Float3.Step(new Float3(col.G, col.R, col.R), col) * Float3.Step(new Float3(col.B, col.B, col.G), col);
            Float3 h = mask * (new Float3(0.0f, 2.0f, 4.0f) + (new Float3(col.G, col.B, col.R) - new Float3(col.B, col.R, col.G)) / (maxc - minc + eps)) / 6.0f;
            return new Float3(FMath.Frac(1.0f + h.X + h.Y + h.Z), (maxc - minc) / (1.0f - FMath.Abs(minc + maxc - 1.0f) + eps), (minc + maxc) * 0.5f); // H, S, L
        }

        #endregion

        public static float GetLuminanceFromRGB(Float3 rgbColor)
        {
            return rgbColor.Dot(new Float3(0.2126f, 0.7152f, 0.0722f));
        }
    }
}
