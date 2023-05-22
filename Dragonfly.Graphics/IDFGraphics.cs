using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;
using System.Drawing;
using System.Collections.Generic;


namespace Dragonfly.Graphics
{
    public interface IDFGraphics
    {
        #region Properties

        IGraphicsAPI GraphicsAPI { get; }

        bool IsAvailable { get; }

        List<Int2> SupportedDisplayResolutions { get; }

        int CurWidth { get; }

        int CurHeight { get; }

        IReadOnlyCollection<string> GetShaderTemplates(string effectName);

        #endregion

        #region Graphic Calls

        /// <summary>
        /// Starts a new frame.
        /// </summary>
        /// <returns>False if the device is not ready to render, true otherwise.</returns>
        bool NewFrame();

        void StartRender();

        void DisplayRender();

        void Release();

        void SetScreen(IntPtr targetHandle, bool fullScreen, int preferredWidth, int preferredHeight);

        #endregion

        #region Resource Creation

        VertexBuffer CreateVertexBuffer(VertexType vtype, int vertexCount);

        IndexBuffer CreateIndexBuffer(int indexCount);

        Texture CreateTexture<T>(int width, int height, T[] pixelData) where T : struct;

        Texture CreateTexture(int width, int height, SurfaceFormat format);

        /// <summary>
        /// Create a texture from its file bytes. Supported files may vary with the chosen API, but should always load files supported by WIC.
        /// </summary>
        Texture CreateTexture(byte[] fileData);

        RenderTarget CreateRenderTarget(int width, int height, SurfaceFormat format, bool depthTestSupported);

        RenderTarget CreateRenderTarget(float backBufferSizePercent, SurfaceFormat format, bool depthTestSupported);

        /// <summary>
        /// Create a shader resource.
        /// </summary>
        /// <param name="effectName">The name of the effect defining the programs, inputs and options to be used.</param>
        /// <param name="customVariantStates">Variant names and values pair that define the wanted shader variation.</param>
        /// <param name="templateName">Template name that can be used to change the actual used implementation for the same effect.</param>
        /// <returns></returns>
        Shader CreateShader(string effectName, ShaderStates states, KeyValuePair<string, string>[] customVariantStates = null, string templateName = "");

        /// <summary>
        /// Create a shader resource that share the same parameters of another parent shader. A shader created with this call will not be usable after the parent resource is released.
        /// </summary>
        /// <param name="parent">Another shader from which the created resource will inherit parameter, effect and variant states.</param>
        /// <param name="states">Render states that will be used when drawing using this resource.</param>
        /// <param name="templateName">Template name that can be used to change the actual used implementation for the same effect.</param>
        /// <returns></returns>
        Shader CreateShader(Shader parent, ShaderStates states, string templateName);

        CommandList CreateCommandList();

        #endregion

        #region Tracing and Debug

        /// <summary>
        /// Starts a section visible from frame debug and tracing tools on CPU.
        /// </summary>
        void StartTracedSection(Byte4 markerColor, string name);

        /// <summary>
        /// Starts a section visible from frame debug and tracing tools on both CPU and GPU.
        /// </summary>
        void StartTracedSection(CommandList commandList, Byte4 markerColor, string name);

        /// <summary>
        /// Ends a debug section started with DebugSectionStart()
        /// </summary>
        void EndTracedSection(CommandList commandList);

        /// <summary>
        /// Ends a debug section started with DebugSectionStart()
        /// </summary>
        void EndTracedSection();

        #endregion

    }

}
