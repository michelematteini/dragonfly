using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Graphics.Shaders;
using DragonflyGraphicsWrappers;
using DragonflyGraphicsWrappers.DX11;
using System.IO;
using DragonflyUtils;
using System.Threading;
using Dragonfly.Utils;

namespace Dragonfly.Graphics.API.Directx11
{
    internal class Directx11Graphics : DirectxGraphics
    {
        internal const int MAX_INSTANCE_COUNT = 1024;
        internal const int MAX_TEXTURE_BIND_INDEX = 32;
        internal const uint INSTANCE_MATRIX_SIZE = 64;

        private DF_D3D11Device device;
        private CmdListCoordinator cmdListCoordinator;
        private ThreadLocal<DirectxPadder> padder;
        private object CMDLIST_SYNC; // synchronization object for command list queue

        //directx resources
        private Dictionary<GraphicResourceID, DF_Buffer11> vertexBuffers;
        private Dictionary<GraphicResourceID, DF_Buffer11> indexBuffers;
        private Dictionary<GraphicResourceID, DF_Texture11> textures;
        private Dictionary<GraphicResourceID, DF_Texture11> depthBuffers; // for render targets
        private Dictionary<GraphicResourceID, DF_Texture11> rtStaging;
        private Dictionary<GraphicResourceID, Directx11CmdList> cmdLists; // command list ID -> deferred context
        private DF_Buffer11 instanceVB;
        private Dictionary<GraphicResourceID, CBufferInstance> localCBuffers; // shader ID -> CBuffer
        private CBAllocator cbAllocator;

        // shared states ( one instance per device, device R/W, context read only
        private Dictionary<GraphicResourceID, RenderTargetParams> rtParams; // rt ID -> user creation and current rt parameters 
        private Dictionary<GraphicResourceID, Dictionary<string, TexBindingState>> texBindingStates; // shader ID -> list of texture and rts binded to the shader
        private PSOShaders shaderCache;
        private PSOInputLayout inputLayoutCache;
        private PSORaster rasterCache;
        private PSOBlend blendCache;
        private PSOSampler samplerCache;
        private PSODepthStencil depthStencilCache;
        private HashSet<GraphicResourceID> updatedShaders; // list of IDs of shaders for which the local cbuffer has been modified


        // globals state
        private GlobalTexManager<Directx11CmdList> globalTexManager;
        private CBufferInstance globalCBuffer;

        public Directx11Graphics(DFGraphicSettings factory, Directx11API api) : base(factory, api)
        {
            if (!DF_Directx3D11.IsAvailable()) return;

            IsFullScreen = false; // switch to full screen later...
            device = new DF_D3D11Device(CurTarget, IsFullScreen, CurWidth, CurHeight, AntialiasingEnabled);
            device.SetPrimitiveTopology(DF_PrimitiveType.TriangleList);

            vertexBuffers = new Dictionary<GraphicResourceID, DF_Buffer11>();
            indexBuffers = new Dictionary<GraphicResourceID, DF_Buffer11>();
            textures = new Dictionary<GraphicResourceID, DF_Texture11>();
            localCBuffers = new Dictionary<GraphicResourceID, CBufferInstance>();         
            rtParams = new Dictionary<GraphicResourceID, RenderTargetParams>();
            depthBuffers = new Dictionary<GraphicResourceID, DF_Texture11>();
            rtStaging = new Dictionary<GraphicResourceID, DF_Texture11>();
            texBindingStates = new Dictionary<GraphicResourceID, Dictionary<string, TexBindingState>>();
            cmdLists = new Dictionary<GraphicResourceID, Directx11CmdList>();
            CMDLIST_SYNC = new object();
            updatedShaders = new HashSet<GraphicResourceID>();

            // utility modules and management
            cbAllocator = new CBAllocator(device);
            padder = new ThreadLocal<DirectxPadder>(() => new DirectxPadder(), false);
            globalTexManager = new GlobalTexManager<Directx11CmdList>(ShaderBindingTable, SetGlobalTexture, SetGlobalTarget);
            cmdListCoordinator = new CmdListCoordinator();

            // create and bind globals cbuffer
            string globalCBufferName = CBufferBinding.CreateName(true, 0);
            if (ShaderBindingTable.ContainsInput(Directx11ShaderCompiler.GLOBAL_CB_SHADER_NAME, globalCBufferName))
            {
                globalCBuffer = createCBufferFromBinding((CBufferBinding)ShaderBindingTable.GetInput(Directx11ShaderCompiler.GLOBAL_CB_SHADER_NAME, globalCBufferName), true);
                device.SetVSConstantBuffer(globalCBuffer.GPUResource, 0);
                device.SetPSConstantBuffer(globalCBuffer.GPUResource, 0);
            }

            // create and bind instance buffer
            instanceVB = device.CreateVertexBuffer(MAX_INSTANCE_COUNT * INSTANCE_MATRIX_SIZE, DF_Usage11.Dynamic);

            // initialize PSO cache
            shaderCache = new PSOShaders(device, ShaderBindingTable, GraphicsAPI);
            inputLayoutCache = new PSOInputLayout(device, shaderCache);
            rasterCache = new PSORaster(device);
            rasterCache.CacheAllStates();
            blendCache = new PSOBlend(device);
            blendCache.CacheAllStates();
            samplerCache = new PSOSampler(device);
            depthStencilCache = new PSODepthStencil(device);
            depthStencilCache.CacheAllStates();

            // initialize required rendering state
            if (factory.FullScreen)
                SetScreen(CurTarget, true, CurWidth, CurHeight);
        }

