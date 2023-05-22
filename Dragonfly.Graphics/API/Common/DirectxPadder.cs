using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Common
{
    /// <summary>
    /// Used to pad shader uniforms as requested from the DirectX shader compiler.
    /// </summary>
    internal class DirectxPadder
    {
        private Dictionary<int, float[]> floatBuffers;
        private Dictionary<int, int[]> intBuffers;

        public DirectxPadder()
        {
            floatBuffers = new Dictionary<int, float[]>();
            intBuffers = new Dictionary<int, int[]>();
        }

        private float[] PrepareFloatBuffer(int size)
        {
            float[] buffer = null;
            if(!floatBuffers.TryGetValue(size, out buffer))
            {
                buffer = new float[size];
                floatBuffers[size] = buffer;
            }

            return buffer;
        }

        private int[] PrepareIntBuffer(int size)
        {
            int[] buffer = null;
            if (!intBuffers.TryGetValue(size, out buffer))
            {
                buffer = new int[size];
                intBuffers[size] = buffer;
            }

            return buffer;
        }

        public int[] Pad(int value)
        {
            int[] buffer = PrepareIntBuffer(4);
            buffer[0] = value;
            buffer[1] = 0;
            buffer[2] = 0;
            buffer[3] = 0;
            return buffer;
        }

        public int[] Pad(Int3 value)
        {
            int[] buffer = PrepareIntBuffer(4);
            buffer[0] = value.X;
            buffer[1] = value.Y;
            buffer[2] = value.Z;
            buffer[3] = 0;
            return buffer;
        }

        public int[] Pad(bool value)
        {
            int[] buffer = PrepareIntBuffer(4);
            buffer[0] = value ? 1 : 0;
            buffer[1] = 0;
            buffer[2] = 0;
            buffer[3] = 0;
            return buffer;
        }

        public float[] Pad(float value)
        {
            float[] buffer = PrepareFloatBuffer(4);
            buffer[0] = value;
            buffer[1] = 0;
            buffer[2] = 0;
            buffer[3] = 0;
            return buffer;
        }

        public float[] Pad(Float2 value)
        {
            float[] buffer = PrepareFloatBuffer(4);
            buffer[0] = value.X;
            buffer[1] = value.Y;
            buffer[2] = 0;
            buffer[3] = 0;
            return buffer;
        }

        public float[] Pad(Float3 value)
        {
            float[] buffer = PrepareFloatBuffer(4);
            buffer[0] = value.X;
            buffer[1] = value.Y;
            buffer[2] = value.Z;
            buffer[3] = 0;
            return buffer;
        }

        public float[] Pad(Float4 value)
        {
            float[] buffer = PrepareFloatBuffer(4);
            buffer[0] = value.X;
            buffer[1] = value.Y;
            buffer[2] = value.Z;
            buffer[3] = value.W;
            return buffer;
        }

        public int[] Pad(int[] value)
        {
            int[] paddedArray = PrepareIntBuffer(value.Length * 4);
            for (int i = 0; i < paddedArray.Length;)
            {
                int vi = i / 4;
                paddedArray[i++] = value[vi];
                paddedArray[i++] = 0;
                paddedArray[i++] = 0;
                paddedArray[i++] = 0;
            }

            return paddedArray;
        }

        public float[] PadAsFloat(int[] value)
        {
            float[] paddedArray = PrepareFloatBuffer(value.Length * 4);
            for (int i = 0; i < paddedArray.Length;)
            {
                int vi = i / 4;
                paddedArray[i++] = value[vi];
                paddedArray[i++] = 0;
                paddedArray[i++] = 0;
                paddedArray[i++] = 0;
            }

            return paddedArray;
        }

        public float[] Pad(float[] value)
        {
            float[] paddedArray = PrepareFloatBuffer(value.Length * 4);
            for (int i = 0; i < paddedArray.Length;)
            {
                int vi = i / 4;
                paddedArray[i++] = value[vi];
                paddedArray[i++] = 0;
                paddedArray[i++] = 0;
                paddedArray[i++] = 0;
            }

            return paddedArray;
        }

        public float[] Pad(Float2[] value)
        {
            float[] paddedArray = PrepareFloatBuffer(value.Length * 4);
            for (int i = 0; i < paddedArray.Length;)
            {
                int vi = i / 4;
                paddedArray[i++] = value[vi].X;
                paddedArray[i++] = value[vi].Y;
                paddedArray[i++] = 0;
                paddedArray[i++] = 0;
            }

            return paddedArray;
        }

        public float[] Pad(Float3[] value)
        {
            float[] paddedArray = PrepareFloatBuffer(value.Length * 4);
            for (int i = 0; i < paddedArray.Length;)
            {
                int vi = i / 4;
                paddedArray[i++] = value[vi].X;
                paddedArray[i++] = value[vi].Y;
                paddedArray[i++] = value[vi].Z;
                paddedArray[i++] = 0;
            }

            return paddedArray;
        }

        public float[] Pad(Float4[] value)
        {
            float[] paddedArray = PrepareFloatBuffer(value.Length * 4);
            for (int i = 0; i < paddedArray.Length;)
            {
                int vi = i / 4;
                paddedArray[i++] = value[vi].X;
                paddedArray[i++] = value[vi].Y;
                paddedArray[i++] = value[vi].Z;
                paddedArray[i++] = value[vi].W;
            }

            return paddedArray;
        }

        public float[] Pad(Float4x4 value)
        {
            float[] paddedArray = PrepareFloatBuffer(16);
            value.CopyTo(paddedArray, 0);
            return paddedArray;
        }

        public float[] Pad(Float3x3 value)
        {
            float[] paddedArray = PrepareFloatBuffer(12);

            int i = 0;
            paddedArray[i++] = value.A11;
            paddedArray[i++] = value.A21;
            paddedArray[i++] = value.A31;
            paddedArray[i++] = 0;

            paddedArray[i++] = value.A12;
            paddedArray[i++] = value.A22;
            paddedArray[i++] = value.A32;
            paddedArray[i++] = 0;

            paddedArray[i++] = value.A13;
            paddedArray[i++] = value.A23;
            paddedArray[i++] = value.A33;
            paddedArray[i++] = 0;

            return paddedArray;
        }

        public float[] Pad(Float4x4[] value)
        {
            float[] paddedArray = PrepareFloatBuffer(value.Length * 16);
            for (int i = 0; i < value.Length; i++)
            {
                value[i].CopyTo(paddedArray, i * 16);
            }
            return paddedArray;
        }

    }
}
