using Dragonfly.Engine.Core;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A component that store caching buffers to load geometry to gpu resources
    /// </summary>
    public class CompMeshGeomBuffers : Component
    {
        public const int MAX_VERTEX_COUNT = 65536;
        public const int MAX_INDEX_COUNT = 524288;

        public CompMeshGeomBuffers(Component parent) : base(parent)
        {
            VertexList = new VertexTexNorm[MAX_VERTEX_COUNT];
            IndexList = new ushort[MAX_INDEX_COUNT];
        }
        
        public VertexTexNorm[] VertexList { get; private set; }

        public ushort[] IndexList { get; private set; }
    }
}