        public override bool IsAvailable
        {
            get
            {
                if (device == null) return false;
                return true;
            }
        }

        public Bitmap BackbufferToImage()
        {
            string tmpFile = Path.GetTempFileName();
            device.SaveBackbufferToFile(tmpFile);
            Image tmpImage = Bitmap.FromFile(tmpFile);
            Bitmap memoryImage = new Bitmap(tmpImage);
            tmpImage.Dispose();
            File.Delete(tmpFile);
            return memoryImage;
        }

        private void DisableRenderTarget(Directx11CmdList cmdList, GraphicResourceID rtID)
        {
            int bindIndex = cmdList.RTState.GetBindIndex(rtID);
            if (bindIndex < 0) return;
            cmdList.RTState.SetRenderTarget(null, bindIndex);
            if (depthBuffers.ContainsKey(rtID))
                cmdList.RTState.SetDepthStencil(null);

            CommitRenderTargetState(cmdList);
        }

        public override void DisplayRender()
        {
            device.Present();
        }

        private void UpdatePSO(Directx11CmdList cmdList)
        {
            if (cmdList.RasterState.Changed)
            {
                cmdList.Context.SetRasterState(rasterCache.GetState(cmdList.RasterState));
                cmdList.RasterState.Changed = false;
            }
            if (cmdList.BlendState.Changed)
            {
                cmdList.Context.SetBlendState(blendCache.GetState(cmdList.BlendState));
                cmdList.BlendState.Changed = false;
            }
            if (cmdList.InputLayoutState.Changed)
            {
                cmdList.Context.SetInputLayout(inputLayoutCache.GetState(cmdList.InputLayoutState));
                cmdList.InputLayoutState.Changed = false;
            }
            if(cmdList.ShaderState.Changed)
            {
                cmdList.Context.SetVertexShader(shaderCache.GetState(cmdList.ShaderState).VS);
                cmdList.Context.SetPixelShader(shaderCache.GetState(cmdList.ShaderState).PS);
                cmdList.ShaderState.Changed = false;
            }
            if (cmdList.DepthStencilState.Changed)
            {
                cmdList.Context.SetDepthStencilState(depthStencilCache.GetState(cmdList.DepthStencilState));
                cmdList.DepthStencilState.Changed = false;
            }
            CommitRenderTargetState(cmdList);
            UpdateGlobalShaderConstants(cmdList);
        }

        void CommitRenderTargetState(Directx11CmdList cmdList)
        {
            if (cmdList.RTState.Changed)
                cmdList.RTState.Commit(cmdList.Context, textures, depthBuffers);           

            if (cmdList.Viewport.Changed)
            {
                AARect scaledVp = GetScaledViewport(cmdList);
                cmdList.Context.SetViewport((uint)scaledVp.X1, (uint)scaledVp.Y1, (uint)scaledVp.Size.X, (uint)scaledVp.Size.Y);
                cmdList.Viewport.Changed = false;
            }
        }

        public override bool NewFrame()
        {
            cmdListCoordinator.NewFrame();
            return base.NewFrame() && device.CanRender();
        }

        private void UpdateLocalShaderConstants()
        {
            foreach(GraphicResourceID shaderID in updatedShaders)
            {
                CBufferInstance cb;
                if (localCBuffers.TryGetValue(shaderID, out cb))
                {
                    device.SetResourceData<byte>(cb.GPUResource, cb.CPUValue.ToByteArray());
                    cb.CPUValue.Changed = false;
                }
            }
            updatedShaders.Clear();
        }

        private void UpdateGlobalShaderConstants(Directx11CmdList cmdList)
        {
            if (cmdList.GlobalCBuffer.IsAvailable && cmdList.GlobalCBuffer.CPUValue.Changed)
            {
                cmdList.Context.SetResourceData<byte>(cmdList.GlobalCBuffer.GPUResource, cmdList.GlobalCBuffer.CPUValue.ToByteArray());
                cmdList.GlobalCBuffer.CPUValue.Changed = false;
            }
        }

        public override void StartRender() 
        {
            cmdListCoordinator.SolveRenderStages();

            // synchronously execute all closed command lists
            while (!cmdListCoordinator.EndOfFrame)
            {
                HashSet<GraphicResourceID> lists;
                cmdListCoordinator.ToBeExecuted.TryDequeue(out lists);
                foreach (GraphicResourceID cmdList in lists)
                    commandList_Execute(cmdList);
            }
        }

        protected override void release()
        {
            //release pipeline state
            shaderCache.Release();
            inputLayoutCache.Release();
            rasterCache.Release();
            blendCache.Release();
            samplerCache.Release();

            // release resources
            foreach (Directx11CmdList cmdList in cmdLists.Values) cmdList.Context.ReleaseCommandList();
            cmdLists.Clear();
            foreach (DF_Buffer11 vb in vertexBuffers.Values) vb.Release();
            vertexBuffers.Clear();
            foreach (DF_Buffer11 ib in indexBuffers.Values) ib.Release();
            indexBuffers.Clear();
            foreach (DF_Texture11 tex in textures.Values) tex.Release();
            textures.Clear();
            foreach (DF_Texture11 tex in rtStaging.Values) tex.Release();
            rtStaging.Clear();
            foreach (CBufferInstance cb in localCBuffers.Values) cbAllocator.ReleaseCB(cb.GPUResource);
            localCBuffers.Clear();

            if (globalCBuffer.IsAvailable) globalCBuffer.GPUResource.Release();
            globalCBuffer = new CBufferInstance();

            if (IsFullScreen) device.SetFullscreen(false);
            device.Release();
        }
  
