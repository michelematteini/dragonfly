using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// Mesh geometry for a single terrain tile.
    /// </summary>
    internal class CompTerrainTileGeom : Component, IMeshGeometry
    {
        private CompTerrainTessellator tessellator;
        private TerrainEdgeTessellation edgeTess;
        private IndexBuffer cachedIndexBuffer; // cached index buffer saves from having to search for it each time

        public CompTerrainTileGeom(Component parent, CompTerrainTessellator tessellator) : base(parent)
        {
            this.tessellator = tessellator;
            EdgeTesselation = new TerrainEdgeTessellation(1);
        }

        public TerrainEdgeTessellation EdgeTesselation 
        {
            get 
            {
                return edgeTess;
            }
            set
            {
                edgeTess = value;
                cachedIndexBuffer = null;
                tessellator.RequestEdgeTessellation(edgeTess);
            }
        }

        public AABox BoundingBox { get; set; }

        public bool Available
        {
            get
            {
                if (cachedIndexBuffer == null)
                    cachedIndexBuffer = tessellator.Indices[EdgeTesselation];
                return cachedIndexBuffer != null && VertexBuffer != null;
            }
        }

        public VertexBuffer VertexBuffer 
        {
            get
            {
                return tessellator.Vertices;
            } 
            
        }

        public IndexBuffer IndexBuffer
        {
            get
            {
                if (cachedIndexBuffer == null)
                    cachedIndexBuffer = tessellator.Indices[EdgeTesselation];
                return cachedIndexBuffer;
            }
        }
    }
}
