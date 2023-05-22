using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Dragonfly.Graphics.Math
{
    public struct Float4 : IComparable
    {
		public static readonly Float4 One = new Float4(1.0f, 1.0f, 1.0f, 1.0f);
		public static readonly Float4 Zero = new Float4(0, 0, 0, 0);
		public static readonly Float4 UnitX = new Float4(1.0f, 0, 0, 0);
        public static readonly Float4 UnitY = new Float4(0, 1.0f, 0, 0);
        public static readonly Float4 UnitZ = new Float4(0, 0, 1.0f, 0);
        public static readonly Float4 UnitW = new Float4(0, 0, 0, 1.0f);
		
        public float X, Y, Z, W;

        public float this[int index]
        {
            get
            {
                return index == 0 ? X : (index == 1 ? Y : (index == 2 ? Z : W));
            }
            set
            {
                if (index == 0) X = value;
                else if (index == 1) Y = value;
                else if (index == 2) Z = value;
                else W = value;
            }
        }

        [XmlIgnore]
        public Float3 XYZ
        {
            get
            {
                return new Float3(X, Y, Z);
            }
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        [XmlIgnore]
        public Float3 RGB
        {
            get
            {
                return XYZ;
            }
            set
            {
                XYZ = value;
            }
        }

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
        public Float2 XW
        {
            get
            {
                return new Float2(X, W);
            }
            set
            {
                X = value.X;
                W = value.Y;
            }
        }

        [XmlIgnore]
        public Float2 ZW
        {
            get
            {
                return new Float2(Z, W);
            }
            set
            {
                Z = value.X;
                W = value.Y;
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
        public float A
        {
            get
            {
                return W;
            }
            set
            {
                W = value;
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

        [XmlIgnore]
        public int IntA
        {
            get
            {
                return W.ToByteInt();
            }
            set
            {
                W = value.ToSatFloat();
            }
        }

        public Float4(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        public Float4(Float3 xyz, float w)
        {
            this.X = xyz.X;
            this.Y = xyz.Y;
            this.Z = xyz.Z;
            this.W = w;
        }

        public Float4(Float2 xy, Float2 zw)
        {
            this.X = xy.X;
            this.Y = xy.Y;
            this.Z = zw.X;
            this.W = zw.Y;
        }

        /// <summary>
        /// Build a Float4 from an hex color code (e.g. "#FFAA3C"). W is set to 1.
        /// </summary>
        /// <param name="hex"></param>
        public Float4(string hex) : this(new Float3(hex), 1.0f) { }

        public static Float4 FromRGBA(int r, int g, int b, int a)
        {
            return new Float4(r.ToSatFloat(), g.ToSatFloat(), b.ToSatFloat(), a.ToSatFloat());
        }

        public float[] ToArray()
        {
            return new float[] { X, Y, Z, W };
        }

        public void CopyTo(float[] destArray, int startIndex)
        {
            destArray[startIndex++] = X;
            destArray[startIndex++] = Y;
            destArray[startIndex++] = Z;
            destArray[startIndex] = W;
        }

        public float Dot(Float4 vector)
		{
			return Float4.Dot(this, vector);
		}

        /// <summary>
        /// Return the homogeneous 3d vector in inhomogeneous coordinates dividing by W
        /// </summary>
        /// <returns></returns>
        public Float3 ToFloat3()
        {
            return XYZ / W;
        }

        public static Float4 Lerp(Float4 v1, Float4 v2, float ammount)
        {
            return v2 * ammount + v1 * (1.0f - ammount);
        }

        public Float4 Lerp(Float4 other, float ammount)
        {
            return Lerp(this, other, ammount);
        }

        public Float4 Clamp(float min, float max)
        {
            return new Float4(X.Clamp(min, max), Y.Clamp(min, max), Z.Clamp(min, max), W.Clamp(min, max));
        }

        public Float4 Saturate()
        {
            return new Float4(X.Saturate(), Y.Saturate(), Z.Saturate(), W.Saturate());
        }

        public Byte4 ToByte4()
        {
            return new Byte4(A.ToByteInt(), R.ToByteInt(), G.ToByteInt(), B.ToByteInt());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float4 Sign()
        {
            return new Float4(System.Math.Sign(X), System.Math.Sign(Y), System.Math.Sign(Z), System.Math.Sign(W));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Float4 Step(Float4 thr)
        {
            return new Float4(FMath.Step(X, thr.X), FMath.Step(Y, thr.Y), FMath.Step(Z, thr.Z), FMath.Step(W, thr.W));
        }

        #region Operators

        public static explicit operator Float4(float value)
        {
            return new Float4(value, value, value, value);
        }


        //Float4 x Float4

        public static Float4 operator +(Float4 v1, Float4 v2)
		{
			return new Float4(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z, v1.W + v2.W);
		}
		
		public static Float4 operator -(Float4 v1, Float4 v2)
		{
			return new Float4(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z, v1.W - v2.W);
		}
		
		public static Float4 operator -(Float4 v)
		{
			return Float4.Zero - v;
		}
		
		public static Float4 operator *(Float4 v1, Float4 v2)
		{
			return new Float4(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z, v1.W * v2.W);
		}
		
		public static Float4 operator /(Float4 v1, Float4 v2)
		{
			return new Float4(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z, v1.W / v2.W);
		}

        //Float4 x float

        public static Float4 operator +(Float4 v1, float k)
        {
            return new Float4(v1.X + k, v1.Y + k, v1.Z + k, v1.W + k);
        }

        public static Float4 operator +(float k, Float4 v1)
        {
            return new Float4(k + v1.X, k + v1.Y, k + v1.Z, k + v1.W);
        }

        public static Float4 operator -(Float4 v1, float k)
        {
            return new Float4(v1.X - k, v1.Y - k, v1.Z - k, v1.W - k);
        }

        public static Float4 operator -(float k, Float4 v1)
        {
            return new Float4(k - v1.X, k - v1.Y, k - v1.Z, k - v1.W);
        }

        public static Float4 operator *(Float4 v1, float k)
		{
			return new Float4(v1.X * k, v1.Y * k, v1.Z * k, v1.W * k);
		}
		
		public static Float4 operator *(float k, Float4 v1)
		{
			return v1 * k;
		}
		
		public static Float4 operator /(Float4 v1, float k)
		{
			return new Float4(v1.X / k, v1.Y / k, v1.Z / k, v1.W / k);
		}

        public static Float4 operator %(Float4 v1, float k)
        {
            return new Float4(v1.X % k, v1.Y % k, v1.Z % k, v1.W % k);
        }

        //Geometric products

        public static float Dot(Float4 v1, Float4 v2)
		{
			return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z + v1.W * v2.W;
		}

		#endregion

		public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}, {3}]", X, Y, Z, W);
        }

        public int CompareTo(object obj)
        {
            //here only for compatibility with constraints on generics
            return 0;
        }
    }
}
