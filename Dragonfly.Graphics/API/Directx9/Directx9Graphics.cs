using System;
using System.Collections.Generic;
using System.Linq;
using DragonflyGraphicsWrappers;
using DragonflyGraphicsWrappers.DX9;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Graphics.Shaders;
using Dragonfly.Graphics.API.Common;
using Dragonfly.Utils;

namespace Dragonfly.Graphics.API.Directx9
{
    internal class Directx9Graphics : DirectxGraphics
    {
        private DF_Directx3D9 directx;
        private DF_D3D9Device device;

        private class VTypeInfo
        {
            public DF_VertexDeclaration Declaration;
            public int TypeHash;
            public int RefCount;

            public VTypeInfo(DF_VertexDeclaration decl, int typeHash)
            {
                this.Declaration = decl;
                this.TypeHash = typeHash;
                this.RefCount = 1;
            }

            public void NewReference()
            {
                RefCount++;
            }

            public int ReleaseReference()
            {
                return --RefCount;
            }
        }

        //mapping dictionaries ( for binding options translation )
        private Dictionary<TextureBindingOptions, DF_TextureFilterType> filterMap;

        //directx resources
        private Dictionary<GraphicResourceID, DF_VertexBuffer> vertexBuffers;
        private Dictionary<GraphicResourceID, DF_IndexBuffer> indexBuffers;
        private Dictionary<GraphicResourceID, VTypeInfo> vertexTypes;
        private Dictionary<GraphicResourceID, DF_Texture> textures;
        private HashSet<GraphicResourceID> renderTargets;
        private Dictionary<GraphicResourceID, DF_Surface> rtDepthBuffers;
        private Dictionary<GraphicResourceID, DF_VertexShader> vertexShaders; // [resID] -> vertex shader
        private Dictionary<GraphicResourceID, DF_PixelShader> pixelShaders; // [resID] -> pixel shader
        private Dictionary<GraphicResourceID, DF_Surface> rtSnapshots;
        private Dictionary<GraphicResourceID, DF_Surface> rtSystemSnapshots; // render target content saved before resetting the device
        private Dictionary<GraphicResourceID, Directx9CmdList> cmdLists; // [resID] -> cmd list

        //graphic state variables
        private uint clipPlaneEnableMask;
        private VertexBuffer curVertexBuffer;
        private VertexType curVertexType;
        private IndexBuffer curIndexBuffer;
        private Shader curShader;
        private Dictionary<int, TextureBindingOptions> curSamplerConfig;
        private Dictionary<uint, GraphicResourceID> curTextureStages;
        private Dictionary<GraphicResourceID, int> curRenderTargets; // rt GraphicResourceID -> stage on which is set
        private Dictionary<GraphicResourceID, RenderTargetParams> renderTargetData; // rt creation data, used to create them back after device lost
        private Dictionary<int, DF_Surface> surfaces; // render target inded -> current render target surface
        private DF_Surface backBuffer, autoDepthBuffer;
        private ShaderStates curShaderStates;
        private Dictionary<GraphicResourceID, Dictionary<string, Directx9ShaderParamValue>> shaderParamValues; // [shader resID] -> list of shader params values
        private Dictionary<string, Directx9ShaderParamValue> globalParamValues; // [global shader param name] -> value
        private bool globalsChanged;
        private ViewportState viewport;

        // helpers
        private DirectxPadder padder;
        private GlobalTexManager<int> globalTextures;
        private CmdListCoordinator cmdListCoordinator;

        public Directx9Graphics(DFGraphicSettings settings, Directx9API api) : base(settings, api)
        {
            directx = new DF_Directx3D9();
            if (!directx.IsAvailable()) throw new UnsupportedAPIException();
            device = directx.CreateDevice(CurTarget, IsFullScreen, CurWidth, CurHeight, AntialiasingEnabled);

            vertexBuffers = new Dictionary<GraphicResourceID, DF_VertexBuffer>();
            indexBuffers = new Dictionary<GraphicResourceID, DF_IndexBuffer>();
            vertexTypes = new Dictionary<GraphicResourceID, VTypeInfo>();
            textures = new Dictionary<GraphicResourceID, DF_Texture>();
            renderTargets = new HashSet<GraphicResourceID>();
            vertexShaders = new Dictionary<GraphicResourceID, DF_VertexShader>();
            pixelShaders = new Dictionary<GraphicResourceID, DF_PixelShader>();
            surfaces = new Dictionary<int, DF_Surface>();
            renderTargetData = new Dictionary<GraphicResourceID, RenderTargetParams>();
            rtDepthBuffers = new Dictionary<GraphicResourceID, DF_Surface>();
            rtSnapshots = new Dictionary<GraphicResourceID, DF_Surface>();
            rtSystemSnapshots = new Dictionary<GraphicResourceID, DF_Surface>();
            shaderParamValues = new Dictionary<GraphicResourceID, Dictionary<string, Directx9ShaderParamValue>>();
            globalParamValues = new Dictionary<string, Directx9ShaderParamValue>();
            cmdLists = new Dictionary<GraphicResourceID, Directx9CmdList>();
            viewport = new ViewportState { Current = ViewportState.Default };

            //initialize graphic state
            resetGraphicState();

            //initialize sampler mapping translation
            filterMap = new Dictionary<TextureBindingOptions, DF_TextureFilterType>();
            filterMap[TextureBindingOptions.NoFilter] = DF_TextureFilterType.Point;
            filterMap[TextureBindingOptions.LinearFilter] = DF_TextureFilterType.Linear;
            filterMap[TextureBindingOptions.Anisotropic] = DF_TextureFilterType.Anisotropic;

            padder = new DirectxPadder();
            globalTextures = new GlobalTexManager<int>(ShaderBindingTable, SetGlobalTexture, SetGlobalTarget);
            cmdListCoordinator = new CmdListCoordinator();
        }

        private void resetGraphicState()
        {
            clipPlaneEnableMask = 0;
            curVertexBuffer = null;
            curVertexType = null;
            curIndexBuffer = null;
            curShader = null;
            backBuffer = null;
            autoDepthBuffer = null;
            curSamplerConfig = new Dictionary<int, TextureBindingOptions>();
            curTextureStages = new Dictionary<uint, GraphicResourceID>();
            curRenderTargets = new Dictionary<GraphicResourceID, int>();
            curShaderStates.BlendMode = BlendMode.Opaque;
            curShaderStates.CullMode = CullMode.CounterClockwise;
            curShaderStates.DepthBufferEnable = true;
            curShaderStates.DepthBufferWriteEnable = true;
            curShaderStates.FillMode = FillMode.Solid;
        }

        #region API Properties

        public override bool IsAvailable
        {
            get
            {
                device.TestCooperativeLevel();
                int state = DF_D3DErrors.GetLastErrorCode();
                switch (state)
                {
                    case D3D9CallResults.DEVICE_OK:
                        return true;

                    case D3D9CallResults.DEVICE_NOT_RESET:
                        {
                            try
                            {
                                resetDevice();
                            }
                            catch (D3DException)
                            {
                                return false;
                            }
                            device.TestCooperativeLevel();
                            return DF_D3DErrors.GetLastErrorCode() == D3D9CallResults.DEVICE_OK;
                        }

                    default:
                        return false;
                }
            }
        }

