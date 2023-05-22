
using System;

namespace Dragonfly.Graphics.Math
{
    public struct TiledFloat3
    {
        public static TiledFloat3 Zero = new TiledFloat3(Float3.Zero, Int3.Zero);

        public TiledFloat X, Y, Z;

        public TiledFloat3(Float3 value, Int3 tile)
        {
            X = new TiledFloat() { Value = value.X, Tile = tile.X };
            Y = new TiledFloat() { Value = value.Y, Tile = tile.Y };
            Z = new TiledFloat() { Value = value.Z, Tile = tile.Z };
        }

        public Float3 ToFloat3(Int3 referenceTile)
        {
            return new Float3(X.ToFloat(referenceTile.X), Y.ToFloat(referenceTile.Y), Z.ToFloat(referenceTile.Z));
        }

        public Float3 ToFloat3()
        {
            return ToFloat3(Int3.Zero);
        }

        public Float3 Value
        {
            get
            {
                return new Float3(X.Value, Y.Value, Z.Value);
            }
            set
            {
                X.Value = value.X;
                Y.Value = value.Y;
                Z.Value = value.Z;
            }
        }

        public Int3 Tile
        {
            get
            {
                return new Int3(X.Tile, Y.Tile, Z.Tile);
            }
            set
            {
                X.Tile = value.X;
                Y.Tile = value.Y;
                Z.Tile = value.Z;
            }
        }

        public TiledFloat Length
        {
            get
            {
                double x = X.ToDouble(), y = Y.ToDouble(), z = Z.ToDouble();
                return new TiledFloat(System.Math.Sqrt(x * x + y * y + z * z));
            }
        }

        public TiledFloat LengthSquared
        {
            get
            {
                return this.Dot(this);
            }
        }

        public static TiledFloat3 operator +(TiledFloat3 v1, TiledFloat3 v2)
        {
            return new TiledFloat3 { X = v1.X + v2.X, Y = v1.Y + v2.Y, Z = v1.Z + v2.Z };
        }

        public static TiledFloat3 operator -(TiledFloat3 v1, TiledFloat3 v2)
        {
            return new TiledFloat3 { X = v1.X - v2.X, Y = v1.Y - v2.Y, Z = v1.Z - v2.Z };
        }

        public static TiledFloat3 operator +(TiledFloat3 v1, Float3 v2)
        {
            return v1 + new TiledFloat3(v2, Int3.Zero);
        }

        public static TiledFloat3 operator *(TiledFloat3 v, float k)
        {
            return new TiledFloat3() { X = v.X * k, Y = v.Y * k, Z = v.Z * k };
        }

        public static TiledFloat3 operator *(float k, TiledFloat3 v)
        {
            return new TiledFloat3 { X = v.X * k, Y = v.Y * k, Z = v.Z * k };
        }

        public static TiledFloat3 operator *(TiledFloat3 v, TiledFloat k)
        {
            return new TiledFloat3() { X = v.X * k, Y = v.Y * k, Z = v.Z * k };
        }

        public static TiledFloat3 operator *(TiledFloat k, TiledFloat3 v)
        {
            return new TiledFloat3 { X = v.X * k, Y = v.Y * k, Z = v.Z * k };
        }

        public static TiledFloat3 operator /(TiledFloat3 v, TiledFloat k)
        {
            return v * new TiledFloat(1.0 / k.ToDouble());
        }

        public static implicit operator TiledFloat3(Float3 v)
        {
            return new TiledFloat3(v, Int3.Zero);
        }

        public TiledFloat Dot(Float3 v)
        {
            return X * v.X + Y * v.Y + Z * v.Z;
        }

        public TiledFloat Dot(TiledFloat3 v)
        {
            return X * v.X + Y * v.Y + Z * v.Z;
        }

        public static TiledFloat3 Lerp(TiledFloat3 v1, TiledFloat3 v2, float alpha)
        {
            if (alpha == 0)
                return v1;
            if (alpha == 1)
                return v2;

            Int3 tile = alpha < 0.5f ? v1.Tile : v2.Tile; // choose the reference tile that best preserve precision
            Float3 value = v1.ToFloat3(tile).Lerp(v2.ToFloat3(tile), alpha);

            return new TiledFloat3(value, tile).NormalizeTile();
        }

        /// <summary>
        /// Project this vector on a line specified by the two given points.
        /// </summary>
        public TiledFloat3 ProjectTo(TiledFloat3 linePnt1, TiledFloat3 linePnt2)
        {
            TiledFloat3 lineVec = linePnt2 - linePnt1;
            Float3 lineDir = lineVec.ToFloat3().Normal();
            return (this - linePnt1).Dot(lineDir) * lineDir + linePnt1;
        }

        /// <summary>
        /// Returns true if this point is between the specified points (or equals them).
        /// <para/> If this point doesn't lie on a line beween the two points, the result refers to the projection of this point to that line.
        /// </summary>
        public bool IsBetween(TiledFloat3 includedStartPnt, TiledFloat3 includedEndPnt)
        {
            return (this - includedStartPnt).Dot((this - includedEndPnt).ToFloat3().Normal()).ToFloat() <= 0;
        }

        /// <summary>
        /// Returns a value that is the linear interpolation factor between the specified two points that generates this point.
        /// <para/> If this point doesn't lie on a line beween the two points, the interpolation value refers to the projection of this point to that line.
        /// </summary>
        public double InterpolatesAt(TiledFloat3 v1, TiledFloat3 v2)
        {
            TiledFloat3 lineVec = v2 - v1;
            return (this - v1).Dot(lineVec).ToDouble() / lineVec.LengthSquared.ToDouble();
        }

        public TiledFloat3 NormalizeTile()
        {
            return new TiledFloat3() { X = X.NormalizeTile(), Y = Y.NormalizeTile(), Z = Z.NormalizeTile() };
        }

        public override string ToString()
        {
            return Tile.ToString() + ToFloat3().ToString();
        }

        public override int GetHashCode()
        {
            int hash = 352654597;
            hash = ((hash << 5) + hash + (hash >> 27)) ^ Tile.GetHashCode();
            hash = ((hash << 5) + hash + (hash >> 27)) ^ Value.GetHashCode();
            return hash;
        }

        public static bool operator ==(TiledFloat3 v1, TiledFloat3 v2)
        {
            return v1.Value == v2.Value && v1.Tile == v2.Tile;
        }

        public static bool operator !=(TiledFloat3 v1, TiledFloat3 v2)
        {
            return !(v1 == v2);
        }

        public override bool Equals(object obj)
        {
            if (obj is TiledFloat3 v)
                return this == v;
            return false;
        }

    }
}
