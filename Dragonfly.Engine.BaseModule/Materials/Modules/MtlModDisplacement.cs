using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A set of parameters used by materials to support vertex displacement.
    /// </summary>
    public class MtlModDisplacement : MaterialModule
    {
        public MtlModDisplacement(CompMaterial parentMaterial, CompTextureRef displacementMapRef) : base(parentMaterial)
        {
            Scale = MakeParam(0.0f);
            Offset = MakeParam(0.0f);
            if (displacementMapRef != null)
                Map = displacementMapRef;
            else
                Map = new CompTextureRef(Material, Color.Black);

            MonitoredParams.Add(Map);
        }

        public bool Available { get { return Map != null; } }

        /// <summary>
        /// A grayscale map that used to displace vertices along their normals 
        /// </summary>
        public CompTextureRef Map { get; private set; }

        /// <summary> 
        /// A Scaling factor that will be applied to the value read from the displacement map. 
        /// </summary>
        public CompMaterial.Param<float> Scale { get; set; }

        /// <summary>
        /// An offset to be added to the displacement ammount.
        /// </summary>
        public CompMaterial.Param<float> Offset { get; set; }

        protected override void UpdateAdditionalParams(Shader s)
        {
            s.SetParam("displacementMap", Map);
            s.SetParam("displacementScaleOffset", new Float2(Scale, Offset));
        }

        public void CopyFrom(MtlModDisplacement other)
        {
            Scale.Value = other.Scale;
            Offset.Value = other.Offset;
            MonitoredParams.Remove(Map);
            Map = other.Map;
            MonitoredParams.Add(other.Map);
        }


    }
}
