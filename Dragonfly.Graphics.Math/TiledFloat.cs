
using System;

namespace Dragonfly.Graphics.Math
{

    public struct TiledFloat
    {
        /// <summary>
        /// This constant fix the tile size for all tiled values in this library. 
        /// Should be set to the wanted value before using tiled structs and never modified again!
        /// NB: this size should be a integer value that can be exactly represented as float!
        /// </summary>
        public static float TileSize = 1024.0f;


        public static readonly TiledFloat Zero = new TiledFloat() { Value = 0, Tile = 0 };

        public float Value;
        public int Tile;

        public TiledFloat(double value)
        {
            Tile = (int)System.Math.Floor(value / TileSize + 0.5);
            Value = (float)(value - Tile * TileSize);
        }

        public TiledFloat(float value)
        {
            Tile = (int)FMath.Floor(value / TileSize + 0.5f);
            Value = value - Tile * TileSize;
        }

        public static implicit operator TiledFloat(float value)
        {
            return new TiledFloat(value);
        }

        public TiledFloat NormalizeTile()
        {
            TiledFloat normTF = new TiledFloat();

            int tileDiff = (int)FMath.Floor(Value / TileSize + 0.5f);
            normTF.Tile = Tile + tileDiff;
            normTF.Value = Value - tileDiff * TileSize;
            return normTF;
        }

        #region Operators

        public static TiledFloat operator +(TiledFloat v1, TiledFloat v2)
        {
            return new TiledFloat() { Value = v1.Value + v2.Value, Tile = v1.Tile + v2.Tile }.NormalizeTile();
        }

        public static TiledFloat operator -(TiledFloat v1, TiledFloat v2)
        {
            return new TiledFloat() { Value = v1.Value - v2.Value, Tile = v1.Tile - v2.Tile }.NormalizeTile();
        }

        public static TiledFloat operator +(TiledFloat v1, float v2)
        {
            return new TiledFloat() { Value = v1.Value + v2, Tile = v1.Tile }.NormalizeTile();
        }

        public static TiledFloat operator -(TiledFloat v1, float v2)
        {
            return new TiledFloat() { Value = v1.Value - v2, Tile = v1.Tile }.NormalizeTile();
        }

        public static TiledFloat operator *(TiledFloat v1, TiledFloat v2)
        {
            // cross part of multiplication (in tiles)
            double tileMul = (double)v1.Value * v2.Tile + (double)v2.Value * v1.Tile;
            int tileMulInt = (int)tileMul;

            // value part of mul, plus the value reminder of the cross mul
            double valueMul = (double)v1.Value * v2.Value + (tileMul - tileMulInt) * TileSize; // everything that must be expanded, depending on the values some rounding may occur, but double should keep it below the single precision

            // move the whole tiles in the value part to the tile part
            int valueTileMulInt = (int)(valueMul / TileSize);
            tileMulInt += valueTileMulInt;
            valueMul -= valueTileMulInt * TileSize;

            // add the tile-only part with no rounding
#if DEBUG
            checked
            {
                tileMulInt += v1.Tile * v2.Tile * (int)TileSize;
            }
#else
            tileMulInt += v1.Tile * v2.Tile * (int)TileSize;
#endif
            return new TiledFloat() { Value = (float)valueMul, Tile = tileMulInt }.NormalizeTile();
        }

        public static TiledFloat operator *(TiledFloat v, float k)
        {
            return v * new TiledFloat() { Value = k };
        }

        public static TiledFloat operator *(float k, TiledFloat v)
        {
            return v * new TiledFloat() { Value = k };
        }

        public static TiledFloat3 operator *(Float3 v, TiledFloat scale)
        {
            return new TiledFloat3() { X = v.X * scale, Y = v.Y * scale, Z = v.Z * scale };
        }

        public static TiledFloat3 operator *(TiledFloat scale, Float3 v)
        {
            return new TiledFloat3() { X = v.X * scale, Y = v.Y * scale, Z = v.Z * scale };
        }

        public static TiledFloat operator /(TiledFloat v, float k)
        {
            return v * new TiledFloat() { Value = 1.0f / k };
        }

        public static TiledFloat operator /(TiledFloat v1, TiledFloat v2)
        {
            return v1 * new TiledFloat(1.0f / v2.ToDouble());
        }

        public static bool operator ==(TiledFloat v1, TiledFloat v2)
        {
            return v1.Tile == v2.Tile && v1.Value == v2.Value;
        }

        public static bool operator !=(TiledFloat v1, TiledFloat v2)
        {
            return !(v1 == v2);
        }

        public override bool Equals(object obj)
        {
            if (obj is TiledFloat f)
                return f == this;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return ToDouble().GetHashCode();
        }

        public static bool operator >(TiledFloat v1, TiledFloat v2)
        {
            return (v1 - v2).ToFloat() > 0;
        }

        public static bool operator <(TiledFloat v1, TiledFloat v2)
        {
            return (v2 - v1).ToFloat() > 0;
        }

#endregion


        public float ToFloat(int refTile)
        {
            return Value + (Tile - refTile) * TileSize;
        }

        public float ToFloat()
        {
            return ToFloat(0);
        }

        public double ToDouble()
        {
            return (double)Tile * (double)TileSize + (double)Value;
        }

        public override string ToString()
        {
            return $"{ToFloat()} ({Tile}T + {Value})";
        }

    }
}
