using System;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompCamOrthographic : CompCamera, ICompResizable
    {
        public CompCamOrthographic(Component parent, float height) : this(parent, height, height, true) { }

        public CompCamOrthographic(Component parent, float width, float height) : this(parent, width, height, false) { }

        private CompCamOrthographic(Component parent, float width, float height, bool autoAdjustWidth) : base(parent)
        {
            NearPlane = 0.1f;
            FarPlane = 10000.0f;
            Width = new CompValue<float>(this, width);
            Height = new CompValue<float>(this, height);
            AutoAspectRatio = autoAdjustWidth;
            ScreenResized(Context.TargetWindow.Width, Context.TargetWindow.Height);
        }

        public CompValue<float> Width { get; private set; }

        public CompValue<float> Height { get; private set; }

        public float AspectRatio
        {
            get
            {
                return Width.GetValue() / Height.GetValue();
            }
            set
            {
                Width.Set(Height.GetValue() * value);
            }
        }

        public bool AutoAspectRatio { get; set; }

        public float NearPlane { get; set; }

        public float FarPlane { get; set; }

        public void ScreenResized(int width, int height)
        {
            if (AutoAspectRatio)
                AspectRatio = (float)width / height;
        }

        protected override Float4x4 getValue()
        {
            return Float4x4.Orthographic(Width.GetValue(), Height.GetValue(), NearPlane, FarPlane);
        }
    }
}
