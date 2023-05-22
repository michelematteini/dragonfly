using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Dragonfly.Graphics.Math
{
    public struct Float3 : IComparable, IEquatable<Float3>
    {
		public static readonly Float3 One = new Float3(1.0f, 1.0f, 1.0f);
		public static readonly Float3 Zero = new Float3(0, 0, 0);
        public static readonly Float3 UnitX = new Float3(1.0f, 0, 0);
        public static readonly Float3 UnitY = new Float3(0, 1.0f, 0);
        public static readonly Float3 UnitZ = new Float3(0, 0, 1.0f);


        public static Float3 FromRGB(int r, int g, int b)
        {
            return new Float3(r.ToSatFloat(), g.ToSatFloat(), b.ToSatFloat());
        }

        /// <summary>
        /// Returns the component-wise minimum of the two vectors.
        /// </summary>
        public static Float3 Min(Float3 v1, Float3 v2)
        {
            return new Float3(System.Math.Min(v1.X, v2.X), System.Math.Min(v1.Y, v2.Y), System.Math.Min(v1.Z, v2.Z));
        }

        /// <summary>
        /// Returns the component-wise maximum of the two vectors.
        /// </summary>
        public static Float3 Max(Float3 v1, Float3 v2)
        {
            return new Float3(System.Math.Max(v1.X, v2.X), System.Math.Max(v1.Y, v2.Y), System.Math.Max(v1.Z, v2.Z));
        }

        /// <summary>
        /// Returns the component-wise power of a vector to the specified exponent.
        /// </summary>
        public static Float3 Pow(Float3 v, float exp)
        {
            return new Float3(FMath.Pow(v.X, exp), FMath.Pow(v.Y, exp), FMath.Pow(v.Z, exp));
        }

        public static Float3 Step(Float3 v1, Float3 v2)
        {
            return new Float3(FMath.Step(v1.X, v2.X), FMath.Step(v1.Y, v2.Y), FMath.Step(v1.Z, v2.Z));
        }

        /// <summary>
        /// Choose an appropiate axis that is not near-parallel to the  specified direction.
        /// </summary>
        public static Float3 NotParallelAxis(Float3 notParallelTo, Float3 defaultAxis)
        {
            if (!notParallelTo.IsAlmostParallelTo(defaultAxis))
                return defaultAxis;

            if (!notParallelTo.IsAlmostParallelTo(UnitY))
                return UnitY;

            return UnitZ;
        }

        public static Float3 NotParallelAxis(Float3 notParallelTo1, Float3 notParallelTo2, Float3 defaultAxis)
        {
            if (!notParallelTo1.IsAlmostParallelTo(defaultAxis) && !notParallelTo2.IsAlmostParallelTo(defaultAxis))
                return defaultAxis;

            if (!notParallelTo1.IsAlmostParallelTo(UnitY) && !notParallelTo2.IsAlmostParallelTo(UnitY))
                return UnitY;

            if (!notParallelTo1.IsAlmostParallelTo(UnitZ) && !notParallelTo2.IsAlmostParallelTo(UnitZ))
                return UnitZ;

            return UnitX;
        }

        public bool IsAlmostParallelTo(Float3 other)
        {
            return (System.Math.Abs(Dot(this, other)) - 1.0f).IsAlmostZero();
        }


        public float X, Y, Z;

        public float this[int index]
        {
            get
            {
                return index == 0 ? X : (index == 1 ? Y : Z);
            }
            set
            {
                if (index == 0) X = value;
                else if (index == 1) Y = value;
                else Z = value;
            }
        }

        [XmlIgnore]
        public float R
        {
            get
            {
                return X;
            }
            set
            {
                X = value;
            }
        }

        [XmlIgnore]
        public float G
        {
            get
            {
                return Y;
            }
            set
            {
                Y = value;
            }
        }

        [XmlIgnore]
        public float B
        {
            get
            {
                return Z;
            }
            set
            {
                Z = value;
            }
        }

        [XmlIgnore]
        public int IntR
        {
            get
            {
                return X.ToByteInt();
            }
            set
            {
                X = value.ToSatFloat();
            }
        }

        [XmlIgnore]
        public int IntG
        {
            get
            {
                return Y.ToByteInt();
            }
            set
            {
                Y = value.ToSatFloat();
            }
        }

        [XmlIgnore]
        public int IntB
        {
            get
            {
                return Z.ToByteInt();
            }
            set
            {
                Z = value.ToSatFloat();
            }
        }

        #region Swizzles

        [XmlIgnore]
        public Float2 XY
        {
            get
            {
                return new Float2(X, Y);
            }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        [XmlIgnore]
        public Float2 XZ
        {
            get
            {
                return new Float2(X, Z);
            }
            set
            {
                X = value.X;
                Z = value.Y;
            }
        }

        [XmlIgnore]
        public Float2 YZ
        {
            get
            {
                return new Float2(Y, Z);
            }
            set
            {
                Y = value.X;
                Z = value.Y;
            }
        }

        [XmlIgnore]
        public Float3 XZY
        {
            get
            {
                return new Float3(X, Z, Y);
            }
        }

        #endregion

        public Float3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Float3(Float2 xy, float z)
        {
            this.X = xy.X;
            this.Y = xy.Y;
            this.Z = z;
        }

        /// <summary>
        /// Build a Float3 from an hex color code (e.g. "#FFAA3C")
        /// </summary>
        /// <param name="hex"></param>
        public Float3(string hex) : this(
                int.Parse(hex.Substring(1, 2), NumberStyles.HexNumber).ToSatFloat(),
                int.Parse(hex.Substring(3, 2), NumberStyles.HexNumber).ToSatFloat(),
                int.Parse(hex.Substring(5, 2), NumberStyles.HexNumber).ToSatFloat())
        { }

        public float[] ToArray()
        {
            return new float[] { X, Y, Z };
        }

        public void CopyTo(float[] destArray, int startIndex)
        {
            destArray[startIndex++] = X;
            destArray[startIndex++] = Y;
            destArray[startIndex] = Z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Dot(Float3 vector)
		{
			return Float3.Dot(this, vector);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float3 Cross(Float3 vector)
		{
			return Float3.Cross(this, vector);
		}

        public Float3 Lerp(Float3 other, float amount)
        {
            return other * amount + this * (1.0f - amount);
        }

        public Float3 SmoothStep(Float3 other, float amount)
        {
            amount = (3 - 2 * amount) * amount * amount;
            return other * amount + this * (1.0f - amount);
        }

        public Float3 Clamp(float min, float max)
        {
            return new Float3(X.Clamp(min, max), Y.Clamp(min, max), Z.Clamp(min, max));
        }

        public Float3 Saturate()
        {
            return new Float3(X.Saturate(), Y.Saturate(), Z.Saturate());
        }

        /// <summary>
        /// Returns a Float3 where each component is the minimum of the same component between this and the specified vector.
        /// </summary>
        public Float3 Min(Float3 other)
        {
            return Float3.Min(this, other);
        }

        public Float3 Max(Float3 other)
        {
            return Float3.Max(this, other);
        }

        /// <summary>
        /// Returns the component-wise maximum value of this vector.
        /// </summary>
        public float CMax()
        {
            return System.Math.Max(System.Math.Max(X, Y), Z);
        }

        /// <summary>
        /// Returns the component-wise minimum value.
        /// </summary>
        public float CMin()
        {
            return System.Math.Min(System.Math.Min(X, Y), Z);
        }

        /// <summary>
        /// True if any components are non-zero, false otherwise.
        /// </summary>
        public bool Any()
        {
            return X != 0.0f || Y != 0.0f || Z != 0.0f;
        }

        public Float3 Trunc()
        {
            return new Float3(
                (float)System.Math.Truncate(X),
                (float)System.Math.Truncate(Y),
                (float)System.Math.Truncate(Z));
        }

        public Float3 Frac()
        {
            return new Float3(X.Frac(), Y.Frac(), Z.Frac());
        }

        /// <summary>
        /// Compute and returns the modulus operator on each component of this vector.
        /// </summary>
        public Float3 Mod(float divisor)
        {
            return new Float3(FMath.Mod(X, divisor), FMath.Mod(Y, divisor), FMath.Mod(Z, divisor));
        }

        /// <summary>
        /// Compute and returns the floor operator on each component of this vector.
        /// </summary>
        public Float3 Floor()
        {
            return new Float3(FMath.Floor(X), FMath.Floor(Y), FMath.Floor(Z));
        }

        /// <summary>
        /// Return the component-wise absolute values of this vector
        /// </summary>
        public Float3 Abs()
        {
            return new Float3(System.Math.Abs(X), System.Math.Abs(Y), System.Math.Abs(Z));
        }

        /// <summary>
        /// Project this vector to the given direction.
        /// </summary>
        /// <param name="direction">Direction of the projection, must be a unit vector.</param>
        public Float3 ProjectTo(Float3 direction)
        {
            return Dot(direction) * direction;
        }

        /// <summary>
        /// Project this vector on a line specified by the two given points.
        /// </summary>
        public Float3 ProjectTo(Float3 linePnt1, Float3 linePnt2)
        {
            Float3 lineVec = linePnt2 - linePnt1;
            return Dot(lineVec, this - linePnt1) * lineVec / lineVec.LengthSquared + linePnt1;
        }

        /// <summary>
        /// Returns true if this point is between the specified points (or equals them).
        /// <para/> If this point doesn't lie on a line beween the two points, the result refers to the projection of this point to that line.
        /// </summary>
        public bool IsBetween(Float3 includedStartPnt, Float3 includedEndPnt)
        {
            return Dot(this - includedStartPnt, this - includedEndPnt) <= 0;
        }

        /// <summary>
        /// Returns a value that is the linear interpolation factor between the specified two points that generates this point.
        /// <para/> If this point doesn't lie on a line beween the two points, the interpolation value refers to the projection of this point to that line.
        /// </summary>
        public float InterpolatesAt(Float3 v1, Float3 v2)
        {
            Float3 lineVec = v2 - v1;
            return (this - v1).Dot(lineVec) / lineVec.LengthSquared;
        }
		
		public float Length
		{
			get
			{
				return (float)System.Math.Sqrt(Float3.Dot(this, this));
			}
		}

        public float LengthSquared
        {
            get
            {
                return Float3.Dot(this, this);
            }
        }

        public Float4 ToFloat4(float w)
        {
            return new Float4(this, w);
        }
		
		public Float3 Normal()
		{
			return Float3.Normal(this);
		}

        #region Operators

        public static explicit operator Float3(float value)
        {
            return new Float3(value, value, value);
        }

        //Float3 x Float3

        public static Float3 operator +(Float3 v1, Float3 v2)
		{
			return new Float3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
		}
		
		public static Float3 operator -(Float3 v1, Float3 v2)
		{
			return new Float3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
		}
		
		public static Float3 operator -(Float3 v)
		{
			return Float3.Zero - v;
		}
		
		public static Float3 operator *(Float3 v1, Float3 v2)
		{
			return new Float3(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
		}
		
		public static Float3 operator /(Float3 v1, Float3 v2)
		{
			return new Float3(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z);
		}

        //Float3 x float

        public static Float3 operator +(Float3 v1, float k)
        {
            return new Float3(v1.X + k, v1.Y + k, v1.Z + k);
        }

        public static Float3 operator +(float k, Float3 v1)
        {
            return new Float3(v1.X + k, v1.Y + k, v1.Z + k);
        }

        public static Float3 operator -(Float3 v1, float k)
        {
            return new Float3(v1.X - k, v1.Y - k, v1.Z - k);
        }

        public static Float3 operator -(float k, Float3 v1)
        {
            return new Float3(k - v1.X, k - v1.Y, k - v1.Z);
        }

        public static Float3 operator *(Float3 v1, float k)
		{
			return new Float3(v1.X * k, v1.Y * k, v1.Z * k);
		}
		
		public static Float3 operator *(float k, Float3 v1)
		{
			return v1 * k;
		}
		
		public static Float3 operator /(Float3 v1, float k)
		{
            float d = 1.0f / k;
			return new Float3(v1.X * d, v1.Y * d, v1.Z * d);
		}

        public static Float3 operator /(float k, Float3 v1)
        {
            return new Float3(k / v1.X, k / v1.Y, k / v1.Z);
        }

        public static Float3 operator %(Float3 v1, float k)
        {
            return new Float3(k % v1.X, k % v1.Y, k % v1.Z);
        }

        //Geometric products

        public static float Dot(Float3 v1, Float3 v2)
		{
			return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
		}
		
		public static Float3 Cross(Float3 v1, Float3 v2)
		{
			return new Float3(
				v1.Y * v2.Z - v1.Z * v2.Y,
				v1.Z * v2.X - v1.X * v2.Z,
				v1.X * v2.Y - v1.Y * v2.X
			);
		}
		
		public static Float3 Normal(Float3 v)
		{
            float len = v.Length;
            if (len < float.Epsilon)
                return (Float3)FMath.RSQRT_3;
            return v / len;
        }

		#endregion

        public Byte4 ToByte4()
        {
            return new Byte4(255, R.ToByteInt(), G.ToByteInt(), B.ToByteInt());
        }
		
		public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}]", X, Y, Z);
        }

        public string ToString(string format)
        {
            return String.Format("[{0}, {1}, {2}]", X.ToString(format), Y.ToString(format), Z.ToString(format));
        }

        public string ToHexColor()
        {
            return "#" + R.ToByteInt().ToString("X2") + G.ToByteInt().ToString("X2") + B.ToByteInt().ToString("X2");
        }

        public int CompareTo(object obj)
        {
            //here only for compatibility with constraints on generics
            return 0;
        }

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is Float3 other)
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
                hash = ((hash << 5) + hash + (hash >> 27)) ^ Z.GetHashCode();
                return hash;
            }
        }

        public bool Equals(Float3 other)
        {
            return this == other;
        }

        public static bool operator ==(Float3 v1, Float3 v2)
        {
            return v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z;
        }

        public static bool operator !=(Float3 v1, Float3 v2)
        {
            return !(v1 == v2);
        }

        #endregion
    }
}
