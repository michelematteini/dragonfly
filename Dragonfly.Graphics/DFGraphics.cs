using System;
using Dragonfly.Graphics.Resources;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Shaders;
using System.IO;
using System.Collections.Generic;
using Dragonfly.Utils;
using System.Linq;

namespace Dragonfly.Graphics
{
    internal abstract class DFGraphics : IDFGraphics
    {
        protected DFGraphics(DFGraphicSettings factory, IGraphicsAPI api)
		{
            GraphicsAPI = api;

            // prepare binding table
            ShaderBindingTable = new ShaderBindingTable();
            string shaderTablePath = ShaderCompiler.GetBindingTablePath(api, factory.ResourceFolder);
            if (File.Exists(shaderTablePath))
            {
                // load precompiled shaders
                byte[] sbtBytes = File.ReadAllBytes(shaderTablePath);
                this.ShaderBindingTable = new ShaderBindingTable(api, sbtBytes);
            }
        }
	
		#region Properties

		public abstract int CurWidth { get; protected set; }

        public abstract int CurHeight { get; protected set; }

        public IGraphicsAPI GraphicsAPI { get; protected set; }

		public abstract bool IsAvailable { get; }

        public bool Released { get; private set; }

		protected internal ShaderBindingTable ShaderBindingTable
		{
			get;
			private set;
		}

        #endregion

        public abstract List<Int2> SupportedDisplayResolutions { get; }

        public IReadOnlyCollection<string> GetShaderTemplates(string effectName)
        {
            return ShaderBindingTable.GetAllTemplatesFor(effectName);
        }

        #region Graphic Resources

        public VertexBuffer CreateVertexBuffer(VertexType vtype, int vertexCount)
        {
            GraphicResourceID resID = createVertexBuffer(vtype, vertexCount);
            return new MDF_VertexBuffer(this, resID, vtype, vertexCount);
        }

        public IndexBuffer CreateIndexBuffer(int indexCount)
        {
            GraphicResourceID resID = createIndexBuffer(indexCount);
            return new MDF_IndexBuffer(this, resID, indexCount);
        }

        public Texture CreateTexture(int width, int height, SurfaceFormat format)
        {
            GraphicResourceID resID = createTexture<int>(width, height, format, null);
            return new MDF_Texture(this, resID, width, height, format);
        }
        public Texture CreateTexture<T>(int width, int height, T[] pixelData) where T : struct
        {
            SurfaceFormat format = SurfaceFormat.Color;
            if (typeof(T) == typeof(float))
                format = SurfaceFormat.Float;
            else if (typeof(T) == typeof(Float2))
                format = SurfaceFormat.Float2;
            else if (typeof(T) == typeof(Float4))
                format = SurfaceFormat.Float4;

            GraphicResourceID resID = createTexture<T>(width, height, format, pixelData);
            return new MDF_Texture(this, resID, width, height, format);
        }

        public Texture CreateTexture(byte[] fileData)
		{
            int width, height;
			GraphicResourceID resID = createTexture(fileData, out width, out height);
            return new MDF_Texture(this, resID, width, height, SurfaceFormat.DDS);
		}

        public RenderTarget CreateRenderTarget(int width, int height, SurfaceFormat format, bool depthTestSupported)
        {
            GraphicResourceID resID = createRenderTarget(width, height, format, depthTestSupported);
            return new MDF_RenderTarget(this, resID, format);
        }

        public RenderTarget CreateRenderTarget(float backBufferSizePercent, SurfaceFormat format, bool depthTestSupported)
        {
            GraphicResourceID resID = createRenderTarget(backBufferSizePercent, format, depthTestSupported);
            return new MDF_RenderTarget(this, resID, format);
        }

        public Shader CreateShader(string effectName, ShaderStates states, KeyValuePair<string, string>[] customVariantStates, string templateName = "")
		{
            string variantID = CalcShaderVariantID(effectName, customVariantStates);
            if (string.IsNullOrEmpty(templateName))
                templateName = ShaderBindingTable.GetEffectDefaultTemplate(effectName);
			GraphicResourceID resID = createShader(effectName, states, variantID, templateName, null);
			return new MDF_Shader(this, resID, effectName, variantID, states, templateName, null);
		}

