using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    public class MtlModTextureCoords : MaterialModule
    {
        public MtlModTextureCoords(CompMaterial parentMaterial) : base(parentMaterial)
        {
            Scale = MakeParam(Float2.One);
            Offset = MakeParam(Float2.Zero);
        }

        /// <summary>
        /// A scaling value to be applied to the vertex texture coords.
        /// </summary>
        public CompMaterial.Param<Float2> Scale;

        /// <summary>
        /// An offset to be added to the vertex texture coords.
        /// </summary>
        public CompMaterial.Param<Float2> Offset;

        protected override void UpdateAdditionalParams(Shader s)
        {
            s.SetParam("texCoordsScaleOffset", new Float4(Scale, Offset));
        }

        public void CopyFrom(MtlModTextureCoords other)
        {
            Scale.Value = other.Scale;
            Offset.Value = other.Offset;
        }

    }
}
