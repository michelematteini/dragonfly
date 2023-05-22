using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public class CompTimer : Component, ICompUpdatable
    {
        private Action onTimerTick;

        private PreciseFloat lastEventSeconds;

        public CompValue<float> IntervalSeconds { get; private set; }

        /// <summary>
        /// If set to true, triggers this timer even if the specified interval has not passed.
        /// </summary>
        public bool TriggerNow { get; set; }

        public CompTimer(Component owner, float intervalSeconds, Action onTimerTick) : base(owner)
        {
            IntervalSeconds = new CompValue<float>(owner, intervalSeconds);
            lastEventSeconds = Context.Time.SecondsFromStart;
            this.onTimerTick = onTimerTick;         
        }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        public void Update(UpdateType updateType)
        {
            PreciseFloat curTime = Context.Time.SecondsFromStart;
            PreciseFloat dt = curTime - lastEventSeconds;

            if(TriggerNow || dt >= IntervalSeconds.GetValue())
            {
                if (onTimerTick != null) onTimerTick.Invoke();
                lastEventSeconds = curTime;
                TriggerNow = false;
            }
        }
    }
}
