using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using System.Collections.Generic;
using System;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;

namespace Dragonfly.BaseModule
{
    public class CompMesh : CompDrawable
    {
        private CompMeshGeometry editableGeometry;

        public CompMesh(Component owner, CompMaterial material = null, IMeshGeometry geometry = null) : base(owner)
        {
            Geometry = geometry;
            if (Geometry == null)
                EditableGeometry = new CompMeshGeometry(owner, new List<VertexTexNorm>(), new List<ushort>());

            if (material != null)
                Materials.Add(material);

            CastShadows = true;
        }

        public CompMesh(Component owner, CompMaterial material, List<VertexTexNorm> vertices, List<ushort> indices) : this(owner, material, new CompMeshGeometry(owner, vertices, indices))
        {
            EditableGeometry = Geometry as CompMeshGeometry;
        }

        public IMeshGeometry Geometry { get; private set; }

        internal CompMeshGeometry EditableGeometry
        {
            get { return editableGeometry; }
            set
            {
                editableGeometry = value;
                Geometry = value;
            }
        }

        /// <summary>
        /// Access the first material rendered in the main pass.
        /// </summary>
        public CompMaterial MainMaterial
        {
            get
            {
                BaseMod baseMod = Context.GetModule<BaseMod>();
                if (baseMod.MainPass == null)
                    return null; // main material make sense only if a baseMod pipeline is initialized
                return GetFirstMaterialOfClass(baseMod.MainPass.MainClass);
            }
            set
            {
                BaseMod baseMod = Context.GetModule<BaseMod>();
                if (baseMod.MainPass == null)
                    return; // main material make sense only if a baseMod pipeline is initialized
                RemoveMaterialsOfClass(baseMod.MainPass.MainClass);
                if (value != null)
                    Materials.Add(value.OfClass(baseMod.MainPass.MainClass));
            }
        }

        public override AABox GetBoundingBox()
        {
            return Geometry.BoundingBox;
        }

        /// <summary>
        /// Search and remove materials of the specified class.
        /// </summary>
        /// <param name="disposeRemoved">If true, any removed material is also disposed.</param>
        public void RemoveMaterialsOfClass(string matClass, bool disposeRemoved = false)
        {
            for (int i = Materials.Count - 1; i >= 0; i--)
                if (Materials[i].Class.Contains(matClass))
                {
                    if (disposeRemoved) Materials[i].Dispose();
                    Materials.RemoveAt(i);
                }
        }

        /// <summary>
        /// Returns the list of materials with the specified class.
        /// </summary>
        public List<CompMaterial> GetMaterialsOfClass(string matClass)
        {
            List<CompMaterial> result = new List<CompMaterial>();
            for (int i = 0; i < Materials.Count; i++)
            {
                CompMaterial m = Materials[i];
                if (m.Class.Contains(matClass))
                    result.Add(m);
            }
            return result;
        }

        public CompMaterial GetFirstMaterialOfClass(string matClass)
        {
            for (int i = 0; i < Materials.Count; i++)
            {
                CompMaterial m = Materials[i];
                if (m.Class.Contains(matClass))
                    return m;
            }
            return null;
        }

        public bool Editable { get { return EditableGeometry != null; } }

        public override bool Ready
        {
            get { return Geometry.Available; }
        }

        private List<CompMeshAsyncGeometry> asyncGeomSearchCache = new List<CompMeshAsyncGeometry>();
        public IObject3D AsObject3D()
        {
            if (!Editable)
                throw new InvalidOperationException("This mesh is not editable and cannot be used as IObject3D.");

            // remove all currently active async updaters (to avoid the current geometry deletion
            foreach (CompMeshAsyncGeometry geomUpdater in GetChildren<CompMeshAsyncGeometry>(asyncGeomSearchCache))
                geomUpdater.Dispose();

            return EditableGeometry;
        }

        public IObject3D AsAsyncObject3D()
        {
            if (!Editable)
                throw new InvalidOperationException("This mesh is not editable and cannot be used as IObject3D.");
            return new CompMeshAsyncGeometry(this);
        }

        public override VertexBuffer GetVertexBuffer()
        {
            return Geometry.VertexBuffer;
        }

        public override IndexBuffer GetIndexBuffer()
        {
            return Geometry.IndexBuffer;
        }

        public bool CastShadows
        {
            get 
            {
                if (MainMaterial == null)
                    return false;
                BaseModSettings settings = Context.GetModule<BaseMod>().Settings;
                return MainMaterial.IsTemplateEnabled(settings.ShaderTemplates.ShadowMaps); 
            }
            set 
            {
                if (MainMaterial != null)
                {
                    BaseModSettings settings = Context.GetModule<BaseMod>().Settings;
                    MainMaterial.SetTemplateEnabled(settings.ShaderTemplates.ShadowMaps, value);
                }
            }
        }

    }
}
