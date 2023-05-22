using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dragonfly.BaseModule
{
    public class CompUiWindow : CompUiContainer
    {
        private string title;
        private CompEvent windowMouseDownEvent;
        private Action onHide;
        private CompUiDragHandle mouseDrag;
        private CompUiCtrlButton closeButton;
        public CompUiWindow(Component parent, IUiCanvas parentCanvas, UiSize size, UiCoords position, PositionOrigin positionPivot) : base(parent, parentCanvas, size, position, positionPivot)
        {
            title = "";
            TextRenderMode = TextRenderMode.Crisp;
            Active = false;
            windowMouseDownEvent = new CompEventMouseDownInArea(this, new CompFunction<AARect>(this, () => GetScreenArea()), Coords).Event;
            new CompActionOnEvent(windowMouseDownEvent, OnMouseDown);
            mouseDrag = new CompUiDragHandle(this);
            CloseButtonEnabled = true;
        }

        public CompUiWindow(Component parent, IUiCanvas parentCanvas, UiSize size, UiCoords position) : this(parent, parentCanvas, size, position, PositionOrigin.TopLeft) { }

        public CompUiWindow(CompUiContainer parent, UiSize size, UiCoords position, PositionOrigin positionPivot) : this(parent, parent, size, position, positionPivot) { }

        public CompUiWindow(CompUiContainer parent, UiSize size, UiCoords position) : this(parent, parent, size, position, PositionOrigin.TopLeft) { }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                Invalidate(null);
            }
        }

        public bool Visible
        {
            get
            {
                return Active;
            }
            set
            {
                // update visibility
                if (value == Visible) 
                    return; // visibility did not change
                Active = value;

                // === SHOW
                if (value) 
                {
                    Focus();
                }
                // === HIDE
                else
                {
                    // perform action on hide if available
                    if (onHide != null)
                    {
                        onHide();
                        onHide = null;
                    }

                    // re-assign focus if currently in-focus 
                    if (ZIndex == 0)
                    {
                        // find the next window elegible for focus
                        CompUiWindow focusDest = null;
                        foreach (CompUiWindow wnd in GetComponents<CompUiWindow>())
                        {
                            // skip windows that are not on the same canvas
                            if (wnd.ParentCanvas != this.ParentCanvas)
                                continue;

                            // skip same window
                            if (wnd == this)
                                continue; 

                            // shift all windows z-index 
                            if (focusDest == null || focusDest.ZIndex > wnd.ZIndex)
                                focusDest = wnd;
                        }

                        // set focus to the found window, if any
                        if (focusDest != null)
                            focusDest.Focus();
                    }
                }
            }
        }

        public bool Borderless { get; set; }

        public void Hide()
        {
            Visible = false;
        }

        public void Show()
        {
            Visible = true;
        }

        public void Show(Action onHide)
        {
            this.onHide = onHide;
            Show();
        }

        /// <summary>
        /// If set to true, this window cannot be moved using the mouse.
        /// </summary>
        public bool PositionLocked
        {
            get
            {
                return mouseDrag == null;
            }
            set
            {
                if (!value && mouseDrag == null)
                    mouseDrag = new CompUiDragHandle(this);
                else if (value && mouseDrag != null)
                {
                    mouseDrag.Dispose();
                    mouseDrag = null;
                }
            }
        }

        public bool CloseButtonEnabled
        {
            get
            {
                return closeButton != null;
            }
            set
            {
                if (value == CloseButtonEnabled)
                    return;

                if (value)
                    AddCloseButton();
                else
                {
                    closeButton.Dispose();
                    closeButton = null;
                }
                Invalidate(null);
            }
        }

        void AddCloseButton()
        {
            CoordContext.Push(Coords);
            BaseModUiSettings UI = Context.GetModule<BaseMod>().Settings.UI;
            closeButton = new CompUiCtrlButton(this, UiCoords.Zero + Size.GetValue().Width + UI.WindowCloseBtnOffset, "X", UI.WindowCloseBtnSize);
            new CompActionOnEvent(closeButton.Clicked, () => this.Hide());
            CoordContext.Pop();
        }

        private void OnMouseDown()
        {
            if (!Visible) return;

            // check if the current mouse-down affected other windows higher in the focus stack
            bool elegibleForFocus = true;
            foreach (CompUiWindow wnd in GetComponents<CompUiWindow>())
            {
                // skip windows that are not on the same canvas
                if (wnd.ParentCanvas != this.ParentCanvas)
                    continue;

                elegibleForFocus = !wnd.windowMouseDownEvent.GetValue() || wnd.ZIndex >= ZIndex;
                if (!elegibleForFocus) break; // found another window higher in the focus stack, exit the search loop
            }

            if (elegibleForFocus)
                Focus(); // apply focus on this window

        }

        public void Focus()
        {
            if (!Visible) return;

            // retrieve and sort windows, based on their z-index
            List<CompUiWindow> windowStack = GetComponents<CompUiWindow>().Where(wnd => wnd != this && wnd.ParentCanvas == ParentCanvas).ToList();

            // update the other windows z-index and focus (if any)
            if (windowStack.Count > 0)
            {
                windowStack.Sort((wnd1, wnd2) => wnd1.ZIndex.CompareTo(wnd2.ZIndex));

                // set the z-index of the window stack
                for (int i = 0; i < windowStack.Count; i++)
                    windowStack[i].ZIndex = (uint)(i + 1);

                // call focus lost on the previously in-focus window
                windowStack[0].OnFocusLost();
            }

            // set the z-index of this window to the lowest to show it on top
            ZIndex = 0;
            OnFocus();
        }

        /// <summary>
        /// If set to true, this window will be hidden when it looses focus.
        /// </summary>
        public bool HideOnFocusLost { get; set; }

        protected virtual void OnFocusLost()
        {
            if (HideOnFocusLost)
                Hide();
        }

        protected virtual void OnFocus()
        {

        }

        protected override void UpdateContainerGeometry(IUiControlUpdateArgs args)
        {
            base.UpdateContainerGeometry(args);
            BaseModUiSettings uiSettings = Context.GetModule<BaseMod>().Settings.UI;

            Primitives.ScreenResizablePanel(
                args.SkinGeometry,
                new Float2(-1, 1), // top left
                new Float2(1, -1), // bottom right
                Borderless ? uiSettings.SkinCoords.PanelTopLeft : uiSettings.SkinCoords.WindowTopLeft,
                Borderless ? uiSettings.SkinCoords.PanelBottomRight : uiSettings.SkinCoords.WindowBottomRight,
                uiSettings.WindowBorderSize.ConvertTo(UiUnit.ScreenSpace, Coords).XY
            );

            if (!Borderless && !string.IsNullOrEmpty(title))
                args.AddText(title, uiSettings.DefaultTextColorDark, uiSettings.WindowTitleSize, uiSettings.WindowTitlePosition, uiSettings.DefaultFontFace);
        }

        public override string ToString()
        {
            return "CompUiWindow : " + Title;
        }

    }
}
