using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A baker that generate vertex buffers int the [position.xyz,texCoords.xy, normal.xyz] format from a custom screen space material that renders its vertices.
    /// </summary>
    public class CompBakerVertexArray : Component
    {
        private CompRenderBuffer destBuffer;
        private CompScreenPass vbRenderingPass;

        public CompBakerVertexArray(Component parent, Int2 vertexGridSize, CompMaterial vertexBakingMaterial = null) : this(parent, vertexGridSize.X * vertexGridSize.Y, vertexGridSize, vertexBakingMaterial) { }

        public CompBakerVertexArray(Component parent, int vertexCount, CompMaterial vertexBakingMaterial = null) : this(parent, vertexCount, new Int2((int)FMath.Ceil(FMath.Sqrt(vertexCount))), vertexBakingMaterial) { }

        private CompBakerVertexArray(Component parent, int vertexCount, Int2 vertexGridSize, CompMaterial vertexBakingMaterial = null) : base(parent)
        {
            VertexCount = vertexCount;
            VertexGridSize = new Int2(1, 1) * (int)FMath.Ceil(FMath.Sqrt(vertexCount));

            // create a size fitting the vertex data (1 vertex = adjacent 2 pixels)
            destBuffer = new CompRenderBuffer(parent, Graphics.SurfaceFormat.Float4, 2 * vertexGridSize.X, vertexGridSize.Y);

            // create vertex rendering pass
            vbRenderingPass = new CompScreenPass(this, Name + ID + "_VBRenderPass", destBuffer);
            vbRenderingPass.Material = vertexBakingMaterial;

            // create baker
            Baker = new CompBaker(this, vbRenderingPass.Pass, OnVBufferRendered, new CompEvent(this, () => vbRenderingPass.Material != null && vbRenderingPass.Material.Ready));
        }

        public CompMaterial VBMaterial
        {
            get { return vbRenderingPass.Material; }
            set { vbRenderingPass.Material = value; }
        }

        public int VertexCount { get; private set; }

        public Int2 VertexGridSize { get; private set; }

        /// <summary>
        /// The list of baked vertices. This can be assigned externally to an array with a length of at least VertexCount before baking that will be filled when vertices are ready.
        /// <para/> If left unassigned, a new buffer is created before calling OnVertexBufferReady().
        /// </summary>
        public VertexTexNorm[] Vertices { get; set; }

        private void OnVBufferRendered(RenderTargetRef[] vbTargetData)
        {
            // request rendered data and wait for it to be available
            CompEventRtSnapshotReady snapshotReady = new CompEventRtSnapshotReady(this, vbTargetData[0].GetValue());
            new CompActionOnEvent(snapshotReady.Event, () =>
            {
                // data is ready! copy to a temp buffer
                if (Vertices == null || Vertices.Length < VertexCount)
                    Vertices = new VertexTexNorm[VertexCount];
                vbTargetData[0].GetValue().GetSnapshotData<VertexTexNorm>(Vertices);

                // signal completion
                if (OnVerticesReady != null)
                    OnVerticesReady(Vertices);

                snapshotReady.Dispose();
            });
            vbTargetData[0].GetValue().SaveSnapshot();
        }

        public Action<VertexTexNorm[]> OnVerticesReady { get; set; }

        public CompBaker Baker { get; private set; }

    }
}
