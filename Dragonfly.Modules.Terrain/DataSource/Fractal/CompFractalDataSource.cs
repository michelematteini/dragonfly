using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// A terrain data source that generate texture and displacements from parametric fractal noises.
    /// </summary>
    public class CompFractalDataSource : Component
    {
        struct NoiseCachedTile
        {
            public TiledRect3 Area;
            internal CompTextureRef NoiseCache;
        }

        class FractalCustomData
        {
            public RenderTargetRef AlbedoNoiseTex;
        }

        private CompGPUDataSource gpuSource;
        private Dictionary<TiledRect3, NoiseCachedTile> baseNoiseCache;
        private Dictionary<TiledRect3, NoiseCachedTile> detailNoiseCache;
        private object noiseCacheLock;
        private float maxTileWorldSize;

        public CompFractalDataSource(Component parent, Int2 tileTextureSize, int tileTessellation) : base(parent)
        {
            gpuSource = new CompGPUDataSource(this, tileTextureSize, tileTessellation);
            gpuSource.InitializeTileData = InitializeTileData;
            gpuSource.CreateTexBakingMaterial = CreateTexBakingMaterial;
            gpuSource.CreateDisplaceBakingMaterial = CreateDisplaceBakingMaterial;
            gpuSource.OnTileDataDelete = OnTileDataDelete;
            gpuSource.CanRenderArea = CanRenderArea;
            Source = gpuSource;
            ProceduralParams = new FractalDataSourceParams(this);
            baseNoiseCache = new Dictionary<TiledRect3, NoiseCachedTile>();
            detailNoiseCache = new Dictionary<TiledRect3, NoiseCachedTile>();
            noiseCacheLock = new object();
        }

        public ITerrainDataSource Source { get; private set; }

        private bool CanRenderArea(TiledRect3 area)
        {
            // use the first request to initialize the maximum tile size
            // ( assumes the root tile il rendered first )
            if (maxTileWorldSize == 0)
                maxTileWorldSize = area.Size.X;

            if (!ProceduralParams.AlbedoLUT.Loaded)
                return false;

            int areaSolvableOctave = GetMaxSolvableOctave(area);

            // check that cached noises are ready (if not the parent tiles should be rendered first)
            if (areaSolvableOctave > GetNoiseCacheRenderOctave(ProceduralParams.GetFirstDetailEndOctave()) && !TryFindNoiseCache(detailNoiseCache, area, out _))
                return false; 
            if (areaSolvableOctave > GetNoiseCacheRenderOctave(ProceduralParams.GetContinentEndOctave()) && !TryFindNoiseCache(baseNoiseCache, area, out _))
                return false;

            return true;
        }

        private void InitializeTileData(TerrainTileBakingArgs args, CompGPUDataSource.OnTileDataInitializedCallback onDataReadyCallback)
        {
            // initialize tile struct
            TerrainTileData tileData = new TerrainTileData();
            tileData.Area = args.Area;
            tileData.TexCoordsOffset = Float2.Zero;
            tileData.TexCoordsScale = Float2.One;
            tileData.DisplacementOffset = 0;
            tileData.DisplacementScale = 1;
            tileData.Albedo = new CompTextureRef(args.ResultsParent);
            tileData.Normal = new CompTextureRef(args.ResultsParent);
            tileData.Displacement = new CompTextureRef(args.ResultsParent);
            args.CustomData = new FractalCustomData();

            
            int passLeft = 1;
            Action continueBaking = () =>
            {
                // execute the callback when all the declared passes are completed
                passLeft--;
                if (passLeft == 0)
                    onDataReadyCallback(tileData);
            };

            int tileMaxSolvableOctave = GetMaxSolvableOctave(tileData.Area);

            // bake base noise if needed
            if (tileMaxSolvableOctave == GetNoiseCacheRenderOctave(ProceduralParams.GetContinentEndOctave()) && !TryFindNoiseCache(baseNoiseCache, args.Area, out _))
            {
                passLeft++;

                // bake current base noise tile
                TiledRect3 baseNoiseArea = GetNoiseCacheArea(ProceduralParams.GetContinentEndOctave(), tileData.Area);
                CompBakerScreenSpace noiseBaker = gpuSource.AddBakerToArea(args.Area, "TerrainBaseNoise", gpuSource.TileDisplacementTexSize, new SurfaceFormat[] { SurfaceFormat.Float4 });
                noiseBaker.Material = CreateBaseNoiseBakingMaterial(args, baseNoiseArea);
                noiseBaker.Baker.Paused = false;
                
                NoiseCachedTile noiseCache = new NoiseCachedTile();
                noiseCache.Area = baseNoiseArea;
                noiseCache.NoiseCache = new CompTextureRef(this);
                noiseBaker.Baker.OnCompletion = targets =>
                {        
                    noiseBaker.Baker.Paused = true;
                    noiseCache.NoiseCache.SetSource(targets[0], TexRefFlags.None, true);

                    lock (noiseCacheLock)
                    {
                        // cache baked noise
                        baseNoiseCache.Add(noiseCache.Area, noiseCache);
                    }

                    continueBaking();
                };
            }

            // bake detail noise if needed
            if (tileMaxSolvableOctave == GetNoiseCacheRenderOctave(ProceduralParams.GetFirstDetailEndOctave()) && !TryFindNoiseCache(detailNoiseCache, args.Area, out _))
            {
                passLeft++;

                // bake current detail noise tile
                TiledRect3 detailNoiseArea = GetNoiseCacheArea(ProceduralParams.GetFirstDetailEndOctave(), tileData.Area);
                CompBakerScreenSpace noiseBaker = gpuSource.AddBakerToArea(args.Area, "TerrainDetailNoise", gpuSource.TileDisplacementTexSize, new SurfaceFormat[] { SurfaceFormat.Float4 });
                noiseBaker.Material = CreateDetailNoiseBakingMaterial(args, detailNoiseArea);
                noiseBaker.Baker.Paused = false;

                NoiseCachedTile noiseCache = new NoiseCachedTile();
                noiseCache.Area = detailNoiseArea;
                noiseCache.NoiseCache = new CompTextureRef(this);
                noiseBaker.Baker.OnCompletion = targets =>
                {
                    noiseBaker.Baker.Paused = true;
                    noiseCache.NoiseCache.SetSource(targets[0], TexRefFlags.None, true);

                    lock (noiseCacheLock)
                    {
                        // cache baked noise
                        detailNoiseCache.Add(noiseCache.Area, noiseCache);
                    }

                    continueBaking();
                };
            }

            // albedo noise pre-bake
            {
                passLeft++;

                // bake current base noise tile
                CompBakerScreenSpace noiseBaker = gpuSource.AddBakerToArea(args.Area, "TerrainAlbedoNoise", gpuSource.TileTextureSize, new SurfaceFormat[] { SurfaceFormat.Color });
                noiseBaker.Material = CreateAlbedoNoiseBakingMaterial(args);
                noiseBaker.Baker.Paused = false;
                noiseBaker.Baker.OnCompletion = targets =>
                {
                    noiseBaker.Baker.Paused = true;
                    (args.CustomData as FractalCustomData).AlbedoNoiseTex = targets[0];
                    continueBaking();
                };
            }


            continueBaking();
        }

        /// <summary>
        /// Paramters that are used to generate the terrain heightmap and textures
        /// </summary>
        public FractalDataSourceParams ProceduralParams { get; set; }

        /// <summary>
        /// Returns the octave at which the noise cache should be rendered
        /// </summary>
        private int GetNoiseCacheRenderOctave(int lastNoiseOctave)
        {
            int fullTerrainSolveOctave = GPUNoise.MaxSolvableOctave(maxTileWorldSize, gpuSource.TileDisplacementTexSize.X);
            return Math.Max(lastNoiseOctave, fullTerrainSolveOctave);
        }

        /// <summary>
        /// Returns the area for which the noise cache should be rendered that contains the specified child area.
        /// </summary>
        private TiledRect3 GetNoiseCacheArea(int lastNoiseOctave, TiledRect3 area)
        {
            int areaSolvableOctave = GPUNoise.MaxSolvableOctave(area.Size.X, gpuSource.TileDisplacementTexSize.X);
            area.Size *= FMath.Exp2(areaSolvableOctave - lastNoiseOctave);
            return area;
        }

        /// <summary>
        /// Return the maximum octave that can be solved by the specified area.
        /// </summary>
        private int GetMaxSolvableOctave(TiledRect3 area)
        {
            return GPUNoise.MaxSolvableOctave(area.Size.X, gpuSource.TileDisplacementTexSize.X);
        }

        private bool TryFindNoiseCache(Dictionary<TiledRect3, NoiseCachedTile> cache, TiledRect3 area, out NoiseCachedTile noiseCache)
        {
            lock (noiseCacheLock)
            {
                foreach (TiledRect3 cachedArea in cache.Keys)
                {
                    if (area.XSideDir != cachedArea.XSideDir || area.YSideDir != cachedArea.YSideDir || !area.IsCoplanarWith(cachedArea))
                        continue; // not from the same terrain / surface!

                    if (cachedArea.GetCoordsAt(area.Center).IsBetween(0, 1))
                    {
                        noiseCache = cache[cachedArea];
                        return true;
                    }
                }
            }

            noiseCache = new NoiseCachedTile();
            return false;
        }

        private CompMaterial CreateBakingMaterial(TerrainTileBakingArgs args, string effectName, Int2 gridSize)
        {
            CompMtlFractalDataSource m = new CompMtlFractalDataSource(args.BakingParent, effectName, args.Curvature, args.Area)
            {
                Noise = ProceduralParams,
                TileTextureSize = gridSize,
                BaseNoiseSource = CompMtlFractalDataSource.NoiseSrc.Distribution
            };
            m.AlbedoNoiseTex = (args.CustomData as FractalCustomData).AlbedoNoiseTex;

            if (effectName == CompMtlFractalDataSource.DisplacementEffectName)
            {
                int maxSolvableOctave = GetMaxSolvableOctave(args.Area);

                if (maxSolvableOctave >= GetNoiseCacheRenderOctave(ProceduralParams.GetContinentEndOctave()))
                {
                    NoiseCachedTile noiseCache;
                    if (TryFindNoiseCache(baseNoiseCache, args.Area, out noiseCache))
                    {
                        // baked base noise, reusing base noise from previously baked tiles
                        m.BaseNoiseSource = CompMtlFractalDataSource.NoiseSrc.Texture;
                        m.BaseNoiseTex = noiseCache.NoiseCache;
                        m.BaseNoiseTexOffset = noiseCache.Area.GetCoordsAt(args.Area.Position);
                        m.BaseNoiseRegionSize = noiseCache.Area.GetCoordsAt(args.Area.EndCorner) - m.BaseNoiseTexOffset;
                    }
                    else
                    {
                        // cache miss should not happen here!
                    }
                }

                if (maxSolvableOctave >= GetNoiseCacheRenderOctave(ProceduralParams.GetFirstDetailEndOctave()))
                {
                    NoiseCachedTile noiseCache;
                    if (TryFindNoiseCache(detailNoiseCache, args.Area, out noiseCache))
                    {
                        // baked base noise, reusing base noise from previously baked tiles
                        m.DetailNoiseSource = CompMtlFractalDataSource.NoiseSrc.Texture;
                        m.DetailNoiseTex = noiseCache.NoiseCache;
                        m.DetailNoiseTexOffset = noiseCache.Area.GetCoordsAt(args.Area.Position);
                        m.DetailNoiseRegionSize = noiseCache.Area.GetCoordsAt(args.Area.EndCorner) - m.DetailNoiseTexOffset;
                    }
                    else
                    {
                        // cache miss should not happen here!
                    }
                }
            }
            return m;
        }

        private CompMaterial CreateTexBakingMaterial(TerrainTileBakingArgs args)
        {
            return CreateBakingMaterial(args, CompMtlFractalDataSource.TexturesEffectName, gpuSource.TileTextureSize);
        }

        private CompMaterial CreateDisplaceBakingMaterial(TerrainTileBakingArgs args)
        {
            return CreateBakingMaterial(args, CompMtlFractalDataSource.DisplacementEffectName, gpuSource.TileDisplacementTexSize);
        }

        private CompMaterial CreateBaseNoiseBakingMaterial(TerrainTileBakingArgs args, TiledRect3 areaOverride)
        {
            CompMtlFractalDataSource m = new CompMtlFractalDataSource(args.BakingParent, CompMtlFractalDataSource.BaseNoiseEffectName, args.Curvature, areaOverride)
            {
                Noise = ProceduralParams,
                TileTextureSize = gpuSource.TileDisplacementTexSize,
                BaseNoiseSource = CompMtlFractalDataSource.NoiseSrc.Distribution,
                DetailNoiseSource = CompMtlFractalDataSource.NoiseSrc.Distribution
            };
            return m;
        }

        private CompMaterial CreateDetailNoiseBakingMaterial(TerrainTileBakingArgs args, TiledRect3 areaOverride)
        {
            CompMtlFractalDataSource m = new CompMtlFractalDataSource(args.BakingParent, CompMtlFractalDataSource.DetailNoiseEffectName, args.Curvature, areaOverride)
            {
                Noise = ProceduralParams,
                TileTextureSize = gpuSource.TileDisplacementTexSize,
                BaseNoiseSource = CompMtlFractalDataSource.NoiseSrc.Distribution,
                DetailNoiseSource = CompMtlFractalDataSource.NoiseSrc.Distribution
            };
            return m;
        }

        private CompMaterial CreateAlbedoNoiseBakingMaterial(TerrainTileBakingArgs args)
        {
            return CreateBakingMaterial(args, CompMtlFractalDataSource.AlbedoNoiseEffectName, gpuSource.TileTextureSize);
        }

        private void OnTileDataDelete(TiledRect3 area)
        {
            lock (noiseCacheLock)
            {
                NoiseCachedTile noiseCache;
                if (GetMaxSolvableOctave(area) == GetNoiseCacheRenderOctave(ProceduralParams.GetContinentEndOctave()) && baseNoiseCache.TryGetValue(area, out noiseCache))
                {
                    noiseCache.NoiseCache.Dispose();
                    baseNoiseCache.Remove(area);
                }
                if (GetMaxSolvableOctave(area) == GetNoiseCacheRenderOctave(ProceduralParams.GetFirstDetailEndOctave()) && detailNoiseCache.TryGetValue(area, out noiseCache))
                {
                    noiseCache.NoiseCache.Dispose();
                    detailNoiseCache.Remove(area);
                }
            }
        }

    }

}
