using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// An horizontal size for the UI.
    /// </summary>
    public struct UiWidth : IEquatable<UiWidth>
    {
        public float Value;
        public UiUnit Unit;

        public UiWidth(float value, UiUnit unit)
        {
            Value = value;
            Unit = unit;
        }

        /// <summary>
        /// Initialize this size in a css-like way (e.g. "100px", "50%", "-0.5ss")
        /// </summary>
        public static implicit operator UiWidth(string style)
        {
            UiWidth sizeV;
            UiValue.ParseStyle(style, out sizeV.Value, out sizeV.Unit);
            return sizeV;
        }

        /// <summary>
        /// Convert this size value to another unit and returns the new size.
        /// </summary>
        public UiWidth ConvertTo(UiUnit destUnit, CoordContext context)
        {
            UiWidth result = this;

            if (Unit < destUnit)
            {
                // Em -> Pixels
                if (Unit <= UiUnit.Em && destUnit >= UiUnit.Pixels)
                    result.Value *= context.FontSize;

                // Pixels -> Percent
                if (Unit <= UiUnit.Pixels && destUnit >= UiUnit.Percent)
                    result.Value = result.Value / context.Canvas.PixelSize.Width;

                // Percent -> ScreenSpace
                if (Unit <= UiUnit.Percent && destUnit >= UiUnit.ScreenSpace)
                    result.Value = result.Value * 2.0f;

            }
            else if (Unit > destUnit)
            {
                // ScreenSpace -> Percent
                if (Unit >= UiUnit.ScreenSpace && destUnit <= UiUnit.Percent)
                    result.Value *= 0.5f;

                // Percent -> Pixels
                if (Unit >= UiUnit.Percent && destUnit <= UiUnit.Pixels)
                    result.Value *= context.Canvas.PixelSize.Width;

                // Pixels -> Em
                if (Unit >= UiUnit.Pixels && destUnit <= UiUnit.Em)
                    result.Value /= context.FontSize;
            }

            result.Unit = destUnit;
            return result;
        }

        public UiWidth ConvertTo(UiUnit destUnit)
        {
            return ConvertTo(destUnit, CoordContext.Current);
        }

        public UiHeight ToHeight()
        {
            CoordContext context = CoordContext.Current;
            UiHeight result;
            result.Unit = UiUnit.Pixels;
            result.Value = ConvertTo(UiUnit.Pixels, context).Value;
            return result.ConvertTo(Unit);
        }

        public static UiWidth operator +(UiWidth s1, UiWidth s2)
        {
            CoordContext context = CoordContext.Current;
            UiWidth result;
            result.Unit = s1.Unit;
            result.Value = s1.Value + s2.ConvertTo(s1.Unit, context).Value;
            return result;
        }

        public static UiWidth operator -(UiWidth s1, UiWidth s2)
        {
            CoordContext context = CoordContext.Current;
            UiWidth result;
            result.Unit = s1.Unit;
            result.Value = s1.Value - s2.ConvertTo(s1.Unit, context).Value;
            return result;
        }

        public static UiWidth operator *(UiWidth s1, float k)
        {
            UiWidth result = s1;
            result.Value *= k;
            return result;
        }

        public static UiWidth operator *(float k, UiWidth s1)
        {
            return s1 * k;
        }

        public override string ToString()
        {
            return UiValue.ToStyle(Value, Unit);
        }

        public bool Equals(UiWidth other)
        {
            return Value == other.Value && Unit == other.Unit;
        }
    }
}
