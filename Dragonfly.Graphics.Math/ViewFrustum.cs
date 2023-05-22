using System;

namespace Dragonfly.Graphics.Math
{
    public struct ViewFrustum : IVolume
    {
        private Float4x4 cameraMatrix;

        public ViewFrustum(Float4x4 cameraMatrix)
        {
            this.cameraMatrix = cameraMatrix;
        }

        public Float4 LeftPlane
        {
            get { return cameraMatrix.GetColumn(3) + cameraMatrix.GetColumn(0); }
        }

        public Float4 RightPlane
        {
            get { return cameraMatrix.GetColumn(3) - cameraMatrix.GetColumn(0); }
        }

        public Float4 TopPlane
        {
            get { return cameraMatrix.GetColumn(3) - cameraMatrix.GetColumn(1); }
        }

        public Float4 BottomPlane
        {
            get { return cameraMatrix.GetColumn(3) + cameraMatrix.GetColumn(1); }
        }
        public Float4 NearPlane
        {
            get { return cameraMatrix.GetColumn(3) - cameraMatrix.GetColumn(2); }
        }

        public Float4 FarPlane
        {
            get { return cameraMatrix.GetColumn(2); }
        }

        public void GetPlanes(out Float4 leftPlane, out Float4 rightPlane, out Float4 topPlane, out Float4 bottomPlane, out Float4 nearPlane, out Float4 farPlane)
        {
            Float4 c0 = cameraMatrix.GetColumn(0), c1 = cameraMatrix.GetColumn(1), c2 = cameraMatrix.GetColumn(2), c3 = cameraMatrix.GetColumn(3);
            leftPlane = c3 + c0;
            rightPlane = c3 - c0;
            topPlane = c3 - c1;
            bottomPlane = c3 + c1;
            nearPlane = c3 - c2;
            farPlane = c2;
        }

        public bool Contains(Float3 point)
        {
            Float4 hPoint = point.ToFloat4(1.0f);
            Float4 leftPlane, rightPlane, topPlane, bottomPlane, nearPlane, farPlane;
            GetPlanes(out leftPlane, out rightPlane, out topPlane, out bottomPlane, out nearPlane, out farPlane);

            if (leftPlane.Dot(hPoint) < 0) return false;
            if (rightPlane.Dot(hPoint) < 0) return false;
            if (topPlane.Dot(hPoint) < 0) return false;
            if (bottomPlane.Dot(hPoint) < 0) return false;
            if (nearPlane.Dot(hPoint) < 0) return false;
            if (farPlane.Dot(hPoint) < 0) return false;
            return true;
        }

        public bool Contains(Sphere s)
        {
            Float4 hCenter = s.Center.ToFloat4(1.0f);
            Float4 leftPlane, rightPlane, topPlane, bottomPlane, nearPlane, farPlane;
            GetPlanes(out leftPlane, out rightPlane, out topPlane, out bottomPlane, out nearPlane, out farPlane);

            if (leftPlane.Dot(hCenter) < s.Radius) return false;
            if (rightPlane.Dot(hCenter) < s.Radius) return false;
            if (topPlane.Dot(hCenter) < s.Radius) return false;
            if (bottomPlane.Dot(hCenter) < s.Radius) return false;
            if (nearPlane.Dot(hCenter) < s.Radius) return false;
            if (farPlane.Dot(hCenter) < s.Radius) return false;

            return true;
        }

        private static bool IsBoxInsidePlane(AABox b, Float4 plane)
        {
            Float4 farthestCorner;
            farthestCorner.X = plane.X < 0 ? b.Max.X : b.Min.X;
            farthestCorner.Y = plane.Y < 0 ? b.Max.Y : b.Min.Y;
            farthestCorner.Z = plane.Z < 0 ? b.Max.Z : b.Min.Z;
            farthestCorner.W = 1.0f;
            return plane.Dot(farthestCorner) >= 0;
        }

