using System;

namespace Dragonfly.Graphics.Math
{
    /// <summary>
    /// Tiled version of a rectangle in 3d space
    /// </summary>
    public struct TiledRect3
    {
        private Float3 xSideDir, ySideDir;
        public TiledFloat3 Position;
        public Float2 Size;

        /// <summary>
        /// Create a new rectangle from its position and sides. The second side direction will be adjusted to make it orthogonal to the first.
        /// </summary>
        /// <param name="position">The 3d position of the first vertex.</param>
        /// <param name="xSideDir">The direction of the first side starting from the first vertex.</param>
        /// <param name="ySideDir">The direction of the second side starting from the first vertex.</param>
        /// <param name="size">The lenght of the first and second sides.</param>
        public TiledRect3(TiledFloat3 position, Float3 xSideDir, Float3 ySideDir, Float2 size)
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

        public TiledFloat3 Center
        {
            get
            {
                return Position + 0.5f * (xSideDir * Size.X + ySideDir * Size.Y);
            }
        }

        /// <summary>
        /// The corner of this rectangle, opposite to its position.
        /// </summary>
        public TiledFloat3 EndCorner
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
        public TiledFloat3 GetPositionAt(Float2 coords)
        {
            return Position + (coords.X * xSideDir * Size.X + coords.Y * ySideDir * Size.Y);
        }

        /// <summary>
        /// Returns the coordinates of the specified position, projected to this rectangle
        /// </summary>
        public Float2 GetCoordsAt(TiledFloat3 position)
        {
            Float3 localPos = (position - this.Position).ToFloat3();
            return new Float2(localPos.Dot(xSideDir), localPos.Dot(ySideDir)) / Size;
        }

        /// <summary>
        /// Fills the coordinates of the specified position, projected to this rectangle (with double precision)
        /// </summary>
        public void GetCoordsAt(TiledFloat3 position, out double x, out double y)
        {
            TiledFloat3 localPos = position - this.Position;
            x = (localPos.X.ToDouble() * xSideDir.X + localPos.Y.ToDouble() * xSideDir.Y + localPos.Z.ToDouble() * xSideDir.Z) / Size.X;
            y = (localPos.X.ToDouble() * ySideDir.X + localPos.Y.ToDouble() * ySideDir.Y + localPos.Z.ToDouble() * ySideDir.Z) / Size.Y;
        }

        /// <summary>
        /// Create a tranformation matrix that move, rotate and scale an object from a unit-sized area with a normal facing the positive Y axis to this area.
        /// </summary>
        public TiledFloat4x4 GetLocalToWorldTransform()
        {
            TiledFloat4x4 worldTransform;
            worldTransform.Value = new Float4x4(Size.X * XSideDir, Normal, Size.Y * YSideDir) { Position = this.Position.Value };
            worldTransform.Tile = Position.Tile;
            return worldTransform;
        }

        /// <summary>
        /// Create a tranformation matrix that rotate and scale an world position offset in this area to a unit-sized area with a normal facing the positive Y axis.
        /// </summary>
        public Float4x4 GetWorldOffsetToLocalTransform()
        {
            Float4x4 transform = new Float4x4(XSideDir / Size.X, Normal, YSideDir / Size.Y);
            return transform.Transpose();
        }

        public Rect3 ToRect3(Int3 referenceTile)
        {
            return new Rect3(Position.ToFloat3(referenceTile), xSideDir, ySideDir, Size);
        }

        /// <summary>
        /// Returns the intersection of this rectangle with the specified ray. 
        /// The sign of the direction is not taken into account, and will return intersections both before and after the start point.
        /// Will also return an intersection outside the bounds of the rectangle but on the same plane
        /// </summary>
        /// <param name="rayStart"></param>
        /// <param name="rayDirection"></param>
        /// <returns></returns>
        public TiledFloat3 RayPlaneIntersection(TiledFloat3 rayStart, Float3 rayDirection)
        {
            Float4 rectPlane = Plane.FromNormal(Position.Value, Normal);
            Float3 localRayStart = rayStart.ToFloat3(Position.Tile);
            Float3 localIntersection = FMath.PlaneRayIntersection(rectPlane, localRayStart, rayDirection);
            return new TiledFloat3(localIntersection, Position.Tile);
        }

