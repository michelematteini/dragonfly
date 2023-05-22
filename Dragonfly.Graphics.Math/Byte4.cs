using System;
using System.Runtime.InteropServices;

namespace Dragonfly.Graphics.Math
{
    /// <summary>
    /// Represent an RGB + Alpha color stored in 8 bit / Channel. 
    /// Memory ordering in this stuct is B->G->R->A
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public struct Byte4 : IEquatable<Byte4>
    {
        public byte B;
        public byte G;
        public byte R;
        public byte A;

        /// <summary>
        /// Create a color from a 32bit integer. LSB is blue, MSB is alpha.
        /// </summary>
        /// <param name="argb"></param>
        public Byte4(int argb)
        {
            B = (byte)(argb & 0x000000ff);
            argb >>= 8;
            G = (byte)(argb & 0x000000ff);
            argb >>= 8;
            R = (byte)(argb & 0x000000ff);
            argb >>= 8;
            A = (byte)argb;
        }

        public Byte4(int a, int r, int g, int b)
        {
            A = (byte)a;
            R = (byte)r;
            G = (byte)g;
            B = (byte)b;
        }

        public override int GetHashCode()
        {
            return ToInt();
        }

        public override string ToString()
        {
            return String.Format("[A:{3}, R:{0}, G:{1}, B:{2}]", R, G, B, A);
        }

        public Float4 ToFloat4()
        {
            return Float4.FromRGBA(R, G, B, A);
        }

        public Float3 ToFloat3()
        {
            return Float3.FromRGB(R, G, B);
        }

        public int ToInt()
        {
            int hash = A;
            hash <<= 8;
            hash |= R;
            hash <<= 8;
            hash |= G;
            hash <<= 8;
            hash |= B;
            return hash;
        }

        public bool Equals(Byte4 other)
        {
            return A == other.A && R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Byte4)) return false;
            return Equals((Byte4)obj);
        }

        public static bool operator ==(Byte4 c1, Byte4 c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(Byte4 c1, Byte4 c2)
        {
            return !c1.Equals(c2);
        }

        public static Byte4 Lerp(Byte4 c1, Byte4 c2, float amount)
        {
            int a = (int)(amount * 255.0f);
            int na = 255 - a;
            Byte4 c;
            c.R = (byte)((c1.R * na + c2.R * a) / 255);
            c.G = (byte)((c1.G * na + c2.G * a) / 255);
            c.B = (byte)((c1.B * na + c2.B * a) / 255);
            c.A = (byte)((c1.A * na + c2.A * a) / 255);
            return c;
        }

    }
}
