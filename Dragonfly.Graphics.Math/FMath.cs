using System;
using System.Runtime.CompilerServices;

namespace Dragonfly.Graphics.Math
{
    public static class FMath
    {
        public static readonly float PI = (float)System.Math.PI;
        public static readonly float PI_OVER_2 = (float)(System.Math.PI / 2.0);
        public static readonly float PI_3_OVER_2 = (float)(3.0 * System.Math.PI / 2.0);
        public static readonly float PI_OVER_4 = (float)(System.Math.PI / 4.0);
        public static readonly float TWO_PI = (float)(System.Math.PI * 2.0);
        public static readonly float SQRT_2 = (float)(System.Math.Sqrt(2.0));
        public static readonly float SQRT_3 = (float)(System.Math.Sqrt(3.0));
        public static readonly float RSQRT_2 = 1 / SQRT_2;
        public static readonly float RSQRT_3 = 1 / SQRT_3;
        public static readonly float PHI = 1.61803398874989484820f;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cos(float radians)
        {
            float absRad = System.Math.Abs(radians);
            if (radians == 0 || absRad == TWO_PI) return 1;
            if (absRad == PI) return -1;
            if (absRad == PI_OVER_2 || absRad == PI_3_OVER_2) return 0;
            return (float)System.Math.Cos(radians);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin(float radians)
        {
            float absRad = System.Math.Abs(radians);
            float rSign = System.Math.Sign(radians);
            if (radians == 0 || absRad == PI || absRad == TWO_PI) return 0;
            if (absRad == PI_OVER_2) return rSign;
            if (absRad == PI_3_OVER_2) return -rSign;
            return (float)System.Math.Sin(radians);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Tan(float radians)
        {
            return (float)System.Math.Tan(radians);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sqrt(float value)
        {
            return (float)System.Math.Sqrt(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Pow(float value, float power)
        {
            return (float)System.Math.Pow(value, power);
        }

        /// <summary>
        /// Returns the fractional part of the specified value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Frac(float value)
        {
            return value - (int)value;
        }

        /// <summary>
        /// Returns the fractional part of the specified value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Round(float value)
        {
            return (float)System.Math.Round(value);
        }


        /// <summary>
        /// Returns the largest integer smaller or equal to the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Floor(float value)
        {
            float floor = (int)value;
            if (floor < 0 && floor != value)
                floor -= 1.0f;
            return floor;
        }

        /// <summary>
        /// Returns the smaller integer larger or equal to the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Ceil(float value)
        {
            float ceil = (int)value;
            if (ceil > 0 && ceil != value)
                ceil += 1.0f;
            return ceil;
        }
        /// <summary>
        /// Returns the biggest integer smaller than the absolute value of the one specified.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Saturate(float value)
        {
            return value < 0 ? 0 : (value > 1f ? 1f : value);
        }

        /// <summary>
        /// Convert the specified value in degree to radians
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(float value)
        {
            return value * PI / 180.0f;
        }

        /// <summary>
        /// COnvert the specified value from radians to degrees
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegree(float value)
        {
            return value * 180.0f / PI;
        }

        /// <summary>
        /// If the value lies outside the specified margins, it's assigned to the closest one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        /// <summary>
        /// Returns -1 for negative values, 1 for positive ones, or 0 otherwise.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sign(this float value)
        {
            if (value < 0) return -1.0f;
            if (value > 0) return 1.0f;
            return 0;
        }

        /// <summary>
        /// Interpolates between two values by the given ammount
        /// </summary>
        /// <param name="amount">A number between 0 and 1 that defines the interpolation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float v1, float v2, float amount)
        {
            float alpha = amount.Saturate();
            return v1 * (1.0f - amount) + v2 * amount;
        }

        /// <summary>
        /// Calculates an intepolation value using the smoothstep cubic function.
        /// Returns 0 if x is less than min; 1 if x is greater than max; otherwise, a value between 0 and 1 if x is in the range [min, max].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Smoothstep(float min, float max, float x)
        {
            float alpha = ((x - min) / (max - min)).Saturate();
            return alpha * alpha * (3.0f - 2.0f * alpha);
        }

        /// <summary>
        /// Interpolates between v1 and v2 of the specified amount using a gamma function.
        /// The greater is gamma the lower the curve is (similar to a power > 1). 
        /// With gamma == 0 the interpolation is linear.
        /// With gamme < 0 the higher the curve is, similar to a radix (or a power < 1).
        /// </summary>
        /// <param name="gamma"></param>
        /// <returns></returns>
        public static float GammaInterp(float v1, float v2, float gamma, float amount)
        {
            float x = amount.Saturate();

            if (gamma != 0)
            {
                float k = 1 / Abs(gamma);

                if (gamma > 0)
                    x = k * x / (1.0f + k - x);
                else
                    x = x * (1.0f + k) / (x + k);
            }
            
            return Lerp(v1, v2, x);
        }

        /// <summary>
        /// Equivalent of gamma() function used in shader: y = k * x / (1.0f + k - x)
        /// </summary>
        public static float Gamma(float x, float k)
        {
            return k * x / (1.0f + k - x);
        }

        /// <summary>
        /// Equivalent of gammaInv() function used in shader: y = x * (1.0f + k) / (x + k)
        /// </summary>
        public static float GammaInv(float x, float k)
        {
            return x * (1.0f + k) / (x + k);
        }

        /// <summary>
        /// Interpolates between v1 and v2 of the specified amount using an exp function.
        /// The greater is the exponent the lower the curve is. 
        /// </summary>
        public static float ExpInterp(float v1, float v2, float exponent, float amount)
        {
            float x = amount.Saturate();
            x = (float)((System.Math.Exp(exponent * x) - 1) / (System.Math.Exp(exponent) - 1));
            return Lerp(v1, v2, x);
        }


        /// <summary>
        /// Returns the modulus of the value for the specified divisor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Mod(float value, float divisor)
        {
            return value - divisor * Floor(value / divisor);
        }

        /// <summary>
        /// Returns the absolute value of the one speficied
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Abs(float value)
        {
            return System.Math.Abs(value);
        }

        /// <summary>
        /// Returns 1 if the first value is smaller or equal to the second, 0 otherwise.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Step(float a, float b)
        {
            return a <= b ? 1.0f : 0.0f;
        }

        /// <summary>
        /// Returns the base 2 log of the given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Log2(float value)
        {
            return (float)System.Math.Log(value, 2);
        }

        /// <summary>
        /// Returns the base 2 exponential of the given value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Exp2(int exp)
        {
            float result = 1;
            float mul = exp < 0 ? 0.5f : 2.0f;
            exp = System.Math.Abs(exp);

            for (; exp > 0; exp--)
                result *= mul;

            return result;
        }

        #region Geometric Helpers

        /// <summary>
        /// Create a plucker matrix for a line specified by two points.
        /// </summary>
        public static Float4x4 CreatePluckerMatrix(Float3 a, Float3 b)
        {
            float l01 = a[0] * b[1] - b[0] * a[1];
            float l02 = a[0] * b[2] - b[0] * a[2];
            float l03 = a[0] - b[0];
            float l12 = a[1] * b[2] - b[1] * a[2];
            float l13 = a[1] - b[1];
            float l23 = a[2] - b[2];

            return new Float4x4(0, -l01, -l02, -l03,
                                l01, 0, -l12, -l13,
                                l02, l12, 0, -l23,
                                l03, l13, l23, 0);
        }

        /// <summary>
        /// Intersect 3 planes in homogeneous coordinates and returns the intersection point.
        /// </summary>
        public static Float3 Intersect3Planes(Float4 p1, Float4 p2, Float4 p3)
        {
            return -(p1.W * p2.XYZ.Cross(p3.XYZ) + p2.W * p3.XYZ.Cross(p1.XYZ) + p3.W * p1.XYZ.Cross(p2.XYZ)) / p1.XYZ.Dot(p2.XYZ.Cross(p3.XYZ));
        }

        public static Float3 PlaneRayIntersection(Float4 plane, Float3 rayStart, Float3 rayDirection)
        {
            Float4x4 pluckerLine = FMath.CreatePluckerMatrix(rayStart, rayStart + rayDirection * rayStart.CMax());
            Float4 hIntersection = pluckerLine * plane;
            return hIntersection.ToFloat3();
        }

        /// <summary>
        /// Calculates the intersection points between a ray and a sphere or returns false if no intersection occurs.
        /// The intersection points are always ordered by which is encountered first while moving in rayDir direction. 
        /// If rayStart is inside the sphere, intersection1 is a point that is actually behind the rayStart location.
        /// </summary>
        public static bool RaySphereIntersection(Float3 rayStart, Float3 rayDir, Float3 sphereCenter, float sphereRadius, out Float3 intersection1, out Float3 intersection2)
        {
            intersection1 = (Float3)0;
            intersection2 = (Float3)0;
            float halfIntDist = Float3.Dot(sphereCenter - rayStart, rayDir);
            Float3 halfIntVec = halfIntDist * rayDir;
            Float3 halfIntPoint = halfIntVec + rayStart;
            Float3 halfIntFromCenter = (halfIntPoint - sphereCenter) / sphereRadius;
            float halfIntDistFromCenter2 = Float3.Dot(halfIntFromCenter, halfIntFromCenter);

            if (halfIntDistFromCenter2 > 1.0f)
                return false; // no intersection

            float halfChordLen = Sqrt(1.0f - halfIntDistFromCenter2);
            Float3 halfChord = halfChordLen * rayDir * sphereRadius;
            intersection1 = halfIntPoint - halfChord;
            intersection2 = halfIntPoint + halfChord;

            // check that the ray is intersecting in the specified direction and not before the starting point
            float intersection2Dist = Float3.Dot(intersection2 - rayStart, rayDir);
            return intersection2Dist >= 0;
        }

        #endregion

    }
}
