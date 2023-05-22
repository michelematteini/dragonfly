using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A window that let the user pick a color.
    /// </summary>
    public class CompUiWndColorPicker : Component
    {
        private CompUiCtrlButton btnOk, btnCancel;

        public CompUiWndColorPicker(Component parent, UiCoords startPosition, Float3 initialColor) : base(parent)
        {
            BaseMod baseMod = Context.GetModule<BaseMod>();

            Window = new CompUiWindow(baseMod.UiContainer, "13em 15.5em", startPosition);
            Window.Title = "Color picker";
            Window.Borderless = true;
            Window.HideOnFocusLost = true;
            Window.CloseButtonEnabled = false;
            Picker = new CompUiCtrlColorPicker(Window, UiPositioning.Inside(Window, "0"), initialColor);
            btnCancel = new CompUiCtrlButton(Window, UiPositioning.Below(Picker, "10px"), "Cancel");
            btnCancel.Width = "4.5em";
            btnOk = new CompUiCtrlButton(Window, UiPositioning.RightOf(btnCancel, "1em"), "OK");
            btnOk.Width = "4.5em";
            new CompActionOnEvent(btnCancel.Clicked, () => { 
                Confirmed = false; 
                Window.Hide();
            });
            new CompActionOnEvent(btnOk.Clicked, () => {
                Confirmed = true;
                Window.Hide();
            });
            Window.PositionLocked = true;
        }

        public bool Confirmed { get; private set; }

        public CompUiWindow Window { get; private set; }

        public CompUiCtrlColorPicker Picker { get; private set; }

    }
}