        public Shader CreateShader(Shader parent, ShaderStates states, string templateName)
        {
            GraphicResourceID resID = createShader(parent.EffectName, states, parent.VariantID, templateName, parent.ResourceID);
            return new MDF_Shader(this, resID, parent.EffectName, parent.VariantID, states, templateName, parent);
        }

        private string CalcShaderVariantID(string effectName, KeyValuePair<string, string>[] customVariantStates)
        {
            HashSet<string> validVariants = ShaderBindingTable.GetEffectVariantNames(effectName);
            Dictionary<string, string> variantStates = new Dictionary<string, string>();

            // validate all input variant states
            if (customVariantStates != null)
            {
                foreach (KeyValuePair<string, string> vstate in customVariantStates)
                {
                    if (!validVariants.Contains(vstate.Key))
                        throw new InvalidGraphicCallException(string.Format("The effect \"{0}\" doesn't specify a variant named \"{1}\".", effectName, vstate.Key));
                    if (!ShaderBindingTable.GetVariantValidValues(vstate.Key).Contains(vstate.Value))
                        throw new InvalidGraphicCallException(string.Format("The variant \"{0}\" doesn't define a value named \"{1}\".", vstate.Key, vstate.Value));

                    variantStates.Add(vstate.Key, vstate.Value);
                }
            }

            // add a default value for the missing states
            foreach (string vname in validVariants)
            {
                if (variantStates.ContainsKey(vname))
                    continue; // already specified by the user

                variantStates.Add(vname, ShaderBindingTable.GetVariantValidValues(vname).First()); // use first value as default
            }

            return ShaderCompiler.GetShaderVariantID(variantStates);
        }

        public CommandList CreateCommandList()
        {
            GraphicResourceID resID = createCommandList();
            return new MDF_CommandList(this, resID);
        }

        #endregion

        #region Graphic Calls

        public virtual bool NewFrame()
        {
            return true;
        }

        public abstract void StartRender();

        public abstract void DisplayRender();

        public void Release()
        {
            release();
            ShaderBindingTable = null;
            Released = true;
        }

        protected abstract void release();

        /// <summary>
        /// Reset Graphic Device, setting a different output surface or resolution.
        /// </summary>
        /// <param name="targetHandle">Handle to the control to be used as output surface.</param>
        /// <param name="fullScreen">Use only the specified control surface or the full screen.</param>
        /// <param name="preferredWidth">Horizontal resolution, if its not valid for this graphic device, a default one will be used.</param>
        /// <param name="preferredHeight">Vertical resolution, if its not valid for this graphic device, a default one will be used.</param>
        public void SetScreen(IntPtr target, bool fullScreen, int preferredWidth, int preferredHeight)
        {
            // If full screen is required, check if the resolution is available or choose the closest resolution
            if (fullScreen)
            {
                Int2 closestResolution = GetCompatibledDisplayResolution(preferredWidth, preferredHeight);
                preferredWidth = closestResolution.X;
                preferredHeight = closestResolution.Y;
            }

            setScreen(target, fullScreen, preferredWidth, preferredHeight);
        }

        protected Int2 GetCompatibledDisplayResolution(int preferredWidth, int preferredHeight)
        {
            Int2 closestResolution = Int2.Zero;
            int score = int.MaxValue; // the lower the better
            foreach (Int2 res in SupportedDisplayResolutions)
            {
                bool isSupported = (res.X == preferredWidth && res.Y == preferredHeight);
                int curScore = System.Math.Abs(res.X * res.Y - preferredWidth * preferredWidth) - (isSupported ? 1 : 0);

                if (curScore < score)
                {
                    closestResolution = res;
                    score = curScore;
                }
            }
            return closestResolution;
        }

        protected abstract void setScreen(IntPtr target, bool fullScreen, int width, int height);

        #endregion

#region Tracing and Debug

#if TRACING_ITT
        private DF_ITT ittTracer;
#endif

        public virtual void StartTracedSection(Byte4 markerColor, string name)
        {
#if TRACING_ITT
            if(ittTracer == null)
                ittTracer = new DF_ITT(name);


            ittTracer.TaskBegin(name);
#endif
        }

