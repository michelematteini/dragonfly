using Dragonfly.Graphics.API.Common;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Graphics.Shaders;
using Dragonfly.Utils;
using DragonflyGraphicsWrappers;
using DragonflyGraphicsWrappers.DX12;
using DragonflyUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Dragonfly.Graphics.API.Directx12
{
    internal class Directx12Graphics : DirectxGraphics
    {
        internal class RTInfo
        {
            public DF_Resource12 Resource;
            public DF_Resource12 DepthBuffer;
            public RenderTargetParams Desc;
            internal DF_Resource12 ReadBackBuffer;
            internal long LastCaptureFrameID;
        }

        internal class CmdListInfo
        {
            public DF_CommandList12 CmdList;
            public Directx12RTState RTState;
            public ViewportState Viewport;
            public CBuffer RootConstants;
            public VersionedCBuffer GlobalConstants;
            public PSOState CurrentPSO;
            public VertexType VertexTypeSimple, VertexTypeInstanced;
            internal VBInfo VB;
            internal IBInfo IB;
            public DF_Resource12[] ResCache;
            public InstancesVBuffer InstancesVB;
        }

        internal class ShaderInfo
        {
            internal CBufferCollection.Item LocalCBufferSlot;
            internal CBuffer LocalCBuffer;
            internal PSOState PSO;
            internal VertexType VertexTypeInstanced;
            internal ShaderInfo Parent;
        }

        internal class DynamicUploadResource
        {
            public bool FrequentUpdates, Initialized;
            public DF_Resource12[] UploadBuffers;

            public void ReleaseUploadBuffers(FrameDeferredReleaseList releaseList)
            {
                if (UploadBuffers != null)
                {
                    for (int i = 0; i < UploadBuffers.Length; i++)
                        if (UploadBuffers[i] != null)
                        {
                            releaseList.DeferredRelease(UploadBuffers[i]);
                            UploadBuffers[i] = null;
                        }
                }
            }
        }

        internal class VBInfo : DynamicUploadResource
        {
            public DF_Resource12 Buffer;
            internal int VertexByteSize;
            internal int VertexCount;
            internal VertexType VertexType;
        }

        internal class IBInfo : DynamicUploadResource
        {
            public DF_Resource12 Buffer;
            internal int IndexCount;
        }

        internal class TexInfo : DynamicUploadResource
        {
            public DF_Resource12 Resource;
            public Int2 Resolution;
        }


        private object CMDLIST_SYNC; // synchronization object for command list queues
        private CmdListCoordinator cmdListCoordinator;
        private DF_D3D12Device device;
        private Directx12StaticSamplers samplers;
        private ThreadLocal<DirectxPadder> padder;
        private FrameDeferredReleaseList releaseList;

        // resources
        private Dictionary<GraphicResourceID, CmdListInfo> commandLists;
        private Dictionary<GraphicResourceID, RTInfo> renderTargets;
        internal DF_CommandList12 InnerCommandList { get; private set; } // command list used internally by the device
        private CBufferCollection shaderCbuffers;
        private Dictionary<GraphicResourceID, ShaderInfo> shaders;
        private Dictionary<GraphicResourceID, VBInfo> vertexBuffers;
        private Dictionary<GraphicResourceID, IBInfo> indexBuffers;
        private Dictionary<GraphicResourceID, TexInfo> textures;

        // cache and render states
        private DF_CommandList12[] cmdListCache;
        private CBuffer lastGlobalCBuffer;
        private Directx12PSOCache psoCache;
        private HashSet<GraphicResourceID> updatedShaders; // list of IDs of shaders for which the local cbuffer has been modified
        private long frameID;
        private ObjectPool<PSOState> psoAllocator; // cache of pso state object, that can be re-used
        
        public Directx12Graphics(DFGraphicSettings settings, IGraphicsAPI api) : base(settings, api)
        {
            CBufferBinding rootCbBinding = Directx12ShaderCompiler.GetRootCBFromTable(ShaderBindingTable);
            samplers = new Directx12StaticSamplers();
            padder = new ThreadLocal<DirectxPadder>(() => new DirectxPadder(), false);
            releaseList = new FrameDeferredReleaseList(DF_Directx3D12.GetBackbufferCount());

            // create device object
            if (!DF_Directx3D12.IsAvailable()) return;
            device = new DF_D3D12Device(CurTarget, false, CurWidth, CurHeight, AntialiasingEnabled, rootCbBinding != null ? (uint)rootCbBinding.ByteSize: 0, samplers.ToOptionsArray());

            // resources
            commandLists = new Dictionary<GraphicResourceID, CmdListInfo>();
            renderTargets = new Dictionary<GraphicResourceID, RTInfo>();
            InnerCommandList = device.CreateCommandList(1);
            InnerCommandList_StartRecording(); // start recording immediately
            shaderCbuffers = new CBufferCollection(this, 1000);
            shaders = new Dictionary<GraphicResourceID, ShaderInfo>();
            vertexBuffers = new Dictionary<GraphicResourceID, VBInfo>();
            indexBuffers = new Dictionary<GraphicResourceID, IBInfo>();
            textures = new Dictionary<GraphicResourceID, TexInfo>();

            // other members
            CMDLIST_SYNC = new object();
            cmdListCoordinator = new CmdListCoordinator();
            cmdListCache = new DF_CommandList12[32];
            psoCache = new Directx12PSOCache(device, ShaderBindingTable);
            updatedShaders = new HashSet<GraphicResourceID>();
            psoAllocator = new ObjectPool<PSOState>(() => new PSOState(), pso => pso.Reset()) {  ObjectHashFunction = pso => pso.ID };

            if (IsFullScreen)
            {
                IsFullScreen = false; // force update
                SetScreen(CurTarget, true, CurWidth, CurHeight);
            }
        }

        public override bool IsAvailable
        {
            get
            {
                return device != null;
            }
        }

        internal DF_D3D12Device Device
        {
            get
            {
                return device;
            }
        }

        public override List<Int2> SupportedDisplayResolutions
        {
            get
            {
                return DirectxUtils.DisplayModeToResolutionList(device.GetDisplayModes());
            }
        }

        protected override void setScreen(IntPtr target, bool fullScreen, int width, int height)
        {
            // flush the inner command list (which is recording even outside the frame)
            InnerCommandList_QueueExecution();
            releaseList.DelayAll(DF_Directx3D12.GetBackbufferCount() + 1);

            // resize
            {
                if (target == CurTarget && (IsFullScreen == fullScreen || Win32.IsTopLevelWindowHandle(target)))
                {
                    if (!IsFullScreen && fullScreen) // entering full-screen mode
                        device.SetFullscreen(true);

                    device.Resize((uint)width, (uint)height);

                    if (IsFullScreen && !fullScreen) // exiting full-screen mode
                        device.SetFullscreen(false);
                }
                else
                {
                    device.UpdateSwapChain(target, fullScreen, width, height);
                }
            }

            // notify command lists that the swap chain changed
            foreach (CmdListInfo cmdListInfo in commandLists.Values)
                cmdListInfo.CmdList.OnSwapChainUpdated();
            InnerCommandList.OnSwapChainUpdated();

            // start inner cmd list
            InnerCommandList_StartRecording();

            // update engine resources
            CurTarget = target;
            IsFullScreen = fullScreen;
            CurWidth = width;
            CurHeight = height;
            UpdateRenderTargetResolutions();
        }

        private void InnerCommandList_StartRecording()
        {
            InnerCommandList.Reset();
        }

        private void InnerCommandList_QueueExecution()
        {
            InnerCommandList.Close();
            device.ExecuteCommandList(InnerCommandList);
        }

        public override bool NewFrame()
        {
            cmdListCoordinator.NewFrame();
            shaderCbuffers.OnNewFrame();
            releaseList.NewFrame(device.GetBackBufferIndex());
            return base.NewFrame();
        }

        public override void StartRender()
        {
            // commit the inner command list first
            InnerCommandList_QueueExecution();
            InnerCommandList_StartRecording(); // start recording next frame immediately: this list is always recording

            // pack command lists executions in stages
            cmdListCoordinator.SolveRenderStages();

            // Execute all closed command lists
            while (!cmdListCoordinator.EndOfFrame)
            {
                HashSet<GraphicResourceID> lists;
                cmdListCoordinator.ToBeExecuted.TryDequeue(out lists);
                    ExecuteCommandLists(lists);
            }
        }

        public void UpdateLocalCBuffers()
        {
            if (updatedShaders.Count == 0)
                return;

            ShaderInfo shaderInfo;

            foreach (GraphicResourceID shaderID in updatedShaders)
            {
                if (!shaders.TryGetValue(shaderID, out shaderInfo))
                    continue; // shader released after its update

                if (shaderInfo.LocalCBuffer != null && shaderInfo.LocalCBuffer.Changed)
                {
                    // allocate and update a buffer slot for the local cbuffer
                    shaderCbuffers.ReleaseCBuffer(shaderInfo.LocalCBufferSlot); // release previously used slot
                    shaderInfo.LocalCBufferSlot = shaderCbuffers.AddNewCBuffer(shaderInfo.LocalCBuffer.ToByteArray());
                    shaderInfo.LocalCBuffer.Changed = false;
                }
            }
            updatedShaders.Clear();

            shaderCbuffers.UpdateCBuffers();

        }

        private void ExecuteCommandLists(HashSet<GraphicResourceID> lists)
        {
            int listCount = 0;
            foreach (GraphicResourceID cmdListID in lists)
            {
                cmdListCache[listCount++] = commandLists[cmdListID].CmdList;
            }
            device.ExecuteCommandLists(cmdListCache, listCount);
        }

        public override void DisplayRender()
        {
            device.Present();
            frameID++;
        }

        public override void StartTracedSection(CommandList commandList, Byte4 markerColor, string name)
        {
            base.StartTracedSection(commandList, markerColor, name);
            CmdListInfo clState = commandLists[commandList.ResourceID];
            clState.CmdList.BeginEvent(name, markerColor.R, markerColor.G, markerColor.B);
        }

        public override void StartTracedSection(Byte4 markerColor, string name)
        {
            base.StartTracedSection(markerColor, name);
            Pix.BeginEvent(name, markerColor.R, markerColor.G, markerColor.B);
        }

        public override void EndTracedSection(CommandList commandList)
        {
            base.EndTracedSection(commandList);
            CmdListInfo clState = commandLists[commandList.ResourceID];
            clState.CmdList.EndEvent();
        }

        public override void EndTracedSection()
        {
            base.EndTracedSection();
            Pix.EndEvent();
        }

        #region Command Lists

        protected override GraphicResourceID createCommandList()
        {
            CmdListInfo clState = new CmdListInfo();
            clState.CmdList = device.CreateCommandList(0);
            GraphicResourceID ID = new GraphicResourceID(clState.CmdList.GetResourceHash());
            clState.RTState = new Directx12RTState();
            CBufferBinding rootCbBinding = Directx12ShaderCompiler.GetRootCBFromTable(ShaderBindingTable);
            if (rootCbBinding != null)
                clState.RootConstants = new CBuffer(rootCbBinding);
            CBufferBinding globalCbBinding = Directx12ShaderCompiler.GetGlobalCBFromTable(ShaderBindingTable);
            if (globalCbBinding != null)
                clState.GlobalConstants = new VersionedCBuffer(globalCbBinding, device);
            clState.CurrentPSO =  psoAllocator.CreateNew();
            clState.ResCache = new DF_Resource12[2];
            clState.InstancesVB = new InstancesVBuffer(device, clState.CmdList, releaseList);

            commandLists.Add(ID, clState);
            return ID;
        }

        protected override void commandList_StartRecording(GraphicResourceID resID, IReadOnlyList<GraphicResourceID> requiredLists, bool flushRequired)
        {
            UpdateLocalCBuffers();

            CmdListInfo clState = commandLists[resID];

            cmdListCoordinator.DeclareList(resID, requiredLists); // signal that this command list will be used in this frame
            if (flushRequired)
                cmdListCoordinator.SolveRenderStages();
            clState.CmdList.Reset();

            // reset command list state
            clState.RTState.SetDefaultBuffers(device); // update backbuffer resources
            clState.RTState.Changed = true;
            clState.Viewport = new ViewportState { Current = ViewportState.Default };
            clState.RootConstants.Changed = true;
            clState.GlobalConstants.NewFrame();
            clState.InstancesVB.NewFrame();
            clState.CurrentPSO.Changed = true;
            if (lastGlobalCBuffer != null) // update globals with the version of the last executed cmd list
                lastGlobalCBuffer.CopyTo(clState.GlobalConstants.Current);
        }

        protected override void commandList_ClearSurfaces(GraphicResourceID resID, Float4 clearValue, ClearFlags flags)
        {
            CmdListInfo clState = commandLists[resID];

            // update render targets if needed
            if (clState.RTState.Changed)
                clState.RTState.Commit(clState.CmdList, renderTargets);

            if (clState.Viewport.Current != ViewportState.Default)
            {
                AARect scaledVp = GetScaledViewport(clState);
                clState.RTState.ClearSurfaces(clState.CmdList, clearValue, flags, (int)scaledVp.X1, (int)scaledVp.Y1, (int)scaledVp.X2, (int)scaledVp.Y2);
            }
            else
                clState.RTState.ClearSurfaces(clState.CmdList, clearValue, flags);
        }

        private AARect GetScaledViewport(CmdListInfo clState)
        {
            uint activeWidth = (uint)CurWidth, activeHeight = (uint)CurHeight;
            GraphicResourceID rtId = clState.RTState.GetRenderTarget(0);
            if (rtId != null)
            {
                RenderTargetParams rtData = renderTargets[rtId].Desc;
                activeWidth = rtData.CurWidth;
                activeHeight = rtData.CurHeight;
            }
            return clState.Viewport.Current * new Float2(activeWidth, activeHeight);
        }

        protected override void commandList_DisableRenderTarget(GraphicResourceID resID, RenderTarget rt)
        {
            CmdListInfo clState = commandLists[resID];
            int index = clState.RTState.GetBindIndex(rt.ResourceID);
            if (index >= 0)
                clState.RTState.SetRenderTarget(null, index);
        }

        protected override void commandList_Draw(GraphicResourceID resID)
        {
            CmdListInfo clState = commandLists[resID];
            clState.CurrentPSO.Instanced.Value = false;
            clState.CurrentPSO.VertexType.Value = clState.VertexTypeSimple;
            UpdatePSO(clState);
            clState.CmdList.DrawInstanced((uint)clState.VB.VertexCount, 1);
        }

        protected override void commandList_DrawIndexed(GraphicResourceID resID)
        {
            CmdListInfo clState = commandLists[resID];
            clState.CurrentPSO.Instanced.Value = false;
            clState.CurrentPSO.VertexType.Value = clState.VertexTypeSimple;
            UpdatePSO(clState);
            clState.CmdList.DrawIndexedInstanced((uint)clState.IB.IndexCount, 1);
        }

        protected override void commandList_DrawIndexedInstanced(GraphicResourceID resID, ArrayRange<Float4x4> instances)
        {
            CmdListInfo clState = commandLists[resID];
            clState.CurrentPSO.Instanced.Value = true;
            clState.CurrentPSO.VertexType.Value = clState.VertexTypeInstanced;
            UpdatePSO(clState);
            clState.InstancesVB.SetInstances(clState.CmdList, instances);
            clState.CmdList.DrawIndexedInstanced((uint)clState.IB.IndexCount, (uint)instances.Count);
        }

        private void UpdatePSO(CmdListInfo clState)
        {
            // update render targets
            if (clState.RTState.Changed)
                clState.RTState.Commit(clState.CmdList, renderTargets);

            // update viewport
            if (clState.Viewport.Changed)
            {
                AARect scaledVp = GetScaledViewport(clState);
                clState.CmdList.SetViewport((uint)scaledVp.X1, (uint)scaledVp.Y1, (uint)scaledVp.Size.X, (uint)scaledVp.Size.Y);
                clState.CmdList.SetScissor(0, 0, 16384, 16384); // which disables it...
                clState.Viewport.Changed = false;
            }

            // update root constants
            if (clState.RootConstants.Changed)
            {
                clState.CmdList.SetRootConstants(clState.RootConstants.ToByteArray());
                clState.RootConstants.Changed = false;
            }

            // update global constants
            if(clState.GlobalConstants.Current.Changed)
            {
                clState.GlobalConstants.CommitVersionTo(clState.CmdList);
            }

            // update PSO
            if (clState.CurrentPSO.Changed)
            {
                clState.CmdList.SetPipelineState(psoCache.GetState(clState.CurrentPSO));
                clState.CurrentPSO.Changed = false;
            }
        }

        protected override void commandList_QueueExecution(GraphicResourceID resID)
        {
            CmdListInfo clState = commandLists[resID];
            clState.CmdList.Close();

            lock (CMDLIST_SYNC)
            {
                cmdListCoordinator.QueueExecution(resID);
                lastGlobalCBuffer = clState.GlobalConstants.Current;
            }
        }

        protected override void commandList_Release(GraphicResourceID resID)
        {
            CmdListInfo clState = commandLists[resID];
            clState.GlobalConstants.Release();
            clState.CmdList.Release();
            psoAllocator.Free(clState.CurrentPSO);
            commandLists.Remove(resID);
        }

        protected override void commandList_ResetRenderTargets(GraphicResourceID resID)
        {
            CmdListInfo clState = commandLists[resID];
            clState.RTState.ResetTargets();
            clState.Viewport.Reset();
        }

        protected override void commandList_SetIndices(GraphicResourceID resID, IndexBuffer indices)
        {
            IBInfo ibInfo = indexBuffers[indices.ResourceID];
            CmdListInfo clState = commandLists[resID];
            clState.CmdList.SetIndexBuffer(ibInfo.Buffer);
            clState.IB = ibInfo;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, bool value)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, int value)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, float value)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float2 value)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float3 value)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4 value)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4x4 value)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float3x3 value)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Int3 value)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(value));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, int[] values)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, float[] values)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Texture value)
        {
            TexInfo texInfo = textures[value.ResourceID];
            DirectxPadder curPadder = padder.Value;
            commandList_SetParamNoPad(resID, name, curPadder.Pad(texInfo.Resource.GetSrvIndex()));
            
            // update automatic texel size
            {
                string texelBindName = DirectxUtils.GetTexelSizeConstantName(name);
                commandList_SetParamNoPad(resID, texelBindName, curPadder.Pad(1.0f / (Float2)texInfo.Resolution));
            }
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, RenderTarget value)
        {
            RTInfo rtInfo = renderTargets[value.ResourceID];
            DirectxPadder curPadder = padder.Value;
            commandList_SetParamNoPad(resID, name, curPadder.Pad(rtInfo.Resource.GetSrvIndex()));

            // update automatic texel size
            {
                string texelBindName = DirectxUtils.GetTexelSizeConstantName(name);
                commandList_SetParamNoPad(resID, texelBindName, curPadder.Pad(1.0f / new Float2(rtInfo.Desc.CurWidth, rtInfo.Desc.CurHeight)));
            }
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float2[] values)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float3[] values)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4[] values)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(values));
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4x4[] values)
        {
            commandList_SetParamNoPad(resID, name, padder.Value.Pad(values));
        }

        private void commandList_SetParamNoPad(GraphicResourceID resID, string name, int[] values)
        {
            CmdListInfo clState = commandLists[resID];
            int nameID = name.GetHashCode();
            if (!clState.RootConstants.TrySetValue(nameID, values))
            {
                clState.GlobalConstants.Current.SetValue(nameID, values);
            }
        }

        private void commandList_SetParamNoPad(GraphicResourceID resID, string name, float[] values)
        {
            CmdListInfo clState = commandLists[resID];
            int nameID = name.GetHashCode();
            if (!clState.RootConstants.TrySetValue(nameID, values))
            {
                clState.GlobalConstants.Current.SetValue(nameID, values);
            }
        }

        protected override void commandList_SetRenderTarget(GraphicResourceID resID, RenderTarget rt, int index)
        {
            CmdListInfo clState = commandLists[resID];
            RTInfo rtInfo = renderTargets[rt.ResourceID];
            clState.RTState.SetRenderTarget(rt.ResourceID, index);

            // update depth buffer, could be from the specified render target or an override depth buffer.
            if (index == 0)
            {
                GraphicResourceID depthID = rtInfo.Desc.OverrideZBuffer != null ? rtInfo.Desc.OverrideZBuffer : rt.ResourceID;
                clState.RTState.SetDepthStencil(depthID);
            }

            clState.Viewport.Reset();
        }

        protected override void commandList_SetShader(GraphicResourceID resID, Shader shader)
        {
            // copy needed pso states to the command list
            CmdListInfo clState = commandLists[resID];
            ShaderInfo shaderInfo = shaders[shader.ResourceID];
            clState.CurrentPSO.BlendMode.Value = shaderInfo.PSO.BlendMode;
            clState.CurrentPSO.CullMode.Value = shaderInfo.PSO.CullMode;
            clState.CurrentPSO.DepthEnabled.Value = shaderInfo.PSO.DepthEnabled;
            clState.CurrentPSO.DepthWriteEnabled.Value = shaderInfo.PSO.DepthWriteEnabled;
            clState.CurrentPSO.FillMode.Value = shaderInfo.PSO.FillMode;
            int curRtCount = clState.CurrentPSO.RenderTargetCount, newRtCount = shaderInfo.PSO.RenderTargetCount;
            int toBeUpdatedCount = System.Math.Max(curRtCount, newRtCount);
            for (int i = 0; i < toBeUpdatedCount; i++)
            {
                clState.CurrentPSO.RenderTargetFormats[i].Value =  i < newRtCount ? shaderInfo.PSO.RenderTargetFormats[i] : SurfaceFormat.Color/* set default to preserve hash*/;
            }
            clState.CurrentPSO.RenderTargetCount.Value = newRtCount;
            clState.CurrentPSO.ShaderEffectName.Value = shaderInfo.PSO.ShaderEffectName;
            clState.CurrentPSO.ShaderVariantID.Value = shaderInfo.PSO.ShaderVariantID;
            clState.CurrentPSO.ShaderTemplateName.Value = shaderInfo.PSO.ShaderTemplateName;
            clState.VertexTypeSimple = shaderInfo.PSO.VertexType;
            clState.VertexTypeInstanced = shaderInfo.VertexTypeInstanced;
            clState.RTState.DepthEnabled = shaderInfo.PSO.DepthEnabled || shaderInfo.PSO.DepthWriteEnabled;

            // update local cbuffer
            {
                ShaderInfo cbShaderInfo = shaderInfo;
                if (shaderInfo.Parent != null)
                    cbShaderInfo = shaderInfo.Parent;

                if (cbShaderInfo.LocalCBuffer != null && cbShaderInfo.LocalCBufferSlot != null /* can be null only if no more slots were available */)
                    clState.CmdList.SetLocalConstantBuffer(shaderCbuffers.GetParentResource(cbShaderInfo.LocalCBufferSlot), shaderCbuffers.GetByteOffset(cbShaderInfo.LocalCBufferSlot));
            }
        }

        protected override void commandList_SetVertices(GraphicResourceID resID, VertexBuffer vertices)
        {
            VBInfo vbInfo = vertexBuffers[vertices.ResourceID];
            CmdListInfo clState = commandLists[resID];
            clState.CmdList.SetVertexBuffer(vbInfo.Buffer);
            clState.VB = vbInfo;
        }

        protected override void commandList_SetViewport(GraphicResourceID resID, AARect viewport)
        {
            CmdListInfo clState = commandLists[resID];
            clState.Viewport.Current = viewport;
        }

        #endregion

        #region Index Buffer

        protected override GraphicResourceID createIndexBuffer(int indexCount)
        {
            IBInfo ibInfo = new IBInfo();
            ibInfo.IndexCount = indexCount;
            ibInfo.Buffer = device.CreateIndexBuffer(indexCount, DF_CPUAccess.None);
            GraphicResourceID id = new GraphicResourceID(ibInfo.Buffer.GetResourceHash());
            indexBuffers.Add(id, ibInfo);
            return id;
        }

        protected override void indexBuffer_Release(GraphicResourceID resID)
        {
            IBInfo ibInfo = indexBuffers[resID];
            releaseList.DeferredRelease(ibInfo.Buffer);
            indexBuffers.Remove(resID);
        }

        protected override void indexBuffer_SetIndices(GraphicResourceID resID, ushort[] indices, int indexCount)
        {
            IBInfo ibInfo = indexBuffers[resID];
            if (ibInfo.IndexCount < indexCount)
                ibInfo.ReleaseUploadBuffers(releaseList);
            ibInfo.IndexCount = indexCount;

            // initializing more than one time, makes it dynamic
            if (ibInfo.Initialized && !ibInfo.FrequentUpdates)
            {
                ibInfo.FrequentUpdates = true;
                ibInfo.UploadBuffers = new DF_Resource12[DF_Directx3D12.GetBackbufferCount()];
            }

            // retrieve or create the updload buffer
            DF_Resource12 ibUploadBuffer = null;
            {
                if (ibInfo.FrequentUpdates)
                {
                    ibUploadBuffer = ibInfo.UploadBuffers[device.GetBackBufferIndex()];
                }
                if (ibUploadBuffer == null)
                    ibUploadBuffer = device.CreateIndexBuffer(indexCount, DF_CPUAccess.Write);
                if (ibInfo.FrequentUpdates)
                    ibInfo.UploadBuffers[device.GetBackBufferIndex()] = ibUploadBuffer;
            }
            
            ibUploadBuffer.SetData<ushort>(indices, 0, 0, indexCount, false);
            InnerCommandList.CopyBufferRegion(ibInfo.Buffer, 0, ibUploadBuffer, 0, (ulong)(indexCount * sizeof(short)));

            if (!ibInfo.FrequentUpdates)
            {
                ibInfo.Initialized = true;
                releaseList.DeferredRelease(ibUploadBuffer);
            }
        }

        #endregion

        #region Render Target

        protected override GraphicResourceID CreateDirectxRenderTarget(RenderTargetParams rtParams)
        {
            UpdateRtDimensions(ref rtParams);
            RTInfo rtInfo = new RTInfo();
            rtInfo.Desc = rtParams;
            rtInfo.Resource = device.CreateRenderTarget(rtParams.CurWidth, rtParams.CurHeight, rtParams.Format);
            GraphicResourceID resID = new GraphicResourceID(rtInfo.Resource.GetResourceHash());
            if (rtParams.HasDepthBuffer)
                rtInfo.DepthBuffer = device.CreateDepthBuffer(rtParams.CurWidth, rtParams.CurHeight);
            rtInfo.LastCaptureFrameID = -1;
            renderTargets.Add(resID, rtInfo);
            return resID;
        }

        private void UpdateRenderTargetResolutions()
        {
            foreach (GraphicResourceID rtID in renderTargets.Keys)
            {
                RTInfo rtInfo = renderTargets[rtID];
                if (!rtInfo.Desc.UsePercent)
                    continue; // only RTs that have their size expressed as a percent are resolution-dependent

                UpdateRtDimensions(ref rtInfo.Desc);

                // release current resources
                rtInfo.Resource.Release();
                if (rtInfo.Desc.HasDepthBuffer)
                    rtInfo.DepthBuffer.Release();

                // create new surfaces
                rtInfo.Resource = device.CreateRenderTarget(rtInfo.Desc.CurWidth, rtInfo.Desc.CurHeight, rtInfo.Desc.Format);
                if (rtInfo.Desc.HasDepthBuffer)
                    rtInfo.DepthBuffer = device.CreateDepthBuffer(rtInfo.Desc.CurWidth, rtInfo.Desc.CurHeight);
            }
        }

        protected override int renderTarget_GetHeight(GraphicResourceID resID)
        {
            return (int)renderTargets[resID].Desc.CurHeight;
        }

        protected override void renderTarget_SaveSnapshot(GraphicResourceID resID)
        {
            RTInfo rtInfo = renderTargets[resID];
            rtInfo.Resource.DownloadData(device, InnerCommandList, ref rtInfo.ReadBackBuffer);
            rtInfo.LastCaptureFrameID = frameID;
        }

        protected override void renderTarget_GetSnapshotData<T>(GraphicResourceID resID, T[] destBuffer)
        {
            RTInfo rtInfo = renderTargets[resID];
            if (rtInfo.LastCaptureFrameID < 0 || frameID == rtInfo.LastCaptureFrameID)
                throw new InvalidGraphicCallException("The render target has not been rendered yet!");

            if (frameID - rtInfo.LastCaptureFrameID < DF_Directx3D12.GetBackbufferCount())
            {
                device.WaitForGPU();
            }

            rtInfo.ReadBackBuffer.GetTextureData<T>(destBuffer, rtInfo.Desc.CurWidth, rtInfo.Desc.CurHeight);
        }

        protected override bool renderTarget_TryGetSnapshotData<T>(GraphicResourceID resID, T[] destBuffer)
        {
            RTInfo rtInfo = renderTargets[resID];
            if (rtInfo.LastCaptureFrameID < 0 || frameID - rtInfo.LastCaptureFrameID < DF_Directx3D12.GetBackbufferCount())
                return false;

            if (destBuffer != null)
            {
                rtInfo.ReadBackBuffer.GetTextureData<T>(destBuffer, rtInfo.Desc.CurWidth, rtInfo.Desc.CurHeight);
            }

            return true;
        }

        protected override void renderTarget_CopyToTexture(GraphicResourceID resID, GraphicResourceID destTexture)
        {
            RTInfo rtInfo = renderTargets[resID];
            TexInfo texInfo = textures[destTexture];
            InnerCommandList.CopyTextureRegion(texInfo.Resource, rtInfo.Resource);
        }

        protected override int renderTarget_GetWidth(GraphicResourceID resID)
        {
            return (int)renderTargets[resID].Desc.CurWidth;
        }

        protected override void renderTarget_Release(GraphicResourceID resID)
        {
            RTInfo rtInfo = renderTargets[resID];
            if (rtInfo.DepthBuffer != null)
                releaseList.DeferredRelease(rtInfo.DepthBuffer);
            releaseList.DeferredRelease(rtInfo.Resource);
            renderTargets.Remove(resID);
        }

        protected override void renderTarget_SetDepthWriteTarget(GraphicResourceID resID, RenderTarget depthWriteTarget)
        {
            RenderTargetParams curRtParams = renderTargets[resID].Desc;
            curRtParams.OverrideZBuffer = depthWriteTarget.ResourceID;
            renderTargets[resID].Desc = curRtParams;
        }

        #endregion

        #region Shader

        protected override GraphicResourceID createShader(string effectName, ShaderStates states, string variantID, string templateName, GraphicResourceID useParametersFromShader)
        {
            ShaderInfo shaderInfo = new ShaderInfo();
            GraphicResourceID id = new GraphicResourceID();

            // load shader local parameter info
            if (useParametersFromShader == null)
            {
                // prepare for a new local cbuffer
                CBufferBinding localBindings = (CBufferBinding)ShaderBindingTable.GetEffectInput(effectName, CBufferBinding.CreateName(false, 0));
                if (localBindings.ByteSize > 0)
                    shaderInfo.LocalCBuffer = new CBuffer(localBindings);
                updatedShaders.Add(id);
            }
            else
            {
                // the parent cbuffer will be used
                shaderInfo.Parent = shaders[useParametersFromShader];
            }

            shaders.Add(id, shaderInfo);

            // load PSO
            shaderInfo.PSO = psoAllocator.CreateNew();
            shaderInfo.PSO.BlendMode.Value = states.BlendMode;
            shaderInfo.PSO.CullMode.Value = states.CullMode;
            shaderInfo.PSO.DepthEnabled.Value = states.DepthBufferEnable;
            shaderInfo.PSO.DepthWriteEnabled.Value = states.DepthBufferWriteEnable;
            shaderInfo.PSO.FillMode.Value = states.FillMode;
            EffectBinding effect = ShaderBindingTable.GetEffect(effectName, templateName, variantID);
            shaderInfo.PSO.RenderTargetCount.Value = effect.TargetFormats.Length;
            for (int i = 0; i < effect.TargetFormats.Length; i++)
                shaderInfo.PSO.RenderTargetFormats[i].Value = effect.TargetFormats[i];
            shaderInfo.PSO.ShaderEffectName.Value = effectName;
            shaderInfo.PSO.ShaderVariantID.Value = variantID;
            shaderInfo.PSO.ShaderTemplateName.Value = templateName;
            shaderInfo.PSO.VertexType.Value = effect.InputLayout;
            
            // cache PSO states
            shaderInfo.PSO.Instanced.Value = false;
            psoCache.CacheState(shaderInfo.PSO);
            if (effect.SupportsInstancing)
            {
                // modify pso state for instancing and cache the pso resource
                shaderInfo.PSO.Instanced.Value = true;
                shaderInfo.PSO.VertexType.Value = DirectxUtils.AddInstanceMatrixTo(shaderInfo.PSO.VertexType);
                psoCache.CacheState(shaderInfo.PSO);

                // restore pso and save the vertex type modified for instancing
                shaderInfo.VertexTypeInstanced = shaderInfo.PSO.VertexType;
                shaderInfo.PSO.VertexType.Value = effect.InputLayout;
            }

            return id;
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
            DirectxPadder curPadder = padder.Value;
            TexInfo texInfo = textures[value.ResourceID];
            ShaderInfo sInfo = shaders[shader.ResourceID];
            shader_SetParamNoPad(shader, name, curPadder.Pad(texInfo.Resource.GetSrvIndex()));

            // update automatic texel size if used for this texture        
            string texelBindName = DirectxUtils.GetTexelSizeConstantName(name);
            if(sInfo.LocalCBuffer.Bindings.HasConstant(texelBindName))
            {
                shader_SetParamNoPad(shader, texelBindName, curPadder.Pad(1.0f / (Float2)texInfo.Resolution));
            }
        }

        protected override void shader_SetParam(Shader shader, string name, RenderTarget value)
        {
            DirectxPadder curPadder = padder.Value;
            RTInfo rtInfo = renderTargets[value.ResourceID];
            ShaderInfo sInfo = shaders[shader.ResourceID];
            shader_SetParamNoPad(shader, name, curPadder.Pad(rtInfo.Resource.GetSrvIndex()));

            // update automatic texel size if used for this texture        
            string texelBindName = DirectxUtils.GetTexelSizeConstantName(name);
            if (sInfo.LocalCBuffer.Bindings.HasConstant(texelBindName))
            {
                shader_SetParamNoPad(shader, texelBindName, curPadder.Pad(1.0f / new Float2(rtInfo.Desc.CurWidth, rtInfo.Desc.CurHeight)));
            }
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
            ShaderInfo sInfo = shaders[shader.ResourceID];
            sInfo.LocalCBuffer.SetValue(name, values);
            updatedShaders.Add(shader.ResourceID);
        }

        private void shader_SetParamNoPad(Shader shader, string name, int[] values)
        {
            ShaderInfo sInfo = shaders[shader.ResourceID];
            sInfo.LocalCBuffer.SetValue(name, values);
            updatedShaders.Add(shader.ResourceID);
        }

        protected override void shader_Release(GraphicResourceID resID)
        {
            ShaderInfo sInfo = shaders[resID];
            shaderCbuffers.ReleaseCBuffer(sInfo.LocalCBufferSlot);
            psoAllocator.Free(sInfo.PSO);
            shaders.Remove(resID);
        }

        #endregion

        #region Texture

        protected override GraphicResourceID createTexture<T>(int width, int height, SurfaceFormat format, T[] initialPixelData)
        {
            // create texture and add it to the tracked list
            TexInfo texInfo = new TexInfo();
            texInfo.Resource = device.CreateTexture((uint)width, (uint)height, DirectxUtils.SurfaceFormatToDX(format));
            texInfo.Resolution = new Int2(width, height);
            GraphicResourceID id = new GraphicResourceID(texInfo.Resource.GetResourceHash());
            textures.Add(id, texInfo);

            // update its data if needed
            if (initialPixelData != null)
            {
                texture_SetData<T>(id, initialPixelData, width);
            }
            
            return id;
        }

        protected override GraphicResourceID createTexture(byte[] fileData, out int width, out int height)
        {
            DF_Resource12 uploadResource;
            DF_Resource12 textureResource = device.CreateTexture(fileData, DirectxUtils.IsFileDDS(fileData), InnerCommandList, out uploadResource, out width, out height);
            GraphicResourceID id = new GraphicResourceID(textureResource.GetResourceHash());
            releaseList.DeferredRelease(uploadResource);        
            textures.Add(id, new TexInfo() { Resource = textureResource, Resolution = new Int2(width, height), FrequentUpdates = false });
            return id;
        }

        protected override void texture_Release(GraphicResourceID resID)
        {
            TexInfo texInfo = textures[resID];
            releaseList.DeferredRelease(texInfo.Resource);
            texInfo.ReleaseUploadBuffers(releaseList);
            textures.Remove(resID);
        }

        protected override void texture_SetData<T>(GraphicResourceID resID, T[] data, int rowLength)
        {
            TexInfo texInfo = textures[resID];

            // initializing more than one time, makes it dynamic
            if (texInfo.Initialized && !texInfo.FrequentUpdates)
            {
                texInfo.FrequentUpdates = true;
                texInfo.UploadBuffers = new DF_Resource12[DF_Directx3D12.GetBackbufferCount()];
            }

            if (texInfo.FrequentUpdates)
            {
                // reuse updload resource
                texInfo.Resource.UploadData<T>(device, data, InnerCommandList, ref texInfo.UploadBuffers[device.GetBackBufferIndex()]);
            }
            else
            {
                // create a new upload resource for this update and discard it once its done
                DF_Resource12 uploadResource = null;
                texInfo.Resource.UploadData<T>(device, data, InnerCommandList, ref uploadResource);
                releaseList.DeferredRelease(uploadResource);
                texInfo.Initialized = true;
            }
        }

        #endregion

        #region Vertex Buffer

        protected override GraphicResourceID createVertexBuffer(VertexType vtype, int vertexCount)
        {
            VBInfo vbInfo = new VBInfo();
            vbInfo.VertexByteSize = vtype.ByteSize;
            vbInfo.VertexCount = vertexCount;
            vbInfo.Buffer = device.CreateVertexBuffer(vtype.ByteSize, vertexCount, DF_CPUAccess.None);
            vbInfo.VertexType = vtype;
            GraphicResourceID id = new GraphicResourceID(vbInfo.Buffer.GetResourceHash());
            vertexBuffers.Add(id, vbInfo);
            return id;
        }

        protected override void vertexBuffer_Release(GraphicResourceID resID)
        {
            VBInfo vbInfo = vertexBuffers[resID];
            releaseList.DeferredRelease(vbInfo.Buffer);
            vbInfo.ReleaseUploadBuffers(releaseList);
            vertexBuffers.Remove(resID);
        }

        protected override void vertexBuffer_SetVertices<T>(GraphicResourceID resID, T[] vertices, int vertexCount)
        {
            VBInfo vbInfo = vertexBuffers[resID];
            if (vbInfo.VertexCount < vertexCount)
                vbInfo.ReleaseUploadBuffers(releaseList); // any existing upload buffer can no longer be used since too small
            vbInfo.VertexCount = vertexCount;

            // initializing more than one time, makes it dynamic
            if (vbInfo.Initialized && !vbInfo.FrequentUpdates)
            {
                vbInfo.FrequentUpdates = true;
                vbInfo.UploadBuffers = new DF_Resource12[DF_Directx3D12.GetBackbufferCount()];
            }

            // retrieve or create the updload buffer
            DF_Resource12 vbUploadBuffer = null;
            {
                if (vbInfo.FrequentUpdates)
                {
                    vbUploadBuffer = vbInfo.UploadBuffers[device.GetBackBufferIndex()];
                }
                if (vbUploadBuffer == null)
                    vbUploadBuffer = device.CreateVertexBuffer(vbInfo.VertexByteSize, vertexCount, DF_CPUAccess.Write);
                if (vbInfo.FrequentUpdates)
                    vbInfo.UploadBuffers[device.GetBackBufferIndex()] = vbUploadBuffer;
            }

            // updload data and copy it to the destination buffer
            vbUploadBuffer.SetData<T>(vertices, 0, 0, vertexCount, false);
            InnerCommandList.CopyBufferRegion(vbInfo.Buffer, 0, vbUploadBuffer, 0, (ulong)(vbInfo.VertexByteSize * vertexCount));
            vbInfo.Initialized = true;

            // release upload buffer if its not reused
            if (!vbInfo.FrequentUpdates)
                releaseList.DeferredRelease(vbUploadBuffer);
        }

        #endregion 
        
        protected override void release()
        {
            foreach(GraphicResourceID clID in commandLists.Keys.ToArray())
            {
                commandList_Release(clID);
            }

            foreach (RTInfo rtInfo in renderTargets.Values)
            {
                if (rtInfo.DepthBuffer != null)
                    rtInfo.DepthBuffer.Release();
                rtInfo.Resource.Release();
            }

            foreach (VBInfo vbInfo in vertexBuffers.Values)
                vbInfo.Buffer.Release();

            foreach (IBInfo ibInfo in indexBuffers.Values)
                ibInfo.Buffer.Release();

            foreach (TexInfo texInfo in textures.Values)
            {
                texInfo.Resource.Release();
                if (texInfo.UploadBuffers != null)
                {
                    foreach (DF_Resource12 uploadBuf in texInfo.UploadBuffers)
                        if (uploadBuf != null)
                            uploadBuf.Release();
                }
            }

            shaderCbuffers.Release();
            InnerCommandList.Release();
            device.Release();
        }
    }
}