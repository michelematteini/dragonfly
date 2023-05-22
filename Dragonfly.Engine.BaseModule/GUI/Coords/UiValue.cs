using Dragonfly.Utils;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Helper class that manage UI values composed of a value and a unit.
    /// </summary>
    public static class UiValue
    {
        /// <summary>
        /// Parse an UI value written in a css-like way (e.g. "100px", "50%", "-0.5ss")
        /// </summary>
        public static void ParseStyle(string style, out float value, out UiUnit unit)
        {
            style = style.ToLower();

            if (style.EndsWith("%"))
            {
                value = style.Substring(0, style.Length - 1).ParseInvariantFloat() * 0.01f;
                unit = UiUnit.Percent;
            }
            else if (style.EndsWith("ss"))
            {
                value = style.Substring(0, style.Length - 2).ParseInvariantFloat();
                unit = UiUnit.ScreenSpace;
            }
            else if (style.EndsWith("px"))
            {
                value = style.Substring(0, style.Length - 2).ParseInvariantFloat();
                unit = UiUnit.Pixels;
            }
            else if (style.EndsWith("em"))
            {
                value = style.Substring(0, style.Length - 2).ParseInvariantFloat();
                unit = UiUnit.Em;
            }
            else // if no unit is specified, "Pixels" is assumed
            {
                int pixelValue = 0;
                int.TryParse(style, out pixelValue);
                value = pixelValue;
                unit = UiUnit.Pixels;
            }
        }

        public static string ToStyle(float value, UiUnit unit)
        {
            if (unit == UiUnit.Percent)
                value *= 100.0f;
            return System.Math.Round(value, 2).ToString() + unit.ToUnitCode();
        }

    }
}
