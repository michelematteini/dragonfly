using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public class FontParams
    {
        private string fontFace;
        private UiHeight size;
        private Float3 color;

        public event Action FontChanged;

        public string FontFace
        {
            get { return fontFace; }
            set
            {
                fontFace = value;
                if (FontChanged != null)
                    FontChanged();
            }
        }

        public UiHeight Size
        {
            get { return size; }
            set
            {
                size = value;
                if (FontChanged != null)
                    FontChanged();
            }
        }

        public Float3 Color
        {
            get { return color; }
            set
            {
                color = value;
                if (FontChanged != null)
                    FontChanged();
            }
        }

        public static FontParams GetDefaultLight(BaseModUiSettings ui)
        {
            return new FontParams() { Color = ui.DefaultTextColorLight, FontFace = ui.DefaultFontFace, Size = ui.DefaultTextSize };
        }

        public static FontParams GetDefaultDark(BaseModUiSettings ui)
        {
            return new FontParams() { Color = ui.DefaultTextColorDark, FontFace = ui.DefaultFontFace, Size = ui.DefaultTextSize };
        }
    }
}