        protected override void setScreen(IntPtr target, bool fullScreen, int width, int height)
        {
            if(target == CurTarget && (IsFullScreen == fullScreen || Win32.IsTopLevelWindowHandle(target)))
            {
                if (!IsFullScreen && fullScreen) // entering full-screen mode
                    device.SetFullscreen(true);

                device.Resize((uint)width, (uint)height);

                if (IsFullScreen && !fullScreen) // exiting full-screen mode
                    device.SetFullscreen(false);
            }
            else
            {
                device.UpdateSwapChain(target, fullScreen, width, height, AntialiasingEnabled);
            }

            CurTarget = target;
            IsFullScreen = fullScreen;
            CurWidth = width;
            CurHeight = height;

            UpdateRenderTargetsResolution();
        }

        public override List<Int2> SupportedDisplayResolutions
        {
            get
            {
                return DirectxUtils.DisplayModeToResolutionList(device.GetDisplayModes());
            }
        }
        private AARect GetScaledViewport(Directx11CmdList cmdList)
        {
            uint activeWidth = (uint)CurWidth, activeHeight = (uint)CurHeight;
            GraphicResourceID rtId = cmdList.RTState.GetRenderTarget(0);
            if (rtId != null)
            {
                RenderTargetParams rtData = rtParams[rtId];
                activeWidth = rtData.CurWidth;
                activeHeight = rtData.CurHeight;
            }
            return cmdList.Viewport.Current * new Float2(activeWidth, activeHeight);
        }

        public override void StartTracedSection(CommandList commandList, Byte4 markerColor, string name)
        {
            base.StartTracedSection(commandList, markerColor, name);
            Directx11CmdList cmdList = cmdLists[commandList.ResourceID];
            cmdList.Context.BeginEvent(name);
        }

        public override void StartTracedSection(Byte4 markerColor, string name)
        {
            base.StartTracedSection(markerColor, name);
            Pix.BeginEvent(name, markerColor.R, markerColor.G, markerColor.B);
        }

        public override void EndTracedSection(CommandList commandList)
        {
            base.EndTracedSection(commandList);
            Directx11CmdList cmdList = cmdLists[commandList.ResourceID];
            cmdList.Context.EndEvent();
        }

        public override void EndTracedSection()
        {
            base.EndTracedSection();
            Pix.EndEvent();
        }

        #region Resource creation

        protected override GraphicResourceID createIndexBuffer(int indexCount)
        {
            DF_Buffer11 ib = device.CreateIndexBuffer((uint)(sizeof(ushort) * indexCount), DF_Usage11.Default);
            GraphicResourceID id = new GraphicResourceID(ib.GetResourceHash());
            indexBuffers[id] = ib;
            return id;
        }

        protected override GraphicResourceID createShader(string effectName, ShaderStates states, string variantID, string templateName, GraphicResourceID useParametersFromShader)
        {
            GraphicResourceID shaderID = new GraphicResourceID();

            // local cbuffer
            if(useParametersFromShader == null)
            {
                // create a cbuffer for the shader
                CBufferBinding binding = (CBufferBinding)ShaderBindingTable.GetEffectInput(effectName, CBufferBinding.CreateName(false, 0));
                if (binding.ByteSize > 0)
                {
                    localCBuffers[shaderID] = createCBufferFromBinding(binding, true);
                    updatedShaders.Add(shaderID);
                }
            }
            else
            {
                // use the parent shader cbuffer
                localCBuffers[shaderID] = localCBuffers[useParametersFromShader];
            }

            // prepare shader related PSO states
            {
                EffectBinding e = ShaderBindingTable.GetEffect(effectName, templateName, variantID);

                // preprare shader
                PSOShadersState shaderDesc = new PSOShadersState();
                shaderDesc.ShaderEffectName.Value = effectName;
                shaderDesc.ShaderVariantID.Value = variantID;
                shaderDesc.ShaderTemplateName.Value = templateName;
                shaderDesc.Instanced.Value = false;
                shaderCache.CacheState(shaderDesc);
                if (e.SupportsInstancing)
                {
                    shaderDesc.Instanced.Value = true;
                    shaderCache.CacheState(shaderDesc);
                }

                // prepare input layout
                PSOInputLayoutState iaDesc = new PSOInputLayoutState();
                iaDesc.ShaderEffectName.Value = effectName;
                iaDesc.VertexType.Value = e.InputLayout;
                iaDesc.ShaderVariantID.Value = variantID;
                iaDesc.ShaderTemplateName.Value = templateName;
                iaDesc.Instanced.Value = false;
                inputLayoutCache.CacheState(iaDesc);
                if (e.SupportsInstancing)
                {
                    iaDesc.Instanced.Value = true;
                    inputLayoutCache.CacheState(iaDesc);
                }

                // prepare samplers
                PSOSamplerState samplerDesc = new PSOSamplerState();
                foreach(InputBinding inputBinding in ShaderBindingTable.GetAllShaderInputs(ShaderBindingTable.GetParentShaderName(effectName)))
                {
                    TextureBinding texBinding = inputBinding as TextureBinding;
                    if (texBinding == null)
                        continue;
                    samplerDesc.Options.Value = texBinding.TexBindingOptions;
                    samplerCache.CacheState(samplerDesc);
                }
            }

            // create a new entry to save texture states
            Dictionary<string, TexBindingState> localTextures = useParametersFromShader != null ? texBindingStates[useParametersFromShader] : new Dictionary<string, TexBindingState>();
            texBindingStates.Add(shaderID, localTextures);

            return shaderID;
        }

