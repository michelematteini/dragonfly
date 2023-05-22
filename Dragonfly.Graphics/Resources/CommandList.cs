using Dragonfly.Graphics.Math;
using DragonflyUtils;
using System.Collections.Generic;
using Dragonfly.Utils;

namespace Dragonfly.Graphics.Resources
{
    /// <summary>
    /// Wraps a list of commands to be executed by the GPU that can be filled from another thread.
    /// </summary>
    public abstract class CommandList : GraphicResource
    {
        public CommandList(GraphicResourceID id) : base(id)
        {
            RequiredLists = new List<CommandList>();
        }

        /// <summary>
        /// Returns whether this list is in a recording state.
        /// </summary>
        public bool IsRecording { get; private set; }

        /// <summary>
        /// Clear this list and prepares it to record new commands. Must be called before making new graphic calls and after QueueExecution().
        /// Must be called on the main rendering thread.
        /// </summary>
        public virtual void StartRecording()
        {
#if DEBUG
            if (IsRecording)
                throw new InvalidGraphicCallException("This list was already in a recording state! Did you forget to call QueueExecution()?");
#endif
            IsRecording = true;
        }

        /// <summary>
        /// Queue the currently recorded commands for execution, and flush this command list.
        /// Can be called from any thread.
        /// </summary>
        public virtual void QueueExecution()
        {
#if DEBUG
            if (!IsRecording)
                throw new InvalidGraphicCallException("This list was not recording! Did you forget to call StartRecording()?");
#endif
            IsRecording = false;
        }

        /// <summary>
        /// List of CommandLists that should be executed before this one.
        /// </summary>
        public List<CommandList> RequiredLists { get; private set; }

        /// <summary>
        /// If set to true, any list which is recorded after this one, will also be executed after, regardless of whether is required or not.
        /// </summary>
        public bool FlushRequired { get; set; }

        #region Graphic Calls

        public abstract void ClearSurfaces(Float4 clearValue, ClearFlags flags);

        public abstract void Draw();

        public abstract void DrawIndexed();

        public abstract void DrawIndexedInstanced(ArrayRange<Float4x4> instances);

        public abstract void SetViewport(AARect viewport);

        public abstract void SetVertices(VertexBuffer vertices);

        public abstract void SetIndices(IndexBuffer indices);

        public abstract void SetShader(Shader shader);

        public abstract void SetRenderTarget(RenderTarget rt, int index);

        public abstract void DisableRenderTarget(RenderTarget rt);

        public abstract void ResetRenderTargets();

        #endregion

        #region Global parameters
        
        public abstract void SetParam(string name, bool value);
        
        public abstract void SetParam(string name, int value);
        
        public abstract void SetParam(string name, float value);
        
        public abstract void SetParam(string name, Float2 value);
        
        public abstract void SetParam(string name, Float3 values);
        
        public abstract void SetParam(string name, Float4 values);
        
        public abstract void SetParam(string name, Float4x4 value);

        public abstract void SetParam(string name, Int3 value);

        public abstract void SetParam(string name, Float3x3 value);
        
        public abstract void SetParam(string name, int[] values);
        
        public abstract void SetParam(string name, float[] values);
        
        public abstract void SetParam(string name, Texture value);

        public abstract void SetParam(string name, RenderTarget value);

        public abstract void SetParam(string name, Float2[] values);

        public abstract void SetParam(string name, Float3[] values);
    
        public abstract void SetParam(string name, Float4[] value);

        public abstract void SetParam(string name, Float4x4[] values);

        #endregion
    }

    public enum ClearFlags
    {
        None = 0,
        ClearTargets = 1 << 0,
        ClearDepth = 1 << 1
    }
}