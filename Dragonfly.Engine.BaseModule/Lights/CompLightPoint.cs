using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public class CompLightPoint : CompLight
    {
        public CompLightPoint(Component owner, Float3 color, float intensity) : base(owner, color, intensity)
        {
            Radius = new CompValue<float>(this, 0);
        }

        public CompLightPoint(Component owner) : this(owner, Float3.One, 1) { }

        public CompLightPoint(Component owner, Float3 color) : this(owner, color, 1) { }

        public CompValue<float> Radius { get; private set; }

        public override bool HasPosition { get { return true; } }

        public override void AddDebugMesh()
        {
            CompTransformStack lightTransfNode = new CompTransformStack(this);
            lightTransfNode.PushScale(new CompFunction<float>(lightTransfNode, () => Radius.GetValue() == 0 ? 0.1f : Radius.GetValue()));

            CompMesh lightPoint = new CompMesh(lightTransfNode);
            lightPoint.MainMaterial = new CompMtlBasic(lightPoint, LightColor.GetValue());
            Primitives.Spheroid(lightPoint.AsObject3D(), Float3.Zero, Float3.One, 40);
            lightPoint.CastShadows = false;
        }
    }
}