        public TiledFloat3 GetPointClosestTo(TiledFloat3 camPos)
        {
            Float3 relativeCamPos = (camPos - Position).ToFloat3();
            Float3 c0 = Float3.Zero;
            Float3 c1 = xSideDir * Size.X;
            Float3 c2 = xSideDir * Size.X + ySideDir * Size.Y;
            Float3 c3 = ySideDir * Size.Y;
            Float3 c01Closest = c0.Lerp(c1, relativeCamPos.InterpolatesAt(c0, c1).Saturate());
            Float3 c12Closest = c1.Lerp(c2, relativeCamPos.InterpolatesAt(c1, c2).Saturate());
            Float3 c23Closest = c2.Lerp(c3, relativeCamPos.InterpolatesAt(c2, c3).Saturate());
            Float3 c30Closest = c3.Lerp(c0, relativeCamPos.InterpolatesAt(c3, c0).Saturate());
            float d01 = (c01Closest - relativeCamPos).LengthSquared;
            float d12 = (c12Closest - relativeCamPos).LengthSquared;
            float d23 = (c23Closest - relativeCamPos).LengthSquared;
            float d30 = (c30Closest - relativeCamPos).LengthSquared;
            Float3 closestEdgePoint = c01Closest;
            float closestDist = d01;

            if (d12 < closestDist)
            {
                closestEdgePoint = c12Closest;
                closestDist = d12;
            }
            if (d23 < closestDist)
            {
                closestEdgePoint = c23Closest;
                closestDist = d23;
            }
            if (d30 < closestDist)
                closestEdgePoint = c30Closest;

            return Position + closestEdgePoint;
        }

        public Rect3 ToRect3()
        {
            return ToRect3(Int3.Zero);
        }

        public override string ToString()
        {
            return String.Format("{0}=>{1}", Position.ToFloat3(), EndCorner.ToFloat3());
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 352654597;
                hash = ((hash << 5) + hash + (hash >> 27)) ^ xSideDir.GetHashCode();
                hash = ((hash << 5) + hash + (hash >> 27)) ^ ySideDir.GetHashCode();
                hash = ((hash << 5) + hash + (hash >> 27)) ^ Position.GetHashCode();
                hash = ((hash << 5) + hash + (hash >> 27)) ^ Size.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(TiledRect3 r1, TiledRect3 r2)
        {
            return r1.xSideDir == r2.xSideDir && r1.ySideDir == r2.ySideDir && r1.Position == r2.Position && r1.Size == r2.Size;
        }

        public static bool operator !=(TiledRect3 r1, TiledRect3 r2)
        {
            return !(r1 == r2);
        }

        public override bool Equals(object obj)
        {
            if (obj is TiledRect3 iv)
                return this == iv;
            return false;
        }

        public void GetCorners(TiledFloat3[] areaCorners, int startIndex)
        {
            areaCorners[startIndex + 0] = Position;
            areaCorners[startIndex + 1] = Position + xSideDir * Size.X;
            areaCorners[startIndex + 3] = Position + xSideDir * Size.X + ySideDir * Size.Y;
            areaCorners[startIndex + 2] = Position + ySideDir * Size.Y;
        }

        public void GetEdgeMiddlePoints(TiledFloat3[] areaCorners, int startIndex)
        {
            areaCorners[startIndex + 0] = Position + 0.5f * xSideDir * Size.X;
            areaCorners[startIndex + 1] = Position + xSideDir * Size.X + 0.5f * ySideDir * Size.Y;
            areaCorners[startIndex + 3] = Position + 0.5f * xSideDir * Size.X + ySideDir * Size.Y;
            areaCorners[startIndex + 2] = Position + 0.5f * ySideDir * Size.Y;
        }

        public bool IsCoplanarWith(TiledRect3 otherRect)
        {
            Float3 normal = Normal;
            if (otherRect.Normal != normal)
                return false;

            return (Position - otherRect.Position).ToFloat3().Dot(normal) == 0;
        }
    }
}
