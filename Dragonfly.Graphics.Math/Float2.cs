using System;

namespace Dragonfly.Graphics.Math
{
    public struct Float2 : IComparable
    {
		public static readonly Float2 One = new Float2(1.0f, 1.0f);
		public static readonly Float2 Zero = new Float2(0, 0);
        public static readonly Float2 UnitX = new Float2(1.0f, 0);
        public static readonly Float2 UnitY = new Float2(0, 1.0f);

        public static Float2 FromAngle(float radians)
        {
            return new Float2(FMath.Cos(radians), FMath.Sin(radians));
        }
		
        public float X, Y;
		
		public Float2(float x, float y)
		{
			this.X = x;
			this.Y = y;
		}

        public float[] ToArray()
        {
            return new float[] { X, Y };
        }

        public Float3 ToFloat3(float z)
        {
            return new Float3(X, Y, z);
        }

        public void CopyTo(float[] destArray, int startIndex)
        {
            destArray[startIndex++] = X;
            destArray[startIndex] = Y;
        }
		
		public float Dot(Float2 vector)
		{
			return Float2.Dot(this, vector);
		}

        public float Cross(Float2 vector)
        {
            return Float2.Cross(this, vector);
        }

        public Float2 Clamp(float min, float max)
        {
            return new Float2(X.Clamp(min, max), Y.Clamp(min, max));
        }

        public Float2 Saturate()
        {
            return new Float2(X.Saturate(), Y.Saturate());
        }

        /// <summary>
        /// Returns true if all components are in the specified interval.
        /// </summary>
        /// <returns></returns>
        public bool IsBetween(float includedMin, float includedMax)
        {
            return X.IsBetween(includedMin, includedMax) && Y.IsBetween(includedMin, includedMax);
        }

        public float Length
        {
            get
            {
                return (float)System.Math.Sqrt(Float2.Dot(this, this));
            }
        }

        public float LengthSquared
        {
            get
            {
                return Dot(this, this);
            }
        }


        public Float2 Normal()
        {
            return Float2.Normal(this);
        }

        public Float2 Trunc()
        {
            return new Float2(
                (float)System.Math.Truncate(X),
                (float)System.Math.Truncate(Y));
        }

        public Float2 Round()
        {
            return new Float2(
                (float)System.Math.Round(X),
                (float)System.Math.Round(Y));
        }

        public Float2 Frac()
        {
            return new Float2(X.Frac(), Y.Frac());
        }

        public Float2 Floor()
        {
            return new Float2(X.Floor(), Y.Floor());
        }

        public Float2 Min(Float2 other)
        {
            return Float2.Min(this, other);
        }

        public Float2 Max(Float2 other)
        {
            return Float2.Max(this, other);
        }

        public Float2 Lerp(Float2 other, float amount)
        {
            return other * amount + this * (1.0f - amount);
        }

        /// <summary>
        /// Returns the value of the larger component.
        /// </summary>
        public float CMax()
        {
            return X > Y ? X : Y;
        }

        /// <summary>
        /// Returns the value of the smaller component.
        /// </summary>
        public float CMin()
        {
            return X < Y ? X : Y;
        }

        public Float2 Abs()
        {
            return new Float2(System.Math.Abs(X), System.Math.Abs(Y));
        }

        public Float2 Sign()
        {
            return new Float2(FMath.Sign(X), FMath.Sign(Y));
        }

        public Float2 Rotate90()
        {
            return new Float2(-Y, X);
        }

        public Float2 ProjectTo(Float2 vector)
        {
            return Dot(vector) * vector;
        }

        #region Operators

        public static explicit operator Float2(float value)
        {
            return new Float2(value, value);
        }

        //Float2 x Float2

        public static Float2 operator +(Float2 v1, Float2 v2)
		{
			return new Float2(v1.X + v2.X, v1.Y + v2.Y);
		}
		
		public static Float2 operator -(Float2 v1, Float2 v2)
		{
			return new Float2(v1.X - v2.X, v1.Y - v2.Y);
		}
		
		public static Float2 operator -(Float2 v)
		{
			return Float2.Zero - v;
		}
		
		public static Float2 operator *(Float2 v1, Float2 v2)
		{
			return new Float2(v1.X * v2.X, v1.Y * v2.Y);
		}
		
		public static Float2 operator /(Float2 v1, Float2 v2)
		{
			return new Float2(v1.X / v2.X, v1.Y / v2.Y);
		}

        //Float2 x float
        public static Float2 operator +(Float2 v1, float k)
        {
            return new Float2(v1.X + k, v1.Y + k);
        }

        public static Float2 operator +(float k, Float2 v1)
        {
            return new Float2(v1.X + k, v1.Y + k);
        }

        public static Float2 operator -(Float2 v1, float k)
        {
            return new Float2(v1.X - k, v1.Y - k);
        }