        public virtual void StartTracedSection(CommandList commandList, Byte4 markerColor, string name)
        {
#if TRACING_ITT
            if(ittTracer == null)
                ittTracer = new DF_ITT(name);


            ittTracer.TaskBegin(name);
#endif
        }

        public virtual void EndTracedSection(CommandList commandList)
        {
#if TRACING_ITT
            ittTracer.TaskEnd();
#endif
        }

        public virtual void EndTracedSection()
        {
#if TRACING_ITT
            ittTracer.TaskEnd();
#endif
        }

#endregion

#region VertexBuffer

        protected abstract GraphicResourceID createVertexBuffer(VertexType vtype, int vertexCount);

        protected abstract void vertexBuffer_SetVertices<T>(GraphicResourceID resID, T[] vertices, int vertexCount);

        protected abstract void vertexBuffer_Release(GraphicResourceID resID);

        protected class MDF_VertexBuffer : VertexBuffer
        {
            private DFGraphics g;

            public MDF_VertexBuffer(DFGraphics g, GraphicResourceID resID, VertexType vtype, int vertexCount)
                : base(resID, vtype, vertexCount)
            {
                this.g = g;
            }

            protected override void SetVerticesInternal<T>(T[] vertices)
            {
                g.vertexBuffer_SetVertices<T>(ResourceID, vertices, VertexCount);
            }

            public override void Release()
            {
                if (!g.Released) g.vertexBuffer_Release(this.ResourceID);
            }
        }

#endregion

#region IndexBuffer

        protected abstract GraphicResourceID createIndexBuffer(int indexCount);

        protected abstract void indexBuffer_SetIndices(GraphicResourceID resID, ushort[] indices, int indexCount);

        protected abstract void indexBuffer_Release(GraphicResourceID resID);

        protected class MDF_IndexBuffer : IndexBuffer
        {
            private DFGraphics g;

            public MDF_IndexBuffer(DFGraphics g, GraphicResourceID resID, int indexCount)
                : base(resID, indexCount)
            {
                this.g = g;
            }

            protected override void SetIndicesInternal(ushort[] indices)
            {
                g.indexBuffer_SetIndices(this.ResourceID, indices, IndexCount);
            }

            public override void Release()
            {
                if (!g.Released) g.indexBuffer_Release(this.ResourceID);
            }
        }

#endregion

#region Texture

        protected abstract GraphicResourceID createTexture<T>(int width, int height, SurfaceFormat format, T[] initialPixelData) where T : struct;

        protected abstract GraphicResourceID createTexture(byte[] fileData, out int width, out int height);

        protected abstract void texture_SetData<T>(GraphicResourceID resID, T[] data, int rowLength);

        protected abstract void texture_Release(GraphicResourceID resID);

        protected class MDF_Texture : Texture
        {
            private DFGraphics g;

            public MDF_Texture(DFGraphics g, GraphicResourceID resID, int width, int height, SurfaceFormat format)
                : base(resID, width, height, format)
            {
                this.g = g;
            }

            public override void SetData<T>(T[] data)
            {
                g.texture_SetData<T>(this.ResourceID, data, data.Length / Height);
            }

            public override void Release()
            {
                if (!g.Released) g.texture_Release(this.ResourceID);
            }
        }

#endregion

#region RenderTarget

        protected abstract GraphicResourceID createRenderTarget(int width, int height, SurfaceFormat format, bool depthTestSupported);

        protected abstract GraphicResourceID createRenderTarget(float backBufferSizePercent, SurfaceFormat format, bool depthTestSupported);

        protected abstract void renderTarget_Release(GraphicResourceID resID);

        protected abstract int renderTarget_GetWidth(GraphicResourceID resID);

        protected abstract int renderTarget_GetHeight(GraphicResourceID resID);

        protected abstract void renderTarget_SaveSnapshot(GraphicResourceID resID);

        protected abstract bool renderTarget_TryGetSnapshotData<T>(GraphicResourceID resID, T[] destBuffer);

        protected abstract void renderTarget_GetSnapshotData<T>(GraphicResourceID resID, T[] destBuffer);

