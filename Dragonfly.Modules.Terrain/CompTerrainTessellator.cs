using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;
using System.Collections.Generic;
using Dragonfly.Utils;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// Creates index and vertex buffers to be used to render terrain tiles.
    /// </summary>
    internal class CompTerrainTessellator : Component, ICompAllocator
    {
        private int tessellation;
        private BlockingQueue<TerrainEdgeTessellation> neededEdges;
        private ushort[] indexGenCache;

        /// <summary>
        /// Create a geometry tile for a tiled terrain, with edge adaptation.
        /// </summary>
        /// <param name="tessellation">The number of quads the tile is split into along its edge.</param>
        public CompTerrainTessellator(Component parent, int tessellation) : base(parent)
        {
            this.tessellation = Math.Max(1, tessellation);
            this.Indices = new Dictionary<TerrainEdgeTessellation, IndexBuffer>();
            this.neededEdges = new BlockingQueue<TerrainEdgeTessellation>();
            
            LoadingRequired = true;
            indexGenCache = new ushort[this.tessellation * this.tessellation * 6];

            FlatBoundingBox = new AABox(new Float3(0, 0, 0), new Float3(tessellation, 0, tessellation));
            TessToUVTransform = Float4x4.Scale(1.0f / tessellation, 1.0f, 1.0f / tessellation);
            UVToTessTransform = Float4x4.Scale(tessellation, 1.0f, tessellation);
        }

        public Dictionary<TerrainEdgeTessellation, IndexBuffer> Indices { get; private set; }

        public VertexBuffer Vertices { get; private set; }

        /// <summary>
        /// The bounding box of a tile with all heights set to 0.
        /// </summary>
        public AABox FlatBoundingBox { get; private set; }

        public Float4x4 TessToUVTransform { get; private set; }

        public Float4x4 UVToTessTransform { get; private set; }

        public void RequestEdgeTessellation(TerrainEdgeTessellation t)
        {
            if (!Indices.ContainsKey(t))
            {
                Indices[t] = null; // i.e. flag as loading...
                neededEdges.Enqueue(t);
                LoadingRequired = true;
            }
        }

        public bool IsTessellationAvailable(TerrainEdgeTessellation t)
        {
            return Indices.ContainsKey(t) && Indices[t] != null;
        }

        public bool LoadingRequired { get; private set; }

        public void LoadGraphicResources(EngineResourceAllocator g)
        {
            // generate indices
            TerrainEdgeTessellation edge;
            while (neededEdges.TryDequeue(out edge, 0))
            {
                Indices[edge] = GenerateIndexBuffer(g, edge);
            }

            // generate vertices
            if(Vertices == null)
            {
                GenerateVertices(g);
            }

            LoadingRequired = false;
        }

        private void GenerateVertices(EngineResourceAllocator g)
        {
            VertexTexNorm[] vertices = new VertexTexNorm[(tessellation + 1) * (tessellation + 1)];
            VertexTexNorm curVertex;
            curVertex.Normal = Float3.UnitY;
            curVertex.Position.Y = 0;
            for (int z = 0; z <= tessellation; z++)
            {
                int baseIndex = z * (tessellation + 1);
                curVertex.TexCoords.Y = (float)z / tessellation;
                curVertex.Position.Z = z;
                for (int x = 0; x <= tessellation; x++)
                {
                    curVertex.TexCoords.X = (float)x / tessellation;
                    curVertex.Position.X = x;

                    vertices[x + baseIndex] = curVertex;
                }
            }

            Vertices = g.CreateVertexBuffer(VertexTexNorm.VertexType, vertices.Length);
            Vertices.SetVertices<VertexTexNorm>(vertices);
        }

        private IndexBuffer GenerateIndexBuffer(EngineResourceAllocator g, TerrainEdgeTessellation edge)
        {
            int cacheIndex = 0;
            int rowLen = tessellation + 1;

            // tesselate the main central grid without edges
            for (int y = 1; y < rowLen - 2; y++)
            {
                for (int x = 1; x < rowLen - 2; x++)
                {
                    int startIndex = y * rowLen + x;
                    indexGenCache[cacheIndex++] = (ushort)(startIndex);
                    indexGenCache[cacheIndex++] = (ushort)(startIndex + rowLen);
                    indexGenCache[cacheIndex++] = (ushort)(startIndex + rowLen + 1);
                    
                    indexGenCache[cacheIndex++] = (ushort)(startIndex);
                    indexGenCache[cacheIndex++] = (ushort)(startIndex + rowLen + 1);
                    indexGenCache[cacheIndex++] = (ushort)(startIndex + 1);
                }
            }

            // tessellate edges
            GenerateEdgeTessellation(ref cacheIndex, edge.TopDivisor, 0, 1, rowLen, false); // top
            GenerateEdgeTessellation(ref cacheIndex, edge.BottomDivisor, rowLen * rowLen - 1, -1, -rowLen, false); // bottom
            GenerateEdgeTessellation(ref cacheIndex, edge.LeftDivisor, 0, rowLen, 1, true); // left
            GenerateEdgeTessellation(ref cacheIndex, edge.RightDivisor, rowLen * rowLen - 1, -rowLen, -1, true); // right

            // create index buffer resource
            IndexBuffer ibuffer = g.CreateIndexBuffer(cacheIndex);
            ibuffer.SetIndices(indexGenCache, cacheIndex);
            return ibuffer;
        }

        private void GenerateEdgeTessellation(ref int cacheIndex, int divisor, int startIndex, int dx, int dy, bool flip)
        {
            int iflip = flip.ToInt();
            int halfDiv = (divisor + 1) / 2;
            for (int bx = 0; bx < tessellation; bx += divisor)
            {
                int baseIndex = startIndex + dx * bx;
                int tmax = Math.Min(divisor, tessellation - bx - 1);
                for (int t = (bx == 0).ToInt(); t < tmax; t++)
                {
                    indexGenCache[cacheIndex] = (ushort)(baseIndex + dx * divisor * (t >= halfDiv).ToInt());
                    indexGenCache[cacheIndex + 1 + iflip] = (ushort)(baseIndex + dx * t + dy);
                    indexGenCache[cacheIndex + 2 - iflip] = (ushort)(baseIndex + dx * (t + 1) + dy);
                    cacheIndex += 3;
                }

                int halfBlock = halfDiv - (bx + halfDiv == tessellation).ToInt();
                indexGenCache[cacheIndex] = (ushort)(baseIndex);
                indexGenCache[cacheIndex + 1 + iflip] = (ushort)(baseIndex + dx * halfBlock + dy);
                indexGenCache[cacheIndex + 2 - iflip] = (ushort)(baseIndex + dx * divisor);
                cacheIndex += 3;
            }
        }

        public void ReleaseGraphicResources()
        {
            foreach (IndexBuffer ib in Indices.Values)
                ib.Release();
            Indices.Clear();
        }
    }
}
