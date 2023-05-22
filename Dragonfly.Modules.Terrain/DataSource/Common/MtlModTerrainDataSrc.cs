using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;

namespace Dragonfly.Terrain
{
    public class MtlModTerrainDataSrc : MaterialModule
    {
        public MtlModTerrainDataSrc(CompMaterial parentMaterial, CompTerrainCurvature curvature, TiledRect3 tileArea) : base(parentMaterial)
        {
            TileArea = tileArea;
            TileCurvature = new MtlModTileCurvature(parentMaterial, curvature, tileArea);
        }

        public TiledRect3 TileArea { get; private set; }

        public MtlModTileCurvature TileCurvature { get; private set; }

        protected override void UpdateAdditionalParams(Shader s)
        {
            s.SetParam("normal", TileArea.Normal);
            s.SetParam("basePosition", TileArea.Position.Value);
            s.SetParam("baseTile", TileArea.Position.Tile - TileCurvature.CurvatureCenter.Tile);
            s.SetParam("tileSize", TileArea.Size);
            s.SetParam("xDir", TileArea.XSideDir);
            s.SetParam("yDir", TileArea.YSideDir);
        }
    }
}
