using Dragonfly.Graphics;
using Dragonfly.Graphics.Resources;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Dragonfly.Engine.Core
{
    /// <summary>
    /// Engine graphics resource allocator.
    /// </summary>
    public class EngineResourceAllocator
    {
        private IDFGraphics g;

        internal EngineResourceAllocator(IDFGraphics g)
        {
            this.g = g;
        }

        public VertexBuffer CreateVertexBuffer(VertexType vtype, int vertexCount)
        {
            return g.CreateVertexBuffer(vtype, vertexCount);
        }

        public IndexBuffer CreateIndexBuffer(int indexCount)
        {
            return g.CreateIndexBuffer(indexCount);
        }

        public Texture CreateTexture(int width, int height, SurfaceFormat format)
        {
            return g.CreateTexture(width, height, format);
        }

        public Texture CreateTexture<T>(int width, int height, T[] pixelData) where T : struct
        {
            return g.CreateTexture<T>(width, height, pixelData);
        }

        public Texture CreateTexture(string path)
        {
            byte[] fileBytes = File.ReadAllBytes(path);
            return g.CreateTexture(fileBytes);
        }

        public Texture CreateTexture(byte[] fileData)
        {
            return g.CreateTexture(fileData);
        }

        public Texture CreateTexture(Bitmap image)
        {
            MemoryStream imgByteStream = new MemoryStream();
            bool isMemoryBitmap = image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.MemoryBmp);
            image.Save(imgByteStream, isMemoryBitmap ? System.Drawing.Imaging.ImageFormat.Png : image.RawFormat);
            
            byte[] imgBytes = imgByteStream.ToArray();
            return g.CreateTexture(imgBytes);
        }

        public RenderTarget CreateRenderTarget(int width, int height, SurfaceFormat format, bool depthTestSupported)
        {
            return g.CreateRenderTarget(width, height, format, depthTestSupported);
        }

        public RenderTarget CreateRenderTarget(float backBufferSizePercent, SurfaceFormat format, bool depthTestSupported)
        {
            return g.CreateRenderTarget(backBufferSizePercent, format, depthTestSupported);
        }

        public Shader CreateShader(string effectName, ShaderStates states, KeyValuePair<string, string>[] customVariantStates = null, string templateName = "")
        {
            return g.CreateShader(effectName, states, customVariantStates, templateName);
        }

        public Shader CreateShader(Shader parent, ShaderStates states, string templateName)
        {
            return g.CreateShader(parent, states, templateName);
        }

        public IReadOnlyCollection<string> GetShaderTemplates(string effectName)
        {
            return g.GetShaderTemplates(effectName);
        }

        public CommandList CreateCommandList()
        {
            return g.CreateCommandList();
        }
    }

}
