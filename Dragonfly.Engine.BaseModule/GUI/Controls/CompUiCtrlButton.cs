using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompUiCtrlButton : CompUiControl, ICompUpdatable
    {
        private string text;

        public CompUiCtrlButton(CompUiContainer parent, UiCoords position, string text, UiSize size) : base(parent, position, size)
        {
            Text = text;
            FontSize = "1em";
            Hovered = new CompEventMouseInArea(this, new CompFunction<AARect>(this, GetParentScreenArea), Container.Coords).Event;
            Hovered = new CompEventAnd(this, Hovered, Container.HasFocus).Event;
            IsDown = new CompEvent(this, () => Hovered.GetValue() && Context.Input.GetDevice<Mouse>().IsLeftButtonPressed);
            IsDown = new CompEventAnd(this, IsDown, Container.HasFocus).Event;

            // consume input even with no listeners
            new CompActionOnEvent(Clicked, () => GetComponent<CompInputFocus>().TryConsumeInput(InputType.Mouse));
        }

        public CompUiCtrlButton(CompUiContainer parent, UiCoords position, string text) : this(parent, position, text, "7em 1.6em") { }

        public CompEvent Hovered { get; private set; }

        public CompEvent IsDown { get; private set; }

        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
                Container.Invalidate(this);
            }
        }

        public UiHeight FontSize { get; set; }

        public UpdateType NeededUpdates
        {
            get
            {
                return (Hovered.ValueChanged || IsDown.ValueChanged) ? UpdateType.FrameStart1 : UpdateType.None;
            }
        }

        public override void UpdateControl(IUiControlUpdateArgs args)
        {
            CoordContext.Push(Container.Coords);

            Float2 topLeftCoords = Ui.SkinCoords.ButtonTopLeft;
            Float2 bottomRightCoords = Ui.SkinCoords.ButtonBottomRight;
            if(IsDown.GetValue())
            {
                topLeftCoords = Ui.SkinCoords.ButtonDownTopLeft;
                bottomRightCoords = Ui.SkinCoords.ButtonDownBottomRight;
            }
            else if(Hovered.GetValue())
            {
                topLeftCoords = Ui.SkinCoords.ButtonHoverTopLeft;
                bottomRightCoords = Ui.SkinCoords.ButtonHoverBottomRight;
            }

            Primitives.ScreenResizablePanel(args.SkinGeometry, TopLeft, BottomRight, topLeftCoords, bottomRightCoords, Ui.ButtonBorderSize.ConvertTo(UiUnit.ScreenSpace, Container.Coords).XY);

            if (!string.IsNullOrEmpty(text))
            {
                UiCoords textPos = Position + Size * 0.5f - FontSize * 0.5f - args.MeasureText(text, Ui.DefaultTextSize, Ui.DefaultFontFace) * 0.5f;
                args.AddText(text, Ui.DefaultTextColorDark, Ui.DefaultTextSize, textPos, Ui.DefaultFontFace);
            }

            CoordContext.Pop();
        }

        public void Update(UpdateType updateType)
        {
            Container.Invalidate(this); 
        }
    }
}
