
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A buffer to pass parameters to a shader, implemented with a dynamic texture.
    /// </summary>
    public class CompTextureBuffer : Component, ICompAllocator
    {
        public Float4[] Values { get; private set; }

        public Int2 Size { get; private set; }

        public Texture GpuBuffer { get; private set; }

        public Float4 this[int x, int y]
        {
            get
            {
                return Values[x + y * Size.Width];
            }
            set
            {
                Values[x + y * Size.Width] = value;
            }
        }

        public CompTextureBuffer(Component parent, Int2 sizeInFloat4) : base(parent)
        {
            Size = sizeInFloat4;
            Values = new Float4[sizeInFloat4.X * sizeInFloat4.Y];
            LoadingRequired = true;
        }

        public bool LoadingRequired { get; private set; }

        public void UploadValues()
        {
            if (GpuBuffer != null)
            {
                GpuBuffer.SetData<Float4>(Values);
            }
        }

        public void LoadGraphicResources(EngineResourceAllocator g)
        {
            GpuBuffer = g.CreateTexture(Size.X, Size.Y, Graphics.SurfaceFormat.Float4);
            UploadValues();
            LoadingRequired = false;
        }

        public void ReleaseGraphicResources()
        {
            if (GpuBuffer != null)
            {
                GpuBuffer.Release();
                GpuBuffer = null;
            }
            LoadingRequired = true;
        }
    }

    public static class CompTextureBufferHelpers
    {
        /// <summary>
        /// Set the texture buffer to the shader if available, or do nothing if unavailable.
        /// </summary>
        public static void SetParam(this Shader s, string name, CompTextureBuffer buffer)
        {
            if (buffer == null || buffer.LoadingRequired) 
                return;

            s.SetParam(name, buffer.GpuBuffer);
        }

        /// <summary>
        /// Set the texture buffer as a global if available, or do nothing if unavailable.
        /// </summary>
        public static void SetParam(this EngineGlobals globals, string name, CompTextureBuffer buffer)
        {
            if (buffer == null || buffer.LoadingRequired)
                return;

            globals.SetParam(name, buffer.GpuBuffer);
        }

    }
}
