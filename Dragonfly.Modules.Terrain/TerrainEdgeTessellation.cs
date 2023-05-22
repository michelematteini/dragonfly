using System;
using Dragonfly.Graphics.Math;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// Specify the level of detail of the edges in a terrain tile. This is needed to avoid gaps between terrain tiles with different LOD.
    /// </summary>
    public struct TerrainEdgeTessellation : IEquatable<TerrainEdgeTessellation>
    {
        public int TopDivisor, BottomDivisor, LeftDivisor, RightDivisor;

        public TerrainEdgeTessellation(int top, int bottom, int left, int right)
        {
            TopDivisor = top;
            BottomDivisor = bottom;
            LeftDivisor = left;
            RightDivisor = right;
        }

        public TerrainEdgeTessellation(int defaultValue) : this(defaultValue, defaultValue, defaultValue, defaultValue) { }

        /// <summary>
        /// Returns a Float4 with the divisors in the order left, top, right, bottom.
        /// </summary>
        /// <returns></returns>
        public Float4 ToFloat4()
        {
            return new Float4(LeftDivisor, TopDivisor, RightDivisor, BottomDivisor);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            hash = (hash << 8) + TopDivisor & 0xff;
            hash = (hash << 8) + BottomDivisor & 0xff;
            hash = (hash << 8) + LeftDivisor & 0xff;
            hash = (hash << 8) + RightDivisor & 0xff;
            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is TerrainEdgeTessellation))
                return false;
            TerrainEdgeTessellation other = (TerrainEdgeTessellation)obj;
            return this == other;
        }

        public static bool operator ==(TerrainEdgeTessellation t1, TerrainEdgeTessellation t2)
        {
            return t1.TopDivisor == t2.TopDivisor && t1.BottomDivisor == t2.BottomDivisor && t1.LeftDivisor == t2.LeftDivisor && t1.RightDivisor == t2.RightDivisor;
        }
        public static bool operator !=(TerrainEdgeTessellation t1, TerrainEdgeTessellation t2)
        {
            return !(t1 == t2);
        }

        public static TerrainEdgeTessellation operator *(TerrainEdgeTessellation t, int mul)
        {
            return new TerrainEdgeTessellation(t.TopDivisor * mul, t.BottomDivisor * mul, t.LeftDivisor * mul, t.RightDivisor * mul);
        }

        public override string ToString()
        {
            return string.Format("TOP:{0}, BOTTOM:{1}, LEFT:{2}, RIGHT:{3}", TopDivisor, BottomDivisor, LeftDivisor, RightDivisor);
        }

        public bool Equals(TerrainEdgeTessellation other)
        {
            return this == other;
        }
    }
}
