using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System.Collections.Generic;

namespace Dragonfly.Engine.Core
{
    public abstract class CompDrawable : Component
    {
        private ObservableList<CompMaterial> materials;

        protected CompDrawable(Component parent) : base(parent)
        {
            materials = new ObservableList<CompMaterial>();
            materials.ItemAdded += item => { item.UsedBy.Add(this); OnMaterialsChanged(); };
            materials.ItemRemoved += item => { item.UsedBy.Remove(this); OnMaterialsChanged(); };

            IsBounded = true;
            Instances = new List<Float4x4>();
        }

        protected virtual void OnMaterialsChanged() { }

        public IList<CompMaterial> Materials
        {
            get { return materials; }
        }

        public bool IsBounded { get; set; }

        public virtual AABox GetBoundingBox()
        {
            return AABox.Infinite;
        }

        public abstract VertexBuffer GetVertexBuffer();

        public abstract IndexBuffer GetIndexBuffer();

        public List<Float4x4> Instances { get; protected set; }

    }

}