        protected void SetAlphaTestEnable(bool value)
        {
            device.SetRenderStateFLag(DF_RenderStateFlag.AlphaTestEnable, value);

        }

        protected void SetAlphaReference(float value)
        {
            device.SetAlphaRef((uint)value.ToByteInt());

        }

        public override List<Int2> SupportedDisplayResolutions => GraphicsAPI.DefaultDisplayResolutions;
        #endregion

        #region Graphic Calls

        private void BeforeDrawCall()
        {
            if (globalsChanged)
                SetShaderParams(curShader, globalParamValues);
            globalsChanged = false;
        }

        public override bool NewFrame()
        {
            if (!base.NewFrame()) return false;
            if (!this.IsAvailable) return false; // check / restore device
            cmdListCoordinator.NewFrame();
            device.BeginScene();
            return true;
        }

        public override void StartRender()
        {
            cmdListCoordinator.SolveRenderStages();

            // synchronously execute all closed command lists
            while(!cmdListCoordinator.EndOfFrame)
            {
                HashSet<GraphicResourceID> lists;
                cmdListCoordinator.ToBeExecuted.TryDequeue(out lists);
                foreach (GraphicResourceID cmdList in lists)
                    commandList_Execute(cmdList);
            }

            // end the current frame
            device.EndScene();
        }

        public override void DisplayRender()
        {
            device.Present();
        }

        protected override void release()
        {
            releaseAllResource();
            device.Release();
        }

        protected override void setScreen(IntPtr target, bool fullScreen, int width, int height)
        {
            CurTarget = target;
            IsFullScreen = fullScreen;
            CurWidth = width;
            CurHeight = height;
            resetDevice();
        }

        private void resetDevice()
        {
            releaseUnmanagedResources();

            device.Reset(CurTarget, IsFullScreen, CurWidth, CurHeight);
            resetGraphicState();

            restoreUnmanagedResources();
        }

        private void releaseUnmanagedResources()
        {
            device.TestCooperativeLevel();
            bool deviceAlreadyLost = DF_D3DErrors.GetLastErrorCode() != D3D9CallResults.DEVICE_OK;

            // render targets
            {
                GraphicResourceID[] ids = renderTargets.ToArray();
                for (int i = 0; i < ids.Length; i++)
                {
                    // save rt content to system mem
                    if (!deviceAlreadyLost)
                    {
                        RenderTargetParams rtParams = renderTargetData[ids[i]];
                        if (!rtParams.UsePercent) // save only fixed size render targets ( the others will just change resolution and cannot be restored later)
                        {
                            try
                            {
                                DF_Surface rtSurface = textures[ids[i]].GetSurfaceLevel(0);
                                rtSystemSnapshots[ids[i]] = device.GetRenderTargetData(rtSurface, rtParams.CurWidth, rtParams.CurHeight, rtParams.Format);
                                rtSurface.Release();
                            }
                            catch { } // may fail if device is already lost, render targets should be released anyway.
                        }
                    }

                    // release render target
                    textures[ids[i]].Release();
                    if (rtDepthBuffers[ids[i]] != null) rtDepthBuffers[ids[i]].Release();
                }
            }
        }

        private void restoreUnmanagedResources()
        {
            GraphicResourceID[] ids;

            // render targets
            ids = renderTargets.ToArray();
            for (int i = 0; i < ids.Length; i++)
            {
                // create render target
                RenderTargetParams rtData = renderTargetData[ids[i]];
                DF_Texture rt;
                DF_Surface rtDepth;
                createRenderTargetResources(ref rtData, out rt, out rtDepth);
                renderTargetData[ids[i]] = rtData;
                textures[ids[i]] = rt;
                rtDepthBuffers[ids[i]] = rtDepth;

                // restore render target data from the snapshot if available
                if (rtSystemSnapshots.ContainsKey(ids[i]))
                {
                    DF_Surface rtSurface = rt.GetSurfaceLevel(0);
                    device.SetRenderTargetData(rtSystemSnapshots[ids[i]], rtSurface);
                    rtSurface.Release();
                    rtSystemSnapshots[ids[i]].Release();
                    rtSystemSnapshots.Remove(ids[i]);
                }
            }
        }


        private void releaseAllResource()
        {
            GraphicResourceID[] ids;

            //vertex buffers 
            ids = vertexBuffers.Keys.ToArray();
            for (int i = 0; i < ids.Length; i++) vertexBuffer_Release(ids[i]);
            vertexBuffers.Clear();

            //index buffers
            ids = indexBuffers.Keys.ToArray();
            for (int i = 0; i < ids.Length; i++) indexBuffer_Release(ids[i]);
            indexBuffers.Clear();

            //textures and render targets (with surfaces)
            ids = textures.Keys.ToArray();
            for (int i = 0; i < ids.Length; i++)
            {
                if (renderTargets.Contains(ids[i]))
                    renderTarget_Release(ids[i]);
                else
                    texture_Release(ids[i]);
            }
            textures.Clear();
            renderTargets.Clear();
            renderTargetData.Clear();
            rtDepthBuffers.Clear();

            //shaders
            ids = vertexShaders.Keys.ToArray();
            for (int i = 0; i < ids.Length; i++) shader_Release(ids[i]);
            vertexShaders.Clear();
            pixelShaders.Clear();
        }


        private void SetShaderParams(Shader destShader, Dictionary<string, Directx9ShaderParamValue> paramList)
        {
            foreach (KeyValuePair<string, Directx9ShaderParamValue> p in paramList)
            {
                if (p.Value.FloatValues != null)
                {
                    this.device.SetVertexShaderConstantF(p.Value.Address, p.Value.FloatValues);
                    this.device.SetPixelShaderConstantF(p.Value.Address, p.Value.FloatValues);
                }
                else if (p.Value.TextureValue != null)
                {
                    SetTexture(destShader, p.Key, p.Value.TextureValue.ResourceID, p.Value.TextureValue.Resolution);
                }
#if DX9_INT_SUPPORT
                else if (p.Value.IntValues != null)
                {
                    this.device.SetVertexShaderConstantI(p.Value.Address, p.Value.IntValues);
                    this.device.SetPixelShaderConstantI(p.Value.Address, p.Value.IntValues);
                }
#endif
                else if (p.Value.RtValue != null)
                {
                    if (!curRenderTargets.ContainsKey(p.Value.RtValue.ResourceID))
                    {
                        SetTexture(destShader, p.Key, p.Value.RtValue.ResourceID, p.Value.RtValue.Resolution);
                    }
                }
                else if (p.Value.BoolValues != null)
                {
                    this.device.SetVertexShaderConstantB(p.Value.Address, p.Value.BoolValues);
                    this.device.SetPixelShaderConstantB(p.Value.Address, p.Value.BoolValues);
                }
            }
        }

