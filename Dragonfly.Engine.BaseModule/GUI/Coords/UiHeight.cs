using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A vertical size for the UI.
    /// </summary>
    public struct UiHeight : IEquatable<UiHeight>
    {
        public float Value;
        public UiUnit Unit;

        public UiHeight(float value, UiUnit unit)
        {
            Value = value;
            Unit = unit;
        }

        /// <summary>
        /// Initialize this size in a css-like way (e.g. "100px", "50%", "-0.5ss")
        /// </summary>
        public static implicit operator UiHeight(string style)
        {
            UiHeight sizeV;
            UiValue.ParseStyle(style, out sizeV.Value, out sizeV.Unit);
            return sizeV;
        }

        /// <summary>
        /// Initialize this size from an integer pixel size
        /// </summary>
        public static implicit operator UiHeight(int pixelSize)
        {
            return new UiHeight(pixelSize, UiUnit.Pixels);
        }

        public UiWidth ToWidth()
        {
            CoordContext context = CoordContext.Current;
            UiWidth result; 
            result.Unit = UiUnit.Pixels;
            result.Value = ConvertTo(UiUnit.Pixels, context).Value;
            return result.ConvertTo(Unit, context);
        }

        /// <summary>
        /// Convert this size value to another unit and returns the new size.
        /// </summary>
        public UiHeight ConvertTo(UiUnit destUnit, CoordContext coords)
        {
            UiHeight result = this;

            if (Unit < destUnit)
            {
                // Em -> Pixels
                if (Unit <= UiUnit.Em && destUnit >= UiUnit.Pixels)
                    result.Value *= coords.FontSize;

                // Pixels -> Percent
                if (Unit <= UiUnit.Pixels && destUnit >= UiUnit.Percent)
                    result.Value = result.Value / coords.Canvas.PixelSize.Height;

                // Percent -> ScreenSpace
                if (Unit <= UiUnit.Percent && destUnit >= UiUnit.ScreenSpace)
                    result.Value = result.Value * 2.0f;

            }
            else if (Unit > destUnit)
            {
                // ScreenSpace -> Percent
                if (Unit >= UiUnit.ScreenSpace && destUnit <= UiUnit.Percent)
                    result.Value = result.Value * 0.5f;

                // Percent -> Pixels
                if (Unit >= UiUnit.Percent && destUnit <= UiUnit.Pixels)
                    result.Value *= coords.Canvas.PixelSize.Height;

                // Pixels -> Em
                if (Unit >= UiUnit.Pixels && destUnit <= UiUnit.Em)
                    result.Value /= coords.FontSize;
            }

            result.Unit = destUnit;
            return result;
        }

        public UiHeight ConvertTo(UiUnit destUnit)
        {
            return ConvertTo(destUnit, CoordContext.Current);
        }

        public static UiHeight operator +(UiHeight s1, UiHeight s2)
        {
            CoordContext context = CoordContext.Current;
            UiHeight result;
            result.Unit = s1.Unit;
            result.Value = s1.Value + s2.ConvertTo(s1.Unit, context).Value;
            return result;
        }

        public static UiHeight operator -(UiHeight s1, UiHeight s2)
        {
            CoordContext context = CoordContext.Current;
            UiHeight result;
            result.Unit = s1.Unit;
            result.Value = s1.Value - s2.ConvertTo(s1.Unit, context).Value;
            return result;
        }

        public static UiHeight operator *(UiHeight s1, float k)
        {
            UiHeight result = s1;
            result.Value *= k;
            return result;
        }

        public static UiHeight operator *(float k, UiHeight s1)
        {
            return s1 * k;
        }

        public override string ToString()
        {
            return UiValue.ToStyle(Value, Unit);
        }

        public bool Equals(UiHeight other)
        {
            return Value == other.Value && Unit == other.Unit;
        }
    }
}
