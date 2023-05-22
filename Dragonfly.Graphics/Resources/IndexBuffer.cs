using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Resources
{
    public abstract class IndexBuffer : GraphicResource
    {
        protected IndexBuffer(GraphicResourceID resID, int capacity)
            : base(resID)
        {
            Capacity = capacity;
        }

        /// <summary>
        /// Maximum number of vertices that this buffer can store.
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// Get or sets the number of indices currently used in this buffer. 
        /// </summary>
        public int IndexCount { get; protected set; }

        public void SetIndices(ushort[] indices)
        {
#if DEBUG
            if (indices.Length > Capacity)
                throw new InvalidGraphicCallException("The specified vertex count exceed the source array or this buffer size.");
#endif
            IndexCount = indices.Length;
            SetIndicesInternal(indices);
        }

        public void SetIndices(ushort[] buffer, int indexCount)
        {
#if DEBUG
            if (indexCount > Capacity || indexCount > buffer.Length)
                    throw new InvalidGraphicCallException("The specified vertex count exceed the source array or this buffer size.");
#endif
            IndexCount = indexCount;
            SetIndicesInternal(buffer);
        }

        protected abstract void SetIndicesInternal(ushort[] indices);
    }
}
