using Dragonfly.Engine.Core;

namespace Dragonfly.Terrain
{
    public interface ITerrainMaterialFactory
    {
        CompMaterial CreateMaterialFromData(CompTerrainTile tile, Component parent, TerrainTileData data);
    }
}