        private void SetTexture(Shader shader, string name, GraphicResourceID textureID, Int2 resolution)
        {
            if (!textures.ContainsKey(textureID))
                return;

            TextureBinding binding = ShaderBindingTable.GetEffectInput(shader.EffectName, name) as TextureBinding;

            if (binding == null)
                return;

            //skip updating texture binding if the same texture is already on that texture stage
            uint bindIndex = (uint)binding.Address;
            if (curTextureStages.ContainsKey(bindIndex) && curTextureStages[bindIndex] == textureID)
                return;

            // set texture to device
            TextureBindingOptions texVisibility = binding.TexBindingOptions & TextureBindingOptions.Visibility;
            if (texVisibility != TextureBindingOptions.GeometryOnly)
                this.device.SetTexture(bindIndex, this.textures[textureID]);
            if (texVisibility != TextureBindingOptions.ShadingOnly)
                this.device.SetVertexShaderTexture(bindIndex, this.textures[textureID]);
            curTextureStages[bindIndex] = textureID;

            /*=====Convert sampler states=========*/

            bool configAvailable = curSamplerConfig.ContainsKey(binding.Address);
            TextureBindingOptions curConfig = configAvailable ? curSamplerConfig[binding.Address] : TextureBindingOptions.None;

            //if this configuration has not changed, skip sampler calls
            if (!configAvailable || curConfig != binding.TexBindingOptions)
            {

                //filtering
                TextureBindingOptions filter = binding.TexBindingOptions & TextureBindingOptions.Filter;
                if (!configAvailable || (curConfig & TextureBindingOptions.Filter) != filter)
                {
                    this.device.SetSamplerState(bindIndex, DF_SamplerStateType.MagFilter, (uint)filterMap[filter]);
                    this.device.SetSamplerState(bindIndex, DF_SamplerStateType.MinFilter, (uint)filterMap[filter]);
                    if (filter == TextureBindingOptions.Anisotropic)
                        this.device.SetSamplerState(bindIndex, DF_SamplerStateType.MaxAnisotropy, 16);
                }

                //mipmaps
                TextureBindingOptions mipMapMode = binding.TexBindingOptions & TextureBindingOptions.MipMapMode;
                if (!configAvailable || (curConfig & TextureBindingOptions.MipMapMode) != mipMapMode)
                {
                    if (mipMapMode == TextureBindingOptions.MipMaps)
                    //default!
                    {
                        this.device.SetSamplerState(bindIndex, DF_SamplerStateType.MipFilter, (uint)DF_TextureFilterType.Linear);
                        this.device.SetSamplerState(bindIndex, DF_SamplerStateType.MipMapBias, -0.9f);  // sharper mipmapping with a little more noise
                    }
                    else if (mipMapMode == TextureBindingOptions.NoMipMaps)
                    {
                        this.device.SetSamplerState(bindIndex, DF_SamplerStateType.MipFilter, (uint)DF_TextureFilterType.None);
                    }
                }

                //tex coords
                if (!configAvailable || (curConfig & TextureBindingOptions.Coords) != (binding.TexBindingOptions & TextureBindingOptions.Coords))
                {
                    TextureBindingOptions addressing = binding.TexBindingOptions & TextureBindingOptions.Coords;    
                    uint texCoords = (uint)DirectxUtils.AddressBindToDX(addressing);
                    this.device.SetSamplerState(bindIndex, DF_SamplerStateType.AddressX, texCoords);
                    this.device.SetSamplerState(bindIndex, DF_SamplerStateType.AddressY, texCoords);
                    this.device.SetSamplerState(bindIndex, DF_SamplerStateType.AddressZ, texCoords);
                    if (addressing  == TextureBindingOptions.BorderBlack ||
                        addressing == TextureBindingOptions.BorderTransparent ||
                        addressing == TextureBindingOptions.BorderWhite)
                    {
                        Float4 bgCol = new Float4();
                        DirectxUtils.AddressBindToDXBorderColor(addressing, out bgCol.X, out bgCol.Y, out bgCol.Z, out bgCol.W);
                        this.device.SetSamplerState(bindIndex, DF_SamplerStateType.BorderColor, device.GetDxColor(bgCol.X, bgCol.Y, bgCol.Z, bgCol.W));
                    }
                }

                //save current config (for optimizzation reasons)
                curSamplerConfig[binding.Address] = binding.TexBindingOptions;
            }
        }

        public void SetClipPlane(Float4 plane, int index)
        {
            device.SetClipPlane((uint)index, plane.X, plane.Y, plane.Z, plane.W);
            clipPlaneEnableMask |= (uint)(1 << index);
            device.SetClipPlaneEnableMask(clipPlaneEnableMask);
        }

        public void ResetClipPlanes()
        {
            clipPlaneEnableMask = 0;
            device.SetClipPlaneEnableMask(clipPlaneEnableMask);
        }

        /// <summary>
        /// Set the vertex declaration, if different from the available one.
        /// </summary>
        /// <param name="vtype"></param>
        private void setVertexType(GraphicResourceID vertexBufferID, VertexType vtype)
        {
            int vtypeHash = vertexTypes[vertexBufferID].TypeHash;
            if (curVertexType != null && curVertexType.GetHashCode() == vtypeHash) return;

            device.SetVertexDeclaration(vertexTypes[vertexBufferID].Declaration);

            this.curVertexType = vtype;
        }
        public void GetBackbufferData<T>(T[] destBuffer)
        {
            DF_Surface rtCopy = device.CopyRenderTarget(device.GetRenderTarget(0), (uint)CurWidth, (uint)CurHeight, DF_SurfaceFormat.A8R8G8B8);
            DF_Surface rtOffscreenCopy = device.GetRenderTargetData(rtCopy, (uint)CurWidth, (uint)CurHeight, DF_SurfaceFormat.A8R8G8B8);
            rtOffscreenCopy.GetData<T>(destBuffer, true);
            rtCopy.Release();
            rtOffscreenCopy.Release();
        }

        public System.Drawing.Bitmap BackbufferToImage()
        {
            byte[] bbPixels = new byte[CurWidth * CurHeight * 4];
            GetBackbufferData<byte>(bbPixels);
            RenderTarget.RtBytesClearAlpha(bbPixels, 255);
            System.Drawing.Bitmap screenShot = RenderTarget.RtBytesToBitmap(bbPixels, CurWidth, CurHeight);
            return screenShot;
        }

        private void disableRenderTarget(GraphicResourceID rtID)
        {
            if (curRenderTargets.ContainsKey(rtID))
            {
                int index = curRenderTargets[rtID];

                // release surface
                surfaces[index].Release();
                surfaces.Remove(index);

                // update state ( restore backbuffer if index was 0 )
                if (index == 0)
                {
                    device.SetRenderTarget((uint)index, backBuffer);
                    backBuffer.Release();
                    backBuffer = null;

                    device.SetDepthStencilBuffer(autoDepthBuffer);
                    autoDepthBuffer.Release();
                    autoDepthBuffer = null;
                }
                else
                {
                    device.SetRenderTarget((uint)index, null);
                }

                // update rt list
                curRenderTargets.Remove(rtID);
            }
        }

