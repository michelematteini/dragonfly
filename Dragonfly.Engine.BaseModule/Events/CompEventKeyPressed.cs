using System;
using Dragonfly.Engine.Core;
using Dragonfly.Utils;

namespace Dragonfly.BaseModule
{
    public class CompEventKeyPressed : Component
    {
        public CompEventKeyPressed(Component owner, VKey monitoredKey) : this(owner, monitoredKey, EventTriggerType.Occurring) { }

        public CompEventKeyPressed(Component owner, VKey monitoredKey, EventTriggerType triggerType) : base(owner)
        {
            MonitoredKey = monitoredKey;
            Event = new CompEvent(this, IsOccurring, triggerType);
        }

        public CompEvent Event { get; private set; }

        public VKey MonitoredKey { get; set; }

        private bool IsOccurring()
        {
            return Context.Input.GetDevice<Keyboard>().KeyPressed(MonitoredKey);
        }
    }
}