        public static Float2 operator -(float k, Float2 v1)
        {
            return new Float2(k - v1.X, k - v1.Y);
        }

        public static Float2 operator *(Float2 v1, float k)
		{
			return new Float2(v1.X * k, v1.Y * k);
		}
		
		public static Float2 operator *(float k, Float2 v1)
		{
			return v1 * k;
		}
		
		public static Float2 operator /(Float2 v1, float k)
		{
			return new Float2(v1.X / k, v1.Y / k);
		}

        public static Float2 operator /(float k, Float2 v1)
        {
            return new Float2(k / v1.X, k / v1.Y);
        }

        public static Float2 operator %(Float2 v1, float k)
        {
            return new Float2(v1.X % k, v1.Y % k);
        }

        //Geometric products

        public static float Dot(Float2 v1, Float2 v2)
		{
			return v1.X * v2.X + v1.Y * v2.Y;
		}

        public static Float2 Normal(Float2 v)
        {
            float len = v.Length;
            if (len < float.Epsilon)
                return (Float2)FMath.RSQRT_2;
            return v / len;
        }

        #endregion

        #region Static functions

        public static Float2 Orthocenter(Float2 v1, Float2 v2, Float2 v3)
        {
            Float2 orthocenter = Float2.Zero;

            // calc two of the altitude slopes
            float s1 = (v1.X - v2.X) / (v2.Y - v1.Y);
            float s2 = (v1.X - v3.X) / (v3.Y - v1.Y);

            // solve intersection
            if (float.IsInfinity(s1) && float.IsInfinity(s2) || (s1 - s2).IsAlmostZero() || (s1.IsAlmostZero() && s2.IsAlmostZero()))
            {
                orthocenter = v1; // right or degenerate triangle
            }
            else if (float.IsInfinity(s1))
            {
                orthocenter.X = v3.X;
                orthocenter.Y = s2 * (orthocenter.X - v2.X) + v2.Y;
            }
            else
            {
                if (float.IsInfinity(s2)) orthocenter.X = v2.X;
                else orthocenter.X = (s1 * v3.X - s2 * v2.X + v2.Y - v3.Y) / (s1 - s2);
                orthocenter.Y = s1 * (orthocenter.X - v3.X) + v3.Y;
            }

            return orthocenter;
        }

        /// <summary>
        /// Return the winding order of the triangle v1, v2, v3.
        /// 0 = Collinear, 1 = Clockwise, -1 = Counterclockwise
        /// </summary>
        public static int Orientation(Float2 v1, Float2 v2, Float2 v3)
        {
            return System.Math.Sign((v2.Y - v1.Y) * (v3.X - v2.X) - (v2.X - v1.X) * (v3.Y - v2.Y));
        }

        /// <summary>
        /// Check if the segment (a1, a2) intersects (b1, b2)
        /// </summary>
        public static bool SegmentIntersects(Float2 a1, Float2 a2, Float2 b1, Float2 b2)
        {
            return Orientation(a1, a2, b1) != Orientation(a1, a2, b2) && Orientation(b1, b2, a1) != Orientation(b1, b2, a2);
        }

        /// <summary>
        /// Cross product in homogeneous coordinates.
        /// </summary>
        public static float Cross(Float2 v1, Float2 v2)
        {
            return v1.X * v2.Y - v1.Y * v2.X;           
        }

        /// <summary>
        /// Returns the component-wise minimum of the two vectors.
        /// </summary>
        public static Float2 Min(Float2 v1, Float2 v2)
        {
            return new Float2(System.Math.Min(v1.X, v2.X), System.Math.Min(v1.Y, v2.Y));
        }

        /// <summary>
        /// Returns the component-wise maximum of the two vectors.
        /// </summary>
        public static Float2 Max(Float2 v1, Float2 v2)
        {
            return new Float2(System.Math.Max(v1.X, v2.X), System.Math.Max(v1.Y, v2.Y));
        }

        public static Float2 Lerp(Float2 v1, Float2 v2, float alpha)
        {
            return v1 * (1.0f - alpha) + v2 * alpha;
        }

        #endregion

        public override string ToString()
        {
            return String.Format("[{0}, {1}]", X, Y);
        }

        public int CompareTo(object obj)
        {
            //here only for compatibility with constraints on generics
            return 0;
        }

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is Float2 other)
                return other == this;
            else return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 352654597;
                hash = ((hash << 5) + hash + (hash >> 27)) ^ X.GetHashCode();
                hash = ((hash << 5) + hash + (hash >> 27)) ^ Y.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Float2 v1, Float2 v2)
        {
            return v1.X == v2.X && v1.Y == v2.Y;
        }

        public static bool operator !=(Float2 v1, Float2 v2)
        {
            return !(v1 == v2);
        }

        #endregion
    }
}
