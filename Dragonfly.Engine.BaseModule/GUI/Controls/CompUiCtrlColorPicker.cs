using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    public class CompUiCtrlColorPicker : CompUiControl, ICompUpdatable
    {
        private static readonly float HUE_SLIDER_HEIGHT_PERC = 0.15f;
        private static readonly float COLOR_PREVIEW_HEIGHT = 0.15f;
        private static readonly float SLIDER_MARGINS = 0.05f;


        private CompMtlColorPicker pickerMaterial;
        private CompEvent hueDrag, svDrag;
        private CompValue<Float3> selectedHSV;

        public CompUiCtrlColorPicker(CompUiContainer parent, UiCoords position, Float3 initialColor, UiSize size) : base(parent, position, size)
        {
            pickerMaterial = new CompMtlColorPicker(this);
            CompMesh colorPickerMesh = AddCustomMesh(pickerMaterial);
            CustomMeshTransform.Push(new CompFunction<Float4x4>(CustomMeshTransform, GetLocalToParentTransform));
            Primitives.ScreenQuad(colorPickerMesh.AsObject3D());

            selectedHSV = new CompValue<Float3>(this, Color.Rgb2Hsv(initialColor));
            CompActionOnChange.MonitorValue(selectedHSV, c => Container.Invalidate(this));

            hueDrag = new CompEventMouseDrag(this, new CompFunction<AARect>(this, () => GetHueSliderArea()), Container.Coords).Event;
            hueDrag = new CompEventAnd(this, hueDrag, Container.HasFocus).Event;
            svDrag = new CompEventMouseDrag(this, new CompFunction<AARect>(this, () => GetSatValueArea()), Container.Coords).Event;
            svDrag = new CompEventAnd(this, svDrag, Container.HasFocus).Event;
        }

        public CompUiCtrlColorPicker(CompUiContainer parent, UiCoords position, Float3 initialColor) : this(parent, position, initialColor, "10em") { }

        public CompUiCtrlColorPicker(CompUiContainer parent, UiCoords position) : this(parent, position, Color.Orange.ToFloat3(), "10em") { }

        public Float3 SelectedColor 
        {
            get 
            {
                return Color.Hsv2Rgb(selectedHSV.GetValue());
            }
            set 
            {
                selectedHSV.Set(Color.Rgb2Hsv(value));
            }
        }

        public UpdateType NeededUpdates
        {
            get
            {
                return (hueDrag.GetValue() || svDrag.GetValue()) ? UpdateType.FrameStart1 : UpdateType.None;
            }
        }

        public void Update(UpdateType updateType)
        {
            CoordContext.Push(Container.Coords);

            if (GetComponent<CompInputFocus>().TryConsumeInput(InputType.Mouse))
            {
                Float3 hsv = selectedHSV.GetValue();

                if (hueDrag.GetValue())
                {
                    float cursorRunStart = ToParentScreen(Position).X;
                    float cursorRunEnd = ToParentScreen(Position + Size).X;
                    float mouseScreenX = Context.Input.GetDevice<Mouse>().Position.ConvertTo(UiUnit.ScreenSpace, Container.ParentCanvas.Coords).X;

                    hsv.X = ((mouseScreenX - cursorRunStart) / (cursorRunEnd - cursorRunStart)).Saturate();
                    selectedHSV.Set(hsv);
                }

                if (svDrag.GetValue())
                {
                    Float2 areaStart = ToParentScreen(Position + Size.Height * (HUE_SLIDER_HEIGHT_PERC + SLIDER_MARGINS));
                    Float2 areaEnd = ToParentScreen(Position + Size - Size.Height * (COLOR_PREVIEW_HEIGHT + SLIDER_MARGINS));
                    Float2 mouseScreen = Context.Input.GetDevice<Mouse>().Position.ConvertTo(UiUnit.ScreenSpace, Container.ParentCanvas.Coords).XY;

                    hsv.Y = ((mouseScreen.X - areaStart.X) / (areaEnd.X - areaStart.X)).Saturate();
                    hsv.Z = ((areaStart.Y - mouseScreen.Y) / (areaEnd.Y - areaStart.Y) + 1.0f).Saturate();
                    selectedHSV.Set(hsv);
                }
            }

            CoordContext.Pop();
        }

        private AARect GetHueSliderArea()
        {
            AARect ssArea = GetParentScreenArea();
            return new AARect(ssArea.Min.X, ssArea.Min.Y + ssArea.Height * (1.0f - HUE_SLIDER_HEIGHT_PERC), ssArea.Max.X, ssArea.Max.Y);
        }

        private AARect GetSatValueArea()
        {
            AARect ssArea = GetParentScreenArea();
            return new AARect(ssArea.Min.X, ssArea.Min.Y + ssArea.Height * (COLOR_PREVIEW_HEIGHT + SLIDER_MARGINS), ssArea.Max.X, ssArea.Max.Y - ssArea.Height * (HUE_SLIDER_HEIGHT_PERC + SLIDER_MARGINS));
        }

        public override void UpdateControl(IUiControlUpdateArgs args) 
        {
            CoordContext.Push(Container.Coords);

            // draw hex version of the selected color over its preview
            Float3 rgbColor = SelectedColor;
            Float3 constrastColor = Color.GetLuminanceFromRGB(rgbColor) < 0.5f ? Float3.One : Float3.Zero;
            string colorHexStr = rgbColor.ToHexColor();
            UiHeight colorStrHeight = Size.Height * COLOR_PREVIEW_HEIGHT * 0.8f;
            UiCoords textLocation = Position + Size.Height;
            textLocation = textLocation - 0.5f * (Size.Height * COLOR_PREVIEW_HEIGHT + colorStrHeight); // center vertically
            textLocation = textLocation + 0.5f * (Size.Width - args.MeasureText(colorHexStr, colorStrHeight, Ui.DefaultFontFace)); // center horizontally
            args.AddText(rgbColor.ToHexColor(), constrastColor, colorStrHeight, textLocation, Ui.DefaultFontFace);

            CoordContext.Pop();
        }

        private class CompMtlColorPicker : CompMaterial
        {
            private CompUiCtrlColorPicker picker;

            public CompMtlColorPicker(CompUiCtrlColorPicker parent) : base(parent)
            {
                picker = parent;
                BlendMode = BlendMode.AlphaBlend;
                CullMode = Graphics.CullMode.None;
                UpdateEachFrame = true;
            }

            public override string EffectName => "ColorPicker";

            protected override void UpdateParams()
            {
                Shader.SetParam("hueHeightPerc", HUE_SLIDER_HEIGHT_PERC);
                Shader.SetParam("svMarginPerc", SLIDER_MARGINS);
                Shader.SetParam("previewHeightPerc", COLOR_PREVIEW_HEIGHT);
                Shader.SetParam("selectedHSV", picker.selectedHSV.GetValue());

                Float2 size = picker.Size.ConvertTo(UiUnit.Pixels, picker.Container.Coords).XY;
                Shader.SetParam("aspect", size.X / size.Y);
            }
        }

    }
}
