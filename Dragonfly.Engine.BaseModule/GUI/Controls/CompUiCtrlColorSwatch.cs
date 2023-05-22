using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public class CompUiCtrlColorSwatch : CompUiControl
    {
        private CompMtlBasic swatchMaterial;
        private bool autosizeFont;
        private CompUiWndColorPicker wndColorPicker;

        public CompUiCtrlColorSwatch(CompUiContainer parent, UiCoords position, UiSize size) : base(parent, position, size)
        {
            swatchMaterial = new CompMtlBasic(this, new Float3("#FF4000"));
            swatchMaterial.CullMode = Graphics.CullMode.None;
            CompMesh swatchMesh = AddCustomMesh(swatchMaterial);
            Primitives.ScreenQuad(swatchMesh.AsObject3D());
            CustomMeshTransform.Push(new CompFunction<Float4x4>(CustomMeshTransform, GetLocalToParentTransform));
            FontSize = Ui.DefaultTextSize;
            Editable = true;
            autosizeFont = true;
            SelectedColor = new CompValue<Float3>(this, Color.Orange.ToFloat3());
            CompActionOnChange.MonitorValue(SelectedColor, OnColorChanged);
            wndColorPicker = new CompUiWndColorPicker(Context.GetModule<BaseMod>().UiContainer, "0ss 0ss", SelectedColor.GetValue());
            wndColorPicker.Window.Hide();
            new CompActionOnEvent(Clicked, ShowPikingWindow);
        }
        public CompUiCtrlColorSwatch(CompUiContainer parent, UiCoords position) : this(parent, position, "7em 1.6em"){ }

        public CompUiCtrlColorSwatch(CompUiContainer parent) : this(parent, UiCoords.Zero, "7em 1.6em") { }

        private void OnColorPicked()
        {
            if (!wndColorPicker.Confirmed)
                return;
            SelectedColor.Set(wndColorPicker.Picker.SelectedColor);
        }

        private void ShowPikingWindow()
        {
            if (Editable && GetComponent<CompInputFocus>().TryConsumeInput(InputType.Mouse))
            {
                CoordContext.Push(Container.Coords);
                wndColorPicker.Picker.SelectedColor = SelectedColor.GetValue();
                wndColorPicker.Window.Position.Set(new UiCoords(ToParentScreen((Position + Size.Height - Ui.PanelContentMargin.Width)), UiUnit.ScreenSpace));
                wndColorPicker.Window.Show(OnColorPicked);
                CoordContext.Pop();
            }
        }


        public CompValue<Float3> SelectedColor { get; private set; }

        private void OnColorChanged(Float3 color)
        {
            swatchMaterial.Color.Value = color;
            Container.Invalidate(this);
        }

        public bool Editable { get; set; }

        public UiHeight FontSize { get; set; }

        /// <summary>
        /// If true, the size of the color name inside this control changes automatically with the control size.
        /// </summary>
        public bool AutosizeFont {
            get
            {
                return autosizeFont;
            }
            set 
            {
                autosizeFont = true;
                Container.Invalidate(this);
            } 
        }

        public override void UpdateControl(IUiControlUpdateArgs args)
        {
            CoordContext.Push(Container.Coords);

            // draw hex version of the selected color over its preview
            Float3 rgbColor = SelectedColor.GetValue();
            Float3 constrastColor = Color.GetLuminanceFromRGB(rgbColor) < 0.5f ? Float3.One : Float3.Zero;
            string colorHexStr = rgbColor.ToHexColor();
            UiHeight colorStrHeight = autosizeFont ? (Size.Height * 0.7f) : FontSize;
            UiCoords textLocation = Center;
            textLocation = textLocation - 0.5f * colorStrHeight; // center vertically
            textLocation = textLocation - 0.5f * args.MeasureText(colorHexStr, colorStrHeight, Ui.DefaultFontFace); // center horizontally
            args.AddText(rgbColor.ToHexColor(), constrastColor, colorStrHeight, textLocation, Ui.DefaultFontFace);

            CoordContext.Pop();
        }
    }
}
