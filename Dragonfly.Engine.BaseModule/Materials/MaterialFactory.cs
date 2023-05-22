using Dragonfly.Engine.Core;

namespace Dragonfly.BaseModule
{
    public abstract class MaterialFactory
    {
        /// <summary>
        /// A material class added to all the materials created with this factory.
        /// <para/> If this value is null or empy, no class is added.
        /// </summary>
        public string MaterialClass { get; set; }

        public CompMaterial CreateMaterial(MaterialDescription matDescr, Component parent)
        {
            CompMaterial m = CreateMaterialFromDescr(matDescr, parent);
            if (!string.IsNullOrEmpty(MaterialClass))
                m.Class.Add(MaterialClass);
            return m;
        }

        protected abstract CompMaterial CreateMaterialFromDescr(MaterialDescription matDescr, Component parent);
    }




}