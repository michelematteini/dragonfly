using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;

namespace Dragonfly.Terrain
{
    internal class CompMtlFractalDataSource : CompMaterial
    {
        public static readonly string DisplacementEffectName = "TerrainSrcDisplFractal";
        public static readonly string TexturesEffectName = "TerrainSrcTexFractal";
        public static readonly string BaseNoiseEffectName = "TerrainSrcBaseNoiseFractal";
        public static readonly string DetailNoiseEffectName = "TerrainSrcDetailNoiseFractal";
        public static readonly string AlbedoNoiseEffectName = "TerrainSrcAlbedoNoiseFractal";

        public enum NoiseSrc
        {
            Distribution = 0,
            Texture = 1
        }

        private string effectName;
        private NoiseSrc baseNoiseSrc, detailNoiseSrc;

        public CompMtlFractalDataSource(Component parent, string effectName, CompTerrainCurvature curvature, TiledRect3 tileArea) : base(parent)
        {
            this.effectName = effectName;
            DepthBufferEnable = false;
            DataSrc = new MtlModTerrainDataSrc(this, curvature, tileArea);
            Float2 terrainUVMin = curvature.TerrainArea.GetCoordsAt(tileArea.Position);
            Float2 terrainUVMax = curvature.TerrainArea.GetCoordsAt(tileArea.EndCorner);
            TerrainUVScaleOffset = new Float4(terrainUVMax - terrainUVMin, terrainUVMin);
        }

        public override bool Ready 
        { 
            get => base.Ready && GetComponent<CompRandom>().NoiseLut.Loaded; 
            protected set => base.Ready = value; 
        }

        public MtlModTerrainDataSrc DataSrc { get; private set; }

        public override string EffectName { get { return effectName; } }

        public Int2 TileTextureSize { get; set; }
        
        public RenderTargetRef AlbedoNoiseTex { get; set; }

        public FractalDataSourceParams Noise { get; set; }

        public NoiseSrc BaseNoiseSource
        {
            get
            {
                return baseNoiseSrc;
            }
            set
            {
                baseNoiseSrc = value;
                SetVariantValue("baseNoiseSrc", baseNoiseSrc.ToString());
            }
        }

        public CompTextureRef BaseNoiseTex { get; internal set; }

        public Float2 BaseNoiseTexOffset { get; set; }

        public Float2 BaseNoiseRegionSize { get; set; }

        public NoiseSrc DetailNoiseSource
        {
            get
            {
                return detailNoiseSrc;
            }
            set
            {
                detailNoiseSrc = value;
                SetVariantValue("detailNoiseSrc", detailNoiseSrc.ToString());
            }
        }

        public CompTextureRef DetailNoiseTex { get; internal set; }

        public Float2 DetailNoiseTexOffset { get; set; }

        public Float2 DetailNoiseRegionSize { get; set; }

        public Float4 TerrainUVScaleOffset { get; private set; }

        protected override void UpdateParams()
        {
            Shader.SetParam("gridTexelSize", 1.0f / (Float2)TileTextureSize);
            Shader.SetParam("terrainUVScaleOffset", TerrainUVScaleOffset);
            if (AlbedoNoiseTex != null)
                Shader.SetParam("albedoNoiseTex", AlbedoNoiseTex.GetValue());

            if(baseNoiseSrc == NoiseSrc.Texture)
            {
                Shader.SetParam("baseNoiseTex", BaseNoiseTex);
                Shader.SetParam("baseNoiseTexOffsetSize", new Float4(BaseNoiseTexOffset, BaseNoiseRegionSize));
            }

            if (detailNoiseSrc == NoiseSrc.Texture)
            {
                Shader.SetParam("detailNoiseTex", DetailNoiseTex);
                Shader.SetParam("detailNoiseTexOffsetSize", new Float4(DetailNoiseTexOffset, DetailNoiseRegionSize));
            }

            int maxSolvableOctave = FractalDataSourceParams.MaxOctave;
            if (effectName == TexturesEffectName)
            { 
                // remove aliasing octaves from textures
                // cannot be removed from displacement since it would create gaps
                maxSolvableOctave = GPUNoise.MaxSolvableOctave(DataSrc.TileArea.Size.X, TileTextureSize.X);
            }

            Noise.UpdateShader(Shader, maxSolvableOctave);
        }
    }
}

