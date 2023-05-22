using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Render a equirectangular environment map to a 2d cubemap where faces are fitted in a 4x2 grid layout with layout:
    /// <para/> Row1 = +X, +Y, +Z, -Z
    /// <para/> Row2 = -X, -Y, -Z, -Z
    /// </summary>
    public class CompMtlEquirectToCube2D : CompMtlImage
    {
        public CompMtlEquirectToCube2D(Component parent) : base(parent)
        {
            ExposureMultiplier = MakeParam(1.0f);
            HorizontalRotation = MakeParam(0.0f);
            DepthBufferEnable = false;
            DepthBufferWriteEnable = false;
            CullMode = Graphics.CullMode.None;
        }

        public override string EffectName => "EquirectToCube2D";

        public Param<float> ExposureMultiplier { get; private set; }

        public Param<float> HorizontalRotation { get; private set; }

        protected override void UpdateParams()
        {
            Shader.SetParam("rgbeTex", Image);
            Shader.SetParam("isInputHDR", Image.IsHdr);
            Shader.SetParam("exposureMul", ExposureMultiplier);
            Float3x3 bgRotation = (Float3x3)Float4x4.RotationY(HorizontalRotation);
            Shader.SetParam("rotationMatrix", bgRotation);
        }
    }
}
