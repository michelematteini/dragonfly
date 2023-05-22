using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompEventMouseDrag : Component
    {
        private Component<AARect> area;
        private CoordContext coords;
        private bool isDragging, wasLeftMouseDown;

        public CompEventMouseDrag(Component owner, Component<AARect> screenSpaceDraggedArea, CoordContext coords) : base(owner)
        {
            this.area = screenSpaceDraggedArea;
            this.coords = coords;
            Event = new CompEvent(this, IsOccurring);
        }
        public CompEvent Event { get; private set; }

        public UpdateType NeedUpdate => UpdateType.FrameStart1;

        private bool IsOccurring()
        {
            bool isLeftMouseDown = Context.Input.GetDevice<Mouse>().IsLeftButtonPressed;
            if (isDragging)
            {
                isDragging = isLeftMouseDown;
            }
            else if (isLeftMouseDown && !wasLeftMouseDown)
            {
                Float2 mouseScreenPos = Context.Input.GetDevice<Mouse>().Position.ConvertTo(UiUnit.ScreenSpace, coords).XY;
                isDragging = area.GetValue().Contains(mouseScreenPos);
            }

            wasLeftMouseDown = isLeftMouseDown;

            return isDragging;
        }
    }
}
