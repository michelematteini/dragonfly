using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public struct UiCoords : IEquatable<UiCoords>
    {
        public static readonly UiCoords Zero = new UiCoords(0, 0);

        public float X, Y;
        public UiUnit XUnit, YUnit;

        public UiCoords(Float2 xy, UiUnit unit)
        {
            X = xy.X;
            Y = xy.Y;
            XUnit = YUnit = unit;
        }

        public UiCoords(int xPixels, int yPixels)
        {
            X = xPixels;
            Y = yPixels;
            XUnit = YUnit = UiUnit.Pixels;
        }

        /// <summary>
        /// Initialize this coordinates in a css-like way (e.g. "100px", "50%", "-0.5ss")
        /// </summary>
        public static implicit operator UiCoords(string style)
        {
            UiCoords coords;
            string[] styles = style.Split(' ');
            if (styles.Length == 2)
            {
                UiValue.ParseStyle(styles[0], out coords.X, out coords.XUnit);
                UiValue.ParseStyle(styles[1], out coords.Y, out coords.YUnit);
            }
            else
            {
                UiValue.ParseStyle(style, out coords.X, out coords.XUnit);
                UiValue.ParseStyle(style, out coords.Y, out coords.YUnit);
            }
            return coords;
        }

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

        /// <summary>
        /// Convert this size value to another unit and returns the new size.
        /// </summary>
        public UiCoords ConvertTo(UiUnit destUnit, CoordContext context)
        {
            UiCoords result = this;
            result.X = ConvertCoord(result.X, result.XUnit, destUnit, context, false);
            result.Y = ConvertCoord(result.Y, result.YUnit, destUnit, context, true);
            result.XUnit = result.YUnit = destUnit;
            return result;
        }

        public UiCoords ConvertTo(UiUnit destUnit)
        {
            return ConvertTo(destUnit, CoordContext.Current);
        }

        private float ConvertCoord(float value, UiUnit srcUnit, UiUnit destUnit, CoordContext coords, bool isVert)
        {
            float result = value;

            if (srcUnit < destUnit)
            {
                // Em -> Pixels
                if (srcUnit <= UiUnit.Em && destUnit >= UiUnit.Pixels)
                    result *= coords.FontSize;

                // Pixels -> Percent
                if (srcUnit <= UiUnit.Pixels && destUnit >= UiUnit.Percent)
                    result = result / (isVert ? coords.Canvas.PixelSize.Height : coords.Canvas.PixelSize.Width);

                // Percent -> ScreenSpace
                if (srcUnit <= UiUnit.Percent && destUnit >= UiUnit.ScreenSpace)
                {
                    result = result * 2.0f - 1.0f;
                    if (isVert) result = -result;
                }

            }
            else if (srcUnit > destUnit)
            {
                // ScreenSpace -> Percent
                if (srcUnit >= UiUnit.ScreenSpace && destUnit <= UiUnit.Percent)
                {
                    if (isVert) result = -result;
                    result += (result + 1.0f) * 0.5f;
                }

                // Percent -> Pixels
                if (srcUnit >= UiUnit.Percent && destUnit <= UiUnit.Pixels)
                    result *= (isVert ? coords.Canvas.PixelSize.Height : coords.Canvas.PixelSize.Width);

                // Pixels -> Em
                if (srcUnit >= UiUnit.Pixels && destUnit <= UiUnit.Em)
                    result /= coords.FontSize;
            }


            return result;
        }

        public static UiCoords operator +(UiCoords e1, UiWidth e2)
        {
            UiCoords result = e1;
            result.X += e2.ConvertTo(result.XUnit).Value;
            return result;
        }

        public static UiCoords operator +(UiCoords e1, UiHeight e2)
        {
            CoordContext context = CoordContext.Current;
            UiCoords result = e1;
            result.Y += e2.ConvertTo(result.YUnit, context).Value * (result.YUnit == UiUnit.ScreenSpace ? -1 : 1);
            return result;
        }

        public static UiCoords operator -(UiCoords e1, UiWidth e2)
        {
            CoordContext context = CoordContext.Current;
            UiCoords result = e1;
            result.X -= e2.ConvertTo(result.XUnit, context).Value;
            return result;
        }

        public static UiCoords operator -(UiCoords e1, UiHeight e2)
        {
            CoordContext context = CoordContext.Current;
            UiCoords result = e1;
            result.Y -= e2.ConvertTo(result.YUnit, context).Value * (result.YUnit == UiUnit.ScreenSpace ? -1 : 1);
            return result;
        }

        public static UiCoords operator +(UiCoords e1, UiSize e2)
        {
            CoordContext context = CoordContext.Current;
            UiCoords result = e1;
            result.X += e2.Width.ConvertTo(result.XUnit, context).Value;
            result.Y += e2.Height.ConvertTo(result.YUnit, context).Value * (result.YUnit == UiUnit.ScreenSpace ? -1 : 1);
            return result;
        }

        public static UiCoords operator -(UiCoords e1, UiSize e2)
        {
            CoordContext context = CoordContext.Current;
            UiCoords result = e1;
            result.X -= e2.Width.ConvertTo(result.XUnit, context).Value;
            result.Y -= e2.Height.ConvertTo(result.YUnit, context).Value * (result.YUnit == UiUnit.ScreenSpace ? -1 : 1);
            return result;
        }

        public static UiSize operator -(UiCoords e1, UiCoords e2)
        {
            CoordContext context = CoordContext.Current;
            UiCoords pixE1 = e1.ConvertTo(UiUnit.Pixels, context);
            UiCoords pixE2 = e2.ConvertTo(UiUnit.Pixels, context);
            UiSize result = new UiSize(pixE1.XY - pixE2.XY, UiUnit.Pixels);
            result.Width = result.Width.ConvertTo(e1.XUnit, context);
            result.Height = result.Height.ConvertTo(e1.YUnit, context);
            return result;
        }

        public override string ToString()
        {
            return UiValue.ToStyle(X, XUnit) + " " + UiValue.ToStyle(Y, YUnit);
        }

        public bool Equals(UiCoords other)
        {
            return X == other.X && Y == other.Y && XUnit == other.XUnit && YUnit == other.YUnit;
        }
    }
}
