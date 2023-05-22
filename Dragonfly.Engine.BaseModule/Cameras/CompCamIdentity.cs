using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompCamIdentity : CompCamera
    {
        public CompCamIdentity(Component owner) : base(owner)
        {
        }

        protected override Float4x4 getValue()
        {
            return Float4x4.Identity;
        }
    }
}
