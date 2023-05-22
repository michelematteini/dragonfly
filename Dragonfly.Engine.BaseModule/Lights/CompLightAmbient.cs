using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;


namespace Dragonfly.BaseModule
{
    public class CompLightAmbient : CompLight
    {
        public CompLightAmbient(Component owner, Float3 color, float intensity) : base(owner, color, intensity)
        {
            
        }

        public CompLightAmbient(Component owner) : this(owner, Color.Gray.ToFloat3(), 1) { }

        public CompLightAmbient(Component owner, Float3 color) : this(owner, color, 1) { }

        public override bool HasPosition { get { return false; } }

        public override bool CastShadow { get { return false; } set { } }
    }
}
