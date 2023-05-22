using System;
using System.Xml.Serialization;

namespace Dragonfly.Graphics.Math
{
    public struct Int2 : IEquatable<Int2>
    {
        public static Int2 Zero = new Int2(0, 0);
        public static Int2 One = new Int2(1, 1);

        public int X, Y;

        [XmlIgnore]
        public int Width
        {
            get { return X; }
            set { X = value; }
        }

        [XmlIgnore]
        public int Height
        {
            get { return Y; }
            set { Y = value; }
        }

        public Int2(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public Int2(int xy)
        {
            this.X = xy;
            this.Y = xy;
        }

        public static Int2 operator +(Int2 v1, Int2 v2)
        {
            return new Int2(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Int2 operator -(Int2 v1, Int2 v2)
        {
            return new Int2(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Int2 operator +(Int2 v1, int c)
        {
            return new Int2(v1.X + c, v1.Y + c);
        }

        public static Int2 operator -(Int2 v1, int c)
        {
            return new Int2(v1.X - c, v1.Y - c);
        }

        public static Int2 operator *(Int2 v, int mul)
        {
            return new Int2(v.X * mul, v.Y * mul);
        }

        public static Int2 operator *(int mul, Int2 v)
        {
            return v * mul;
        }

        public static Int2 operator *(Int2 v1, Int2 v2)
        {
            return new Int2(v1.X * v2.X, v1.Y * v2.Y);
        }

        public static Int2 operator /(Int2 v, int div)
        {
            return new Int2(v.X / div, v.Y / div);
        }

        public static Int2 operator -(Int2 v)
        {
            return new Int2(-v.X, -v.Y);
        }

        public static implicit operator Float2(Int2 v)
        {
            return new Float2(v.X, v.Y);
        }

        public static explicit operator Int2(int v)
        {
            return new Int2(v, v);
        }

        public static explicit operator Int2(Float2 v)
        {
            return new Int2((int)v.X, (int)v.Y);
        }

        public float Length
        {
            get
            {
                return (float)System.Math.Sqrt(X * X + Y * Y);
            }
        }

        public override string ToString()
        {
            return String.Format("[{0}, {1}]", X, Y);
        }


        public static bool operator ==(Int2 v1, Int2 v2)
        {
            return v1.Equals(v2);
        }

        public static bool operator !=(Int2 v1, Int2 v2)
        {
            return !v1.Equals(v2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return Equals((Int2)obj);
        }

        public bool Equals(Int2 other)
        {
            return X == other.X && Y == other.Y;
        }

        public override int GetHashCode()
        {
            return new Tuple<int, int>(X, Y).GetHashCode();
        }


    }
}
