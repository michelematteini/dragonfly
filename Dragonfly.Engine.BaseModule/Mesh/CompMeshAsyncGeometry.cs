using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    public class CompMeshAsyncGeometry : Component, IObject3D, ICompUpdatable
    {
        private CompMesh parentMesh;
        private CompMeshGeometry geom;
        private int frameDelay, elapsedFrames;
        
        public CompMeshAsyncGeometry(CompMesh parentMesh, int frameDelay) : base(parentMesh)
        {
            this.parentMesh = parentMesh;
            this.geom = new CompMeshGeometry(parentMesh);
            this.frameDelay = frameDelay;
            elapsedFrames = 0;
            NeededUpdates = UpdateType.None;
        }

        public CompMeshAsyncGeometry(CompMesh parentMesh) : this(parentMesh, 3) { }

        public int VertexCount { get { return geom.VertexCount; } }

        public void AddIndex(ushort index)
        {
            geom.AddIndex(index);
        }

        public void AddNormal(Float3 normal)
        {
            geom.AddNormal(normal);
        }

        public void AddTexCoord(Float2 coords)
        {
            geom.AddTexCoord(coords);
        }

        public void AddVertex(Float3 position)
        {
            geom.AddVertex(position);
        }

        public void ClearGeometry()
        {
            geom.ClearGeometry();
        }

        public void UpdateGeometry()
        {
            NeededUpdates = UpdateType.FrameStart1;
            geom.UpdateGeometry();
        }

        public UpdateType NeededUpdates { get; private set; }

        public void Update(UpdateType updateType)
        {
            elapsedFrames++;

            if(elapsedFrames >= frameDelay)
            {
                parentMesh.EditableGeometry.Dispose();
                parentMesh.EditableGeometry = geom; // commit to mesh
                this.Dispose();
            }
        }

        
    }
}
