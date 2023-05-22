using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using System;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// A terrain data source that only returns the specified defaults.
    /// </summary>
    public class CompFlatDataSource : Component
    {
        private CompGPUDataSource gpuSource;

        public CompFlatDataSource(Component parent, Float3 defaultColor) : base(parent)
        {
            DefaultColor = defaultColor;
            gpuSource = new CompGPUDataSource(this, new Int2(512), 16);
            gpuSource.InitializeTileData = InitializeTileData;
            gpuSource.CreateTexBakingMaterial = CreateTexBakingMaterial;
            gpuSource.CreateDisplaceBakingMaterial = CreateDisplaceBakingMaterial;
            Source = gpuSource;
        }

        public Float3 DefaultColor { get; set; }

        public ITerrainDataSource Source { get; private set; }

        private void InitializeTileData(TerrainTileBakingArgs args, CompGPUDataSource.OnTileDataInitializedCallback onDataReadyCallback)
        {
            TerrainTileData tileData = new TerrainTileData();
            tileData.Area = args.Area;
            tileData.TexCoordsOffset = Float2.Zero;
            tileData.TexCoordsScale = Float2.One;
            tileData.DisplacementMin = 0;
            tileData.DisplacementMax = 1;
            tileData.DisplacementOffset = 0;
            tileData.DisplacementScale = 1;
            tileData.Albedo = new CompTextureRef(args.ResultsParent);
            tileData.Normal = new CompTextureRef(args.ResultsParent);
            tileData.Displacement = new CompTextureRef(args.ResultsParent);
            onDataReadyCallback(tileData);
        }

        private CompMaterial CreateTexBakingMaterial(TerrainTileBakingArgs args)
        {
            return new CompMtlFlatDataSrc(args.ResultsParent, "TerrainSrcTexFlat", args.Curvature, args.Area) { DefaultColor = DefaultColor};
        }

        private CompMaterial CreateDisplaceBakingMaterial(TerrainTileBakingArgs args)
        {
            return new CompMtlFlatDataSrc(args.ResultsParent, "TerrainSrcDisplFlat", args.Curvature, args.Area) { DefaultColor = DefaultColor };
        }

    }
}
