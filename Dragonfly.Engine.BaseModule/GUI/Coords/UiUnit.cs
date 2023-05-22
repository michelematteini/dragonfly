
namespace Dragonfly.BaseModule
{
    public enum UiUnit
    {
        Em = -1,
        Pixels = 0,
        Percent = 1,
        ScreenSpace = 2,
    }

    public static class UiUnitEx
    {
        public static string ToUnitCode(this UiUnit unit)
        {
            switch (unit)
            {
                default:
                case UiUnit.Pixels:
                    return "px";
                case UiUnit.Em:
                    return "em";
                case UiUnit.Percent:
                    return "%";
                case UiUnit.ScreenSpace:
                    return "ss";
            }

        }
    }

}
