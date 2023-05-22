using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{

    /// <summary>
    /// A mesh geometry that support primitive / data input from CPU code. 
    /// </summary>
    public class CompMeshGeometry : Component, ICompAllocator, IObject3D, IMeshGeometry
    {
        private int nextObjPos, nextObjNorm, nextObjCoord; // IObject3D sequential state

        public CompMeshGeometry(Component owner, List<VertexTexNorm> vertices, List<ushort> indices) : base(owner)
        {
            Vertices = vertices;
            Indices = indices;
            LoadingRequired = IsCpuStateDrawable();
            Available = false;
            nextObjPos = nextObjNorm = nextObjCoord = VertexCount;
            BoundingBox = AABox.Infinite;
        }

        public CompMeshGeometry(Component owner) : this(owner, new List<VertexTexNorm>(), new List<ushort>()) { }

        public List<ushort> Indices { get; private set; }

        public List<VertexTexNorm> Vertices { get; private set; }

        public VertexBuffer VertexBuffer { get; private set; }

        public IndexBuffer IndexBuffer { get; private set; }

        public bool Available { get; private set; }

        public bool LoadingRequired { get; private set; }

        // used for caching and indexing purposes
        internal string Guid { get; set; }

        public AABox BoundingBox { get; private set; }

        #region IObject3D

        public int VertexCount { get { return Vertices.Count; } }

        public void AddVertex(Float3 position)
        {
            prepareVertices(nextObjPos);
            VertexTexNorm v = Vertices[nextObjPos];
            v.Position = position;
            Vertices[nextObjPos++] = v;
        }

        public void AddNormal(Float3 normal)
        {
            prepareVertices(nextObjNorm);
            VertexTexNorm v = Vertices[nextObjNorm];
            v.Normal = normal;
            Vertices[nextObjNorm++] = v;
        }

        public void AddTexCoord(Float2 coords)
        {
            prepareVertices(nextObjCoord);
            VertexTexNorm v = Vertices[nextObjCoord];
            v.TexCoords = coords;
            Vertices[nextObjCoord++] = v;
        }

        private void prepareVertices(int newIndex)
        {
            if (newIndex >= VertexCount)
                Vertices.Add(new VertexTexNorm(Float3.Zero, Float2.Zero, Float3.UnitY));
        }

        public void AddIndex(ushort index)
        {
            Indices.Add(index);
        }

        public void UpdateGeometry()
        {
            LoadingRequired = true;
        }

        public void ClearGeometry()
        {
            Indices.Clear();
            Vertices.Clear();
            nextObjPos = nextObjNorm = nextObjCoord = 0;
        }

        #endregion

        public void LoadGraphicResources(EngineResourceAllocator g)
        {
            LoadingRequired = false;

            if (!IsCpuStateDrawable())
            {
                Available = false;
                return;
            }

            CompMeshGeomBuffers buffers = GetComponent<CompMeshGeomBuffers>();

            // update vertex buffer
            {
                int vertexCount = Math.Min(Vertices.Count, CompMeshGeomBuffers.MAX_VERTEX_COUNT);

                // create a new buffer if the current capacity is exceeded
                if (VertexBuffer == null || VertexBuffer.Capacity < vertexCount)
                {

                    if (VertexBuffer != null)
                        VertexBuffer.Release();
                    VertexBuffer = g.CreateVertexBuffer(VertexTexNorm.VertexType, vertexCount);
                }

                // upload new vertices
                Vertices.CopyTo(0, buffers.VertexList, 0, vertexCount);
                VertexBuffer.SetVertices<VertexTexNorm>(buffers.VertexList, vertexCount);
            }

            // update index buffer
            {
                int indexCount = Math.Min(Indices.Count, CompMeshGeomBuffers.MAX_INDEX_COUNT);
                
                // create a new buffer if the current capacity is exceeded
                if (IndexBuffer == null || IndexBuffer.Capacity < indexCount)
                {
                    if (IndexBuffer != null)
                        IndexBuffer.Release();
                    IndexBuffer = g.CreateIndexBuffer(indexCount);
                }

                // upload new indices
                Indices.CopyTo(0, buffers.IndexList, 0, indexCount);
                IndexBuffer.SetIndices(buffers.IndexList, indexCount);
            }

            // update bounding box
            BoundingBox = AABox.Empty;
            for (int i = 0; i < Vertices.Count; i++)
                BoundingBox = BoundingBox.Add(Vertices[i].Position);
   
            Available = true; // after the first resource loading, there is always a valid state to be drawn
        }

        private bool IsCpuStateDrawable()
        {
            return Indices.Count > 2 && Vertices.Count > 2;
        }

        public void ReleaseGraphicResources()
        {
            if (VertexBuffer != null) VertexBuffer.Release();
            if (IndexBuffer != null) IndexBuffer.Release();
            VertexBuffer = null;
            IndexBuffer = null;
            LoadingRequired = true;
            Available = false;
        }

    }
}
