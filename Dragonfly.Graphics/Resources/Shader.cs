using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;

namespace Dragonfly.Graphics.Resources
{
    public abstract class Shader : GraphicResource
    {
        protected Shader(GraphicResourceID resID, string effectName, string variantID, ShaderStates states, string templateName, Shader parent)
            : base(resID)
        {
            EffectName = effectName;
            VariantID = variantID;
            States = states;
            TemplateName = templateName;
            Parent = parent;
        }

        public string EffectName { get; private set; }

        public string TemplateName { get; private set; }

        public string VariantID { get; private set; }

        public ShaderStates States { get; private set; }

        public Shader Parent { get; private set; }

        public abstract void SetParam(string name, bool value);

        public abstract void SetParam(string name, int value);

        public abstract void SetParam(string name, float value);
		
		public abstract void SetParam(string name, Float2 value);
		
		public abstract void SetParam(string name, Float3 value);
		
		public abstract void SetParam(string name, Float4 value);

        public abstract void SetParam(string name, Float4x4 value);

        public abstract void SetParam(string name, Float3x3 value);

        public abstract void SetParam(string name, Int3 value);

        public abstract void SetParam(string name, int[] values);

        public abstract void SetParam(string name, float[] values);

        public abstract void SetParam(string name, Float2[] values);

        public abstract void SetParam(string name, Float3[] values);

        public abstract void SetParam(string name, Float4[] values);

        public abstract void SetParam(string name, Float4x4[] values);

        public abstract void SetParam(string name, Texture value);

        public abstract void SetParam(string name, RenderTarget value);
    }

    public enum BlendMode
    {
        Opaque = 0,
        AlphaBlend
    }

    public struct ShaderStates : IEquatable<ShaderStates>
    {
        public bool DepthBufferEnable, DepthBufferWriteEnable;
        public FillMode FillMode;
        public CullMode CullMode;
        public BlendMode BlendMode;

        public static readonly ShaderStates Default = new ShaderStates()
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,
            BlendMode = BlendMode.Opaque,
            FillMode = FillMode.Solid,
            CullMode = CullMode.CounterClockwise
        };

        public override bool Equals(object obj)
        {
            if(obj is ShaderStates s)
                return Equals(s);
            return false;
        }

        public bool Equals(ShaderStates other)
        {
            return
                DepthBufferEnable == other.DepthBufferEnable &&
                DepthBufferWriteEnable == other.DepthBufferWriteEnable &&
                BlendMode == other.BlendMode &&
                FillMode == other.FillMode &&
                CullMode == other.CullMode;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Reset();
            hash.Add(DepthBufferEnable);
            hash.Add(DepthBufferWriteEnable);
            hash.Add(BlendMode, 4);
            hash.Add(FillMode, 4);
            hash.Add(CullMode, 4);
            return hash.Resolve();
        }

        public static bool operator ==(ShaderStates a, ShaderStates b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ShaderStates a, ShaderStates b)
        {
            return !(a == b);
        }
    }
}
