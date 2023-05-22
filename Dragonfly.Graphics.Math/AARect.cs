using System;
using System.Xml.Serialization;

namespace Dragonfly.Graphics.Math
{
    /// <summary>
    /// A rectangle in 2d space, aligned to the axes.
    /// </summary>
    public struct AARect : IEquatable<AARect>
    {
        public float X1, Y1, X2, Y2;

        public static AARect Bounding(Float2 p1, Float2 p2)
        {
            AARect bRect;
            bRect.X1 = p1.X;
            bRect.X2 = p2.X;
            bRect.Y1 = p1.Y;
            bRect.Y2 = p2.Y;
            return bRect;
        }

        public static AARect Bounding(Float2 p1, Float2 p2, Float2 p3)
        {
            AARect bRect;
            bRect.X1 = p1.X < p2.X ? (p1.X < p3.X ? p1.X : p3.X) : (p2.X < p3.X ? p2.X : p3.X);
            bRect.X2 = p1.X > p2.X ? (p1.X > p3.X ? p1.X : p3.X) : (p2.X > p3.X ? p2.X : p3.X);
            bRect.Y1 = p1.Y < p2.Y ? (p1.Y < p3.Y ? p1.Y : p3.Y) : (p2.Y < p3.Y ? p2.Y : p3.Y);
            bRect.Y2 = p1.Y > p2.Y ? (p1.Y > p3.Y ? p1.Y : p3.Y) : (p2.Y > p3.Y ? p2.Y : p3.Y);
            return bRect;
        }

        public AARect(float x1, float y1, float x2, float y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public AARect(Float2 center, float width, float height)
        {
            X1 = center.X - width;
            Y1 = center.Y - height;
            X2 = center.X + width;
            Y2 = center.Y + height;
        }

        /// <summary>
        /// Returns the rectangle that bounds both this rectacngle and the specified point.
        /// </summary>
        public AARect Add(Float2 position)
        {
            return Bounding(new Float2(X1, Y1), new Float2(X2, Y2), position);
        }

        [XmlIgnore]
        public Float2 Min
        {
            get { return new Float2(System.Math.Min(X1, X2), System.Math.Min(Y1, Y2)); }
        }

        [XmlIgnore]
        public Float2 Max
        {
            get { return new Float2(System.Math.Max(X1, X2), System.Math.Max(Y1, Y2)); }
        }

        [XmlIgnore]
        public float Width
        {
            get { return System.Math.Abs(X1 - X2); }
            set
            {
                float avgX = (X1 + X2) * 0.5f;
                X1 = avgX + value * 0.5f * System.Math.Sign(X1 - avgX);
                X2 = avgX + value * 0.5f * System.Math.Sign(X2 - avgX);
            }
        }

        [XmlIgnore]
        public float Height
        {
            get { return System.Math.Abs(Y1 - Y2); }
            set
            {
                float avgY = (Y1 + Y2) * 0.5f;
                Y1 = avgY + value * 0.5f * System.Math.Sign(Y1 - avgY);
                Y2 = avgY + value * 0.5f * System.Math.Sign(Y2 - avgY);
            }
        }

        [XmlIgnore]
        public Float2 Size
        {
            get { return new Float2(System.Math.Abs(X1 - X2), System.Math.Abs(Y1 - Y2)); }
        }

        [XmlIgnore]
        public Float2 Center
        {
            get { return new Float2(X1 + X2, Y1 + Y2) * 0.5f; }
        }

        public static AARect operator *(AARect r, float k)
        {
            return new AARect(r.X1 * k, r.Y1 * k, r.X2 * k, r.Y2 * k);
        }

        public static AARect operator *(float k, AARect r)
        {
            return r * k;
        }

        public static AARect operator *(AARect r, Float2 k)
        {
            return new AARect(r.X1 * k.X, r.Y1 * k.Y, r.X2 * k.X, r.Y2 * k.Y);
        }

        public static AARect operator *(Float2 k, AARect r)
        {
            return r * k;
        }

        public static AARect operator *(AARect r, Float4x4 mat)
        {
            return Bounding(r.Min * mat, r.Max * mat);
        }

        public static AARect operator *(Float4x4 mat, AARect r)
        {
            return Bounding(mat * r.Min, mat * r.Max);
        }

        public static bool operator ==(AARect r1, AARect r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(AARect r1, AARect r2)
        {
            return !(r1 == r2);
        }

        public bool Contains(Float2 point)
        {
            Float2 min = Min, max = Max;
            if (point.X < min.X || point.X > max.X)
                return false;

            if (point.Y < min.Y || point.Y > max.Y)
                return false;

            return true;
        }

        public override string ToString()
        {
            return Min.ToString() + " -> " + Max.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is AARect rect && Equals(rect);
        }

        public bool Equals(AARect other)
        {
            return Min == other.Min && Max == other.Max;
        }

        public override int GetHashCode()
        {
            int hashCode = 268039418;
            hashCode = hashCode * -1521134295 + X1.GetHashCode();
            hashCode = hashCode * -1521134295 + Y1.GetHashCode();
            hashCode = hashCode * -1521134295 + X2.GetHashCode();
            hashCode = hashCode * -1521134295 + Y2.GetHashCode();
            return hashCode;
        }
    }
}
