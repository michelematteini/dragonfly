using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;

namespace Dragonfly.Terrain
{
    public class TerrainPhysicalMaterialFactory : ITerrainMaterialFactory
    {
        public TerrainPhysicalMaterialFactory(Component parent)
        {
            DetailNormalMap = new CompTextureRef(parent, new Byte4(255, 127, 127, 255));
            DetailLargeNormalMap = new CompTextureRef(parent, new Byte4(255, 127, 127, 255));
            DetailTilingPeriod = 1.0f;
            LargeDetailTilingPeriod = 4.0f;
        }

        private CompTextureRef DetailNormalMap { get; set; }

        public float DetailTilingPeriod { get; set; }

        private CompTextureRef DetailLargeNormalMap { get; set; }

        public float LargeDetailTilingPeriod { get; set; }

        public void SetDetail(string detailName)
        {
            DetailNormalMap.SetSource($"textures/terrain/detail/{detailName}_normal.dds");
            DetailLargeNormalMap.SetSource($"textures/terrain/detail/{detailName}_large_normal.dds");
        }

        public CompAtmosphere AtmosphereForRadiance { get; set; }

        public CompMaterial CreateMaterialFromData(CompTerrainTile tile, Component parent, TerrainTileData data)
        {
            CompMtlTerrainPhysical m = new CompMtlTerrainPhysical(parent, tile);
            m.VistaMap.SetSource(data.Albedo);
            m.NormalMap.SetSource(data.Normal);
            m.Displacement.Map.SetSource(data.Displacement);
            m.Displacement.Offset.Value = data.DisplacementOffset;
            m.Displacement.Scale.Value = data.DisplacementScale;
            m.Tessellation.Value = (Int2)tile.ParentTerrain.DataSource.TileTessellation;
            m.PrevTessDivisor.Value = 2.0f;
            m.PrevEdgeDivisors.Value = new TerrainEdgeTessellation(2).ToFloat4();

            // fill params from parent tile
            if (tile.ParentTile != null)
            {
                // initialize LOD morphing params on material creation, so that these are already updated when the tile is displayed.
                TerrainEdgeTessellation scaledPrevEdges;
                scaledPrevEdges.LeftDivisor = tile.OffsetToParentPercent.X == 0 ? 2 * tile.ParentTile.EdgeTessellation.LeftDivisor : 2;
                scaledPrevEdges.TopDivisor = tile.OffsetToParentPercent.Y == 0 ? 2 * tile.ParentTile.EdgeTessellation.TopDivisor : 2;
                scaledPrevEdges.RightDivisor = tile.OffsetToParentPercent.X == 0 ? 2 : 2 * tile.ParentTile.EdgeTessellation.RightDivisor;
                scaledPrevEdges.BottomDivisor = tile.OffsetToParentPercent.Y == 0 ? 2 : 2 * tile.ParentTile.EdgeTessellation.BottomDivisor;
                m.PrevEdgeDivisors.Value = scaledPrevEdges.ToFloat4();

                if (tile.ParentTile.Drawable.MainMaterial is CompMtlTerrainPhysical parentMaterial)
                {
                    // fill params from parent tile material (will be used for the transition / morphing effect)
                    m.GetParentMaterial = () => tile.ParentTile.Drawable.MainMaterial as CompMtlTerrainPhysical;
                    m.PrevVistaUVOffset.Value = tile.OffsetToParentPercent;
                }
            }

            // fill detail parameters
            if (DetailNormalMap != null)
            {
                m.DetailUVScaleOffset.Value = new Float4(
                    tile.Area.Size.X,
                    tile.Area.Size.Y,
                    FMath.Mod(tile.Area.Position.Value.Dot(tile.Area.XSideDir), DetailTilingPeriod), 
                    FMath.Mod(tile.Area.Position.Value.Dot(tile.Area.YSideDir), DetailTilingPeriod)
                    ) / DetailTilingPeriod;
                m.DetailFadingDistanceRange.Value = new Range<float>(2.5f, 5.0f);
                m.DetailNormalMap.SetSource(DetailNormalMap);
            }
            if (DetailLargeNormalMap != null)
            {
                m.DetailLargeUVScaleOffset.Value = new Float4(
                    tile.Area.Size.X,
                    tile.Area.Size.Y,
                    FMath.Mod(tile.Area.Position.Value.Dot(tile.Area.XSideDir), LargeDetailTilingPeriod),
                    FMath.Mod(tile.Area.Position.Value.Dot(tile.Area.YSideDir), LargeDetailTilingPeriod)
                    ) / LargeDetailTilingPeriod;
                m.DetailLargeFadingDistanceRange.Value = new Range<float>(4.0f, 32.0f);
                m.DetailLargeNormalMap.SetSource(DetailLargeNormalMap);
            }

            // update radiance to work with the atmosphere if one is specified
            if (AtmosphereForRadiance != null)
            {
                m.IndirectLighting.UseRadianceFromAtmosphere(AtmosphereForRadiance);
            }

            return m;
        }
    }
}
