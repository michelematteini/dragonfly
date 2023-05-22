using System;
using System.Xml.Serialization;

namespace Dragonfly.Graphics.Math
{
    public struct Int3
    {
        public static readonly Int3 Zero = new Int3(0, 0, 0);

        public int X, Y, Z;
        
        public Int3(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public static Int3 operator +(Int3 v1, Int3 v2)
        {
            return new Int3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Int3 operator -(Int3 v1, Int3 v2)
        {
            return new Int3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        public static Int3 operator -(Int3 v)
        {
            return new Int3(-v.X, -v.Y, -v.Z);
        }

        public static implicit operator Float3(Int3 v)
        {
            return new Float3(v.X, v.Y, v.Z);
        }

        public static explicit operator Int3(Float3 v)
        {
            return new Int3((int)v.X, (int)v.Y, (int)v.Z);
        }

        public static Float3 operator *(Int3 v, float mul)
        {
            return (Float3)v * mul;
        }

        public static Float3 operator *(float mul, Int3 v)
        {
            return (Float3)v * mul;
        }

        public static Int3 operator *(int mul, Int3 v)
        {
            return new Int3(v.X * mul, v.Y * mul, v.Z * mul);
        }

        /// <summary>
        /// Returns the component-wise maximum value.
        /// </summary>
        public int CMax()
        {
            return System.Math.Max(System.Math.Max(X, Y), Z);
        }

        /// <summary>
        /// Returns the component-wise minimum value.
        /// </summary>
        public int CMin()
        {
            return System.Math.Min(System.Math.Min(X, Y), Z);
        }

        [XmlIgnore]
        public float Length
        {
            get
            {
                return (float)System.Math.Sqrt(X * X + Y * Y + Z * Z);
            }
        }

        public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}]", X, Y, Z);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 352654597;
                hash = ((hash << 5) + hash + (hash >> 27)) ^ X;
                hash = ((hash << 5) + hash + (hash >> 27)) ^ Y;
                hash = ((hash << 5) + hash + (hash >> 27)) ^ Z;
                return hash;
            }
        }

        public static bool operator ==(Int3 iv1, Int3 iv2)
        {
            return iv1.X == iv2.X && iv1.Y == iv2.Y && iv1.Z == iv2.Z;
        }

        public static bool operator !=(Int3 iv1, Int3 iv2)
        {
            return !(iv1 == iv2);
        }

        public override bool Equals(object obj)
        {
            if (obj is Int3 iv)
                return this == iv;
            return false;
        }

    }
}
