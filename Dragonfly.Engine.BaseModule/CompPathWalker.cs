using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public class CompPathWalker : Component<Float3>
    {
        private const float DT = 0.01f;

        private Path3D path;
        private float speed;
        private CompTimeSeconds time;

        public CompPathWalker(Component owner, Path3D path, float speed, PreciseFloat startTime) : base(owner)
        {
            this.path = path;
            this.speed = speed;
            time = new CompTimeSeconds(owner, 1.0f, startTime);
        }

        public CompPathWalker(Component owner, Path3D path, float speed, float secondsFromActivation) : base(owner)
        {
            this.path = path;
            this.speed = speed;
            time = new CompTimeSeconds(owner, 1.0f, secondsFromActivation);
        }


        public PathWarkingMode PathWalkingMode { get; set; }

        protected override Float3 getValue()
        {
            switch (PathWalkingMode)
            {
                default: case PathWarkingMode.Position:
                    return GetPosition();
                case PathWarkingMode.Tangent:
                    return GetTangent();
                case PathWarkingMode.Normal:
                    return GetTangent().Cross(Float3.UnitY);
            }
        }

        private Float3 GetPosition()
        {
            return path.GetPositionAt(speed * (time.GetValue()));
        }

        private Float3 GetTangent()
        {
            return path.GetDirectionAt(speed * (time.GetValue()));
        }

        public CompPathWalker Tangent()
        {
            PathWalkingMode = PathWarkingMode.Tangent;
            return this;
        }

    }

    public enum PathWarkingMode
    {
        Position = 0,
        Tangent,
        Normal
    }


}
