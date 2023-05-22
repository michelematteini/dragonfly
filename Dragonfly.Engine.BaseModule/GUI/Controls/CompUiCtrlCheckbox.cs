using Dragonfly.Engine.Core;
using System.Collections.Generic;
using System.Linq;

namespace Dragonfly.BaseModule
{
    public class CompUiCtrlCheckbox : CompUiControl
    {
        private CompValue<bool> isChecked;
        private string onText, offText;
        private HashSet<CompUiCtrlCheckbox> group; // if not null, contains a group of checkbox that are mutually excusive, and only one can be true at a given time

        public CompUiCtrlCheckbox(CompUiContainer parent, UiCoords position, bool initialValue, UiSize size) : base(parent, position, size)
        {
            isChecked = new CompValue<bool>(this, initialValue);
            Value = isChecked;
            new CompActionOnEvent(Clicked, MouseToggle);
            onText = "On";
            offText = "Off";
            CheckedChanged = new CompFunction<bool>(this, () => isChecked.ValueChanged);
        }

        public CompUiCtrlCheckbox(CompUiContainer parent, UiCoords position, bool initialValue = false) : this(parent, position, initialValue, "2.5em 2em") { }

        public bool Checked 
        { 
            get 
            { 
                return isChecked.GetValue(); 
            }
            set
            {
                if (group == null)
                {
                    isChecked.Set(value);
                }
                else
                {
                    CompUiCtrlCheckbox alternative = null;
                    foreach(CompUiCtrlCheckbox groupCheckbox in group)
                    {
                        groupCheckbox.isChecked.Set(false);
                        if (alternative == null && groupCheckbox != this)
                            alternative = groupCheckbox;
                    }

                    if (value)
                        isChecked.Set(true);
                    else
                        alternative.isChecked.Set(true);
                }
                Container.Invalidate(this);
            }
        }

        public string Text
        {
            get { return onText; }
            set
            {
                onText = value;
                offText = value;
                Container.Invalidate(this);
            }
        }

        public string TextWhileChecked
        {
            get { return onText; }
            set
            {
                onText = value;
                Container.Invalidate(this);
            }
        }

        public string TextWhileUnchecked
        {
            get { return offText; }
            set
            {
                offText = value;
                Container.Invalidate(this);
            }
        }

        public Component<bool> Value { get; private set; }

        public Component<bool> CheckedChanged { get; private set; }

        public void MouseToggle()
        {
            if (!GetComponent<CompInputFocus>().TryConsumeInput(InputType.Mouse))
                return;

            Checked = !Checked;
        }

        public override void UpdateControl(IUiControlUpdateArgs args)
        {
            CoordContext.Push(Container.Coords);

            // draw empty slider background
            Primitives.ScreenResizablePanel(args.SkinGeometry, ToScreen(Position), ToScreen(Position + Size), Ui.SkinCoords.SliderBgTopLeft, Ui.SkinCoords.SliderBgBottomRight, ToScreen(Ui.SliderBorderSize));

            if (Checked)
            {
                // draw filled slider area
                Primitives.ScreenResizablePanel(args.SkinGeometry, TopLeft, BottomRight, Ui.SkinCoords.SliderFillTopLeft, Ui.SkinCoords.SliderFillBottomRight, ToScreen(Ui.SliderBorderSize));
            }

            // draw cursor
            UiCoords cursorPos = GetCursorPosition();
            Primitives.ScreenQuad(args.SkinGeometry, Ui.SkinCoords.SliderCursorTopLeft, Ui.SkinCoords.SliderCursorBottomRight, ToScreen(cursorPos - Ui.SliderCursorSize * 0.5f), ToScreen(cursorPos + Ui.SliderCursorSize * 0.5f));

            // draw text
            UiCoords textPos = Position + Size.Width + Ui.CheckboxTextMargin + Size.Height * 0.5f - Ui.DefaultTextSize * 0.5f;
            args.AddText(Checked ? onText : offText, Ui.DefaultTextColorLight, Ui.DefaultTextSize, textPos, Ui.DefaultFontFace);

            CoordContext.Pop();
        }

        private UiCoords GetCursorPosition()
        {
            UiCoords cursorPos = Position + Size.Height * 0.5f + Ui.SliderCursorHMargin;
            if (Checked) cursorPos += (Size.Width - 2.0f * Ui.SliderCursorHMargin);
            return cursorPos;
        }

        public override UiSize AlignmentOffset
        {
            get
            {
                return "0";
            }
        }

        #region Grouping

        /// <summary>
        /// Link the specified checkboxes together so that only one of them is checked at any time.
        /// </summary>
        public static void Group(params CompUiCtrlCheckbox[] options)
        {
            HashSet<CompUiCtrlCheckbox> newGroup = new HashSet<CompUiCtrlCheckbox>(options);
            for (int i = 0; i < options.Length; i++)
            {
                options[i].Ungroup();
                options[i].group = newGroup;
            }
            options[0].Checked = true;
        }

        /// <summary>
        /// Remove this checkbox from its group (see CompUiCtrlCheckbox.Group)
        /// </summary>
        public void Ungroup()
        {
            if (group == null)
                return;

            if (group.Count > 2)
            {
                group.Remove(this);
                if (Checked) // select another in the group if this is currently the checked one
                    group.First().Checked = true;
                group = null;
            }
            else
            {
                foreach (CompUiCtrlCheckbox groupCheckbox in group)
                    groupCheckbox.group = null;
            }
        }

        #endregion

    }
}