        #endregion

        #region VertexBuffer

        protected override GraphicResourceID createVertexBuffer(VertexType vtype, int vertexCount)
        {

            DF_VertexBuffer vb = device.CreateVertexBuffer((uint)(vertexCount * vtype.ByteSize), DF_Usage.WriteOnly);
            GraphicResourceID id = new GraphicResourceID(vb.GetResourceHash());
            createVertexType(vtype, id);
            vertexBuffers.Add(id, vb);
            return id;
        }

        private void createVertexType(VertexType vtype, GraphicResourceID vbID)
        {
            int vtypeID = vtype.GetHashCode();
            foreach (VTypeInfo vtypeInfo in vertexTypes.Values)
            {
                if (vtypeInfo.TypeHash == vtypeID)
                //this vertex type is already available
                {
                    vtypeInfo.NewReference();
                    vertexTypes.Add(vbID, vtypeInfo);
                    return;
                }
            }

            //create complete elements, VertexElement -> DF_VertexElement conversion
            VertexElement[] elems = vtype.Elements;
            DF_VertexElement[] dfElems = DirectxUtils.VertexTypeToDXElems(vtype);

            DF_VertexDeclaration vDecl = device.CreateVertexDeclaration(dfElems);
            vertexTypes.Add(vbID, new VTypeInfo(vDecl, vtype.GetHashCode()));
        }

        protected override void vertexBuffer_SetVertices<T>(GraphicResourceID resID, T[] vertices, int vertexCount)
        {
            this.vertexBuffers[resID].SetVertices<T>(0, (uint)vertexCount, vertices);
        }

        protected override void vertexBuffer_Release(GraphicResourceID resID)
        {
            if (device.Released) return;

            //remove from device before releasing
            if (curVertexBuffer != null && curVertexBuffer.ResourceID == resID)
            {
                device.SetStreamSource(0, null, 0, 0);
                curVertexBuffer = null;
                device.SetVertexDeclaration(null);
                curVertexType = null;
            }

            this.vertexBuffers[resID].Release();
            this.vertexBuffers.Remove(resID);
            //release associated vertex type
            if (vertexTypes[resID].ReleaseReference() == 0)
            {
                vertexTypes[resID].Declaration.Release();
            }
            this.vertexTypes.Remove(resID);
        }

        #endregion

        #region IndexBuffer

        protected override GraphicResourceID createIndexBuffer(int indexCount)
        {
            DF_IndexBuffer ib = device.CreateIndexBuffer((uint)indexCount * 2);
            GraphicResourceID id = new GraphicResourceID(ib.GetResourceHash());
            indexBuffers.Add(id, ib);
            return id;
        }

        protected override void indexBuffer_SetIndices(GraphicResourceID resID, ushort[] indices, int indexCount)
        {
            this.indexBuffers[resID].SetIndices(0, (uint)indexCount, indices);
        }

        protected override void indexBuffer_Release(GraphicResourceID resID)
        {
            if (device.Released) return;
            if (!indexBuffers.ContainsKey(resID)) return;

            if (curIndexBuffer != null && curIndexBuffer.ResourceID == resID)
            //remove from device before releasing
            {
                device.SetIndexBuffer(null);
                curIndexBuffer = null;
            }
            this.indexBuffers[resID].Release();
            this.indexBuffers.Remove(resID);
        }

        #endregion

        #region Texture

        protected override GraphicResourceID createTexture<T>(int width, int height, SurfaceFormat format, T[] initialData)
        {
            DF_Texture tex = device.CreateTexture((uint)width, (uint)height, DF_Usage.None, DirectxUtils.SurfaceFormatToDX(format), false);
            GraphicResourceID id = new GraphicResourceID(tex.GetResourceHash());
            textures.Add(id, tex);
            if (initialData != null)
                texture_SetData<T>(id, initialData, initialData.Length / height);
            return id;
        }

        protected override GraphicResourceID createTexture(byte[] fileData, out int width, out int height)
        {
            DF_Texture tex = device.CreateTexture(fileData, out width, out height);
            GraphicResourceID id = new GraphicResourceID(tex.GetResourceHash());
            textures.Add(id, tex);
            return id;
        }

        protected /*override*/ void texture_GetData<T>(GraphicResourceID resID, T[] destBuffer, bool discard)
        {
            DF_Surface texSurface = this.textures[resID].GetSurfaceLevel(0);
            texSurface.GetData<T>(destBuffer, discard);
            texSurface.Release();
        }

        protected override void texture_SetData<T>(GraphicResourceID resID, T[] srcBuffer, int rowLength)
        {
            DF_Surface texSurface = this.textures[resID].GetSurfaceLevel(0);
            texSurface.SetData<T>(srcBuffer);
            texSurface.Release();
        }

        protected override void texture_Release(GraphicResourceID resID)
        {
            if (device.Released) return;
            if (!textures.ContainsKey(resID)) return;

            // serach texture stages for this texture...
            foreach (uint stage in curTextureStages.Keys)
            {
                if (resID == curTextureStages[stage])
                {
                    // if found, unbind it
                    this.device.SetTexture(stage, null);
                    curTextureStages.Remove(stage);
                    break;
                }
            }

            this.textures[resID].Release();
            this.textures.Remove(resID);
        }

        #endregion

        #region RenderTarget

        protected override GraphicResourceID CreateDirectxRenderTarget(RenderTargetParams rtData)
        {
            // create rt and its id
            DF_Texture rt;
            DF_Surface rtDepth;
            createRenderTargetResources(ref rtData, out rt, out rtDepth);
            GraphicResourceID id = new GraphicResourceID(rt.GetResourceHash());

            // save engine references
            textures.Add(id, rt);
            renderTargets.Add(id);
            renderTargetData.Add(id, rtData);
            rtDepthBuffers.Add(id, rtDepth);
            return id;
        }

        private void createRenderTargetResources(ref RenderTargetParams rtData, out DF_Texture rt, out DF_Surface rtDepth)
        {
            UpdateRtDimensions(ref rtData);
            rt = device.CreateTexture(rtData.CurWidth, rtData.CurHeight, DF_Usage.RenderTarget, rtData.Format, false);
            rtDepth = rtData.HasDepthBuffer ? device.CreateDepthStencilBuffer(rtData.CurWidth, rtData.CurHeight, rtData.Antialiased) : null;
        }

        protected override void renderTarget_Release(GraphicResourceID resID)
        {
            if (device.Released) return;
            if (!textures.ContainsKey(resID)) return;

            // disable it, in case it was set elsewhere
            disableRenderTarget(resID);

            textures[resID].Release();
            textures.Remove(resID);
            renderTargets.Remove(resID);
            renderTargetData.Remove(resID);
            if (rtDepthBuffers[resID] != null) rtDepthBuffers[resID].Release();
            rtDepthBuffers.Remove(resID);
            if (rtSnapshots.ContainsKey(resID))
            {
                rtSnapshots[resID].Release();
                rtSnapshots.Remove(resID);
            }
        }

