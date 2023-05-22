using System;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public abstract class CompLight : Component
    {
        protected CompLight(Component parent, Float3 color, float intensity) : base(parent)
        {
            LightColor = new CompValue<Float3>(parent, color);
            Intensity = new CompValue<float>(parent, intensity);
            CastShadow = false;
        }

        public CompValue<Float3> LightColor { get; private set; }

        public CompValue<float> Intensity { get; private set; }

        public virtual bool CastShadow { get; set; }

        /// <summary>
        /// True if this light source is located somewhere, false if omnipresent and should not be culled.
        /// </summary>
        public abstract bool HasPosition { get; }

        public float GetClippingDistance()
        {
            if (!HasPosition)
                return float.PositiveInfinity;

            float clipDistance = (float)System.Math.Sqrt(Intensity.GetValue() / Context.GetModule<BaseMod>().Settings.LightsClipIntensity);
            return clipDistance;
        }

        public virtual AABox GetBoundingBox()
        {
            if (!HasPosition)
                return AABox.Infinite;

            float clipDistance = GetClippingDistance();
            Float3 lPos = Position;
            return new AABox(lPos - (Float3)clipDistance, lPos + (Float3)clipDistance);
        }

        public Float3 Position
        {
            get
            {
                return GetTransform().Position.ToFloat3(Context.GetModule<BaseMod>().CurWorldTile);
            }
        }

        public Float3 Direction
        {
            get
            {
                return GetTransform().Value.GetZAxis();
            }
        }

        public virtual void AddDebugMesh() { }
    }
}
