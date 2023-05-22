using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using System.Collections.Generic;


namespace Dragonfly.BaseModule
{
    public class CompMeshList : Component, ICompUpdatable
    {
        private List<CompMesh> blocks;
        private List<Float4x4> instances;

        public int MeshCount
        {
            get
            {
                return blocks.Count;
            }
        }

        public CompMesh this[int index]
        {
            get
            {
                return blocks[index];
            }
        }

        public IReadOnlyList<CompMesh> MeshList
        {
            get { return blocks; }
        }

        public UpdateType NeededUpdates { get; private set; }

        public CompMeshList(Component parent) : base(parent)
        {
            blocks = new List<CompMesh>();
            instances = new List<Float4x4>();
        }

        public CompMeshList(Component parent, string objFilePath, MaterialFactory materialFactory) : this(parent)
        {
            AddMesh(objFilePath, materialFactory);
        }

        public CompMeshList(Component parent, string objFilePath, MaterialFactory materialFactory, Float4x4 transform) : this(new CompTransformStack(parent, transform))
        {
            AddMesh(objFilePath, materialFactory);
        }

        public CompMesh AddMesh(CompMeshGeometry geometry, CompMaterial material = null)
        {
            CompMesh mesh = new CompMesh(this, material, geometry);
            blocks.Add(mesh);
            UpdateMesh(mesh);
            return mesh;
        }

        public void AddMesh(string objFilePath)
        {
            ObjParsingArgs args = new ObjParsingArgs();
            args.ChangeFaceOrientation = true;
            GetComponent<CompObjToMesh>().ParseAsync(objFilePath, args, this);
        }

        public void AddMesh(string objFilePath, MaterialFactory matFactory)
        {
            ObjParsingArgs args = new ObjParsingArgs();
            args.ChangeFaceOrientation = true;
            args.MaterialFactory = matFactory;
            GetComponent<CompObjToMesh>().ParseAsync(objFilePath, args, this);
        }

        public void AddMesh(string objFilePath, CompMaterial overrideMaterial)
        {
            ObjParsingArgs args = new ObjParsingArgs();
            args.DestinationMesh = this;
            args.ChangeFaceOrientation = true;
            args.Material = overrideMaterial;
            GetComponent<CompObjToMesh>().ParseAsync(objFilePath, args, this);
        }

        public void AddMesh(string objFilePath, ObjParsingArgs args)
        {
            GetComponent<CompObjToMesh>().ParseAsync(objFilePath, args, this);
        }

        public CompMesh AddMesh()
        {
            CompMesh mesh = new CompMesh(this);
            blocks.Add(mesh);
            UpdateMesh(mesh);
            return mesh;
        }

        public void RemoveMesh(int index)
        {
            CompMesh mesh = blocks[index];
            blocks.RemoveAt(index);
            mesh.Dispose();
        }

        public void SetMainMaterial(CompMaterial m)
        {
            for (int i = 0; i < MeshCount; i++)
                blocks[i].MainMaterial = m;
        }

        public void SetMainMaterials(IList<CompMaterial> materials)
        {
            for (int i = 0; i < MeshCount; i++)
                blocks[i].MainMaterial = materials[i % materials.Count];
        }

        public List<CompMaterial> GetMainMaterials()
        {
            List<CompMaterial> materials = new List<CompMaterial>();
            foreach (CompMesh mesh in blocks)
                materials.Add(mesh.MainMaterial);

            return materials;
        }

        public void Clear()
        {
            for (int i = 0; i < MeshCount; i++)
                blocks[i].Dispose();
            blocks.Clear();
        }

        public void AddInstance(Float4x4 instMatrix)
        {
            instances.Add(instMatrix);
            NeededUpdates = UpdateType.FrameStart1;
        }


        public void ClearInstances()
        {
            instances.Clear();
            NeededUpdates = UpdateType.FrameStart1;
        }

        public void Update(UpdateType updateType)
        {
            for (int i = 0; i < MeshCount; i++)
                UpdateMesh(blocks[i]);

            NeededUpdates = UpdateType.None;
        }

        private void UpdateMesh(CompMesh mesh)
        {
            mesh.Instances.Clear();
            mesh.Instances.AddRange(instances);
        }
    }
}
