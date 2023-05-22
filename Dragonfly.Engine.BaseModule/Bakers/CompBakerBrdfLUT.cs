using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Bake the GGX brdf precalculated split-sub LUT.
    /// </summary>
    public class CompBakerBrdfLUT : Component
    {
        public CompBakerBrdfLUT(Component parent, Int2 resolution) : base(parent)
        {
            CompScreenPass lutGenerationPass = new CompScreenPass(this, Name + ID + "_BRDF_LUT", resolution, new CompMtlBrdfLUT(this));
            Baker = new CompBaker(this, lutGenerationPass.Pass, null, new CompEvent(this, () => true));
        }

        public CompBaker Baker { get; private set; }
    }
}
