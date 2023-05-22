using Dragonfly.Engine.Core;

/// <summary>
/// An event that is triggered only if all the specified sub-events are triggered toghether.
/// </summary>
namespace Dragonfly.BaseModule
{
    public class CompEventAnd : Component
    {
        private CompEvent[] events;

        public CompEventAnd(Component parent, params CompEvent[] events) : base(parent)
        {
            this.events = events;
            Event = new CompEvent(this, () =>
            {
                foreach (CompEvent e in this.events)
                {
                    if (!e.GetValue())
                        return false;
                }
                return true;
            });
        }

        public CompEvent Event { get; private set; }
    }
}
