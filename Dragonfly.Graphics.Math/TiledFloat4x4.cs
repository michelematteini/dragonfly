
using System;

namespace Dragonfly.Graphics.Math
{
    public struct TiledFloat4x4
    {
        public static readonly TiledFloat4x4 Identity = new TiledFloat4x4() { Value = Float4x4.Identity, Tile = Int3.Zero };


        public Float4x4 Value;
        public Int3 Tile;

        /// <summary>
        /// Collapse this tiled transform to a simple matrix, given a reference tile used as origin.
        /// </summary>
        public Float4x4 ToFloat4x4(Int3 referenceTile)
        {
            return Value.Translate((Tile - referenceTile) * TiledFloat.TileSize);
        }

        /// <summary>
        /// Apply a pre-translation to this matrix that will move transformed objects from the current tile to the specified one,
        /// returning a transformation that is equivalent to the current for objects that are  referenced to the specified tile.
        /// NB: This operation degrades the matrix precision, especially if the new reference is distant from the original.
        /// </summary>
        public TiledFloat4x4 Rebase(Int3 referenceTile)
        {
            TiledFloat4x4 translatedTransform;
            translatedTransform.Value = Float4x4.Translation((referenceTile - Tile) * TiledFloat.TileSize) * Value;
            translatedTransform.Tile = referenceTile;
            return translatedTransform;
        }

        public static implicit operator TiledFloat4x4(Float4x4 value)
        {
            return new TiledFloat4x4() { Value = value };
        }

        public Float4x4 ToFloat4x4()
        {
            return ToFloat4x4(Int3.Zero);
        }

        public TiledFloat3 Position
        {
            get
            {
                return new TiledFloat3(Value.Position, Tile);
            }
            set
            {
                Value.Position = value.Value;
                Tile = value.Tile;
            }
        }

        /// <summary>
        /// Sum the given tile offset to the current matrix, which is equivalent to applying a post-translation
        /// </summary>
        public TiledFloat4x4 Translate(Int3 tileOffset)
        {
            return new TiledFloat4x4() { Value = this.Value, Tile = this.Tile + tileOffset };
        }

        /// <summary>
        /// Sum the given tile offset to the current matrix, which is equivalent to applying a post-translation
        /// </summary>
        public TiledFloat4x4 Translate(TiledFloat3 translation)
        {
            return new TiledFloat4x4() { Value = Value.Translate(translation.Value), Tile = Tile + translation.Tile };
        }

        public static TiledFloat4x4 operator *(Float4x4 m1, TiledFloat4x4 m2)
        {
            return new TiledFloat4x4() { Value = m1 * m2.Value, Tile = m2.Tile };
        }

        public static TiledFloat4x4 operator *(TiledFloat4x4 m1, TiledFloat4x4 m2)
        {
            TiledFloat4x4 result;
            if (m2.Value.IsIdentity())
            {
                // translation only, can be accumulated without loosing precition
                result = m1;
                result.Position += m2.Position;
            }
            else
            {
                // complete transform multiplication
#if DEBUG
                if (m1.Tile != Int3.Zero)
                    System.Diagnostics.Debug.WriteLine("Warning: TiledFloat4x4 multiplied, with the left operand containing a tile offset! This can cause precision issues.");
#endif
                result.Tile = m2.Tile;
                result.Value = m1.ToFloat4x4() * m2.Value;
            }
            return result;
        }

        public static TiledFloat4x4 Translation(TiledFloat3 tiledFloat3)
        {
            return new TiledFloat4x4() { Value = Float4x4.Translation(tiledFloat3.Value), Tile = tiledFloat3.Tile };
        }
    }
}