        protected abstract void renderTarget_CopyToTexture(GraphicResourceID resID, GraphicResourceID destTexture);

        protected abstract void renderTarget_SetDepthWriteTarget(GraphicResourceID resID, RenderTarget depthWriteTarget);

        protected class MDF_RenderTarget : RenderTarget
        {
            private DFGraphics g;

            public MDF_RenderTarget(DFGraphics g, GraphicResourceID resID, SurfaceFormat format)
                : base(resID, format)
            {
                this.g = g;
            }

            public override int Height
            {
                get
                {
                    return g.renderTarget_GetHeight(this.ResourceID);
                }
                protected set { }
            }

            public override int Width
            {
                get
                {
                    return g.renderTarget_GetWidth(this.ResourceID);
                }
                protected set { }
            }

            public override void GetSnapshotData<T>(T[] destBuffer)
            {
                g.renderTarget_GetSnapshotData<T>(this.ResourceID, destBuffer);
            }

            public override void CopyToTexture(Texture destTexture)
            {
                if (destTexture.Format != this.Format)
                    throw new ArgumentException("The specified textureformat don't match the format of this render target.");

                g.renderTarget_CopyToTexture(this.ResourceID, destTexture.ResourceID);
            }

            public override void Release()
            {
                if (!g.Released) g.renderTarget_Release(this.ResourceID);
            }

            public override void SaveSnapshot()
            {
                g.renderTarget_SaveSnapshot(this.ResourceID);
            }

            public override bool TryGetSnapshotData<T>(T[] destBuffer)
            {
                return g.renderTarget_TryGetSnapshotData<T>(this.ResourceID, destBuffer);
            }

            public override void SetDepthWriteTarget(RenderTarget depthWriteTarget)
            {
                g.renderTarget_SetDepthWriteTarget(this.ResourceID, depthWriteTarget);
            }

        }

#endregion
		
#region Shader
		
		protected abstract GraphicResourceID createShader(string effectName, ShaderStates states, string variantID, string templateName, GraphicResourceID useParametersFromShader);

        protected abstract void shader_SetParam(Shader shader, string name, bool value);

        protected abstract void shader_SetParam(Shader shader, string name, int value);

        protected abstract void shader_SetParam(Shader shader, string name, float value);

        protected abstract void shader_SetParam(Shader shader, string name, Float2 value);

        protected abstract void shader_SetParam(Shader shader, string name, Float3 values);

        protected abstract void shader_SetParam(Shader shader, string name, Float4 values);

        protected abstract void shader_SetParam(Shader shader, string name, Float4x4 value);

        protected abstract void shader_SetParam(Shader shader, string name, Float3x3 value);

        protected abstract void shader_SetParam(Shader shader, string name, Int3 value);

        protected abstract void shader_SetParam(Shader shader, string name, int[] values);

        protected abstract void shader_SetParam(Shader shader, string name, float[] values);

        protected abstract void shader_SetParam(Shader shader, string name, Texture value);

        protected abstract void shader_SetParam(Shader shader, string name, RenderTarget value);

        protected abstract void shader_SetParam(Shader shader, string name, Float2[] values);

        protected abstract void shader_SetParam(Shader shader, string name, Float3[] values);

        protected abstract void shader_SetParam(Shader shader, string name, Float4[] value);

        protected abstract void shader_SetParam(Shader shader, string name, Float4x4[] value);

        protected abstract void shader_Release(GraphicResourceID resID);

        protected class MDF_Shader : Shader
        {
            private DFGraphics g;

            public MDF_Shader(DFGraphics g, GraphicResourceID resID, string effectName, string variantID, ShaderStates states, string templateName, Shader parent)
                : base(resID, effectName, variantID, states, templateName, parent)
            {
                this.g = g;
            }

            public override void SetParam(string name, bool value)
			{
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
			}

			public override void SetParam(string name, int value)
			{
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
            }

			public override void SetParam(string name, float value)
			{
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
            }
		
			public override void SetParam(string name, Float2 value)
			{
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
            }
			
			public override void SetParam(string name, Float3 value)
			{
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
            }
			
			public override void SetParam(string name, Float4 value)
			{
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
            }