        protected override int renderTarget_GetWidth(GraphicResourceID resID)
        {
            return (int)renderTargetData[resID].CurWidth;
        }

        protected override int renderTarget_GetHeight(GraphicResourceID resID)
        {
            return (int)renderTargetData[resID].CurHeight;
        }

        protected override void renderTarget_SaveSnapshot(GraphicResourceID resID)
        {
            disableRenderTarget(resID);

            if (rtSnapshots.ContainsKey(resID))
                rtSnapshots[resID].Release();

            DF_Surface rtSurface = this.textures[resID].GetSurfaceLevel(0);
            RenderTargetParams rtParams = renderTargetData[resID];

            rtSnapshots[resID] = device.GetRenderTargetData(rtSurface, rtParams.CurWidth, rtParams.CurHeight, rtParams.Format);
            rtSurface.Release();
        }

        protected override bool renderTarget_TryGetSnapshotData<T>(GraphicResourceID resID, T[] destBuffer)
        {
            if (!rtSnapshots.ContainsKey(resID))
                return false;

            if (destBuffer != null)
                rtSnapshots[resID].GetData<T>(destBuffer, true);

            return true;
        }

        protected override void renderTarget_GetSnapshotData<T>(GraphicResourceID resID, T[] destBuffer)
        {
            rtSnapshots[resID].GetData<T>(destBuffer, true);
        }

        protected override void renderTarget_CopyToTexture(GraphicResourceID resID, GraphicResourceID destTexture)
        {
            disableRenderTarget(resID);

            // copy render target to an offscreen surface
            DF_Surface rtSurface = textures[resID].GetSurfaceLevel(0);
            RenderTargetParams rtParams = renderTargetData[resID];
            DF_Surface offScreenSurface = device.GetRenderTargetData(rtSurface, rtParams.CurWidth, rtParams.CurHeight, rtParams.Format);
            rtSurface.Release();

            // copy the surface to the destination texture
            DF_Surface texSurface = textures[destTexture].GetSurfaceLevel(0);
            device.SetTextureData(offScreenSurface, texSurface, renderTargetData[resID].CurHeight);
            texSurface.Release();
        }

        protected override void renderTarget_SetDepthWriteTarget(GraphicResourceID resID, RenderTarget depthWriteTarget)
        {
            RenderTargetParams curRtParams = renderTargetData[resID];
            curRtParams.OverrideZBuffer = depthWriteTarget.ResourceID;
            renderTargetData[resID] = curRtParams;
        }

        #endregion

        #region Shader

        protected override GraphicResourceID createShader(string effectName, ShaderStates states, string varID, string templateName, GraphicResourceID useParametersFromShader)
        {
            EffectBinding effect = ShaderBindingTable.GetEffect(effectName, templateName, varID);

            //create VS
            DF_VertexShader vs = device.CreateVertexShader(ShaderBindingTable.GetProgram(effect.VSName));

            //create PS
            DF_PixelShader ps = device.CreatePixelShader(ShaderBindingTable.GetProgram(effect.PSName));

            // save effect programs instances
            GraphicResourceID id = new GraphicResourceID(vs.GetResourceHash());
            vertexShaders.Add(id, vs);
            pixelShaders.Add(id, ps);

            // prepare param value list
            if (useParametersFromShader != null)
                shaderParamValues[id] = shaderParamValues[useParametersFromShader];
            else
                shaderParamValues[id] = new Dictionary<string, Directx9ShaderParamValue>();

            return id;
        }

        protected override void shader_SetParam(Shader shader, string name, bool value)
        {
            InputBinding binding = ShaderBindingTable.GetEffectInput(shader == null ? Directx9ShaderCompiler.GLOBAL_PARAMS_TAG : shader.EffectName, name);
            (shader == null ? globalParamValues : shaderParamValues[shader.ResourceID])[name] = new Directx9ShaderParamValue(value) { Address = (uint)binding.Address };
            globalsChanged |= shader == null;
        }

        protected override void shader_SetParam(Shader shader, string name, int value)
        {
#if !DX9_INT_SUPPORT
            shader_SetParam(shader, name, (float)value);
#else
            int[] paddedValue = padder.Pad(value);
            InputBinding binding = ShaderBindingTable.GetEffectInput(shader.EffectName, name);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)binding.Address };
#endif
        }

        protected override void shader_SetParam(Shader shader, string name, Int3 value)
        {
#if !DX9_INT_SUPPORT
            shader_SetParam(shader, name, (Float3)value);
#else
            int[] paddedValue = padder.Pad(value);
            InputBinding binding = ShaderBindingTable.GetEffectInput(shader.EffectName, name);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)binding.Address };
