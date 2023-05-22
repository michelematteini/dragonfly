using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Copy the input Cube2D image in RGBE format to another texture of half the size, interpolating RGBE values.
    /// </summary>
    public class CompMtlCube2DHdrMipmap : CompMtlImage
    {
        public enum FilterType
        {
            Bilinear,
            GGX
        }

        public CompMtlCube2DHdrMipmap(Component parent, FilterType filter) : base(parent)
        {
            Filter = filter;
            DepthBufferEnable = false;
            DepthBufferWriteEnable = false;
            CullMode = Graphics.CullMode.None;
            Roughness = MakeParam(0.0f);
        }

        public CompMtlCube2DHdrMipmap(Component parent) : this(parent, FilterType.Bilinear) { }

        public FilterType Filter { get; set; }

        /// <summary>
        /// If the filter type is set to GGX, this value is used as roughness parameter in the GGX distribution.
        /// </summary>
        public Param<float> Roughness { get; private set; }

        public override string EffectName 
        { 
            get 
            {
                switch (Filter)
                {
                    default:
                    case FilterType.Bilinear:
                        return "Cube2DMipMapHDR"; 
                    case FilterType.GGX:
                        return "Cube2DMipMapHDR_GGX";
                }
            } 
        }

        protected override void UpdateParams()
        {
            Shader.SetParam("rgbeTex", Image);
            Shader.SetParam("roughness", Roughness);
        }
    }
}
