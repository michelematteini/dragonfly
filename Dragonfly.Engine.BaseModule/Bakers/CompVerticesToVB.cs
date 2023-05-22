using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// Once created with the specified vertices, create a vertex buffer from them and signal its successfull creation.
    /// <para/> This component destroy itself after completion.
    /// </summary>
    public class CompVerticesToVB<T> : Component, ICompAllocator, ICompUpdatable where T : struct
    {
        private T[] srcVertices;
        private VertexType inputLayout;

        public CompVerticesToVB(Component parent, T[] srcVertices, VertexType inputLayout) : base(parent)
        {
            this.srcVertices = srcVertices;
            this.inputLayout = inputLayout;
        }

        public Action OnCompletion { get; set; }

        public VertexBuffer VertexBuffer { get; private set; }

        public bool LoadingRequired => true;

        public UpdateType NeededUpdates => VertexBuffer != null ? UpdateType.FrameStart2 : UpdateType.None;

        public void LoadGraphicResources(EngineResourceAllocator g)
        {
            VertexBuffer = g.CreateVertexBuffer(inputLayout, srcVertices.Length);
            VertexBuffer.SetVertices<T>(srcVertices);
        }

        public void ReleaseGraphicResources() { }

        public void Update(UpdateType updateType)
        {
            if (OnCompletion != null)
                OnCompletion();
        }
    }
}
