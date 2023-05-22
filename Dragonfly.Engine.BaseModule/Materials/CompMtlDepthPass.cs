using System;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{ 
    /// <summary>
    /// Render a clip space depth value to the red channel. Can be used for shadow maps or other depth pre-calculations.
    /// </summary>
    public class CompMtlDepthPass : CompMaterial
    {
        public CompMtlDepthPass(Component owner) : base(owner)
        {
            TextureCoords = new MtlModTextureCoords(this);
            Displacement = new MtlModDisplacement(this, null);
            CullMode = Context.GetModule<BaseMod>().Settings.DefaultCullMode;
            DepthBias = MakeParam(0.0f);
            NormalBias = MakeParam(0.0f);
            AlphaMasking = new MtlModAlphaMasking(this, new CompTextureRef(this, Color.White));
        }

        public override string EffectName
        {
            get { return "DepthPass"; }
        }

        public MtlModAlphaMasking AlphaMasking { get; private set; }

        public Param<float> DepthBias { get; private set; }
        
        public Param<float> NormalBias { get; private set; }

        /// <summary>
        /// Texture coords modifiers.
        /// </summary>
        public MtlModTextureCoords TextureCoords { get; private set; }

        public MtlModDisplacement Displacement { get; private set; }

        protected override void UpdateParams()
        {
            Shader.SetParam("depthBias", new Float4(DepthBias, NormalBias, 0.0f, 0.0f));
            Shader.SetParam("shadowAlphaMask", AlphaMasking.Map);
        }
        
    }

}
