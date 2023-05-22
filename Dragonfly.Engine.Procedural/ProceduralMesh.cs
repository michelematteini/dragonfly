using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;


namespace Dragonfly.Engine.Procedural
{
    public static class ProceduralMesh
    {
        public static CompMeshList Generate(Component parent, ProceduralMeshDescription md, MaterialFactory matFactory)
        {
            if (md is ProceduralTreeDescr treeDescr)
                return ProceduralTree.Generate(parent, treeDescr, matFactory);

            return null;
        }

        public static CompMeshList Generate(Component parent, string meshDescriptionFile, MaterialFactory matFactory)
        {
            return Generate(parent, ProceduralMeshDescription.LoadFromFile(meshDescriptionFile), matFactory);
        }

    }
}