#endif
        }

        private InputBinding GetInputBinding(Shader s, string paramName)
        {
            return s == null ? ShaderBindingTable.GetInput(Directx9ShaderCompiler.GLOBAL_PARAMS_TAG, paramName) : ShaderBindingTable.GetEffectInput(s.EffectName, paramName);
        }

        protected override void shader_SetParam(Shader shader, string name, float value)
        {
            float[] paddedValue = padder.Pad(value);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, Float2 value)
        {
            float[] paddedValue = padder.Pad(value);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, Float3 value)
        {
            float[] paddedValue = padder.Pad(value);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, Float4 value)
        {
            float[] paddedValue = padder.Pad(value);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, Float4x4 value)
        {
            float[] paddedValue = padder.Pad(value);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, Float3x3 value)
        {
            float[] paddedValue = padder.Pad(value);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, Float2[] values)
        {
            float[] paddedValue = padder.Pad(values);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, Float3[] values)
        {
            float[] paddedValue = padder.Pad(values);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, Float4[] values)
        {
            float[] paddedValue = padder.Pad(values);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, Float4x4[] values)
        {
            float[] paddedValue = padder.Pad(values);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, int[] values)
        {
#if !DX9_INT_SUPPORT
            float[] paddedValue = padder.PadAsFloat(values);
#else
            int[] paddedValues = padder.Pad(values);
#endif
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, float[] values)
        {
            float[] paddedValue = padder.Pad(values);
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(shader, name).Address };
        }

        protected override void shader_SetParam(Shader shader, string name, Texture value)
        {
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue() { TextureValue = value };
            updateTexelSizeParam(shader, name, value.Resolution);
        }

        protected override void shader_SetParam(Shader shader, string name, RenderTarget value)
        {
            shaderParamValues[shader.ResourceID][name] = new Directx9ShaderParamValue() { RtValue = value };
            updateTexelSizeParam(shader, name, value.Resolution);
        }

        private void updateTexelSizeParam(Shader shader, string name, Int2 resolution)
        {
            // update automatic texel size if used for this texture        
            string texelBindName = DirectxUtils.GetTexelSizeConstantName(name);
            if (ShaderBindingTable.ContainsEffectInput(shader.EffectName, texelBindName))
                shader_SetParam(shader, texelBindName, 1.0f / (Float2)resolution);
        }


        protected override void shader_Release(GraphicResourceID resID)
        {
            if (device.Released) return;
            if (!vertexShaders.ContainsKey(resID)) return;

            if (curShader != null && curShader.ResourceID == resID)
            {
                // remove from device before releasing
                device.SetVertexShader(null);
                device.SetPixelShader(null);
                curShader = null;
            }

            // release resources
            vertexShaders[resID].Release();
            vertexShaders.Remove(resID);
            pixelShaders[resID].Release();
            pixelShaders.Remove(resID);
            shaderParamValues.Remove(resID);
        }

        private AARect GetScaledViewport()
        {
            uint activeWidth = (uint)CurWidth, activeHeight = (uint)CurHeight;
            if (surfaces.ContainsKey(0))
            {
                GraphicResourceID rtId = curRenderTargets.First(rtBind => rtBind.Value == 0).Key;
                RenderTargetParams rtParams = renderTargetData[rtId];
                activeWidth = rtParams.CurWidth;
                activeHeight = rtParams.CurHeight;
            }
            return viewport.Current * new Float2(activeWidth, activeHeight);
        }

        #endregion

        #region CommandList

        protected override GraphicResourceID createCommandList()
        {
            GraphicResourceID id = new GraphicResourceID(DF_Resource.ReserveNewID());
            Directx9CmdList cmdList = new Directx9CmdList(id);
            cmdLists[id] = cmdList;
            return id;
        }
        protected override void commandList_StartRecording(GraphicResourceID resID, IReadOnlyList<GraphicResourceID> requiredLists, bool flushRequired)
        {
            Directx9CmdList c = cmdLists[resID];
            c.Reset();
            cmdListCoordinator.DeclareList(resID, requiredLists);
            if (flushRequired)
                cmdListCoordinator.SolveRenderStages();
        }

        protected override void commandList_QueueExecution(GraphicResourceID resID)
        {
            cmdListCoordinator.QueueExecution(resID);
        }

        private void commandList_Execute(GraphicResourceID resID)
        {
            Directx9CmdList c = cmdLists[resID];

            // execute all commands in the list
            int bi = 0, rti = 0, ii = 0, vi = 0, si = 0, ibi = 0, rci = 0, sti = 0, txi = 0, i3i = 0;
            int fi = 0, f2i = 0, f3i = 0, f4i = 0, f44i = 0, f33i = 0, fai = 0, f2ai = 0, f3ai = 0, f4ai = 0, f44ai = 0, iai = 0;
            
            for (int i = 0; i < c.Cmds.Count; i++)
            {
                switch (c.Cmds[i])
                {
                    case Directx9CmdType.ClearSurfaces:
                        commandList_ClearSurfaces_Impl(c.ResourceID, c.Float4s[f4i++], c.Bools[bi++], c.Bools[bi++]);
                        break;
                    case Directx9CmdType.Draw:
						commandList_Draw_Impl(c.ResourceID);
                        break;
                    case Directx9CmdType.SetRenderTarget:
						commandList_SetRenderTarget_Impl(c.ResourceID, c.RTs[rti++], c.Ints[ii++]);
                        break;
                    case Directx9CmdType.DrawIndexed:
						commandList_DrawIndexed_Impl(c.ResourceID);
                        break;
                    case Directx9CmdType.DrawIndexedInstanced:
						commandList_DrawIndexedInstanced_Impl(c.ResourceID, c.Insts, c.Ints[ii++], c.Ints[ii++]);
                        break;
                    case Directx9CmdType.DisableRenderTarget:
						commandList_DisableRenderTarget_Impl(c.ResourceID, c.RTs[rti++]);
                        break;
                    case Directx9CmdType.SetVertices:
						commandList_SetVertices_Impl(c.ResourceID, c.VBs[vi++]);
                        break;
                    case Directx9CmdType.SetShader:
						commandList_SetShader_Impl(c.ResourceID, c.Shaders[si++]);
                        break;
                    case Directx9CmdType.ResetRenderTargets:
						commandList_ResetRenderTargets_Impl(c.ResourceID);
                        break;
                    case Directx9CmdType.SetViewport:
						commandList_SetViewport_Impl(c.ResourceID, c.Rects[rci++]);
                        break;
                    case Directx9CmdType.SetIndices:
						commandList_SetIndices_Impl(c.ResourceID, c.IBs[ibi++]);
                        break;
                    case Directx9CmdType.SetParamBool:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Bools[bi++]);
                        break;
                    case Directx9CmdType.SetParamFloat:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Floats[fi++]);
                        break;
                    case Directx9CmdType.SetParamFloat2:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Float2s[f2i++]);
                        break;
                    case Directx9CmdType.SetParamFloat3:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Float3s[f3i++]);
                        break;
                    case Directx9CmdType.SetParamFloat4:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Float4s[f4i++]);
                        break;
                    case Directx9CmdType.SetParamFloat4x4:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Float4x4s[f44i++]);
                        break;
                    case Directx9CmdType.SetParamFloat3x3:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Float3x3s[f33i++]);
                        break;
                    case Directx9CmdType.SetParamInt3:
                        commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Int3s[i3i++]);
                        break;
                    case Directx9CmdType.SetParamFloat2Array:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Float2As[f2ai++]);
                        break;
                    case Directx9CmdType.SetParamFloat3Array:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Float3As[f3ai++]);
                        break;
                    case Directx9CmdType.SetParamFloat4Array:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Float4As[f4ai++]);
                        break;
                    case Directx9CmdType.SetParamFloat4x4Array:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Float4x4As[f44ai++]);
                        break;
                    case Directx9CmdType.SetParamIntArray:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.IntAs[iai++]);
                        break;
                    case Directx9CmdType.SetParamFloatArray:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.FloatAs[fai++]);
                        break;
                    case Directx9CmdType.SetParamTexture:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Textures[txi++]);
                        break;
                    case Directx9CmdType.SetParamRT:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.RTs[rti++]);
                        break;
                    case Directx9CmdType.SetParamInt:
						commandList_SetParam_Impl(c.ResourceID, c.Strings[sti++], c.Ints[ii++]);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        protected override void commandList_Release(GraphicResourceID resID)
        {
            cmdLists.Remove(resID);
        }

        protected override void commandList_ClearSurfaces(GraphicResourceID resID, Float4 clearValue, ClearFlags flags)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.ClearSurfaces);
			c.Float4s.Add(clearValue);
            bool clearTargets = (flags & ClearFlags.ClearTargets) == ClearFlags.ClearTargets;
            c.Bools.Add(clearTargets);
            bool clearDepth = (flags & ClearFlags.ClearDepth) == ClearFlags.ClearDepth;
            c.Bools.Add(clearDepth);
        }

        private void commandList_ClearSurfaces_Impl(GraphicResourceID resID, Float4 clearValue, bool clearTargets, bool clearDepth)
        {
            if (viewport.Current != ViewportState.Default)
            {
                AARect scaledVp = GetScaledViewport();
                device.Clear(clearValue.IntR, clearValue.IntG, clearValue.IntB, clearValue.IntA, clearTargets, clearDepth, (int)scaledVp.X1, (int)scaledVp.Y1, (int)scaledVp.X2, (int)scaledVp.Y2);
            }
            else
                device.Clear(clearValue.IntR, clearValue.IntG, clearValue.IntB, clearValue.IntA, clearTargets, clearDepth);
        }

        protected override void commandList_Draw(GraphicResourceID resID)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.Draw);
		}

		private void commandList_Draw_Impl(GraphicResourceID resID)
        {
            BeforeDrawCall();
            int primitiveCount = curVertexBuffer.VertexCount / 3;
            device.DrawPrimitive(
                DF_PrimitiveType.TriangleList,
                (uint)0,
                (uint)primitiveCount
            );
        }

        protected override void commandList_SetRenderTarget(GraphicResourceID resID, RenderTarget rt, int index)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetRenderTarget);
            c.RTs.Add(rt);
            c.Ints.Add(index);
		}

		private void commandList_SetRenderTarget_Impl(GraphicResourceID resID, RenderTarget rt, int index)
        {
            viewport.Reset();

            if (rt == null) throw new InvalidGraphicCallException("RenterTarget cannot be null.");

            // disable it, in case it was already set elsewhere
            disableRenderTarget(rt.ResourceID);

            // disable rt set on the same slot (if any)
            GraphicResourceID sameSlotRtID = curRenderTargets.FirstOrDefault(rtBind => rtBind.Value == index).Key;
            if (!ReferenceEquals(sameSlotRtID, null)) disableRenderTarget(sameSlotRtID);

            if (index == 0 && backBuffer == null)
            {
                //save backbuffer and auto depth stencil
                backBuffer = device.GetRenderTarget(0);
                autoDepthBuffer = device.GetDepthStencilBuffer();
            }

            DF_Surface rtSurface = textures[rt.ResourceID].GetSurfaceLevel(0);
            device.SetRenderTarget((uint)index, rtSurface);
            if (index == 0)
            {
                GraphicResourceID depthID = renderTargetData[rt.ResourceID].OverrideZBuffer;
                if (depthID == null) depthID = rt.ResourceID;
                device.SetDepthStencilBuffer(rtDepthBuffers[depthID]);
            }

            // update current surface list
            if (surfaces.ContainsKey(index))
            {
                //release previous surface
                surfaces[index].Release();
            }
            surfaces[index] = rtSurface;

            // update current rt list
            curRenderTargets[rt.ResourceID] = index;
        }

        protected override void commandList_DrawIndexed(GraphicResourceID resID)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.DrawIndexed);
		}

		private void commandList_DrawIndexed_Impl(GraphicResourceID resID)
        {
            BeforeDrawCall();
            int primitiveCount = curIndexBuffer.IndexCount / 3;
            device.DrawIndexedPrimitive(
                DF_PrimitiveType.TriangleList,
                0,
                0,
                (uint)curVertexBuffer.VertexCount,
                0,
                (uint)primitiveCount
            );
        }
        protected override void commandList_DrawIndexedInstanced(GraphicResourceID resID, ArrayRange<Float4x4> instances)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.DrawIndexedInstanced);
            c.SaveInstances(instances);
		}

		private void commandList_DrawIndexedInstanced_Impl(GraphicResourceID resID, Float4x4[] instances, int startIndex, int count)
        {
            BeforeDrawCall();
            float[] paddedValue;
            uint instMatrixAddress = (uint)ShaderBindingTable.GetEffectInput(curShader.EffectName, ShaderCompiler.INSTANCE_MATRIX_NAME).Address;

            for (int i = 0; i < count; i++)
            {
                paddedValue = padder.Pad(instances[i + startIndex]);
                device.SetVertexShaderConstantF(instMatrixAddress, paddedValue);
                device.SetPixelShaderConstantF(instMatrixAddress, paddedValue);
                commandList_DrawIndexed_Impl(resID);
            }
            paddedValue = padder.Pad(Float4x4.Identity);
            device.SetVertexShaderConstantF(instMatrixAddress, paddedValue);
            device.SetPixelShaderConstantF(instMatrixAddress, paddedValue);
        }

        protected override void commandList_DisableRenderTarget(GraphicResourceID resID, RenderTarget rt)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.DisableRenderTarget);
            c.RTs.Add(rt);
		}

		private void commandList_DisableRenderTarget_Impl(GraphicResourceID resID, RenderTarget rt)
        {
            disableRenderTarget(rt.ResourceID);
        }

        protected override void commandList_SetIndices(GraphicResourceID resID, IndexBuffer indices)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetIndices);
            c.IBs.Add(indices);
		}

		private void commandList_SetIndices_Impl(GraphicResourceID resID, IndexBuffer indices)
        {
            device.SetIndexBuffer(indexBuffers[indices.ResourceID]);
            this.curIndexBuffer = indices;
        }
        protected override void commandList_SetVertices(GraphicResourceID resID, VertexBuffer vertices)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetVertices);
            c.VBs.Add(vertices);
		}

		private void commandList_SetVertices_Impl(GraphicResourceID resID, VertexBuffer vertices)
        {
            this.setVertexType(vertices.ResourceID, vertices.VertexType);
            device.SetStreamSource(
                0,
                vertexBuffers[vertices.ResourceID],
                0,
                (uint)vertices.VertexType.ByteSize
            );

            this.curVertexBuffer = vertices;
        }

        protected override void commandList_SetShader(GraphicResourceID resID, Shader shader)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetShader);
            c.Shaders.Add(shader);
		}

		private void commandList_SetShader_Impl(GraphicResourceID resID, Shader shader)
        {
            // update global textures
            globalTextures.MainContext.UpateShader(shader, 0);

            // update shader parameters
            SetShaderParams(shader, shaderParamValues[shader.ResourceID]);

            // set programs
            device.SetVertexShader(vertexShaders[shader.ResourceID]);
            device.SetPixelShader(pixelShaders[shader.ResourceID]);

            // set states if changed
            if (curShaderStates != shader.States)
            {
                device.SetRenderStateFLag(DF_RenderStateFlag.ZEnable, shader.States.DepthBufferEnable);
                device.SetRenderStateFLag(DF_RenderStateFlag.ZWriteEnable, shader.States.DepthBufferWriteEnable);
                device.SetRenderStateFLag(DF_RenderStateFlag.AlphaBlendEnable, shader.States.BlendMode != BlendMode.Opaque);
                if (shader.States.BlendMode != BlendMode.Opaque)
                {
                    device.SetSourceBlend(DF_BlendMode.SrcAlpha);
                    device.SetDestinationBlend(DF_BlendMode.InvSrcAlpha);
                }
                device.SetFillMode(DirectxUtils.FillModeToDX(shader.States.FillMode));
                device.SetCullMode(DirectxUtils.CullModeToDX(shader.States.CullMode));

                curShaderStates = shader.States;
            }

            // reset instance matrix if any
            if (ShaderBindingTable.ContainsEffectInput(shader.EffectName, ShaderCompiler.INSTANCE_MATRIX_NAME))
            {
                uint instMatrixAddress = (uint)ShaderBindingTable.GetEffectInput(shader.EffectName, ShaderCompiler.INSTANCE_MATRIX_NAME).Address;
                float[] paddedValue = padder.Pad(Float4x4.Identity);
                device.SetVertexShaderConstantF(instMatrixAddress, paddedValue);
                device.SetPixelShaderConstantF(instMatrixAddress, paddedValue);
            }

            this.curShader = shader;
        }

        private void SetGlobalTexture(Shader shader, string paramName, Texture value, int unused)
        {
            SetTexture(shader, paramName, value.ResourceID, value.Resolution);
            updateTexelSizeParam(shader, paramName, value.Resolution);
        }

        private void SetGlobalTarget(Shader shader, string paramName, RenderTarget value, int unused)
        {
            if (!curRenderTargets.ContainsKey(value.ResourceID))
            {
                SetTexture(shader, paramName, value.ResourceID, value.Resolution);
                updateTexelSizeParam(shader, paramName, value.Resolution);
            }
        }

        protected override void commandList_SetViewport(GraphicResourceID resID, AARect viewport)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetViewport);
            c.Rects.Add(viewport);
		}

		private void commandList_SetViewport_Impl(GraphicResourceID resID, AARect viewport)
        {
            if (viewport == this.viewport.Current)
                return;
            this.viewport.Current = viewport;
            AARect scaledVp = GetScaledViewport();
            device.SetViewport((uint)scaledVp.X1, (uint)scaledVp.Y1, (uint)scaledVp.Size.X, (uint)scaledVp.Size.Y);
        }

        protected override void commandList_ResetRenderTargets(GraphicResourceID resID)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.ResetRenderTargets);
		}

		private void commandList_ResetRenderTargets_Impl(GraphicResourceID resID)
        {
            viewport.Reset();

            if (curRenderTargets.Count == 0) return;

            GraphicResourceID[] curRenderTargetIDs = curRenderTargets.Keys.ToArray();
            for (int i = 0; i < curRenderTargetIDs.Length; i++)
            {
                disableRenderTarget(curRenderTargetIDs[i]);
            }
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, bool value)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamBool);
            c.Strings.Add(name);
            c.Bools.Add(value);
		}

		private void commandList_SetParam_Impl(GraphicResourceID resID, string name, bool value)
        {
            InputBinding binding = ShaderBindingTable.GetEffectInput(Directx9ShaderCompiler.GLOBAL_PARAMS_TAG, name);
            globalParamValues[name] = new Directx9ShaderParamValue(value) { Address = (uint)binding.Address };
            globalsChanged = true;
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, int value)
        {
#if !DX9_INT_SUPPORT
            commandList_SetParam(resID, name, (float)value);
#else
            int[] paddedValue = padder.Pad(value);
            InputBinding binding = ShaderBindingTable.GetEffectInput(Directx9ShaderCompiler.GLOBAL_PARAMS_TAG, name);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)binding.Address };
