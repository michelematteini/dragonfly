using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Initialize an in-shader lut based random generator.
    /// </summary>
    public class CompRandom: Component, ICompUpdatable
    {
        public CompTextureRef NoiseLut { get; private set; }

        internal CompRandom(Component parent) : base(parent)
        {
            NoiseLut = new CompTextureRef(this, Color.Gray);
            NoiseLut.SetSource("textures/noise.png");
        }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        public void Update(UpdateType updateType)
        {
            Context.Scene.Globals.SetParam("randomLut", NoiseLut);
        }
    }
}
