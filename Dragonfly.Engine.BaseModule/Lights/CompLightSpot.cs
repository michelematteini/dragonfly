using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public class CompLightSpot : CompLight
    {
        public CompLightSpot(Component owner, Float3 color, float intensity, float innerConeAngleRadians, float outerConeAngleRadians) : base(owner, color, intensity)
        {
            Radius = new CompValue<float>(this, 0);
            InnerConeAngleRadians = innerConeAngleRadians.Clamp(0, FMath.PI - FMath.PI / 180);
            OuterConeAngleRadians = outerConeAngleRadians.Clamp(Math.Max(InnerConeAngleRadians, FMath.PI / 180), FMath.PI - FMath.PI / 180);
        }

        public CompLightSpot(Component owner, Float3 color, float intensity, float outerConeAngleRadians) : this(owner, color, intensity, outerConeAngleRadians, outerConeAngleRadians) { }

        public CompLightSpot(Component owner, Float3 color, float intensity) : this(owner, color, intensity, FMath.PI_OVER_4, FMath.PI_OVER_2) { }

        public CompLightSpot(Component owner, float intensity) : this(owner, Float3.One, intensity, FMath.PI_OVER_4, FMath.PI_OVER_2) { }

        public CompValue<float> Radius { get; private set; }

        public float InnerConeAngleRadians { get; set; }

        public float OuterConeAngleRadians { get; set; }

        public override bool HasPosition { get { return true; } }

        public override void AddDebugMesh()
        {
            CompMesh lightPoint = new CompMesh(this);
            lightPoint.MainMaterial = new CompMtlBasic(lightPoint, LightColor.GetValue());
            Primitives.Cone(lightPoint.AsObject3D(), Float3.UnitZ * 0.2f, -Float3.UnitZ, (Float3)0.2, 40);
            lightPoint.CastShadows = false;
        }

        public override AABox GetBoundingBox()
        {
            float clipDistance = GetClippingDistance();
            float coneRadius = FMath.Sin(OuterConeAngleRadians * 0.5f) * clipDistance;
            Float3 lPos = Position, lDir = Direction;
            Cone lightCone = new Cone(lPos, lPos + lDir * clipDistance, coneRadius);

            return lightCone.ToBoundingBox();
        }

    }

}
