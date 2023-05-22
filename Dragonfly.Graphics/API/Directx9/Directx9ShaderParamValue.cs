
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.Graphics.API.Directx9
{
    struct Directx9ShaderParamValue
    {
        public float[] FloatValues;
        public int[] IntValues;
        public bool[] BoolValues;
        public Texture TextureValue;
        public RenderTarget RtValue;
        public uint Address;

        public Directx9ShaderParamValue(float[] values) : this()
        {
            FloatValues = new float[values.Length];
            Array.Copy(values, FloatValues, values.Length);
        }

        public Directx9ShaderParamValue(int[] values) : this()
        {
            IntValues = new int[values.Length];
            Array.Copy(values, IntValues, values.Length);
        }

        public Directx9ShaderParamValue(bool value) : this()
        {
            BoolValues = new bool[] { value };
        }
    }
}
