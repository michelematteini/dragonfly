using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompAudioFxFadeIn : Component<AudioEffect>
    {
        CompTimeSeconds fadeInTime;

        public CompAudioFxFadeIn(Component owner, float fadeInSeconds) : base(owner)
        {
            fadeInTime = new CompTimeSeconds(this, 1.0f / fadeInSeconds, new PreciseFloat(0));
        }

        protected override AudioEffect getValue()
        {
            AudioEffect e;
            e.DeltaPitch = 0;
            e.DeltaDecibels = (fadeInTime.GetValue().Saturate() - 1.0f)  * 80.0f;
            return e;
        }
    }
}
