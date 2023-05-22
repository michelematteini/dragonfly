using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Graphics.Resources;
using Dragonfly.Utils;
using System;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// A physical material dedicated to terrain tiles.
    /// </summary>
    public class CompMtlTerrainPhysical : CompMtlTemplatePhysical, ITerrainMaterial
    {
        public CompMtlTerrainPhysical(Component owner, CompTerrainTile parentTile) : base(owner)
        {
            VistaMap = new CompTextureRef(this, Color.White);
            MonitoredParams.Add(VistaMap);
            NormalMap = new CompTextureRef(this, new Byte4(255, 127, 127, 255));
            MonitoredParams.Add(NormalMap);
            CompTextureRef displMap = new CompTextureRef(this, Color.Black);
            Displacement = new MtlModDisplacement(this, displMap);
            CullMode = Context.GetModule<BaseMod>().Settings.DefaultCullMode;
            MonitoredParams.Add(GetComponent<CompIndirectLightManager>().DefaultBackgroundRadiance);
            Shadows = new MtlModShadowMapBiasing(this);
            MorphTimeStart = MakeParam<PreciseFloat>(PreciseFloat.Infinity);
            MorphDuration = MakeParam<float>(5.0f);
            Tessellation = MakeParam<Int2>((Int2)16);
            PrevVistaUVOffset = MakeParam<Float2>(Float2.Zero);
            GetParentMaterial = () => null;
            PrevEdgeTessellation = new TerrainEdgeTessellation(1);
            PrevEdgeDivisors = MakeParam(Float4.One);
            PrevTessDivisor = MakeParam(1.0f);
            CurvatureWorldPreOffset = parentTile.CurvatureWorldPreOffset;
            Curvature = new MtlModTileCurvature(this, parentTile.ParentTerrain.Curvature, parentTile.Area);
            DetailNormalMap = new CompTextureRef(this, new Byte4(255, 127, 127, 255));
            MonitoredParams.Add(DetailNormalMap);
            DetailFadingDistanceRange = MakeParam<Range<float>>(new Range<float>(1.0f, 4.0f));
            DetailUVScaleOffset = MakeParam(new Float4(1, 1, 0, 0));
            DetailLargeNormalMap = new CompTextureRef(this, new Byte4(255, 127, 127, 255));
            MonitoredParams.Add(DetailLargeNormalMap);
            DetailLargeFadingDistanceRange = MakeParam<Range<float>>(new Range<float>(4.0f, 32.0f));
            DetailLargeUVScaleOffset = MakeParam(new Float4(1, 1, 0, 0));
            DetailTangent = MakeParam(parentTile.Area.XSideDir);
            IndirectLighting = new MtlModIndirectLighting(this);
        }

        public override string EffectName
        {
            get { return "TerrainPhysicalMaterial"; }
        }

        #region Material Params 

        public CompTextureRef VistaMap { get; protected set; }

        public CompTextureRef NormalMap { get; protected set; }

        public CompTextureRef DetailNormalMap { get; protected set; }

        public Param<Float4> DetailUVScaleOffset { get; private set; }
        
        public Param<Range<float>> DetailFadingDistanceRange { get; private set; }

        public CompTextureRef DetailLargeNormalMap { get; protected set; }

        public Param<Float4> DetailLargeUVScaleOffset { get; private set; }

        public Param<Range<float>> DetailLargeFadingDistanceRange { get; private set; }

        public Param<Float3> DetailTangent { get; private set; }

        public Param<Float3> DetailBitangent { get; private set; }

        public Param<Float2> PrevVistaUVOffset { get; private set; }

        public MtlModDisplacement Displacement { get; private set; }

        public MtlModShadowMapBiasing Shadows { get; private set; }

        public Param<PreciseFloat> MorphTimeStart { get; private set; }

        public Param<float> MorphDuration { get; private set; }

        public Param<Int2> Tessellation { get; private set; }

        public Param<Float4> PrevEdgeDivisors { get; set; }

        public Param<float> PrevTessDivisor { get; set; }

        public MtlModTileCurvature Curvature { get; private set; }

        public TiledFloat3 CurvatureWorldPreOffset { get; private set; }

        public MtlModIndirectLighting IndirectLighting { get; private set; }

        #endregion

        public Func<CompMtlTerrainPhysical> GetParentMaterial { get; set; }

        private TerrainEdgeTessellation PrevEdgeTessellation { get; set; }

        private void UpdateMorphTimeParam(CompTerrainTile tile)
        {
            MorphTimeStart.Value = tile.ParentTerrain.IsAnyLODAvailable ? Context.Time.SecondsFromStart : PreciseFloat.Zero;
            MorphDuration.Value = tile.ParentTerrain.DataSource.MinLodSwitchTimeSeconds * 0.99f; // animate almost along the whole lod switch duration
        }

        public void OnLODChanged(CompTerrainTile tile)
        {
            if (tile.WasVisibleInPreviousLOD)
            {
                // displayed again with the same LOD (may have different edges that should morph)
                PrevTessDivisor.Value = 1.0f;
                PrevEdgeDivisors.Value = PrevEdgeTessellation.ToFloat4();
                UpdateMorphTimeParam(tile);
            }
            else if (tile.LastNodeEvent == QuadTreeNodeEvent.Grouped)
            {
                // just grouped, do not animate
            }         
            else
            {
                // new tile, morph
                UpdateMorphTimeParam(tile);
            }

            PrevEdgeTessellation = tile.EdgeTessellation;
        }

        protected override void UpdateParams()
        {
            base.UpdateParams();
            Shader.SetParam("vistaMap", VistaMap);
            Shader.SetParam("normalMap", NormalMap);
            Shader.SetParam("morphTimeRange", new Float4(MorphTimeStart.Value.ToFloat2(), (MorphTimeStart.Value + new PreciseFloat(MorphDuration.Value)).ToFloat2()));
            Shader.SetParam("vertexGridSize", Tessellation.Value);
            Shader.SetParam("prevEdgeDivisors", PrevEdgeDivisors);
            Shader.SetParam("prevTessDivisor", PrevTessDivisor);
            CompMtlTerrainPhysical parentMat = GetParentMaterial();
            Shader.SetParam("prevVistaMap", parentMat != null ? parentMat.VistaMap : VistaMap);
            Shader.SetParam("prevNormalMap", parentMat != null ? parentMat.NormalMap : NormalMap);
            Shader.SetParam("prevVistaUVOffset", PrevVistaUVOffset);
            Shader.SetParam("tilePreOffset", (Float3)CurvatureWorldPreOffset.Tile);
            Shader.SetParam("worldPreOffset", CurvatureWorldPreOffset.Value);

            Shader.SetParam("detailNormalMap", DetailNormalMap);
            Shader.SetParam("detailFadingNearFar", new Float2(DetailFadingDistanceRange.Value.From, DetailFadingDistanceRange.Value.To));
            Shader.SetParam("detailUvScaleOffset", DetailUVScaleOffset.Value);
            Shader.SetParam("detailLargeNormalMap", DetailLargeNormalMap);
            Shader.SetParam("detailLargeFadingNearFar", new Float2(DetailLargeFadingDistanceRange.Value.From, DetailLargeFadingDistanceRange.Value.To));
            Shader.SetParam("detailLargeUvScaleOffset", DetailLargeUVScaleOffset.Value);
            Shader.SetParam("detailTangent", DetailTangent.Value);
        }

    }

}