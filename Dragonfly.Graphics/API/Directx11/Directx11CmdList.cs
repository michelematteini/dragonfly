using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using DragonflyGraphicsWrappers.DX11;
using System;
using System.Collections.Generic;

namespace Dragonfly.Graphics.API.Directx11
{
    /// <summary>
    /// Used to record a set of commands that can be then played on the DX11 device
    /// </summary>
    internal class Directx11CmdList
    {
        public GraphicResourceID ID;
        public DF_D3D11DeviceContext Context;

        public IndexBuffer IndexBuffer;
        public VertexBuffer VertexBuffer;
        public RenderTargetState RTState;
        public ViewportState Viewport;
        public DF_Buffer11 LastBindedLocalCB; // last CB resource binded to the GPU
        public CBufferInstance LastUpdatedLocalCB; // last cbuffer instance updated on the GPU with its CPU state.
        public GraphicResourceID[] TexRegToRtID; // texture reg ID -> rt ID (or null if no rt is binded)
        public GraphicResourceID[] TexRegToSurfaceID; // texture reg ID -> binded surface ID (or null if nothing is binded) 
        public Dictionary<GraphicResourceID, HashSet<uint>> RTBoundToTextureReg; // rt ID -> list of texture reg IDs to which its binded
        public GlobalTexManager<Directx11CmdList>.Context GlobalTextures; // global texture states for the current command list
        public CBufferInstance GlobalCBuffer; // globals cbuffer state for the current command list
        public Float4x4[] Instances; // buffer containing a list of instances used to update the vb for instancing

        // pso state
        public PSOShadersState ShaderState;
        public PSOInputLayoutState InputLayoutState;
        public PSORasterState RasterState;
        public PSOBlendState BlendState;
        public PSOSamplerState SamplerState;
        public PSODepthStencilState DepthStencilState;

        public Directx11CmdList(GlobalTexManager<Directx11CmdList> globalTexManager)
        {
            RTBoundToTextureReg = new Dictionary<GraphicResourceID, HashSet<uint>>();
            TexRegToRtID = new GraphicResourceID[Directx11Graphics.MAX_TEXTURE_BIND_INDEX];
            TexRegToSurfaceID = new GraphicResourceID[Directx11Graphics.MAX_TEXTURE_BIND_INDEX];
            Instances = new Float4x4[Directx11Graphics.MAX_INSTANCE_COUNT];
            RTState = new RenderTargetState();
            GlobalTextures = globalTexManager.MainContext.CreateChild();
            ShaderState = new PSOShadersState();
            InputLayoutState = new PSOInputLayoutState();
            RasterState = new PSORasterState();
            BlendState = new PSOBlendState();
            SamplerState = new PSOSamplerState();
            DepthStencilState = new PSODepthStencilState();
        }

    }
}
