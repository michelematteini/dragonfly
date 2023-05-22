using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    public class Path3D
    {
        public List<Float3> Points { get; private set; }

        public Path3D()
        {
            Points = new List<Float3>();
            SmoothingRadius = 0;
        }

        public Path3D(params Float3[] points)
        {
            Points = new List<Float3>(points);
            SmoothingRadius = 0;
            SmoothingMode = InterpolationType.Cubic;
        }

        public float SmoothingRadius { get; set; }

        /// <summary>
        /// Apply a smoothing radius and return this path for inline functional usage.
        /// </summary>
        public Path3D MakeSmooth(float radius)
        {
            SmoothingRadius = radius;
            return this;
        }

        public InterpolationType SmoothingMode { get; set; }

        public float TotalDistance
        {
            get
            {
                float length = 0;
                for (int i = 1; i < Points.Count; i++)
                {
                    length += (Points[i] - Points[i - 1]).Length;
                }
                return length;
            }
        }

        public Float3 GetDirectionAt(float pos)
        {
            float posAtPrevPoint = 0, r = SmoothingRadius;
            Float3 prevDir = Float3.Zero;

            if (pos <= 0)
                return (Points[1] - Points[0]).Normal();

            for (int i = 1; i < Points.Count; i++)
            {
                Float3 curVec = Points[i] - Points[i - 1];
                Float3 curDir = curVec.Normal();
                float lastLineLen = curVec.Length;

                if (pos > posAtPrevPoint + lastLineLen)
                {
                    posAtPrevPoint += lastLineLen;
                    prevDir = curDir;
                    continue;
                }

                float distFromPrevPoint = pos - posAtPrevPoint;

                if (lastLineLen - distFromPrevPoint < r && i < Points.Count - 1)
                {
                    // === smooth interpolation start
                    float halfDistFromR = (distFromPrevPoint - (lastLineLen - r)) * 0.5f;
                    Float3 nextDir = (Points[i + 1] - Points[i]).Normal();

                    float kr = halfDistFromR / r;
                    return (curDir.Lerp(nextDir, kr.ToInterpolator(SmoothingMode))).Normal();
                }
                else if (distFromPrevPoint < r && i > 1)
                {
                    // === smooth interpolation end
                    float kr = (distFromPrevPoint / r + 1.0f) * 0.5f;
                    return (prevDir.Lerp(curDir, kr.ToInterpolator(SmoothingMode))).Normal();
                }
                else
                    return curDir;               
            }

            return (Points[Points.Count - 1] - Points[Points.Count - 2]).Normal();
        }

        public float GetAccelerationBetween(float pos1, float pos2)
        {
            return (GetDirectionAt(pos2) - GetDirectionAt(pos1)).Length * 0.5f;
        }

        public Float3 GetPositionAt(float pos)
        {
            float posAtPrevPoint = 0, prevLineLen = 0, r = SmoothingRadius;

            if (pos <= 0)
                return Points[0];

            for (int i = 1; i < Points.Count; i++)
            {
                float lastLineLen = (Points[i] - Points[i - 1]).Length;

                if (pos > posAtPrevPoint + lastLineLen)
                {
                    posAtPrevPoint += lastLineLen;
                    prevLineLen = lastLineLen;
                    continue;
                }

                float distFromPrevPoint = pos - posAtPrevPoint;
                
                if (lastLineLen - distFromPrevPoint < r && i < Points.Count - 1)
                {
                    // === smooth interpolation start

                    float halfDistFromR = (distFromPrevPoint - (lastLineLen - r)) * 0.5f;
                    float k1 = (halfDistFromR + (lastLineLen - r)) / lastLineLen;
                    Float3 intPos1 = Points[i - 1].Lerp(Points[i], k1);

                    float nextLineLen = (Points[i + 1] - Points[i]).Length;
                    float k2 = halfDistFromR / nextLineLen;
                    Float3 intPos2 = Points[i].Lerp(Points[i + 1], k2);

                    float kr = halfDistFromR / r;
                    return intPos1.Lerp(intPos2, kr);
                }
                else if (distFromPrevPoint < r && i > 1)
                {
                    // === smooth interpolation end

                    float k1 = (distFromPrevPoint + r) * 0.5f / lastLineLen;
                    Float3 intPos1 = Points[i - 1].Lerp(Points[i], k1);

                    float k0 = (prevLineLen - (r - distFromPrevPoint) * 0.5f) / prevLineLen;
                    Float3 intPos0 = Points[i - 2].Lerp(Points[i - 1], k0);

                    float kr = (distFromPrevPoint / r + 1.0f) * 0.5f;
                    return intPos0.Lerp(intPos1, kr);
                }
                else
                {
                    // === linear interpolation
                    float perc1 = distFromPrevPoint / lastLineLen;
                    return Points[i - 1].Lerp(Points[i], perc1);
                }
            }

            return Points[Points.Count - 1];
        }

    }
}
