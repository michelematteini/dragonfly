using Dragonfly.Engine.Core;
using System;


namespace Dragonfly.BaseModule
{

    /// <summary>
    /// A type of component computing a boolean value indicating if a given state is occurring.
    /// <para/> Allow the user to monitor this state in various ways using a value from EventTriggerType.
    /// </summary>
    public class CompEvent : Component<bool>, ICompUpdatable
    {
        private Func<bool> isOccurring;
        private EventTriggerType trigger;
        private bool occurred, occurring, wasOccurring;
        private bool disposeOnceOccurred;

        public CompEvent(Component owner, Func<bool> isOccurring) : this(owner, isOccurring, EventTriggerType.Occurring) { }

        public CompEvent(Component owner, Func<bool> isOccurring, EventTriggerType trigger) : base(owner)
        {
            this.isOccurring = isOccurring;
            this.trigger = trigger;
        }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        /// <summary>
        /// Returns the number of times this this event was triggered (i.e. the number frame its value was true)
        /// </summary>
        public int TriggeredCount { get; private set; }
        
        public virtual void Update(UpdateType updateType)
        {
            // the call to GetValue() here also update the value whether it's used or not
            // since for all the trigger types to work, it must be recalculated each frame.
            if (GetValue()) 
                TriggeredCount++;
        }

        /// <summary>
        /// If called, this event destroy itself the frame after its occurrence.
        /// </summary>
        public CompEvent DisposeOnceOccurred()
        {
            disposeOnceOccurred = true;
            return this;
        }

        protected override bool getValue()
        {
            wasOccurring = occurring;

            if (disposeOnceOccurred && wasOccurring)
                Dispose();

            if (IsBeingDisposed)
                return false;

            occurring = isOccurring();
            occurred = occurred || occurring;

            switch (trigger)
            {
                case EventTriggerType.Occurring:
                    return occurring;
                case EventTriggerType.Start:
                    return occurring && !wasOccurring;
                case EventTriggerType.Occurred:
                    return occurred;
                case EventTriggerType.End:
                    return !occurring && wasOccurring;
            }

            return false;
        }
    }

    public enum EventTriggerType
    {
        /// <summary>
        /// Returns true while the event is occurring.
        /// </summary>
        Occurring = 0,
        /// <summary>
        /// Returns true if this event just occurred in this frame
        /// </summary>
        Start,
        /// <summary>
        /// Return true if this event ever happened.
        /// </summary>
        Occurred,
        /// <summary>
        /// Returns true if this event was occurring until the previous frame but its not occurring now
        /// </summary>
        End
    }

}
