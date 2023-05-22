using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompAudioFxDirGradient : Component<AudioEffect>
    {
        TiledFloat3 refPnt1, refPnt2;
        float volumedB1, volumedB2;

        public CompAudioFxDirGradient(Component owner, TiledFloat3 refPnt1, float volumedB1, TiledFloat3 refPnt2, float volumedB2) : base(owner)
        {
            this.refPnt1 = refPnt1;
            this.volumedB1 = volumedB1;
            this.refPnt2 = refPnt2;
            this.volumedB2 = volumedB2;
        }

        protected override AudioEffect getValue()
        {
            CompCamera mainCamera = Context.GetModule<BaseMod>().MainPass.Camera;
            float interpolation = mainCamera.LocalPosition.InterpolatesAt(refPnt1.ToFloat3(mainCamera.Position.Tile), refPnt2.ToFloat3(mainCamera.Position.Tile));

            AudioEffect e;
            e.DeltaPitch = 0;
            e.DeltaDecibels = volumedB1.Lerp(volumedB2, interpolation.Saturate());
            return e;
        }
    }
}
