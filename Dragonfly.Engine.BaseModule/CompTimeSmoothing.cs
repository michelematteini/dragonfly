using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    public class CompTimeSmoothing<T> : Component<T>, ICompUpdatable
    {
        private Func<T, T, float, T> lerp;
        private T curValue;

        public CompTimeSmoothing(Component owner, float smoothingTimeSpan, T initialValue, Func<T, T, float, T> lerp) : base(owner)
        {
            SmoothingTimeSpan = smoothingTimeSpan;
            this.lerp = lerp;
            curValue = initialValue;
            TargetValue = initialValue;
        }

        /// <summary>
        /// The duration in seconds of the smoothing animation.
        /// </summary>
        public float SmoothingTimeSpan { get; set; }

        public T TargetValue { get; set; }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        public void Update(UpdateType updateType)
        {
            if (SmoothingTimeSpan == 0)
            {
                curValue = TargetValue;
                return;
            }

            float k = 5.0f / SmoothingTimeSpan;
            float decay = (float)Math.Exp(-k * Context.Time.LastFrameDuration);
            curValue = lerp(TargetValue, curValue, decay);
        }

        public void OverrideCurrentValue(T newValue)
        {
            curValue = newValue;
        }

        protected override T getValue()
        {
            return curValue;
        }
    }
}
