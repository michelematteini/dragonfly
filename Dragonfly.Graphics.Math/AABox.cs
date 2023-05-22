using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Dragonfly.Graphics.Math
{
    public struct AABox : IVolume
    {
        public Float3 Min, Max;

        public static AABox Bounding(params Float3[] containedPoints)
        {
            AABox bb = new AABox();
            bb.Min = bb.Max = containedPoints[0];
            for (int i = 1; i < containedPoints.Length; i++)
            {
                Float3 p = containedPoints[i];
                if (p.X < bb.Min.X) bb.Min.X = p.X;
                if (p.Y < bb.Min.Y) bb.Min.Y = p.Y;
                if (p.Z < bb.Min.Z) bb.Min.Z = p.Z;
                if (p.X > bb.Max.X) bb.Max.X = p.X;
                if (p.Y > bb.Max.Y) bb.Max.Y = p.Y;
                if (p.Z > bb.Max.Z) bb.Max.Z = p.Z;
            }
            return bb;
        }

        public static AABox Bounding<T>(IList<T> containedPoints, Func<T, Float3> getPosition)
        {
            AABox bb = new AABox();
            bb.Min = bb.Max = getPosition(containedPoints[0]);
            for (int i = 1; i < containedPoints.Count; i++)
            {
                Float3 p = getPosition(containedPoints[i]);
                if (p.X < bb.Min.X) bb.Min.X = p.X;
                if (p.Y < bb.Min.Y) bb.Min.Y = p.Y;
                if (p.Z < bb.Min.Z) bb.Min.Z = p.Z;
                if (p.X > bb.Max.X) bb.Max.X = p.X;
                if (p.Y > bb.Max.Y) bb.Max.Y = p.Y;
                if (p.Z > bb.Max.Z) bb.Max.Z = p.Z;
            }
            return bb;
        }

        public AABox(Float3 min, Float3 max)
        {
            Min = min;
            Max = max;
        }

        public static readonly AABox Infinite = new AABox((Float3)float.MinValue, (Float3)float.MaxValue);

        public static readonly AABox Empty = new AABox((Float3)float.MaxValue, (Float3)float.MinValue);

        public AABox Add(Float3 p)
        {
            AABox newBox = this;
            if (p.X < Min.X) newBox.Min.X = p.X;
            if (p.Y < Min.Y) newBox.Min.Y = p.Y;
            if (p.Z < Min.Z) newBox.Min.Z = p.Z;
            if (p.X > Max.X) newBox.Max.X = p.X;
            if (p.Y > Max.Y) newBox.Max.Y = p.Y;
            if (p.Z > Max.Z) newBox.Max.Z = p.Z;
            return newBox;
        }

        public AABox Add(AABox b)
        {
            return Add(b.Min).Add(b.Max);
        }

        public Float3 Center
        {
            get { return (Min + Max) * 0.5f; }
        }

        public void GetCorners(out Float3 c0, out Float3 c1, out Float3 c2, out Float3 c3, out Float3 c4, out Float3 c5, out Float3 c6, out Float3 c7)
        {
            c0 = new Float3(Min.X, Min.Y, Min.Z);
            c1 = new Float3(Min.X, Min.Y, Max.Z);
            c2 = new Float3(Min.X, Max.Y, Min.Z);
            c3 = new Float3(Min.X, Max.Y, Max.Z);
            c4 = new Float3(Max.X, Min.Y, Min.Z);
            c5 = new Float3(Max.X, Min.Y, Max.Z);
            c6 = new Float3(Max.X, Max.Y, Min.Z);
            c7 = new Float3(Max.X, Max.Y, Max.Z);
        }

        public float DistanceFrom(Float3 point)
        {
            Float3 distanceVec = Float3.Max(Min - point, point - Max);
            return distanceVec.Max(Float3.Zero).Length;
        }

        public bool Contains(Float3 point)
        {
            return (point - Min).CMin() >= 0 && (Max - point).CMin() >= 0;
        }

        public bool Contains(Sphere s)
        {
            return (s.Center - Min).Min(Max - s.Center).CMin() >= s.Radius;
        }

        public bool Contains(AABox b)
        {
            return Contains(b.Min) && Contains(b.Max);
        }

        public bool Intersects(AABox b)
        {
            if (Min.X > b.Max.X || Max.X < b.Min.X) return false;
            if (Min.Z > b.Max.Z || Max.Z < b.Min.Z) return false;
            if (Min.Y > b.Max.Y || Max.Y < b.Min.Y) return false;
            return true;
        }

        public bool Intersects(Sphere s)
        {
            return (Min - s.Center).CMax() <= s.Radius && (s.Center - Max).CMax() <= s.Radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MinMaxSum(float a, float b, ref float minAcc, ref float maxAcc)
        {
            if (a < b)
            {
                minAcc += a;
                maxAcc += b;
            }
            else
            {
                minAcc += b;
                maxAcc += a;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AABox operator * (AABox box, Float4x4 transform)
        {
            // Unoptimized version:

            //Float3 c0, c1, c2, c3, c4, c5, c6, c7;
            //box.GetCorners(out c0, out c1, out c2, out c3, out c4, out c5, out c6, out c7);

            //AABox transfBox = AABox.Empty;
            //transfBox = transfBox.Add(c0 * transform);
            //transfBox = transfBox.Add(c1 * transform);
            //transfBox = transfBox.Add(c2 * transform);
            //transfBox = transfBox.Add(c3 * transform);
            //transfBox = transfBox.Add(c4 * transform);
            //transfBox = transfBox.Add(c5 * transform);
            //transfBox = transfBox.Add(c6 * transform);
            //transfBox = transfBox.Add(c7 * transform);

            float wmin = 1.0f / (box.Min.X * transform.A14 + box.Min.Y * transform.A24 + box.Min.Z * transform.A34 + transform.A44);
            float wmax = 1.0f / (box.Max.X * transform.A14 + box.Max.Y * transform.A24 + box.Max.Z * transform.A34 + transform.A44);


            AABox transfBox = new AABox();
            MinMaxSum(box.Min.X * transform.A11 * wmin, box.Max.X * transform.A11 * wmax, ref transfBox.Min.X, ref transfBox.Max.X);
            MinMaxSum(box.Min.Y * transform.A21 * wmin, box.Max.Y * transform.A21 * wmax, ref transfBox.Min.X, ref transfBox.Max.X);
            MinMaxSum(box.Min.Z * transform.A31 * wmin, box.Max.Z * transform.A31 * wmax, ref transfBox.Min.X, ref transfBox.Max.X);
            MinMaxSum(transform.A41 * wmin, transform.A41 * wmax, ref transfBox.Min.X, ref transfBox.Max.X);

            MinMaxSum(box.Min.X * transform.A12 * wmin, box.Max.X * transform.A12 * wmax, ref transfBox.Min.Y, ref transfBox.Max.Y);
            MinMaxSum(box.Min.Y * transform.A22 * wmin, box.Max.Y * transform.A22 * wmax, ref transfBox.Min.Y, ref transfBox.Max.Y);
            MinMaxSum(box.Min.Z * transform.A32 * wmin, box.Max.Z * transform.A32 * wmax, ref transfBox.Min.Y, ref transfBox.Max.Y);
            MinMaxSum(transform.A42 * wmin, transform.A42 * wmax, ref transfBox.Min.Y, ref transfBox.Max.Y);

            MinMaxSum(box.Min.X * transform.A13 * wmin, box.Max.X * transform.A13 * wmax, ref transfBox.Min.Z, ref transfBox.Max.Z);
            MinMaxSum(box.Min.Y * transform.A23 * wmin, box.Max.Y * transform.A23 * wmax, ref transfBox.Min.Z, ref transfBox.Max.Z);
            MinMaxSum(box.Min.Z * transform.A33 * wmin, box.Max.Z * transform.A33 * wmax, ref transfBox.Min.Z, ref transfBox.Max.Z);
            MinMaxSum(transform.A43 * wmin, transform.A43 * wmax, ref transfBox.Min.Z, ref transfBox.Max.Z);

            return transfBox;
        }

        /// <summary>
        /// Returns a bounding box scaled by the specified scalar, around its center.
        /// </summary>
        public static AABox operator *(AABox box, float scale)
        {
            // extend the box around its center
            Float3 c = box.Center;
            return new AABox(scale * (box.Min - c) + c, scale * (box.Max - c) + c);
        }

        public override string ToString()
        {
            return String.Format("{0}=>{1}", Min, Max);
        }

    }
}
