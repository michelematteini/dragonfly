using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Resources                                                              
{
    public abstract class VertexBuffer : GraphicResource
    {
        protected VertexBuffer(GraphicResourceID resID, VertexType vtype, int capacity)
            : base(resID)
        {
            this.VertexType = vtype;
            this.Capacity = capacity;
        }

        /// <summary>
        /// Maximum number of vertices that this buffer can store.
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// Get or sets the number of vertices currently used in this buffer. 
        /// </summary>
        public int VertexCount { get; protected set; }

        /// <summary>
        /// The vertex type description for the vertices used by this buffer.
        /// </summary>
        public VertexType VertexType { get; private set; }

        /// <summary>
        /// Initialize or update the vertices in this buffer.
        /// </summary>
        /// <param name="vertices">An array containing the source vertices.</param>
        /// <param name="vertexCount">The number of input vertices to be used. The VertexCount property will be updated to this value.</param>
        public void SetVertices<T>(T[] vertices, int vertexCount) where T : struct
        {
#if DEBUG
            if (vertexCount > Capacity || vertexCount > vertices.Length)
                throw new InvalidGraphicCallException("The specified vertex count exceed the source array or this buffer size.");
#endif
            VertexCount = vertexCount;
            SetVerticesInternal<T>(vertices);
        }

        public void SetVertices<T>(T[] vertices) where T : struct
        {
            VertexCount = System.Math.Min(vertices.Length, Capacity);
            SetVerticesInternal<T>(vertices);
        }

        protected abstract void SetVerticesInternal<T>(T[] vertices) where T : struct;
    }
}
