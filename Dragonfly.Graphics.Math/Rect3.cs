using System;

namespace Dragonfly.Graphics.Math
{
    /// <summary>
    /// A rectangle in 3d space
    /// </summary>
    public struct Rect3
    {
        private Float3 xSideDir, ySideDir;       
        public Float3 Position;
        public Float2 Size;

        /// <summary>
        /// Create a new rectangle from its position and sides. The second side direction will be adjusted to make it orthogonal to the first.
        /// </summary>
        /// <param name="position">The 3d position of the first vertex.</param>
        /// <param name="xSideDir">The direction of the first side starting from the first vertex.</param>
        /// <param name="ySideDir">The direction of the second side starting from the first vertex.</param>
        /// <param name="size">The lenght of the first and second sides.</param>
        public Rect3(Float3 position, Float3 xSideDir, Float3 ySideDir, Float2 size)
        {
            Position = position;
            Size = size; 
            this.xSideDir = xSideDir.Normal();
            this.ySideDir = ySideDir.Normal(); 
            YSideDir = ySideDir;
        }
        
        public Float3 XSideDir
        {
            get { return xSideDir; }
            set
            {
                xSideDir = value;
                xSideDir = Normal.Cross(ySideDir).Normal();
            }
        }

        public Float3 YSideDir
        {
            get { return ySideDir; }
            set
            {
                ySideDir = value;
                ySideDir = xSideDir.Cross(Normal).Normal();
            }
        }

        public Float3 Normal
        {
            get
            {
                return ySideDir.Cross(xSideDir);
            }
        }

        public Float3 Center
        {
            get
            {
                return Position + 0.5f * (xSideDir * Size.X + ySideDir * Size.Y);
            }
        }

        /// <summary>
        /// The corner of this rectangle, opposite to its position.
        /// </summary>
        public Float3 EndCorner
        {
            get
            {
                return Position + xSideDir * Size.X + ySideDir * Size.Y;
            }
        }

        /// <summary>
        /// Interpolate a position on this rectangle, from a given coordinate.
        /// </summary>
        /// <param name="coords">Coordinates in the [0, 1] range over this rectiogle where (0, 0) is this rectangle position.</param>
        /// <returns></returns>
        public Float3 GetPositionAt(Float2 coords)
        {
            return Position + coords.X * xSideDir * Size.X + coords.Y * ySideDir * Size.Y;
        }

        /// <summary>
        /// Returns the coordinated of the specified position, projected to this rectangle
        /// </summary>
        public Float2 GetCoordsAt(Float3 position)
        {
            Float3 localPos = position - this.Position;
            return new Float2(localPos.Dot(XSideDir), localPos.Dot(ySideDir)) / Size;
        }

        /// <summary>
        /// Create a tranformation matrix that move, rotate and scale an object from a unit-sized area with a normal facing the positive Y axis to this area.
        /// </summary>
        public Float4x4 ToWorldTransform()
        {
            return new Float4x4(Size.X * XSideDir, Normal, Size.Y * YSideDir) { Position = this.Position};
        }

        public override string ToString()
        {
            return String.Format("{0}=>{1}", Position, EndCorner);
        }

        public override bool Equals(object obj)
        {
            if (obj is Rect3 other)
                return other == this;
            else return false;
        }

        public override int GetHashCode()
        {
            return new Tuple<Float3, Float3, Float3, Float2>(xSideDir, ySideDir, Position, Size).GetHashCode();
        }

        public static bool operator ==(Rect3 r1, Rect3 r2)
        {
            return r1.xSideDir == r2.xSideDir && r1.ySideDir == r2.ySideDir && r1.Position == r2.Position && r1.Size == r2.Size;
        }

        public static bool operator !=(Rect3 r1, Rect3 r2)
        {
            return !(r1 == r2);
        }
    }
}
