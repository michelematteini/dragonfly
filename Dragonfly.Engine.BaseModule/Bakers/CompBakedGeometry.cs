using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A mesh geometry that allow for efficient GPU based initialization
    /// </summary>
    public class CompBakedGeometry : Component, IMeshGeometry, ICompAllocator
    {
        public bool ownIndexBuffer;

        public CompBakedGeometry(Component parent, Int2 gridSize) : base(parent)
        {
            TessellationType = BakedGeometryTessellation.VertexGrid;
            VertexBaker = new CompBakerVertexArray(this, gridSize, null);
            VertexBaker.OnVerticesReady = v => LoadingRequired = true;
        }

        public CompBakedGeometry(Component parent, int vertexCount) : base(parent)
        {
            TessellationType = BakedGeometryTessellation.TriangleList;
            VertexBaker = new CompBakerVertexArray(this, vertexCount, null);
        }

        public CompBakerVertexArray VertexBaker { get; private set; }

        /// <summary>
        /// The material used to generate vertices, once this field is set to a valid value the vertex baking process start.
        /// </summary>
        public CompMaterial VertexBakingMaterial
        {
            get { return VertexBaker.VBMaterial; }
            set { VertexBaker.VBMaterial = value; }
        }

        /// <summary>
        /// Specify how the generated vertices should be grouped into faces for rendering.
        /// </summary>
        public BakedGeometryTessellation TessellationType { get; set; }

        public bool Available
        {
            get
            {
                return VertexBuffer != null && (TessellationType == BakedGeometryTessellation.TriangleList || IndexBuffer != null);
            }
        }

        public bool LoadingRequired { get; private set; }

        public AABox BoundingBox { get; private set; }

        public VertexBuffer VertexBuffer { get; private set; }

        /// <summary>
        /// The index buffer for this geometry. If set to null it's automatically generated when needed.
        /// </summary>
        public IndexBuffer IndexBuffer { get; set; }

        public void LoadGraphicResources(EngineResourceAllocator g)
        {
            LoadingRequired = false;
            if (VertexBuffer != null)
                ReleaseGraphicResources();

            // prepare index buffer
            if (IndexBuffer == null && TessellationType == BakedGeometryTessellation.VertexGrid)
            {
                ushort[] indices = Primitives.GridIndices(0, VertexBaker.VertexGridSize.Width, VertexBaker.VertexGridSize.Height);
                IndexBuffer = g.CreateIndexBuffer(indices.Length);
                IndexBuffer.SetIndices(indices);
                ownIndexBuffer = true;
            }

            // prepare vertex buffer
            VertexBuffer = g.CreateVertexBuffer(VertexTexNorm.VertexType, VertexBaker.VertexCount);
            VertexBuffer.SetVertices<VertexTexNorm>(VertexBaker.Vertices);

            // update bounding box
            BoundingBox = AABox.Bounding(VertexBaker.Vertices, vert => vert.Position);
        }

        public void ReleaseGraphicResources()
        {
            if (IndexBuffer != null && ownIndexBuffer)
                IndexBuffer.Release();
            if (VertexBuffer != null)
                VertexBuffer.Release();
        }
    }

    public enum BakedGeometryTessellation
    {
        /// <summary>
        /// Three consicutive certices form a face.
        /// </summary>
        TriangleList,
        /// <summary>
        /// Vertices are generated in a grid pattern and should be connected to form a surface.
        /// </summary>
        VertexGrid
    }
}
