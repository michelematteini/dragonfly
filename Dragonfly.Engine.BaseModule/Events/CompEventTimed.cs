using Dragonfly.Engine.Core;
using System;


namespace Dragonfly.BaseModule
{
    /// <summary>
    /// An event that can only be triggered once at a specific time
    /// </summary>
    public class CompEventTimed : Component
    {
        private bool activated;
        private float secondsFromActivation;
        private DateTime when;

        public CompEventTimed(Component owner, DateTime when) : base(owner)
        {
            this.when = when;
            activated = true;  
            Event = new CompEvent(this, IsOccurring, EventTriggerType.Start);
        }

        public CompEventTimed(Component owner, float secondsFromActivation) : base(owner)
        {
            this.secondsFromActivation = secondsFromActivation;
            activated = false;
            Event = new CompEvent(this, IsOccurring, EventTriggerType.Start);
        }

        public CompEvent Event { get; private set; }

        private bool IsOccurring()
        {
            // activate the event on update if needed
            if (!activated)
            {
                when = Context.Time.Now.AddSeconds(secondsFromActivation);
                activated = true;
            }

            // evaluate if the event should be triggered
            return Context.Time.Now > when;
        }
    }
}
