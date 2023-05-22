using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    internal class CompMtlBrdfLUT : CompMtlImage
    {
        public CompMtlBrdfLUT(Component parent) : base(parent) 
        {
        }

        public override string EffectName => "BrdfSplitSumLUT";

        protected override void UpdateParams() { }
    }
}
