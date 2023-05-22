using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public struct UiSize : IEquatable<UiSize>
    {
        public UiWidth Width;
        public UiHeight Height;

        public UiSize(UiWidth width, UiHeight height)
        {
            Width = width;
            Height = height;
        }

        public UiSize(Float2 xy, UiUnit unit)
        {
            Width = new UiWidth(xy.X, unit);
            Height = new UiHeight(xy.Y, unit);
        }

        public UiSize(float x, float y, UiUnit unit)
        {
            Width = new UiWidth(x, unit);
            Height = new UiHeight(y, unit);
        }

        public Float2 XY
        {
            get
            {
                return new Float2(Width.Value, Height.Value);
            }
            set
            {
                Width.Value = value.X;
                Height.Value = value.Y;
            }
        }

        /// <summary>
        /// Initialize this size in a css-like way (e.g. "100px", "50%", "-0.5ss")
        /// </summary>
        public static implicit operator UiSize(string style)
        {
            UiSize size;
            string[] styles = style.Split(' ');
            if (styles.Length == 2)
            {
                size.Width = styles[0];
                size.Height = styles[1];
            }
            else
            {
                size.Width = style;
                size.Height = style;
            }

            return size;
        }

        /// <summary>
        /// Convert this size value to another unit and returns the new size.
        /// </summary>
        public UiSize ConvertTo(UiUnit destUnit, CoordContext coords)
        {
            UiSize result = this;
            result.Width = result.Width.ConvertTo(destUnit, coords);
            result.Height = result.Height.ConvertTo(destUnit, coords);
            return result;
        }

        public UiSize ConvertTo(UiUnit destUnit)
        {
            CoordContext context = CoordContext.Current;
            UiSize result = this;
            result.Width = result.Width.ConvertTo(destUnit, context);
            result.Height = result.Height.ConvertTo(destUnit, context);
            return result;
        }

        public static UiSize operator +(UiSize s1, UiSize s2)
        {
            UiSize result;
            result.Width = s1.Width + s2.Width;
            result.Height = s1.Height + s2.Height;
            return result;
        }

        public static UiSize operator -(UiSize s1, UiSize s2)
        {
            UiSize result;
            result.Width = s1.Width + s2.Width;
            result.Height = s1.Height + s2.Height;
            return result;
        }

        public static UiSize operator *(UiSize s1, float k)
        {
            UiSize result = s1;
            result.Width *= k;
            result.Height *= k;
            return result;
        }

        public static UiSize operator *(float k, UiSize s1)
        {
            return s1 * k;
        }

        public static UiSize operator +(UiSize s1, UiWidth s2)
        {
            UiSize result = s1;
            result.Width += s2;
            return result;
        }

        public static UiSize operator +(UiWidth s1, UiSize s2)
        {
            return s2 + s1;
        }

        public static UiSize operator -(UiSize s1, UiWidth s2)
        {
            UiSize result = s1;
            result.Width -= s2;
            return result;
        }

        public static UiSize operator -(UiWidth s1, UiSize s2)
        {
            UiSize result = s2;
            result.Width = s1 - result.Width;
            return result;
        }

        public static UiSize operator +(UiSize s1, UiHeight s2)
        {
            UiSize result = s1;
            result.Height += s2;
            return result;
        }

        public static UiSize operator +(UiHeight s1, UiSize s2)
        {
            return s2 + s1;
        }

        public static UiSize operator -(UiSize s1, UiHeight s2)
        {
            UiSize result = s1;
            result.Height -= s2;
            return result;
        }

        public static UiSize operator -(UiHeight s1, UiSize s2)
        {
            UiSize result = s2;
            result.Height = s1 - result.Height;
            return result;
        }

        public override string ToString()
        {
            return Width.ToString() + " " + Height.ToString();
        }

        public bool Equals(UiSize other)
        {
            return Width.Equals(other.Width) && Height.Equals(other.Height);
        }
    }
}