        private CBufferInstance createCBufferFromBinding(CBufferBinding binding, bool dedicatedResourceRequired)
        {
            CBufferInstance cb = new CBufferInstance();
            cb.CPUValue = new CBuffer(binding);
            cb.GPUResource = cbAllocator.CreateCB(cb.CPUValue.Bindings.ByteSize, dedicatedResourceRequired);
            return cb;
        }

        protected override GraphicResourceID createTexture<T>(int width, int height, SurfaceFormat format, T[] pixelData)
        {
            DF_Texture11 tex = device.CreateTexture((uint)width, (uint)height, DirectxUtils.SurfaceFormatToDX(format), false, pixelData == null ? DF_Usage11.Dynamic : DF_Usage11.Default, DF_TexBinding.ShaderResource);
            GraphicResourceID id = new GraphicResourceID(tex.GetResourceHash());
            textures[id] = tex;
            if (pixelData != null)
                texture_SetData<T>(id, pixelData, pixelData.Length / height);
            return id;
        }

        protected override GraphicResourceID createTexture(byte[] fileData, out int width, out int height)
        {
            DF_Texture11 tex = device.CreateTexture(fileData, DirectxUtils.IsFileDDS(fileData));
            GraphicResourceID id = new GraphicResourceID(tex.GetResourceHash());
            textures[id] = tex;
            width = tex.GetWidth();
            height = tex.GetHeight();
            return id;
        }

        protected override GraphicResourceID createVertexBuffer(VertexType vtype, int vertexCount)
        {
            DF_Buffer11 vb = device.CreateVertexBuffer((uint)(vtype.ByteSize * vertexCount), DF_Usage11.Default);
            GraphicResourceID id = new GraphicResourceID(vb.GetResourceHash());
            vertexBuffers[id] = vb;
            return id;
        }

        protected override GraphicResourceID createCommandList()
        {
            Directx11CmdList cmdList = new Directx11CmdList(globalTexManager);
            cmdList.Context = device.CreateDeferredContext();
            cmdList.ID = new GraphicResourceID(cmdList.Context.GetResourceHash());

            cmdLists[cmdList.ID] = cmdList;
            return cmdList.ID;
        }

        #endregion // Resource creation

        #region IndexBuffer

        protected override void indexBuffer_Release(GraphicResourceID resID)
        {
            indexBuffers[resID].Release();
            indexBuffers.Remove(resID);
        }

        protected override void indexBuffer_SetIndices(GraphicResourceID resID, ushort[] indices, int indexCount)
        {
            device.UpdateResource(indexBuffers[resID], indices, indexCount);
        }

        #endregion

        #region Shader

        protected override void shader_Release(GraphicResourceID resID)
        {
            if (localCBuffers.ContainsKey(resID))
            {
                cbAllocator.ReleaseCB(localCBuffers[resID].GPUResource);
                localCBuffers.Remove(resID);
                texBindingStates.Remove(resID);
            }
        }

