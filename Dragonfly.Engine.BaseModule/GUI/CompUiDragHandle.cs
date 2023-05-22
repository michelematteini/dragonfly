using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Drags its parent container position using mouse input from a top region of its on-screen area.
    /// </summary>
    public class CompUiDragHandle : CompUiControl, ICompUpdatable
    {
        private CompEvent parentDrag;
        private UiCoords dragStartWinPos;
        private UiCoords dragStartMousePos;

        public CompUiDragHandle(CompUiContainer parent) : base(parent, "0", "0")
        {
            parentDrag = new CompEventMouseDrag(this, new CompFunction<AARect>(this, () => GetTopBarArea()), Container.Coords).Event;
            parentDrag = new CompEventAnd(this, parentDrag, Container.HasFocus).Event;
        }
        private AARect GetTopBarArea()
        {
            CoordContext.Push(Container.Coords);
            AARect topBarArea = AARect.Bounding(ToParentScreen(Position), ToParentScreen(Position + Container.Size.GetValue().Width + Ui.WindowContentMargin.Height));
            CoordContext.Pop();
            return topBarArea;
        }

        public override void UpdateControl(IUiControlUpdateArgs args)
        {

        }
        public UpdateType NeededUpdates 
        {
            get 
            { 
                return (parentDrag.GetValue()) ? UpdateType.FrameStart1 : UpdateType.None; 
            }
        }

        public void Update(UpdateType updateType)
        {
            if (!GetComponent<CompInputFocus>().TryConsumeInput(InputType.Mouse))
                return;

            if (parentDrag.ValueChanged)
            {
                // start drag
                dragStartWinPos = Container.Position.GetValue();
                dragStartMousePos = Context.Input.GetDevice<Mouse>().Position;
            }
            else
            {
                // update drag
                CoordContext.Push(Container.ParentCanvas.Coords);
                UiSize mouseDelta = Context.Input.GetDevice<Mouse>().Position - dragStartMousePos;
                UiCoords newWinPos = dragStartWinPos + mouseDelta;
                Container.Position.Set(newWinPos.ConvertTo(dragStartWinPos.XUnit));
                Container.Invalidate(this);
                CoordContext.Pop();
            }
        }
    }
}
