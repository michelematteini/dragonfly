using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Randomly changes the volume of an audio track in time.
    /// </summary>
    public class CompAudioFxVolumeRnd : Component<AudioEffect>
    {
        float volumeChangesPerSecond, deltadBMin, deltadBMax;
        PreciseFloat lastUpdated;
        float curVolume, nextVolume;
        FRandom rnd;

        public CompAudioFxVolumeRnd(Component owner, float volumeChangesPerSecond, float deltadBMin, float deltadBMax) : base(owner)
        {
            this.volumeChangesPerSecond = volumeChangesPerSecond;
            this.deltadBMin = deltadBMin;
            this.deltadBMax = deltadBMax;
            lastUpdated = new PreciseFloat(0);
            rnd = new FRandom();
            UpdateRandomVolume();
            UpdateRandomVolume();
        }

        private void UpdateRandomVolume()
        {
            curVolume = nextVolume;
            nextVolume = deltadBMin.Lerp(deltadBMax, rnd.NextFloat());
            lastUpdated = Context.Time.SecondsFromStart;
        }

        protected override AudioEffect getValue()
        {
            PreciseFloat curTime = Context.Time.SecondsFromStart;
            if ((curTime - lastUpdated) * volumeChangesPerSecond > 1.0f)
                UpdateRandomVolume();

            AudioEffect e;
            e.DeltaPitch = 0;
            e.DeltaDecibels = curVolume.Lerp(nextVolume, (curTime - lastUpdated).FloatValue * volumeChangesPerSecond);
            return e;
        }
    }
}
