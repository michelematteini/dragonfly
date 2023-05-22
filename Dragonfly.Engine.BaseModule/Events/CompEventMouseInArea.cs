using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompEventMouseInArea : Component
    {
        private Component<AARect> area;
        private CoordContext coords;

        public CompEventMouseInArea(Component owner, Component<AARect> screenArea, CoordContext coords) : base(owner)
        {
            this.area = screenArea;
            this.coords = coords;
            Event = new CompEvent(this, IsOccurring);
        }

        public CompEvent Event { get; private set; }

        private bool IsOccurring()
        {
            Float2 mouseScreenPos = Context.Input.GetDevice<Mouse>().Position.ConvertTo(UiUnit.ScreenSpace, coords).XY;
            return area.GetValue().Contains(mouseScreenPos);
        }
    }
}
