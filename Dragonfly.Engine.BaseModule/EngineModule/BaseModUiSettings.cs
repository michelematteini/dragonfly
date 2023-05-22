using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Configuration and setting for the Ui system
    /// </summary>
    public class BaseModUiSettings
    {
        public BaseModUiSettings()
        {
            WindowBorderSize = "14em";
            WindowContentMargin = "1.5em 3.5em";
            PanelContentMargin = "1.5em 1.5em";
            WindowTitlePosition = "2.0em 1.5em";
            WindowTitleSize = "1.07143em";
            WindowCloseBtnSize = "1.8em 1.8em";
            WindowCloseBtnOffset = "-3.2em 1.1em";
            DefaultFontFace = "arial";
            DefaultTextSize = "1em";
            DefaultTextColorLight = new Float3("#e0e0e0");
            DefaultTextColorDark = new Float3("#303030");
            ButtonBorderSize = "3em";
            SliderBorderSize = "1em";
            SliderCursorSize = "2em";
            SliderCursorHMargin = "0.6em";
            CheckboxTextMargin = "0.5em";
            FontPixSize = 14;
            TextInputTextMargin = "0.5em 0.25em";

            SkinCoords = new UiLayoutCoords();
        }

        public int FontPixSize { get; set; }

        public UiSize WindowBorderSize { get; set; }

        public UiSize WindowContentMargin { get; set; }

        public UiSize WindowCloseBtnSize { get; set; }

        public UiSize WindowCloseBtnOffset { get; set; }

        public UiSize PanelContentMargin { get; set; }

        public UiCoords WindowTitlePosition { get; set; }

        public UiHeight WindowTitleSize { get; set; }

        public string DefaultFontFace { get; set; }

        public UiHeight DefaultTextSize { get; set; }

        public Float3 DefaultTextColorLight { get; set; }

        public Float3 DefaultTextColorDark { get; set; }

        public UiSize ButtonBorderSize { get; set; }

        public UiSize SliderBorderSize { get; set; }

        public UiSize SliderCursorSize { get; set; }

        public UiWidth SliderCursorHMargin { get; set; }

        public UiWidth CheckboxTextMargin { get; set; }

        public UiLayoutCoords SkinCoords { get; private set; }

        public UiSize TextInputTextMargin { get; private set; }
    }

    /// <summary>
    /// List of texure coordinates of all the Ui controls in the Ui skin atlas
    /// </summary>
    public class UiLayoutCoords
    {
        public UiLayoutCoords()
        {
            WindowTopLeft = new Float2(0, 0);
            WindowBottomRight = new Float2(0.22436f, 0.22436f);

            PanelTopLeft = new Float2(0.287f, 0);
            PanelBottomRight = new Float2(0.51136f, 0.22436f);

            ButtonTopLeft = new Float2(0.007293701f, 0.2343699f);
            ButtonBottomRight = new Float2(0.05569711f, 0.282725f);
            ButtonHoverTopLeft = new Float2(0.06585693f, 0.2343699f);
            ButtonHoverBottomRight = new Float2(0.1142883f, 0.282725f);
            ButtonDownTopLeft = new Float2(0.1244736f, 0.2343699f);
            ButtonDownBottomRight = new Float2(0.1728948f, 0.282725f);

            SliderBgTopLeft = new Float2(0.2273127f, 0.001220703f);
            SliderBgBottomRight = new Float2(0.2761104f, 0.05004373f);
            SliderFillTopLeft = new Float2(0.2273127f, 0.05982465f);
            SliderFillBottomRight = new Float2(0.2761104f, 0.1086375f);
            SliderCursorTopLeft = new Float2(0.2273127f, 0.1184133f);
            SliderCursorBottomRight = new Float2(0.2761104f, 0.1672262f);

            TextInputBgTopLeft = new Float2(0.1828614f, 0.2343699f);
            TexInputBgBottomRight = new Float2(0.2316769f, 0.282725f);
        }

        public Float2 ButtonTopLeft { get; set; }

        public Float2 ButtonBottomRight { get; set; }

        public Float2 ButtonHoverTopLeft { get; set; }

        public Float2 ButtonHoverBottomRight { get; set; }

        public Float2 ButtonDownTopLeft { get; set; }

        public Float2 ButtonDownBottomRight { get; }

        public Float2 SliderBgTopLeft { get; set; }

        public Float2 SliderBgBottomRight { get; set; }

        public Float2 SliderFillTopLeft { get; set; }

        public Float2 SliderFillBottomRight { get; set; }

        public Float2 SliderCursorTopLeft { get; set; }

        public Float2 SliderCursorBottomRight { get; set; }

        public Float2 WindowTopLeft { get; set; }

        public Float2 WindowBottomRight { get; set; }

        public Float2 PanelTopLeft { get; set; }

        public Float2 PanelBottomRight { get; set; }

        public Float2 TextInputBgTopLeft { get; }

        public Float2 TexInputBgBottomRight { get; }
    }

}
