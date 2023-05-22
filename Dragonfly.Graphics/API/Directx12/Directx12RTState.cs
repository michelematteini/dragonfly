using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using DragonflyGraphicsWrappers.DX12;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx12
{
    internal class Directx12RTState
    {
        private const int MAX_RT_COUNT = 8;

        private DF_Resource12 defaultDepthBuffer, defaultBackbuffer;
        private GraphicResourceID[] renderTargets;
        private GraphicResourceID depthStencil;
        private DF_Resource12[] rtCache;
        private DF_Resource12 dstCache;
        private bool depthEnabled;

        public Directx12RTState()
        {
            renderTargets = new GraphicResourceID[MAX_RT_COUNT];
            rtCache = new DF_Resource12[MAX_RT_COUNT];
            Changed = false;
        }

        public bool Changed { get; set; }
        
        public void SetDefaultBuffers(DF_D3D12Device device)
        {
            defaultBackbuffer = device.GetBackBuffer();
            defaultDepthBuffer = device.GetBackBufferDepth();
            depthEnabled = true;
            ResetTargets();
        }

        public void SetRenderTarget(GraphicResourceID rt, int index)
        {
            Changed = Changed || renderTargets[index] != rt;
            renderTargets[index] = rt;
        }

        public GraphicResourceID GetRenderTarget(int index)
        {
            return renderTargets[index];
        }

        public void SetDepthStencil(GraphicResourceID depthStencil)
        {
            Changed = Changed || this.depthStencil != depthStencil;
            this.depthStencil = depthStencil;
        }

        public bool DepthEnabled
        {
            get
            {
                return depthEnabled;
            }
            set
            {
                Changed = Changed || (value != depthEnabled);
                depthEnabled = value;
            }
        }

        public int GetBindIndex(GraphicResourceID rt)
        {
            for (int i = 0; i < MAX_RT_COUNT; i++)
                if (renderTargets[i] == rt) return i;
            return -1;
        }

        public void ResetTargets()
        {
            depthEnabled = true;
            SetRenderTarget(null, 0);
            for (int i = 1; i < MAX_RT_COUNT; i++)
                SetRenderTarget(null, i);

            SetDepthStencil(null);
        }


        public void Commit(DF_CommandList12 cmdList, Dictionary<GraphicResourceID, Directx12Graphics.RTInfo> rtPool)
        {
            dstCache = null;
            Directx12Graphics.RTInfo rtInfo;

            if (depthEnabled)
            {
                if (depthStencil == null)
                    dstCache = defaultDepthBuffer;
                else
                {
                    if (rtPool.TryGetValue(depthStencil, out rtInfo))
                        dstCache = rtInfo.DepthBuffer;
                }
            }

            for (int i = 0; i < MAX_RT_COUNT; i++)
                rtCache[i] = null;

            for (int i = 0; i < MAX_RT_COUNT; i++)
            {
                if (i > 0 && renderTargets[i] == null) 
                    break;

                if (renderTargets[i] == null)
                    rtCache[i] = defaultBackbuffer;
                else if (rtPool.TryGetValue(renderTargets[i], out rtInfo))
                    rtCache[i] = rtInfo.Resource;
                else
                {
#if DEBUG
                    throw new System.Exception("DX12 RTState reference a render target that is no longer available!");
#else
                    break; // stop filling render targets if an invalid one is encountered
#endif
                }
            }

            cmdList.SetRenderTargets(rtCache, dstCache);
            Changed = false;
        }

        public void ClearSurfaces(DF_CommandList12 cmdList, Float4 clearValue, ClearFlags flags)
        {
            if ((flags & ClearFlags.ClearTargets) == ClearFlags.ClearTargets)
            {
                for (int i = 0; i < MAX_RT_COUNT; i++)
                {
                    if (rtCache[i] == null)
                        break;
                    cmdList.ClearRenderTargetView(rtCache[i], clearValue.R, clearValue.G, clearValue.B, clearValue.A);
                }
            }

            if (dstCache != null && (flags & ClearFlags.ClearDepth) == ClearFlags.ClearDepth) 
                cmdList.ClearDepthStencilView(dstCache, 0.0f);
        }

        public void ClearSurfaces(DF_CommandList12 cmdList, Float4 clearValue, ClearFlags flags, int x1, int y1, int x2, int y2)
        {
            if ((flags & ClearFlags.ClearTargets) == ClearFlags.ClearTargets)
            {
                for (int i = 0; i < MAX_RT_COUNT; i++)
                {
                    if (rtCache[i] == null)
                        break;
                    cmdList.ClearRenderTargetView(rtCache[i], clearValue.R, clearValue.G, clearValue.B, clearValue.A, x1, y1, x2, y2);
                }
            }

            if (dstCache != null && (flags & ClearFlags.ClearDepth) == ClearFlags.ClearDepth)
                cmdList.ClearDepthStencilView(dstCache, 0.0f);
        }
    }
}
