using System;
using Dragonfly.Engine.Core;

namespace Dragonfly.BaseModule
{
    public class CompEventLoadingScreenVisible : Component
    {
        public CompEventLoadingScreenVisible(Component owner) : this(owner, EventTriggerType.Occurring) { }

        public CompEventLoadingScreenVisible(Component owner, EventTriggerType trigger) : base(owner) 
        {
            Event = new CompEvent(this, IsOccurring, trigger);
        }

        public CompEvent Event { get; private set; }

        private bool IsOccurring()
        {
            return Context.GetModule<BaseMod>().LoadingScreen.Visible;
        }
    }
}