        protected override void shader_SetParam(Shader shader, string name, bool value)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(value));
        }

        protected override void shader_SetParam(Shader shader, string name, int value)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(value));
        }

        protected override void shader_SetParam(Shader shader, string name, float value)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(value));
        }

        protected override void shader_SetParam(Shader shader, string name, Float2 value)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(value));
        }

        protected override void shader_SetParam(Shader shader, string name, Float3 value)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(value));
        }

        protected override void shader_SetParam(Shader shader, string name, Float4 value)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(value));
        }

        protected override void shader_SetParam(Shader shader, string name, Float4x4 value)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(value));
        }

        protected override void shader_SetParam(Shader shader, string name, Float3x3 value)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(value));
        }

        protected override void shader_SetParam(Shader shader, string name, Int3 value)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(value));
        }

        protected override void shader_SetParam(Shader shader, string name, int[] values)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(values));
        }

        protected override void shader_SetParam(Shader shader, string name, float[] values)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(values));
        }

        protected override void shader_SetParam(Shader shader, string name, Texture value)
        {
            texBindingStates[shader.ResourceID][name] = new TexBindingState() { Surface = value, IsRenderTarget = false };
            UpdateTexelSizeParam(shader, name, value.Resolution);
        }

        protected override void shader_SetParam(Shader shader, string name, RenderTarget value)
        {
            texBindingStates[shader.ResourceID][name] = new TexBindingState() { Surface = value, IsRenderTarget = true };
            UpdateTexelSizeParam(shader, name, value.Resolution);
        }

        private void UpdateTexelSizeParam(Shader shader, string texParamName, Int2 resolution)
        {
            string texelBindName = DirectxUtils.GetTexelSizeConstantName(texParamName);
            if (ShaderBindingTable.ContainsEffectInput(shader.EffectName, texelBindName))
                shader_SetParam(shader, texelBindName, 1.0f / (Float2)resolution);
        }

        protected override void shader_SetParam(Shader shader, string name, Float2[] values)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(values));
        }

        protected override void shader_SetParam(Shader shader, string name, Float3[] values)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(values));
        }

        protected override void shader_SetParam(Shader shader, string name, Float4[] values)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(values));
        }

        protected override void shader_SetParam(Shader shader, string name, Float4x4[] values)
        {
            shader_SetParamNoPad(shader, name, padder.Value.Pad(values));
        }

        private void shader_SetParamNoPad(Shader shader, string name, float[] values)
        {
            CBuffer cb = localCBuffers[shader.ResourceID].CPUValue;
            cb.SetValue(name, values);
            updatedShaders.Add(shader.ResourceID);
        }

        private void shader_SetParamNoPad(Shader shader, string name, int[] values)
        {
            CBuffer cb = localCBuffers[shader.ResourceID].CPUValue;
            cb.SetValue(name, values);
            updatedShaders.Add(shader.ResourceID);
        }

        #endregion // Shader

        #region VertexBuffer

        protected override void vertexBuffer_Release(GraphicResourceID resID)
        {
            vertexBuffers[resID].Release();
            vertexBuffers.Remove(resID);
        }

        protected override void vertexBuffer_SetVertices<T>(GraphicResourceID resID, T[] vertices, int vertexCount)
        {
            device.UpdateResource(vertexBuffers[resID], vertices, vertexCount);
        }

        #endregion

        #region Texture

        protected override void texture_Release(GraphicResourceID resID)
        {
            textures[resID].Release();
            textures.Remove(resID);
        }

        protected override void texture_SetData<T>(GraphicResourceID resID, T[] data, int rowLength)
        {
            device.UpdateResource2D<T>(textures[resID], data, rowLength);
        }

        #endregion // Texture

        #region RenderTarget

        protected override void renderTarget_SaveSnapshot(GraphicResourceID resID)
        {
            if (!rtStaging.ContainsKey(resID))
            {
                // create a staging texture for reading the render target data
                RenderTargetParams rt = rtParams[resID];
                rtStaging[resID] = device.CreateTexture(rt.CurWidth, rt.CurHeight, rt.Format, false, DF_Usage11.Staging, DF_TexBinding.None);
            }

            device.CopyResource(textures[resID], rtStaging[resID]);
        }   

        protected override bool renderTarget_TryGetSnapshotData<T>(GraphicResourceID resID, T[] destBuffer)
        {
            if (!rtStaging.ContainsKey(resID))
                return false;

            // if the destination buffer is null, just check if the snapshot is ready
            if (destBuffer == null)
                return device.IsResourceDataAvailable(rtStaging[resID]);

            // try reading the render target snapshot data
            RenderTargetParams rtp = rtParams[resID];
            return device.GetResourceData2D<T>(rtStaging[resID], destBuffer, rtp.CurWidth, rtp.CurHeight, false);
        }

        protected override void renderTarget_GetSnapshotData<T>(GraphicResourceID resID, T[] destBuffer)
        {
            RenderTargetParams rtp = rtParams[resID];
            device.GetResourceData2D<T>(rtStaging[resID], destBuffer, rtp.CurWidth, rtp.CurHeight, true);
        }

        protected override void renderTarget_CopyToTexture(GraphicResourceID resID, GraphicResourceID destTexture)
        {
            device.CopyResource(textures[resID], textures[destTexture]);
        }

        protected override int renderTarget_GetHeight(GraphicResourceID resID)
        {
            return (int)rtParams[resID].CurHeight;
        }

        protected override int renderTarget_GetWidth(GraphicResourceID resID)
        {
            return (int)rtParams[resID].CurWidth;
        }

        protected override void renderTarget_Release(GraphicResourceID resID)
        {
            textures[resID].Release();
            textures.Remove(resID);
            if (rtParams[resID].HasDepthBuffer)
            {
                depthBuffers[resID].Release();
                depthBuffers.Remove(resID);
            }
            if (rtStaging.ContainsKey(resID))
            {
                rtStaging[resID].Release();
                rtStaging.Remove(resID);
            }
        }
        private void UpdateRenderTargetsResolution()
        {
            // save current rend
            GraphicResourceID[] rtIds = new GraphicResourceID[rtParams.Keys.Count];
            rtParams.Keys.CopyTo(rtIds, 0);

            // update backbuffer-dependent render targets
            for(int i = 0; i < rtIds.Length; i++)
            {
                RenderTargetParams rtp = rtParams[rtIds[i]];
                if (!rtp.UsePercent) continue; // no update needed
                renderTarget_Release(rtIds[i]);
                CreateRenderTarget(rtp, rtIds[i]);
            }
        }

        protected override GraphicResourceID CreateDirectxRenderTarget(RenderTargetParams rt)
        {
            return CreateRenderTarget(rt, null);
        }

        private GraphicResourceID CreateRenderTarget(RenderTargetParams rt, GraphicResourceID overrideID)
        {
            UpdateRtDimensions(ref rt);

            // create main surface
            DF_Texture11 tex = device.CreateTexture(rt.CurWidth, rt.CurHeight, rt.Format, false, DF_Usage11.Default, DF_TexBinding.RenderTarget | DF_TexBinding.ShaderResource);
            GraphicResourceID id = overrideID;
            if (id == null) id = new GraphicResourceID(tex.GetResourceHash());

            // create depth buffer
            if (rt.HasDepthBuffer)
            {
                DF_Texture11 rtDepth = device.CreateTexture(rt.CurWidth, rt.CurHeight, DF_SurfaceFormat.DEFAULT_DEPTH_FORMAT, false, DF_Usage11.Default, DF_TexBinding.DepthStencil);
                depthBuffers[id] = rtDepth;
            }

            textures[id] = tex;
            rtParams[id] = rt;

            return id;
        }

        protected override void renderTarget_SetDepthWriteTarget(GraphicResourceID resID, RenderTarget depthWriteTarget)
        {
            RenderTargetParams curRtParams = rtParams[resID];
            curRtParams.OverrideZBuffer = depthWriteTarget.ResourceID;
            rtParams[resID] = curRtParams;
        }



        #endregion //RenderTarget

        #region CommandList

        protected override void commandList_ClearSurfaces(GraphicResourceID resID, Float4 clearValue, ClearFlags clearFlags)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            CommitRenderTargetState(cmdList);

            if (cmdList.Viewport.Current != ViewportState.Default)
            {
                AARect scaledVp = GetScaledViewport(cmdList);
                cmdList.RTState.ClearSurfaces(cmdList.Context, clearValue, clearFlags, (int)scaledVp.X1, (int)scaledVp.Y1, (int)scaledVp.X2, (int)scaledVp.Y2);
            }
            else
                cmdList.RTState.ClearSurfaces(cmdList.Context, clearValue, clearFlags);
        }

        protected override void commandList_Draw(GraphicResourceID resID)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            UpdatePSO(cmdList);
            cmdList.Context.Draw((uint)cmdList.VertexBuffer.VertexCount, 0);
        }

        protected override void commandList_DrawIndexed(GraphicResourceID resID)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            UpdatePSO(cmdList);
            cmdList.Context.DrawIndexed((uint)cmdList.IndexBuffer.IndexCount, 0);
        }

        protected override void commandList_DrawIndexedInstanced(GraphicResourceID resID, ArrayRange<Float4x4> instances)
        {
            Directx11CmdList cmdList = cmdLists[resID];

            // bind instancing vertex buffer and turn on instancing
            cmdList.Context.SetVertexBuffer(1, instanceVB, 0, INSTANCE_MATRIX_SIZE);
            cmdList.InputLayoutState.Instanced.Value = true;
            cmdList.ShaderState.Instanced.Value = true;

            UpdatePSO(cmdList);

            for (int instancingOffset = 0; instancingOffset < instances.Count; instancingOffset += MAX_INSTANCE_COUNT)
            {
                uint batchCount = (uint)(System.Math.Min(instancingOffset + MAX_INSTANCE_COUNT, instances.Count) - instancingOffset);

                // update instance vb
                for (int i = 0; i < batchCount; i++)
                    cmdList.Instances[i] = instances[instancingOffset + i];
                cmdList.Context.SetResourceData(instanceVB, cmdList.Instances);

                // draw the batch
                cmdList.Context.DrawInstanced((uint)cmdList.IndexBuffer.IndexCount, 0, batchCount);
            }

            // unbind instancing vb and reset state
            cmdList.Context.SetVertexBuffer(1, null, 0, 0);
            cmdList.InputLayoutState.Instanced.Value = false;
            cmdList.ShaderState.Instanced.Value = false;
        }

        protected override void commandList_StartRecording(GraphicResourceID resID, IReadOnlyList<GraphicResourceID> requiredLists, bool flushRequired)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdListCoordinator.DeclareList(resID, requiredLists); // signal that this command list will be used in this frame
            if (flushRequired)
                cmdListCoordinator.SolveRenderStages();
            cmdList.RTState.SetDefaultBuffers(device); // update backbuffer resources

            // set default GPU states
            cmdList.Context.SetPrimitiveTopology(DF_PrimitiveType.TriangleList);

            // reset cached states
            cmdList.IndexBuffer = null;
            cmdList.VertexBuffer = null;
            cmdList.Viewport = new ViewportState { Current = ViewportState.Default };
            cmdList.LastBindedLocalCB = null;
            cmdList.LastUpdatedLocalCB = new CBufferInstance();
            for (int i = 0; i < Directx11Graphics.MAX_TEXTURE_BIND_INDEX; i++)
            {
                cmdList.TexRegToRtID[i] = null;
                cmdList.TexRegToSurfaceID[i] = null;
            }
            cmdList.RTBoundToTextureReg.Clear();
            cmdList.GlobalTextures.Reset();

            // update global param cbuffer with the current device context state and bind it
            if (globalCBuffer.IsAvailable)
            {
                if (!cmdList.GlobalCBuffer.IsAvailable)
                    cmdList.GlobalCBuffer.CPUValue = globalCBuffer.CPUValue.Clone();
                else
                    globalCBuffer.CPUValue.CopyTo(cmdList.GlobalCBuffer.CPUValue);
                cmdList.GlobalCBuffer.GPUResource = globalCBuffer.GPUResource;
                cmdList.Context.SetVSConstantBuffer(cmdList.GlobalCBuffer.GPUResource, 0);
                cmdList.Context.SetPSConstantBuffer(cmdList.GlobalCBuffer.GPUResource, 0);
            }

            // update modified shader cbuffers
            UpdateLocalShaderConstants();

            // invalidate PSO
            cmdList.ShaderState.Changed = true;
            cmdList.InputLayoutState.Changed = true;
            cmdList.RasterState.Changed = true;
            cmdList.BlendState.Changed = true;
            cmdList.SamplerState.Changed = true;
            cmdList.DepthStencilState.Changed = true;
            cmdList.RTState.Changed = true;
        }

        protected override void commandList_QueueExecution(GraphicResourceID resID)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.Context.FinishCommandList();

            lock (CMDLIST_SYNC)
            {
                // save changes to global textures to the main context
                cmdList.GlobalTextures.MergeToParent(); 

                // save changes to global params cbuffer to the main context
                if (cmdList.GlobalCBuffer.IsAvailable)
                    cmdList.GlobalCBuffer.CPUValue.CopyTo(globalCBuffer.CPUValue);

                // queue its execution
                cmdListCoordinator.QueueExecution(resID);
            }
        }

        private void commandList_Execute(GraphicResourceID resID)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            device.ExecuteCommandList(cmdList.Context);
            device.ClearTextureBindings(0, MAX_TEXTURE_BIND_INDEX); // remove SRV binding of render targets that may be used by subsequent steps ( or next frame update steps )
            cmdList.Context.ReleaseCommandList(); // free memory from recorded commands    
        }

        protected override void commandList_SetViewport(GraphicResourceID resID, AARect viewport)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.Viewport.Current = viewport;
        }

        protected override void commandList_SetVertices(GraphicResourceID resID, VertexBuffer vertices)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.InputLayoutState.VertexType.Value = vertices.VertexType;
            cmdList.Context.SetVertexBuffer(0, vertexBuffers[vertices.ResourceID], 0, (uint)vertices.VertexType.ByteSize);
            cmdList.VertexBuffer = vertices;
        }

        protected override void commandList_SetIndices(GraphicResourceID resID, IndexBuffer indices)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.Context.SetIndexBuffer(indexBuffers[indices.ResourceID]);
            cmdList.IndexBuffer = indices;
        }

        protected override void commandList_SetRenderTarget(GraphicResourceID resID, RenderTarget rt, int index)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.Viewport.Reset();
            HashSet<uint> rtBindSet;
            if (cmdList.RTBoundToTextureReg.TryGetValue(rt.ResourceID, out rtBindSet))
            {
                foreach (uint rtBindAddress in rtBindSet)
                {
                    // unbind target from shader
                    cmdList.Context.SetVSTexture(rtBindAddress, null);
                    cmdList.Context.SetPSTexture(rtBindAddress, null);
                    cmdList.TexRegToRtID[rtBindAddress] = null;
                    cmdList.TexRegToSurfaceID[rtBindAddress] = null;
                }
                cmdList.RTBoundToTextureReg.Remove(rt.ResourceID);
            }

            cmdList.RTState.SetRenderTarget(rt.ResourceID, index);

            // update depth buffer, could be from the specified render target or an override depth buffer.
            GraphicResourceID depthID = rtParams[rt.ResourceID].OverrideZBuffer;
            if (depthID == null && depthBuffers.ContainsKey(rt.ResourceID))
                depthID = rt.ResourceID;
            cmdList.RTState.SetDepthStencil(depthID);
        }

        protected override void commandList_DisableRenderTarget(GraphicResourceID resID, RenderTarget rt)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            DisableRenderTarget(cmdList, rt.ResourceID);
        }

        protected override void commandList_ResetRenderTargets(GraphicResourceID resID)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.Viewport.Reset();
            cmdList.RTState.ResetTargets();
        }

        protected override void commandList_Release(GraphicResourceID resID)
        {           
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.Context.Release();
            cmdLists.Remove(resID);
        }

        protected override void commandList_SetShader(GraphicResourceID resID, Shader shader)
        {
            Directx11CmdList cmdList = cmdLists[resID];

            // update current state
            cmdList.InputLayoutState.ShaderEffectName.Value = shader.EffectName;
            cmdList.InputLayoutState.ShaderVariantID.Value = shader.VariantID;
            cmdList.InputLayoutState.ShaderTemplateName.Value = shader.TemplateName;
            cmdList.ShaderState.ShaderEffectName.Value = shader.EffectName;
            cmdList.ShaderState.ShaderVariantID.Value = shader.VariantID;
            cmdList.ShaderState.ShaderTemplateName.Value = shader.TemplateName;
            cmdList.DepthStencilState.DepthEnabled.Value = shader.States.DepthBufferEnable;
            cmdList.DepthStencilState.DepthWriteEnabled.Value = shader.States.DepthBufferWriteEnable;
            cmdList.BlendState.Blend.Value = shader.States.BlendMode;
            cmdList.RasterState.CullMode.Value = shader.States.CullMode;
            cmdList.RasterState.FillMode.Value = shader.States.FillMode;

            // update global textures
            cmdList.GlobalTextures.UpateShader(shader, cmdList);

            // bind local textures
            foreach (var tstate in texBindingStates[shader.ResourceID])
                commandList_SetParam(cmdList, shader, tstate.Key, tstate.Value.Surface, tstate.Value.IsRenderTarget);

            // bind local constants
            if (localCBuffers.ContainsKey(shader.ResourceID))
            {
                // update local cbuffer
                CBufferInstance curLocalBuffer = localCBuffers[shader.ResourceID];
                if (cmdList.LastBindedLocalCB == null || curLocalBuffer.GPUResource.GetResourceHash() != cmdList.LastBindedLocalCB.GetResourceHash())
                {
                    cmdList.Context.SetVSConstantBuffer(curLocalBuffer.GPUResource, 1);
                    cmdList.Context.SetPSConstantBuffer(curLocalBuffer.GPUResource, 1);
                    cmdList.LastBindedLocalCB = curLocalBuffer.GPUResource;
                }
            }
        }

        private void SetGlobalTexture(Shader shader, string paramName, Texture value, Directx11CmdList cmdList)
        {
            commandList_SetParam(cmdList, shader, paramName, value, false);
        }

        private void SetGlobalTarget(Shader shader, string paramName, RenderTarget value, Directx11CmdList cmdList)
        {
            commandList_SetParam(cmdList, shader, paramName, value, true);
        }

        private void commandList_SetParam(Directx11CmdList cmdList, Shader shader, string name, GraphicSurface surface, bool isRenderTarget)
        {
            if (!textures.ContainsKey(surface.ResourceID))
                return;
            
            // retrieve param binding
            TextureBinding binding = ShaderBindingTable.GetEffectInput(shader.EffectName, name) as TextureBinding;
            if (binding == null)
                return;
            uint texAddress = (uint)binding.Address;

            // update automatic texel size for global texture (for locals, this is updated at StartRecording())
            if (binding.IsGlobal)
            {
                string texelBindName = DirectxUtils.GetTexelSizeConstantName(binding.Name);
                commandList_SetParam(cmdList.ID, texelBindName, 1.0f / (Float2)surface.Resolution);
            }

            // skip bindind an already binded texture
            if (cmdList.TexRegToSurfaceID[texAddress] == surface.ResourceID)
                return; // already binded!

            // update render targets binding states
            bool isCurrentlyWrittenTo = isRenderTarget && cmdList.RTState.GetBindIndex(surface.ResourceID) >= 0;
            if (!isCurrentlyWrittenTo)
            {
                // if a texture replace a render target, clear its binding record
                if (cmdList.TexRegToRtID[texAddress] != null)
                {
                    GraphicResourceID replacedRtID = cmdList.TexRegToRtID[texAddress];
                    cmdList.TexRegToRtID[texAddress] = null;
                    HashSet<uint> rtBindedToSet = cmdList.RTBoundToTextureReg[replacedRtID];
                    rtBindedToSet.Remove(texAddress);
                    if (rtBindedToSet.Count == 0)
                        cmdList.RTBoundToTextureReg.Remove(replacedRtID);
                }

                // if the resource to be binded is a render target, save that its binded to a shader and its address
                if (isRenderTarget)
                {
                    if (!cmdList.RTBoundToTextureReg.ContainsKey(surface.ResourceID))
                        cmdList.RTBoundToTextureReg[surface.ResourceID] = new HashSet<uint>();
                    cmdList.RTBoundToTextureReg[surface.ResourceID].Add(texAddress);
                    cmdList.TexRegToRtID[texAddress] = surface.ResourceID;
                }
            }

            // update sampler
            cmdList.SamplerState.Options.Value = binding.TexBindingOptions;
            DF_SamplerState sampler = samplerCache.GetState(cmdList.SamplerState);

            // update texture resource
            TextureBindingOptions texVisibility = binding.TexBindingOptions & TextureBindingOptions.Visibility;
            if (texVisibility == TextureBindingOptions.GeometryOnly || texVisibility == TextureBindingOptions.AlwaysVisible)
            {
                if(!isCurrentlyWrittenTo) // don't bind render targets that are currently used!
                    cmdList.Context.SetVSTexture(texAddress, textures[surface.ResourceID]);
                cmdList.Context.SetVSSampler(sampler, texAddress);
            }
            if (texVisibility == TextureBindingOptions.ShadingOnly || texVisibility == TextureBindingOptions.AlwaysVisible)
            {
                if(!isCurrentlyWrittenTo) // don't bind render targets that are currently used!
                    cmdList.Context.SetPSTexture(texAddress, textures[surface.ResourceID]);
                cmdList.Context.SetPSSampler(sampler, texAddress);
            }

            // save binding to cache
            cmdList.TexRegToSurfaceID[texAddress] = surface.ResourceID;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, bool value)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, int value)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, float value)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float2 value)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float3 values)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4 values)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4x4 value)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float3x3 value)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Int3 value)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, int[] values)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, float[] values)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Texture value)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalTextures.SetGlobalTexture(name, value);
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, RenderTarget value)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalTextures.SetGlobalTexture(name, value);
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float2[] values)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float3[] values)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4[] values)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4x4[] values)
        {
            Directx11CmdList cmdList = cmdLists[resID];
            cmdList.GlobalCBuffer.CPUValue.SetValue(name, padder.Value.Pad(values));
        }

        #endregion // CommandList

    }
}
