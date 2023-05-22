
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Dragonfly.Graphics.Math
{
    public struct Sphere : IVolume
    {
        public Float3 Center;
        public float Radius;

        public static Sphere Bounding(IList<Float3> containedPoints)
        {
            Sphere boundingSphere = new Sphere();

            if (containedPoints.Count > 0)
            {
                // Ritter's bounding sphere:
                int distantPntID = 0, basePntID = 0;
          
                // search a pair of distant points
                for (int it = 0; it < 2; it++)
                {
                    basePntID = distantPntID;

                    // search the most distant point from the currently picked one
                    float maxDistSQ = 0;
                    for (int pi = 0; pi < containedPoints.Count; pi++)
                    {
                        float distSQ = (containedPoints[basePntID] - containedPoints[pi]).LengthSquared;
                        if (distSQ > maxDistSQ)
                        {
                            maxDistSQ = distSQ;
                            distantPntID = pi; // save its index
                        }
                    }
                }

                // use a point in-between as center
                boundingSphere.Center = 0.5f * (containedPoints[basePntID] + containedPoints[distantPntID]);

                // calc the minimum sphere radius
                for (int i = 0; i < containedPoints.Count; i++)
                    boundingSphere.Radius = System.Math.Max(boundingSphere.Radius, (containedPoints[i] - boundingSphere.Center).LengthSquared);
                boundingSphere.Radius = FMath.Sqrt(boundingSphere.Radius);
            }

            return boundingSphere;
        }

        public Sphere(Float3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public bool Intersects(AABox b)
        {
            float sqDist = 0;
            for (int i = 0; i < 3; i++)
            {
                // for each axis count any excess distance outside box extents
                float v = Center[i];
                if (v < b.Min[i]) sqDist += (b.Min[i] - v) * (b.Min[i] - v);
                if (v > b.Max[i]) sqDist += (v - b.Max[i]) * (v - b.Max[i]);
            }

            return sqDist <= Radius * Radius;
        }

        public bool Contains(Float3 point)
        {
            return (point - Center).LengthSquared <= (Radius * Radius);
        }

        public bool Contains(Sphere s)
        {
            if (Radius < s.Radius)
                return false; // a smaller sphere cannot contain a larger one...


            float centerDistSq = (s.Center - Center).LengthSquared;
            float radiusDiff = Radius - s.Radius;
            return centerDistSq <= radiusDiff * radiusDiff;
            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(AABox b)
        {
            Float3 fartherCornerVec = (b.Min - Center).Abs().Max((b.Max - Center).Abs());
            return fartherCornerVec.LengthSquared <= (Radius * Radius);
        }

        public bool Intersects(Sphere s)
        {
            float radiusSum = s.Radius + Radius;
            return (radiusSum * radiusSum) >= (Center - s.Center).LengthSquared;
        }
    }
}