#endif
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, int value)       
        {
            Directx9CmdList c = cmdLists[resID];
            c.Cmds.Add(Directx9CmdType.SetParamInt);
            c.Strings.Add(name);
            c.Ints.Add(value);
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, float value)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloat);
            c.Strings.Add(name);
            c.Floats.Add(value);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, float value)
        {
            float[] paddedValue = padder.Pad(value);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float2 value)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloat2);
            c.Strings.Add(name);
            c.Float2s.Add(value);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Float2 value)
        {
            float[] paddedValue = padder.Pad(value);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float3 value)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloat3);
            c.Strings.Add(name);
            c.Float3s.Add(value);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Float3 value)
        {
            float[] paddedValue = padder.Pad(value);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4 value)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloat4);
            c.Strings.Add(name);
            c.Float4s.Add(value);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Float4 value)
        {
            float[] paddedValue = padder.Pad(value);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4x4 value)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloat4x4);
            c.Strings.Add(name);
            c.Float4x4s.Add(value);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Float4x4 value)
        {
            float[] paddedValue = padder.Pad(value);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float3x3 value)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloat3x3);
            c.Strings.Add(name);
            c.Float3x3s.Add(value);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Float3x3 value)
        {
            float[] paddedValue = padder.Pad(value);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Int3 value)
        {
            Directx9CmdList c = cmdLists[resID];
            c.Cmds.Add(Directx9CmdType.SetParamInt3);
            c.Strings.Add(name);
            c.Int3s.Add(value);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Int3 value)
        {
#if DX9_INT_SUPPORT
            int[] paddedValue = padder.Pad(value);
#else
            float[] paddedValue = padder.Pad((Float3)value);
#endif
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float2[] values)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloat2Array);
            c.Strings.Add(name);
            c.Float2As.Add(values);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Float2[] values)
        {
            float[] paddedValue = padder.Pad(values);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float3[] values)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloat3Array);
            c.Strings.Add(name);
            c.Float3As.Add(values);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Float3[] values)
        {
            float[] paddedValue = padder.Pad(values);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4[] values)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloat4Array);
            c.Strings.Add(name);
            c.Float4As.Add(values);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Float4[] values)
        {
            float[] paddedValue = padder.Pad(values);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Float4x4[] values)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloat4x4Array);
            c.Strings.Add(name);
            c.Float4x4As.Add(values);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Float4x4[] values)
        {
            float[] paddedValue = padder.Pad(values);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, int[] values)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamIntArray);
            c.Strings.Add(name);
            c.IntAs.Add(values);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, int[] values)
        {
#if !DX9_INT_SUPPORT
            float[] paddedValue = padder.PadAsFloat(values);
#else
            int[] paddedValues = padder.Pad(values);
#endif
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, float[] values)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamFloatArray);
            c.Strings.Add(name);
            c.FloatAs.Add(values);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, float[] values)
        {
            float[] paddedValue = padder.Pad(values);
            globalParamValues[name] = new Directx9ShaderParamValue(paddedValue) { Address = (uint)GetInputBinding(null, name).Address };
            globalsChanged = true;
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, Texture value)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamTexture);
            c.Strings.Add(name);
            c.Textures.Add(value);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, Texture value)
        {
            globalTextures.MainContext.SetGlobalTexture(name, value);
        }

        protected override void commandList_SetParam(GraphicResourceID resID, string name, RenderTarget value)
		{
			Directx9CmdList c = cmdLists[resID];
			c.Cmds.Add(Directx9CmdType.SetParamRT);
            c.Strings.Add(name);
            c.RTs.Add(value);
        }

        private void commandList_SetParam_Impl(GraphicResourceID resID, string name, RenderTarget value)
        {
            globalTextures.MainContext.SetGlobalTexture(name, value);
        }


#endregion
    }


}
