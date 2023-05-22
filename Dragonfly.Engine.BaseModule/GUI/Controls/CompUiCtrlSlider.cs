using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompUiCtrlSlider : CompUiControl, ICompUpdatable
    {
        private CompEvent cursorDrag;

        public CompUiCtrlSlider(CompUiContainer parent, UiCoords position, UiSize size, float minValue, float maxValue) : base(parent, position, size)
        {
            Percent = 0.5f;
            MinValue = minValue;
            MaxValue = maxValue;
            Value = new CompFunction<float>(this, () => minValue.Lerp(MaxValue, Percent));
            cursorDrag = new CompEventMouseDrag(this, new CompFunction<AARect>(this, () => GetParentScreenArea()), Container.Coords).Event;
            cursorDrag = new CompEventAnd(this, cursorDrag, Container.HasFocus).Event;
        }

        public CompUiCtrlSlider(CompUiContainer parent, UiCoords position, float minValue, float maxValue) : this(parent, position, "10em 2em", minValue, maxValue) { }

        public CompUiCtrlSlider(CompUiContainer parent, UiCoords position) : this(parent, position, 0, 1.0f) { }

        public CompUiCtrlSlider(CompUiContainer parent) : this(parent, UiCoords.Zero, 0, 1.0f) { }

        public float Percent { get; set; }

        public float MinValue { get; set; }

        public float MaxValue { get; set; }

        public Component<float> Value { get; private set; }

        public float GetPercentFromValue(float value)
        {
            return ((value - MinValue) / (MaxValue - MinValue)).Saturate();
        }

        public UpdateType NeededUpdates
        {
            get
            {
                return (cursorDrag.GetValue()) ? UpdateType.FrameStart1 : UpdateType.None;
            }
        }

        public void Update(UpdateType updateType)
        {
            if (cursorDrag.GetValue() && GetComponent<CompInputFocus>().TryConsumeInput(InputType.Mouse))
            {
                CoordContext.Push(Container.Coords);

                float cursorRunStart = ToParentScreen(Position + Ui.SliderCursorHMargin).X;
                float cursorRunEnd = ToParentScreen(Position + Size - Ui.SliderCursorHMargin).X;
                float mouseScreenX = Context.Input.GetDevice<Mouse>().Position.ConvertTo(UiUnit.ScreenSpace, Container.ParentCanvas.Coords).X;

                Percent = ((mouseScreenX - cursorRunStart) / (cursorRunEnd - cursorRunStart)).Saturate();

                CoordContext.Pop();

                Container.Invalidate(this);
            }
        }

        public override void UpdateControl(IUiControlUpdateArgs args)
        {
            CoordContext.Push(Container.Coords);

            // draw empty slider background
            Primitives.ScreenResizablePanel(args.SkinGeometry, ToScreen(Position), ToScreen(Position + Size), Ui.SkinCoords.SliderBgTopLeft, Ui.SkinCoords.SliderBgBottomRight, ToScreen(Ui.SliderBorderSize));

            // draw filled slider area
            UiWidth fillOffset = Ui.SliderBorderSize.Width + (Size.Width - Ui.SliderBorderSize.Width) * Percent;
            Primitives.ScreenResizablePanel(args.SkinGeometry, ToScreen(Position), ToScreen(Position + Size.Height + fillOffset), Ui.SkinCoords.SliderFillTopLeft, Ui.SkinCoords.SliderFillBottomRight, ToScreen(Ui.SliderBorderSize));

            // draw cursor
            UiCoords cursorPos = GetCursorPosition();
            Primitives.ScreenQuad(args.SkinGeometry, Ui.SkinCoords.SliderCursorTopLeft, Ui.SkinCoords.SliderCursorBottomRight, ToScreen(cursorPos - Ui.SliderCursorSize * 0.5f), ToScreen(cursorPos + Ui.SliderCursorSize * 0.5f));

            CoordContext.Pop();
        }

        private UiCoords GetCursorPosition()
        {
            return Position + Size.Height * 0.5f + Ui.SliderCursorHMargin + Percent * (Size.Width - 2.0f * Ui.SliderCursorHMargin);
        }

        public override UiSize AlignmentOffset
        {
            get
            {
                return "0";
            }
        }

    }
}
