using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.Terrain
{
    public class CompMtlFlatDataSrc : CompMaterial
    {
        private string effectName;

        public CompMtlFlatDataSrc(Component parent, string effectName, CompTerrainCurvature curvature, TiledRect3 tileArea) : base(parent)
        {
            this.effectName = effectName;
            DataSrc = new MtlModTerrainDataSrc(this, curvature, tileArea);
        }

        public MtlModTerrainDataSrc DataSrc { get; private set; }

        public Float3 DefaultColor { get; set; }

        public override string EffectName { get { return effectName; } }

        protected override void UpdateParams()
        {
            Shader.SetParam("albedo", DefaultColor);
        }
    }
}
