using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using DragonflyGraphicsWrappers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Dragonfly.Graphics.API.Common
{
    internal abstract class DirectxGraphics: DFGraphics
    {
        #region Graphic state

        protected bool IsFullScreen { get; set; }

        protected IntPtr CurTarget { get; set; }

        public override int CurWidth { get; protected set; }

        public override int CurHeight { get; protected set; }

        protected bool AntialiasingEnabled { get; set; }

        #endregion

        public DirectxGraphics(DFGraphicSettings settings, IGraphicsAPI api) : base(settings, api)
        {
            CurTarget = settings.TargetControl;
            IsFullScreen = settings.FullScreen;
            CurWidth = settings.PreferredWidth;
            CurHeight = settings.PreferredHeight;
            AntialiasingEnabled = settings.HardwareAntiAliasing;
        }

        protected override GraphicResourceID createRenderTarget(int width, int height, SurfaceFormat format, bool depthTestSupported)
        {
            // collect data for rt creation
            RenderTargetParams rtData = new RenderTargetParams();
            rtData.UsePercent = false;
            rtData.Width = (uint)width;
            rtData.Height = (uint)height;
            rtData.Format = DirectxUtils.SurfaceFormatToDX(format);
            rtData.Antialiased = format == SurfaceFormat.AntialiasedColor;
            rtData.HasDepthBuffer = depthTestSupported;

            return CreateDirectxRenderTarget(rtData);
        }

        protected override GraphicResourceID createRenderTarget(float backBufferSizePercent, SurfaceFormat format, bool depthTestSupported)
        {
            // collect data for rt creation
            RenderTargetParams rtData = new RenderTargetParams();
            rtData.UsePercent = true;
            rtData.BackBufferPercent = backBufferSizePercent;
            rtData.Format = DirectxUtils.SurfaceFormatToDX(format);
            rtData.Antialiased = format == SurfaceFormat.AntialiasedColor;
            rtData.HasDepthBuffer = depthTestSupported;

            return CreateDirectxRenderTarget(rtData);
        }

        protected abstract GraphicResourceID CreateDirectxRenderTarget(RenderTargetParams rtParams);

        protected void UpdateRtDimensions(ref RenderTargetParams rt)
        {
            // compute current rt dimensions
            rt.CurWidth = rt.Width;
            rt.CurHeight = rt.Height;
            if (rt.UsePercent)
            {
                rt.CurWidth = (uint)((float)CurWidth * rt.BackBufferPercent);
                rt.CurHeight = (uint)((float)CurHeight * rt.BackBufferPercent);
                if (rt.CurWidth < 2) rt.CurWidth = 2;
                if (rt.CurHeight < 2) rt.CurHeight = 2;
            }
        }

    }

    internal struct RenderTargetParams
    {
        public bool UsePercent;
        public float BackBufferPercent;
        public uint Width;
        public uint Height;
        public DF_SurfaceFormat Format;
        public uint CurWidth, CurHeight;
        public bool Antialiased;
        public bool HasDepthBuffer;
        public GraphicResourceID OverrideZBuffer;
    }

}
