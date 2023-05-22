
namespace Dragonfly.BaseModule
{
    public static class UiPositioning
    {
        /// <summary>
        /// Get a postion below the specified control, plus an optional margin
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        public static UiCoords Below(CompUiControl ctrl, UiHeight margin = new UiHeight())
        {
            CoordContext.Push(ctrl.Container.Coords);
            UiCoords result = ctrl.Position + ctrl.Size.Height + margin;
            CoordContext.Pop();
            return result;
        }

        /// <summary>
        /// Get a postion below the specified control, plus an optional margin
        /// </summary>
        /// <param name="wnd"></param>
        /// <param name="margin"></param>
        /// <returns></returns>
        public static UiCoords Below(CompUiWindow wnd, UiHeight margin = new UiHeight())
        {
            CoordContext.Push(wnd.ParentCanvas.Coords);
            UiCoords result = wnd.Position.GetValue() + wnd.Size.GetValue().Height + margin;
            CoordContext.Pop();
            return result;
        }

        /// <summary>
        /// Get a position on the right of the specified control, plus an optional margin
        /// </summary>
        public static UiCoords RightOf(CompUiControl ctrl, UiWidth margin = new UiWidth())
        {
            CoordContext.Push(ctrl.Container.Coords);
            UiCoords result = ctrl.Position + ctrl.Size.Width + margin;
            CoordContext.Pop();
            return result;
        }

        /// <summary>
        /// Get a position relative to what in this window is considered the origin.
        /// </summary>
        public static UiCoords Inside(CompUiWindow wnd, UiCoords absolutePosition)
        {
            CoordContext.Push(wnd.Coords);
            BaseModUiSettings uiSettings = wnd.Context.GetModule<BaseMod>().Settings.UI;
            UiCoords result = absolutePosition + (wnd.Borderless ? uiSettings.PanelContentMargin : uiSettings.WindowContentMargin);  
            CoordContext.Pop();
            return result;
        }

        /// <summary>
        /// Align the specified controls so that their centers are vertically aligned to the first specified control.
        /// </summary>
        public static void AlignCenterVertically(params CompUiControl[] controls)
        {
            CoordContext.Push(controls[0].Container.Coords);
            UiCoords srcMiddleLeft = controls[0].Position + controls[0].Size.Height * 0.5f + controls[0].AlignmentOffset;

            for (int i = 1; i < controls.Length; i++)
            {
                UiCoords alignedPos = controls[i].Position;
                alignedPos.Y = (srcMiddleLeft - controls[i].Size.Height * 0.5f - controls[i].AlignmentOffset).ConvertTo(controls[i].Position.YUnit).Y;
                controls[i].Position = alignedPos;
            }
            CoordContext.Pop();
        }


    }
}
