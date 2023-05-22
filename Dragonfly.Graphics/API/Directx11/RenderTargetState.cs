using System;
using System.Collections.Generic;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using DragonflyGraphicsWrappers.DX11;

namespace Dragonfly.Graphics.API.Directx11
{
    internal class RenderTargetState
    {
        private const int MAX_RT_COUNT = 8;
        private static GraphicResourceID DEFAULT_BB_ID = new GraphicResourceID(-5);
        private static GraphicResourceID DEFAULT_DB_ID = new GraphicResourceID(-6);

        private DF_Texture11 defaultDepthBuffer, defaultBackbuffer;
        private GraphicResourceID[] renderTargets;
        private GraphicResourceID depthStencil;
        private DF_Texture11[] rtCache;
        private DF_Texture11 dstCache;

        public RenderTargetState()
        {
            renderTargets = new GraphicResourceID[MAX_RT_COUNT]; 
            rtCache = new DF_Texture11[MAX_RT_COUNT];
            Changed = false;
        }

        public void SetDefaultBuffers(DF_D3D11Device device)
        {
            this.defaultBackbuffer = device.GetBackBuffer();
            this.defaultDepthBuffer = device.GetBackBufferDepth();
            ResetTargets();
        }

        public void SetRenderTarget(GraphicResourceID rt, int index)
        {
            Changed = Changed || renderTargets[index] != rt;
            renderTargets[index] = rt;      
        }

        public GraphicResourceID GetRenderTarget(int index)
        {
            if (renderTargets[index] == DEFAULT_BB_ID)
                return null;

            return renderTargets[index];
        }

        public void SetDepthStencil(GraphicResourceID depthStencil)
        {
            Changed = Changed || this.depthStencil != depthStencil;
            this.depthStencil = depthStencil;
        }

        public int GetBindIndex(GraphicResourceID rt)
        {
            for (int i = 0; i < MAX_RT_COUNT; i++)
                if (renderTargets[i] == rt) return i;
            return -1;
        }

        public void ResetTargets()
        {
            SetRenderTarget(DEFAULT_BB_ID, 0);
            for (int i = 1; i < MAX_RT_COUNT; i++)
                SetRenderTarget(null, i);

            SetDepthStencil(DEFAULT_DB_ID);
        }

        public bool Changed { get; set; }

        public void Commit(DF_D3D11DeviceContext context, Dictionary<GraphicResourceID, DF_Texture11> textures, Dictionary<GraphicResourceID, DF_Texture11> depthBuffers)
        {
            dstCache = null;
            if (depthStencil != null)
            {
                if (depthStencil == DEFAULT_DB_ID)
                    dstCache = defaultDepthBuffer;
                else depthBuffers.TryGetValue(depthStencil, out dstCache);
            }

            for (int i = 0; i < MAX_RT_COUNT; i++)
                rtCache[i] = null;

            for (int i = 0; i < MAX_RT_COUNT; i++)
            {
                if (renderTargets[i] == null) break;

                if (renderTargets[i] == DEFAULT_BB_ID)
                    rtCache[i] = defaultBackbuffer;
                else if (!textures.TryGetValue(renderTargets[i], out rtCache[i]))
                    break;
            }

            context.SetRenderTargets(dstCache, rtCache);
            Changed = false;
        }

        public void ClearSurfaces(DF_D3D11DeviceContext context, Float4 clearValue, ClearFlags clearFlags)
        {
            if ((clearFlags & ClearFlags.ClearTargets) == ClearFlags.ClearTargets)
            {
                for (int i = 0; i < MAX_RT_COUNT; i++)
                {
                    if (rtCache[i] == null) break;
                    context.Clear(rtCache[i], clearValue.R, clearValue.G, clearValue.B, clearValue.A);
                }
            }

            if (dstCache != null && (clearFlags & ClearFlags.ClearDepth) == ClearFlags.ClearDepth) 
                context.ClearDepth(dstCache, 0.0f);
        }

        public void ClearSurfaces(DF_D3D11DeviceContext context, Float4 clearValue, ClearFlags clearFlags, int x1, int y1, int x2, int y2)
        {
            if ((clearFlags & ClearFlags.ClearTargets) == ClearFlags.ClearTargets)
            {
                for (int i = 0; i < MAX_RT_COUNT; i++)
                {
                    if (rtCache[i] == null) break;
                    context.ClearView(rtCache[i], clearValue.R, clearValue.G, clearValue.B, clearValue.A, x1, y1, x2, y2);
                }
            }

            if (dstCache != null && (clearFlags & ClearFlags.ClearDepth) == ClearFlags.ClearDepth)
                context.ClearDepth(dstCache, 0.0f);
        }
    }
}