        public bool Contains(AABox b)
        {
            Float4 leftPlane, rightPlane, topPlane, bottomPlane, nearPlane, farPlane;
            GetPlanes(out leftPlane, out rightPlane, out topPlane, out bottomPlane, out nearPlane, out farPlane);

            if (!IsBoxInsidePlane(b, leftPlane)) return false;
            if (!IsBoxInsidePlane(b, rightPlane)) return false;
            if (!IsBoxInsidePlane(b, topPlane)) return false;
            if (!IsBoxInsidePlane(b, bottomPlane)) return false;
            if (!IsBoxInsidePlane(b, nearPlane)) return false;
            if (!IsBoxInsidePlane(b, farPlane)) return false;

            return true;
        }

        public bool Intersects(Sphere s)
        {
            Float4 hCenter = s.Center.ToFloat4(1.0f);
            Float4 leftPlane, rightPlane, topPlane, bottomPlane, nearPlane, farPlane;
            GetPlanes(out leftPlane, out rightPlane, out topPlane, out bottomPlane, out nearPlane, out farPlane);

            if (leftPlane.Dot(hCenter) < -s.Radius) return false;
            if (rightPlane.Dot(hCenter) < -s.Radius) return false;
            if (topPlane.Dot(hCenter) < -s.Radius) return false;
            if (bottomPlane.Dot(hCenter) < -s.Radius) return false;
            if (nearPlane.Dot(hCenter) < -s.Radius) return false;
            if (farPlane.Dot(hCenter) < -s.Radius) return false;

            return true;
        }

        private static bool IsBoxOutsidePlane(AABox b, Float4 plane)
        {
            Float4 closestCorner;
            closestCorner.X = plane.X > 0 ? b.Max.X : b.Min.X;
            closestCorner.Y = plane.Y > 0 ? b.Max.Y : b.Min.Y;
            closestCorner.Z = plane.Z > 0 ? b.Max.Z : b.Min.Z;
            closestCorner.W = 1.0f;
            return plane.Dot(closestCorner) < 0;
        }

        public bool Intersects(AABox b)
        {
            Float4 leftPlane, rightPlane, topPlane, bottomPlane, nearPlane, farPlane;
            GetPlanes(out leftPlane, out rightPlane, out topPlane, out bottomPlane, out nearPlane, out farPlane);

            if (IsBoxOutsidePlane(b, leftPlane)) return false;
            if (IsBoxOutsidePlane(b, rightPlane)) return false;
            if (IsBoxOutsidePlane(b, topPlane)) return false;
            if (IsBoxOutsidePlane(b, bottomPlane)) return false;
            if (IsBoxOutsidePlane(b, nearPlane)) return false;
            if (IsBoxOutsidePlane(b, farPlane)) return false;

            return true;
        }

        public float Depth
        {
            get
            {
                return System.Math.Abs(NearPlane.W / NearPlane.XYZ.Length + FarPlane.W / FarPlane.XYZ.Length);
            }
        }

        public Float3[] GetCorners()
        {
            Float3[] corners = new Float3[8];
            GetPlaneCorners_Internal(NearPlane, corners, 0);
            GetPlaneCorners_Internal(FarPlane, corners, 4);
            return corners;
        }

        public void GetNearPlaneCorners(Float3[] destArray)
        {
            GetPlaneCorners_Internal(NearPlane, destArray, 0);
        }

        public void GetFarPlaneCorners(Float3[] destArray)
        {
            GetPlaneCorners_Internal(FarPlane, destArray, 0);
        }

        public void GetScreenCornersAt(float atDepth, Float3[] destArray, int destOffset = 0)
        {
            Float4 depthPlane = NearPlane / NearPlane.XYZ.Length;
            depthPlane.W -= atDepth;
            GetPlaneCorners_Internal(depthPlane, destArray, destOffset);
        }

        private void GetPlaneCorners_Internal(Float4 vplane, Float3[] destBuffer, int bufferOffset)
        {
            destBuffer[bufferOffset + 0] = FMath.Intersect3Planes(vplane, LeftPlane, TopPlane);
            destBuffer[bufferOffset + 1] = FMath.Intersect3Planes(vplane, RightPlane, TopPlane);
            destBuffer[bufferOffset + 2] = FMath.Intersect3Planes(vplane, RightPlane, BottomPlane);
            destBuffer[bufferOffset + 3] = FMath.Intersect3Planes(vplane, LeftPlane, BottomPlane);
        }

    }
}
