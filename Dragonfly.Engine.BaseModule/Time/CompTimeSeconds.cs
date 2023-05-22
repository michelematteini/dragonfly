using System;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompTimeSeconds : Component<float>
    {
        bool activated;
        PreciseFloat startOffsetSeconds; // offset in seconds from activation while activated == false, the offset in seconds from start while activated == true

        public CompTimeSeconds(Component owner) : this(owner, 1.0f) { }

        public CompTimeSeconds(Component owner, float multiplier) : base(owner)
        {
            Multiplier = multiplier;
            activated = true;
            startOffsetSeconds = new PreciseFloat(0);
        }

        public CompTimeSeconds(Component owner, float multiplier, float secondsFromActivation) : base(owner)
        {
            Multiplier = multiplier;
            activated = false;
            startOffsetSeconds = new PreciseFloat(secondsFromActivation);
        }

        public CompTimeSeconds(Component owner, float multiplier, PreciseFloat startTime) : base(owner)
        {
            Multiplier = multiplier;
            activated = true;
            startOffsetSeconds = startTime;
        }

        protected override float getValue()
        {
            if(!activated)
            {
                startOffsetSeconds = Context.Time.SecondsFromStart + startOffsetSeconds;
                activated = true;
            }

            return (Context.Time.SecondsFromStart - startOffsetSeconds).FloatValue * Multiplier; 
        }

        public float Multiplier
        {
            get; set;
        }
    }
}
