using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Transform a value given by the sum of the channels to a color heatmap. 
    /// A multiplier and an offset can be optionally be specified and applied to the channels before summing them.
    /// </summary>
    public class CompMtlImgHeatmap : CompMtlImageProc
    {
        public CompMtlImgHeatmap(Component parent) : this(parent, Float4.UnitY, 1.0f)
        {
        }

        public CompMtlImgHeatmap(Component parent, Float4 multiplier, float offset) : base(parent, ImgProcessingType.Heatmap) 
        {
            Multiplier = MakeParam(multiplier);
            Offset = MakeParam(offset);
            DepthBufferEnable = false;
            DepthBufferWriteEnable = false;
        }

        /// <summary>
        /// The scalar product between the value of this multiplier and the color value from the image is used as the heatmap input value. 
        /// </summary>
        public Param<Float4> Multiplier { get; private set; }

        /// <summary>
        /// An offset added to the input value for the heatmap
        /// </summary>
        public Param<float> Offset { get; private set; }

        protected override void UpdateParams()
        {
            Shader.SetParam("inputVector1", Multiplier);
            Shader.SetParam("inputVector2", Float4.UnitX * Offset);
            Shader.SetParam("filteredTex", Image);
        }
    }
}
