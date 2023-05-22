using System;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Graphics;

namespace Dragonfly.Engine.Core
{
    public class CompRenderBuffer : Component, ICompAllocator
    {
        private SurfaceFormat[] formats;
        private RenderTarget[] renderTargets;
        private int preferredWidth, preferredHeight;

        public CompRenderBuffer(Component parent, SurfaceFormat[] formats, RenderBufferResizeStyle resizeStyle) : base(parent)
        {
            if (formats == null || formats.Length == 0) throw new ArgumentNullException("formats");
            ResizeStyle = resizeStyle;
            this.formats = formats;
            LoadingRequired = true;
        }

        public CompRenderBuffer(Component parent, SurfaceFormat[] formats, int width, int height) : base(parent)
        {
            if (formats == null || formats.Length == 0) throw new ArgumentNullException("formats");
            preferredWidth = width;
            preferredHeight = height;
            this.formats = formats;
            LoadingRequired = true;
        }

        public CompRenderBuffer(Component parent, SurfaceFormat format, RenderBufferResizeStyle resizeStyle) : this(parent, new SurfaceFormat[] { format }, resizeStyle) { }

        public CompRenderBuffer(Component parent, SurfaceFormat format, int width, int height) : this(parent, new SurfaceFormat[] { format }, width, height) { }

        /// <summary>
        /// Creates a render buffer with the same surface format and size of the screen.
        /// </summary>
        public CompRenderBuffer(Component parent) : this(parent, new SurfaceFormat[] { SurfaceFormat.Color }, RenderBufferResizeStyle.MatchBackbuffer) { }

        public RenderBufferResizeStyle ResizeStyle { get; private set; }

        public int Width { get { return Resolution.Width; } }

        public int Height { get { return Resolution.Height; } }

        public Int2 Resolution
        {
            get
            {
                if (IsFixedSize)
                    return new Int2(preferredWidth, preferredHeight);
                else if (!LoadingRequired)
                    return new Int2(renderTargets[0].Width, renderTargets[0].Height);
                else
                    return new Int2();
            }
        }

        public bool IsFixedSize { get { return preferredWidth > 0 && preferredHeight > 0; } }

        public RenderTarget this[int surfaceIndex]
        {
            get 
            {
                if (LoadingRequired)
                    return null;

                return renderTargets[surfaceIndex]; 
            }
        }

        public SurfaceFormat GetSurfaceFormat(int surfaceIndex)
        {
            return formats[surfaceIndex];
        }

        public int SurfaceCount
        {
            get
            {
                return formats.Length;
            }
        }

        public bool LoadingRequired
        {
            get; private set;
        }

        public void LoadGraphicResources(EngineResourceAllocator g)
        {
            renderTargets = new RenderTarget[formats.Length];
            float sizePercent = 1.0f;
            switch (ResizeStyle)
            {
                case RenderBufferResizeStyle.HalfBackbuffer: sizePercent = 0.50f; break;
                case RenderBufferResizeStyle.BackbufferOver3: sizePercent = 0.33333333f; break;
                case RenderBufferResizeStyle.BackbufferOver4: sizePercent = 0.25f; break;
            }

            for (int i = 0; i < formats.Length; i++)
            {
                if (IsFixedSize) renderTargets[i] = g.CreateRenderTarget(preferredWidth, preferredHeight, formats[i], i == 0);
                else renderTargets[i] = g.CreateRenderTarget(sizePercent, formats[i], i == 0);
                if (i > 0) renderTargets[i].SetDepthWriteTarget(renderTargets[0]);
            }
            LoadingRequired = false;
        }

        public void ReleaseGraphicResources()
        {
            if (renderTargets != null)
            {
                for (int i = 0; i < renderTargets.Length; i++)
                {
                    renderTargets[i].Release();
                }
            }
            LoadingRequired = true;
        }

        public override string ToString()
        {
            string descr = string.Empty;
            if (formats != null)
            {
                descr = formats[0].ToString();
                for (int i = 1; i < formats.Length; i++)
                    descr += (", " + formats[i]);
            }
            return base.ToString() + "[ " + descr + " ]";
        }
    }

    public enum RenderBufferResizeStyle
    {
        MatchBackbuffer,
        HalfBackbuffer,
        BackbufferOver3,
        BackbufferOver4
    }

}