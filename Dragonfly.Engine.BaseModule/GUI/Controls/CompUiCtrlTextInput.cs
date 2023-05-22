using System;
using Dragonfly.Graphics.Math;
using Dragonfly.Engine.Core;

namespace Dragonfly.BaseModule
{
    public class CompUiCtrlTextInput : CompUiControl, ICompUpdatable
    {
        private const float CURSOR_BLINK_RATE = 0.5f;
        private const float TEXT_AUTODEL_TIMER = 0.6f;
        private const float TEXT_AUTODEL_RATE = 0.025f;
        private string text;
        private PreciseFloat lastCursorBlink, lastBackDown, lastBackDeletion;
        private bool cursorVisible;

        public CompUiCtrlTextInput(CompUiContainer parent, string initialText, UiCoords position) : base(parent, position, "10em 1.5em")
        {
            Text = initialText;
            Font = FontParams.GetDefaultDark(Ui);
            Font.FontChanged += OnFontChanged;
            OnFontChanged();
        }

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                Container.Invalidate(this);
            }
        }

        public FontParams Font { get; private set; }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        private void OnFontChanged()
        {
            Container.Invalidate(this);
        }

        public override void UpdateControl(IUiControlUpdateArgs args)
        {
            CoordContext.Push(Container.Coords);

            // draw background
            Primitives.ScreenResizablePanel(args.SkinGeometry, TopLeft, BottomRight, Ui.SkinCoords.TextInputBgTopLeft, Ui.SkinCoords.TexInputBgBottomRight, Ui.ButtonBorderSize.ConvertTo(UiUnit.ScreenSpace, Container.Coords).XY);

            // draw text
            UiWidth textWidth = new UiWidth();
            if (!string.IsNullOrEmpty(text))
            {
                textWidth = args.MeasureText(text, Font.Size, Font.FontFace);
                UiCoords textPos = Position + Ui.TextInputTextMargin;               
                args.AddText(text, Font.Color, Font.Size, textPos, Font.FontFace);
            }

            // draw cursor
            if (HasFocus.GetValue())
            {
                if (!cursorVisible)
                {
                    UiCoords cursorPos = Position + Ui.TextInputTextMargin + textWidth - Font.Size * 0.2f;
                    args.AddText("|", Float3.Zero, Font.Size * 1.3f, cursorPos, Font.FontFace);
                }
            }

            CoordContext.Pop();
        }

        public void Update(UpdateType updateType)
        {
            bool uiUpdateNeeded = false;

            // update control if...
            bool cursorShouldBlink = (Context.Time.RealSecondsFromStart - lastCursorBlink > CURSOR_BLINK_RATE) && HasFocus.GetValue();
            uiUpdateNeeded |= cursorShouldBlink; // ...cursor should blink now
            uiUpdateNeeded |= HasFocus.ValueChanged; // ...control lost focus

            // capture keyboard input
            if(HasFocus.GetValue() && GetComponent<CompInputFocus>().TryConsumeInput(InputType.Keyboard))
            {
                Keyboard kb = Context.Input.GetDevice<Keyboard>();

                if (kb.KeyPressed(Utils.VKey.VK_BACK) && !string.IsNullOrEmpty(text))
                {
                    // backspace char deletion
                    text = text.Substring(0, text.Length - 1);
                    lastBackDeletion = lastBackDown = Context.Time.RealSecondsFromStart;
                    uiUpdateNeeded = true;
                }
                else if (kb.IsKeyDown(Utils.VKey.VK_BACK) && !string.IsNullOrEmpty(text) && (Context.Time.RealSecondsFromStart - lastBackDown) > TEXT_AUTODEL_TIMER && (Context.Time.RealSecondsFromStart - lastBackDeletion) > TEXT_AUTODEL_RATE)
                {
                    // backspace autodelete
                    text = text.Substring(0, text.Length - 1);
                    lastBackDeletion = Context.Time.RealSecondsFromStart;
                    cursorVisible = true; // avoid blinking cursor while deleting text
                    uiUpdateNeeded = true;
                }
                else
                {
                    // add text from keyboard
                    string inputText = kb.GetTextInput();
                    if (!string.IsNullOrEmpty(inputText))
                    {
                        text += inputText;
                        uiUpdateNeeded = true;
                    }
                }
            }

            // update cursor visibility on blink
            if (cursorShouldBlink)
            {
                cursorVisible = !cursorVisible;
                lastCursorBlink = Context.Time.RealSecondsFromStart;
            }

            if (uiUpdateNeeded)
                Container.Invalidate(this);
        }
    }
}