			public override void SetParam(string name, Float4x4 value)
			{
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
            }

            public override void SetParam(string name, Float3x3 value)
            {
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
            }

            public override void SetParam(string name, Int3 value)
            {
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
            }

            public override void SetParam(string name, int[] values)
			{
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, values);
			}

			public override void SetParam(string name, float[] values)
			{
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, values);
			}

			public override void SetParam(string name, Texture value)
			{
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
			}

            public override void SetParam(string name, Float2[] values)
            {
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, values);
            }

            public override void SetParam(string name, Float3[] values)
            {
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, values);
            }

            public override void SetParam(string name, Float4[] values)
            {
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, values);
            }

            public override void SetParam(string name, Float4x4[] values)
            {
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, values);
            }

            public override void SetParam(string name, RenderTarget value)
            {
                if (Parent != null) throw new InvalidGraphicCallException("Parameters cannot be modified on this shader!");
                g.shader_SetParam(this, name, value);
            }

            public override void Release()
            {
                if (!g.Released) g.shader_Release(this.ResourceID);
            }
        }

#endregion

#region CommandList

        protected abstract GraphicResourceID createCommandList();

        protected abstract void commandList_ClearSurfaces(GraphicResourceID resID, Float4 clearValue, ClearFlags flags);
        
        protected abstract void commandList_Draw(GraphicResourceID resID);
        
        protected abstract void commandList_DrawIndexed(GraphicResourceID resID);
        
        protected abstract void commandList_DrawIndexedInstanced(GraphicResourceID resID, ArrayRange<Float4x4> instances);

        protected abstract void commandList_StartRecording(GraphicResourceID resID, IReadOnlyList<GraphicResourceID> requiredLists, bool flushRequired);

        protected abstract void commandList_QueueExecution(GraphicResourceID resID);
        
        protected abstract void commandList_SetViewport(GraphicResourceID resID, AARect viewport);
        
        protected abstract void commandList_SetVertices(GraphicResourceID resID, VertexBuffer vertices);
        
        protected abstract void commandList_SetIndices(GraphicResourceID resID, IndexBuffer indices);
        
        protected abstract void commandList_SetRenderTarget(GraphicResourceID resID, RenderTarget rt, int index);
        
        protected abstract void commandList_DisableRenderTarget(GraphicResourceID resID, RenderTarget rt);
        
        protected abstract void commandList_ResetRenderTargets(GraphicResourceID resID);
        
        protected abstract void commandList_Release(GraphicResourceID resID);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, bool value);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, int value);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, float value);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Float2 value);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Float3 value);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Float4 value);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Float4x4 value);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Float3x3 value);

        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Int3 value);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, int[] values);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, float[] values);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Texture value);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, RenderTarget value);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Float2[] values);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Float3[] values);
        
        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Float4[] values);

        protected abstract void commandList_SetParam(GraphicResourceID resID, string name, Float4x4[] values);

        protected abstract void commandList_SetShader(GraphicResourceID resID, Shader shader);

        protected class MDF_CommandList : CommandList
        {
            private DFGraphics g;
            private GraphicResourceID[] cachedRequiredLists;

            public MDF_CommandList(DFGraphics g, GraphicResourceID resID)
                : base(resID)
            {
                this.g = g;
            }

            public override void ClearSurfaces(Float4 clearValue, ClearFlags flags)
            {
                g.commandList_ClearSurfaces(ResourceID, clearValue, flags);
            }

            public override void DisableRenderTarget(RenderTarget rt)
            {
                g.commandList_DisableRenderTarget(ResourceID, rt);
            }

            public override void Draw()
            {
                g.commandList_Draw(ResourceID);
            }

            public override void DrawIndexed()
            {
                g.commandList_DrawIndexed(ResourceID);
            }

            public override void DrawIndexedInstanced(ArrayRange<Float4x4> instances)
            {
                g.commandList_DrawIndexedInstanced(ResourceID, instances);
            }

            public override void StartRecording()
            {
                base.StartRecording();
                if (cachedRequiredLists == null || cachedRequiredLists.Length != RequiredLists.Count)
                    cachedRequiredLists = new GraphicResourceID[RequiredLists.Count];
                for (int i = 0; i < RequiredLists.Count; i++)
                    cachedRequiredLists[i] = RequiredLists[i].ResourceID;
                g.commandList_StartRecording(ResourceID, cachedRequiredLists, FlushRequired);
            }

            public override void QueueExecution()
            {
                base.QueueExecution();
                g.commandList_QueueExecution(ResourceID);
            }

            public override void Release()
            {
                if (!g.Released) g.commandList_Release(this.ResourceID);
            }

            public override void ResetRenderTargets()
            {
                g.commandList_ResetRenderTargets(ResourceID);
            }

            public override void SetIndices(IndexBuffer indices)
            {
                g.commandList_SetIndices(ResourceID, indices);
            }

            public override void SetParam(string name, bool value)
            {
                g.commandList_SetParam(ResourceID, name, value);
            }

            public override void SetParam(string name, int value)
            {
                g.commandList_SetParam(ResourceID, name, value);
            }

            public override void SetParam(string name, float value)
            {
                g.commandList_SetParam(ResourceID, name, value);
            }

            public override void SetParam(string name, Float2 value)
            {
                g.commandList_SetParam(ResourceID, name, value);
            }

            public override void SetParam(string name, Float3 values)
            {
                g.commandList_SetParam(ResourceID, name, values);
            }

            public override void SetParam(string name, Float4 values)
            {
                g.commandList_SetParam(ResourceID, name, values);
            }

            public override void SetParam(string name, Float4x4 value)
            {
                g.commandList_SetParam(ResourceID, name, value);
            }

            public override void SetParam(string name, Float3x3 value)
            {
                g.commandList_SetParam(ResourceID, name, value);
            }

            public override void SetParam(string name, Int3 value)
            {
                g.commandList_SetParam(ResourceID, name, value);
            }

            public override void SetParam(string name, int[] values)
            {
                g.commandList_SetParam(ResourceID, name, values);
            }

            public override void SetParam(string name, float[] values)
            {
                g.commandList_SetParam(ResourceID, name, values);
            }

            public override void SetParam(string name, Texture value)
            {
                g.commandList_SetParam(ResourceID, name, value);
            }

            public override void SetParam(string name, RenderTarget value)
            {
                g.commandList_SetParam(ResourceID, name, value);
            }

            public override void SetParam(string name, Float2[] values)
            {
                g.commandList_SetParam(ResourceID, name, values);
            }

            public override void SetParam(string name, Float3[] values)
            {
                g.commandList_SetParam(ResourceID, name, values);
            }

            public override void SetParam(string name, Float4[] value)
            {
                g.commandList_SetParam(ResourceID, name, value);
            }

            public override void SetParam(string name, Float4x4[] values)
            {
                g.commandList_SetParam(ResourceID, name, values);
            }

            public override void SetRenderTarget(RenderTarget rt, int index)
            {
                g.commandList_SetRenderTarget(ResourceID, rt, index);
            }

            public override void SetShader(Shader shader)
            {
                g.commandList_SetShader(ResourceID, shader);
            }

            public override void SetVertices(VertexBuffer vertices)
            {
                g.commandList_SetVertices(ResourceID, vertices);
            }

            public override void SetViewport(AARect viewport)
            {
                g.commandList_SetViewport(ResourceID, viewport);
            }
        }

#endregion

    }

    public enum CullMode
    {
        None,
        Clockwise,
		CounterClockwise
    }

    public static class CullModeEx {
        public static CullMode Invert(this CullMode cullMode)
        {
            switch (cullMode)
            {
                case Graphics.CullMode.Clockwise:
                    cullMode = Graphics.CullMode.CounterClockwise;
                    break;
                case Graphics.CullMode.CounterClockwise:
                    cullMode = Graphics.CullMode.Clockwise;
                    break;
            }

            return cullMode;
        }
    }

    public enum FillMode
    {
        Point,
        Wireframe,
        Solid
    }

    public enum SurfaceFormat
    {
		DDS = 0,
        Color = 1,
        Half = 2,
        Half2 = 3,
        Half4 = 4,
        Float = 5,
        Float2 = 6,
        Float4 = 7,
        AntialiasedColor = 8
    }

    
}
