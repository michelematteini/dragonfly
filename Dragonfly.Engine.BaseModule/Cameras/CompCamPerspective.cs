using System;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public class CompCamPerspective : CompCamera, ICompResizable
    {
        public CompCamPerspective(Component parent) : base(parent)
        {
            NearPlane = 0.1f;
            FarPlane = 1000.0f;

            FOV = new CompValue<float>(this, FMath.PI_OVER_4);
            AspectRatio = new CompValue<float>(this, 1.0f);
            AutoAspectRatio = true;
            ScreenResized(Context.TargetWindow.Width, Context.TargetWindow.Height);
        }

        /// <summary>
        /// The total vertical FOV of this camera, in radians.
        /// </summary>
        public CompValue<float> FOV { get; private set; }

        public CompValue<float> AspectRatio { get; private set; }

        public bool AutoAspectRatio { get; set; }
        
        public float NearPlane { get; set; }
        
        public float FarPlane { get; set; }

        public void ScreenResized(int width, int height)
        {
            if (AutoAspectRatio)
                AspectRatio.Set((float)width / height);
        }

        protected override Float4x4 getValue()
        {
            return Float4x4.Perspective(FOV.GetValue(), AspectRatio.GetValue(), NearPlane, FarPlane);
        }
    }
}
