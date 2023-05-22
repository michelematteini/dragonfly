using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// A GPU-accellerated terrain data source which uses user-provided functions to generate data.
    /// <para/> Manage all data requests and chunk creation pipeline and can be used as a build block of a shader-based terrain data source.
    /// </summary>
    public class CompGPUDataSource : Component, ITerrainDataSource
    {
        public delegate void OnTileDataInitializedCallback(TerrainTileData data);

        private Dictionary<TiledRect3, TerrainTileBakingRequest> activeRequests;
        private int lastBakeFrameID, procStartedThisFrame;
        private float[] displValuesCache;
        private BakerScreenSpacePool bakers;

        public CompGPUDataSource(Component parent, Int2 tileTextureSize, int tileTessellation) : base(parent)
        {
            activeRequests = new Dictionary<TiledRect3, TerrainTileBakingRequest>();
            bakers = new BakerScreenSpacePool();
            TileTextureSize = tileTextureSize;
            TileTessellation = tileTessellation.CeilPower2();
            MaxBakingThreadCount = 20;
            MaxBakeProcessPerFrame = 2;
            lastBakeFrameID = -1;
            procStartedThisFrame = 0;
            MinLodSwitchTimeSeconds = 1.0f;
        }

        /// <summary>
        /// The resolution of the textures generated for a terrain tile.
        /// </summary>
        public Int2 TileTextureSize { get; private set; }

        /// <summary>
        /// The resolution fo the displacement map generated for a terrain tile
        /// </summary>
        public Int2 TileDisplacementTexSize
        {
            get
            {
                return (Int2)(TileTessellation + 1);
            }
        }

        public int TileTessellation { get; private set; }

        public bool IsLoading => activeRequests.Count > 0;

        public float MinLodSwitchTimeSeconds { get; set; }

        /// <summary>
        /// The number of simultaneous tile data baking processes.
        /// </summary>
        public int MaxBakingThreadCount { get; set; }

        public int MaxBakeProcessPerFrame { get; set; }

        public bool TryGetTileData(TiledRect3 area, CompTerrainCurvature curvature, Component dataParent, out TerrainTileData terrainData)
        {
            terrainData = new TerrainTileData();

            if (!activeRequests.ContainsKey(area))
            {
                if (ShouldDelayBakingProcess())
                    return false; // make this request wait, to better distribute the load

                if (CanRenderArea != null && !CanRenderArea(area))
                    return false; // implementation require to delay this area

                // new request, initialize result 
                TerrainTileBakingRequest request = new TerrainTileBakingRequest();
                request.Args = new TerrainTileBakingArgs()
                {
                    Area = area,
                    BakingParent = new CompNode(this),
                    Curvature = curvature,
                    ResultsParent = dataParent
                };
                request.Bakers = new List<CompBakerScreenSpace>();
                request.StartedFrame = Context.Time.FrameIndex;
                activeRequests[area] = request;
                InitializeTileData(request.Args, terrainTileData =>
                {
                    request.Result = terrainTileData;
                    StartBakingProcess(request);
                });

                return false;
            }
            else if (activeRequests[area].CompletedSteps != TerrainTileBakingStep.AllSteps)
            {
                // already submitted, but result is not ready
                return false;
            }
            else
            {
                // data is ready, assign it and remove from queues
                TerrainTileBakingRequest request = activeRequests[area];
                activeRequests.Remove(area);
                terrainData = request.Result;
                request.Args.BakingParent.Dispose(); // free all baking resources

                // reset and keep bakers, will be reused
                foreach(CompBakerScreenSpace baker in request.Bakers)
                {
                    bakers.ReleaseBaker(baker);
                }

                return true;
            }
        }

        private CompBakerScreenSpace AddBakerToRequest(TerrainTileBakingRequest request, string stepName, Int2 resolution, SurfaceFormat[] formats)
        {
#if DEBUG // debug only, since string concatenation floods GC with instances...
            stepName += " - Area: " + request.Args.Area;
#endif
            CompBakerScreenSpace baker = bakers.CreateBaker(this, stepName, resolution, formats);
            request.Bakers.Add(baker);    
            return baker;
        }

        /// <summary>
        /// Create an additional baking step for the specified area. 
        /// This call is only valid after InitializeTileData() has been called for the specified area.
        /// </summary>
        public CompBakerScreenSpace AddBakerToArea(TiledRect3 area, string stepName, Int2 resolution, SurfaceFormat[] formats)
        {
            TerrainTileBakingRequest request = activeRequests[area];
            return AddBakerToRequest(request, stepName, resolution, formats);
        }

        private bool ShouldDelayBakingProcess()
        {
            if (activeRequests.Count >= MaxBakingThreadCount)
                return true; // max number of backing processes exceeded, wait until a slot frees up

            if (lastBakeFrameID < Context.Time.FrameIndex)
            {
                // new frame, reset started process count
                lastBakeFrameID = Context.Time.FrameIndex;
                procStartedThisFrame = 0;
            }

            if (procStartedThisFrame >= MaxBakeProcessPerFrame)
                return true; // too many baking process started this frame

            procStartedThisFrame++;
            return false;
        }

        private void StartBakingProcess(TerrainTileBakingRequest request)
        {
            // bake textures to rt
            {
                CompBakerScreenSpace texBaker = AddBakerToRequest(request, "TerrainTileTextures", TileTextureSize, new SurfaceFormat[] { SurfaceFormat.Color, SurfaceFormat.Color });
                texBaker.Material = CreateTexBakingMaterial(request.Args);
                texBaker.Baker.Paused = false;
                texBaker.Baker.OnCompletion = targets =>
                {
                    // copy baked resources to texture
                    request.Result.Normal.SetSource(targets[0], TexRefFlags.None, true);
                    request.Result.Albedo.SetSource(targets[1], TexRefFlags.None, true);
                    request.CompletedSteps |= TerrainTileBakingStep.NormalMapReady;
                    request.CompletedSteps |= TerrainTileBakingStep.AlbedoMapReady;
                    texBaker.Baker.Paused = true;
                }; // end texture baker ready callback
            }

            // bake displacement to rt
            {
                CompBakerScreenSpace displBaker = AddBakerToRequest(request, "TerrainTileDisplace", TileDisplacementTexSize, new SurfaceFormat[] { SurfaceFormat.Float });
                displBaker.Material = CreateDisplaceBakingMaterial(request.Args);
                displBaker.Baker.Paused = false;
                displBaker.Baker.OnCompletion = targets =>
                {
                    // copy baked displacement to texture
                    request.Result.Displacement.SetSource(targets[0], TexRefFlags.None, true);
                    request.CompletedSteps |= TerrainTileBakingStep.DisplacementReady;
                    displBaker.Baker.Paused = true;

                    // save a snapshot of the displacement (needed to build the tile bounding box)
                    targets[0].GetValue().SaveSnapshot();

                    // wait for the snapshot to be ready...
                    CompEventRtSnapshotReady snapshotReady = new CompEventRtSnapshotReady(request.Args.BakingParent, targets[0].GetValue());
                    new CompActionOnEvent(snapshotReady.Event, () =>
                    {
                        // data is ready! copy to a temp buffer
                        int dataLength = TileDisplacementTexSize.X * TileDisplacementTexSize.Y;
                        if (displValuesCache == null || displValuesCache.Length < dataLength)
                            displValuesCache = new float[dataLength];
                        targets[0].GetValue().GetSnapshotData<float>(displValuesCache);

                        // calc height bounds
                        request.Result.DisplacementMin = displValuesCache[0];
                        request.Result.DisplacementMax = displValuesCache[0];
                        for (int i = 1; i < dataLength; i++)
                        {
                            request.Result.DisplacementMin = Math.Min(request.Result.DisplacementMin, displValuesCache[i]);
                            request.Result.DisplacementMax = Math.Max(request.Result.DisplacementMax, displValuesCache[i]);
                        }

                        request.CompletedSteps |= TerrainTileBakingStep.DisplacementReadback;
                        snapshotReady.Dispose(); // delete before it gets triggered again with an invalid (released) render target.
                    });
                }; // end displacement baker ready callback
            }
        }

        public void DeleteTileData(TiledRect3 area)
        {
            if (OnTileDataDelete != null)
                OnTileDataDelete(area);
        }

#region Functions to be externally provided

        public Action<TerrainTileBakingArgs, OnTileDataInitializedCallback> InitializeTileData { get; set; }

        public Func<TerrainTileBakingArgs, CompMaterial> CreateTexBakingMaterial { get; set; }
        
        public Func<TerrainTileBakingArgs, CompMaterial> CreateDisplaceBakingMaterial { get; set; }

        public Action<TiledRect3> OnTileDataDelete { get; set; }

        /// <summary>
        /// Return true when the data source is ready to render an area. When this call returns false, the render is delayed.
        /// </summary>
        public Func<TiledRect3, bool> CanRenderArea { get; set; }

#endregion
    }

    internal class TerrainTileBakingRequest
    {
        public TerrainTileData Result;
        public TerrainTileBakingArgs Args;
        public TerrainTileBakingStep CompletedSteps;
        public List<CompBakerScreenSpace> Bakers;
        public int StartedFrame;
    }
    public class TerrainTileBakingArgs
    {
        public Component ResultsParent;
        public Component BakingParent;
        public TiledRect3 Area;
        public CompTerrainCurvature Curvature;

        public object CustomData;
    }

    internal enum TerrainTileBakingStep
    {
        None = 0,
        NormalMapReady = 1 << 1,
        AlbedoMapReady = 1 << 2,
        DisplacementReady = 1 << 3,
        DisplacementReadback = 1 << 4,
        AllSteps = NormalMapReady | AlbedoMapReady | DisplacementReady | DisplacementReadback
    }
}
