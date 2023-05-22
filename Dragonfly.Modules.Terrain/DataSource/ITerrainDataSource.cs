using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// Represents a collection of texture and other information about a terrain area, filled by a data source on demand.
    /// </summary>
    public struct TerrainTileData
    {
        /// <summary>
        /// Area covered by this tile, as provided to the data request
        /// </summary>
        public TiledRect3 Area;
        /// <summary>
        /// The color texture for this terrain tile
        /// </summary>
        public CompTextureRef Albedo;
        /// <summary>
        /// The normal texture for this tile in model-space.
        /// </summary>
        public CompTextureRef Normal;
        /// <summary>
        /// Vertical displacement map of vertices to the terrain height.
        /// </summary>
        public CompTextureRef Displacement;
        /// <summary>
        /// Min value written to the Displacement texture.
        /// </summary>
        public float DisplacementMin;
        /// <summary>
        /// Max value written to the Displacement texture.
        /// </summary>
        public float DisplacementMax;
        /// <summary>
        /// Offset to be applied to the heights coming from the Displacement texture.
        /// </summary>
        public float DisplacementOffset;
        /// <summary>
        /// Scale to be applied to the heights coming from the Displacement texture.
        /// </summary>
        public float DisplacementScale;
        /// <summary>
        /// Offset to be applied to the texture coordinates in the vertex buffer.
        /// </summary>
        public Float2 TexCoordsOffset;
        /// <summary>
        /// Scale to be applied to the texture coordinates in the vertex buffer.
        /// </summary>
        public Float2 TexCoordsScale;
    }

    /// <summary>
    /// Contains information about the topology of the terrain to be rendered.
    /// </summary>
    public interface ITerrainDataSource
    {
        /// <summary>
        /// Retrieve terrain data for a specific area. 
        /// This call return false if the data has still not been processed for the specified area.
        /// <para/> Once this call return true, a subsequent call will trigger a new computation.
        /// </summary>
        bool TryGetTileData(TiledRect3 area, CompTerrainCurvature curvature, Component dataParent, out TerrainTileData terrainData);

        /// <summary>
        /// Returns true if this data source is currently processing tiles
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// Tessellation required for each tile.
        /// </summary>
        int TileTessellation { get; }

        /// <summary>
        /// The minimum time a LOD  should be displayed before switching to a new one.
        /// </summary>
        float MinLodSwitchTimeSeconds { get; }

        /// <summary>
        /// Signal to the data source that terrain data for a specific area, previously requested with TryGetTerrainData is no longer needed.
        /// </summary>
        void DeleteTileData(TiledRect3 area);

    }
}
