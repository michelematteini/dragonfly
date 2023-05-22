using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompEventMouseDownInArea : Component
    {
        private Component<AARect> area;
        private CoordContext coords;
        private CompEventKeyPressed mouseDownEvent;

        public CompEventMouseDownInArea(Component owner, Component<AARect> screenArea, CoordContext coords) : base(owner)
        {
            this.area = screenArea;
            this.coords = coords;
            mouseDownEvent  = new CompEventKeyPressed(this, Utils.VKey.VK_LBUTTON, EventTriggerType.Start);
            Event = new CompEvent(this, IsOccurring);

        }
        public CompEvent Event { get; private set; }

        private bool IsOccurring()
        {
            return mouseDownEvent.Event.GetValue() && IsMouseInArea();
        }

        private bool IsMouseInArea()
        {
            Float2 mouseScreenPos = Context.Input.GetDevice<Mouse>().Position.ConvertTo(UiUnit.ScreenSpace, coords).XY;
            return area.GetValue().Contains(mouseScreenPos);
        }
    }
}