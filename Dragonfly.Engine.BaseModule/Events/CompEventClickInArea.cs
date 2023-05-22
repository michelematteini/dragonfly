using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompEventClickInArea : Component
    {
        private Component<AARect> area;
        private CoordContext coords;
        private bool clickStartedInArea;

        public CompEventClickInArea(Component owner, Component<AARect> screenArea, CoordContext coords) : base(owner)
        {
            this.area = screenArea;
            this.coords = coords;
            clickStartedInArea = false;
            new CompActionOnEvent(new CompEventKeyPressed(this, Utils.VKey.VK_LBUTTON, EventTriggerType.Start).Event, OnLeftMouseDown);
            this.Event = new CompEvent(this, IsOccurring);
        }

        public CompEvent Event { get; private set; }

        private void OnLeftMouseDown()
        {
            clickStartedInArea = IsMouseInArea();
        }

        private bool IsOccurring()
        {
            if (!clickStartedInArea)
                return false;

            if (!Context.Input.GetDevice<Mouse>().LeftClicked)
                return false;

            return IsMouseInArea();
        }

        private bool IsMouseInArea()
        {
            Float2 mouseScreenPos = Context.Input.GetDevice<Mouse>().Position.ConvertTo(UiUnit.ScreenSpace, coords).XY;
            return area.GetValue().Contains(mouseScreenPos);
        }
    }
}
