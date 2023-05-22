using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;
 
namespace Dragonfly.Terrain
{
    /// <summary>
    /// A LOD quad-tree based drawable terrain
    /// </summary>
    public class CompTerrain : Component
    {
        private bool wireframeMode;
        private CompTerrainLODUpdater updater;
        private UpdateTileVisibilityTask tileVisibilityTask;

        /// <summary>
        /// Create a new QuadTree based terrain.
        /// </summary>
        /// <param name="area">The area covered by the terrain, that is located on the plane of minimum terrain height</param>
        public CompTerrain(Component parent, TerrainParams terrainParams) : base(parent)
        {
            Area = terrainParams.Area;
            Tessellator = new CompTerrainTessellator(this, terrainParams.DataSource.TileTessellation);
            updater = terrainParams.LodUpdater;
            DataSource = terrainParams.DataSource;
            MaterialFactory = terrainParams.MaterialFactory;
            Curvature = new CompTerrainCurvature(this, terrainParams.Area, !terrainParams.CurvatureEnabled,
                terrainParams.CurvatureRadius, terrainParams.ExplicitCurvatureCenter, terrainParams.CurvatureCenter);
            Tiles = new TerrainQuadTree(this);
            IsProcessingNewLOD = true; // the root is immediately created by the quadtree, and its tile is already processing...
            IsLodIncomplete = true; // first update do not evaluate tessellation, and is thus set to "incomplete" to avoid stopping updates immediately
            IsAnyLODAvailable = false;
            BaseMod baseMod = Context.GetModule<BaseMod>();
            tileVisibilityTask = new UpdateTileVisibilityTask(this, GetComponent<CompTaskScheduler>(), () => baseMod.MainPass.Camera.Position);
            AdjacentTerrains = new List<CompTerrain>();
        }

        internal TerrainQuadTree Tiles;

        public TiledRect3 Area { get; private set; }

        internal CompTerrainTessellator Tessellator { get; private set; }

        public ITerrainDataSource DataSource { get; private set; }

        public ITerrainMaterialFactory MaterialFactory { get; private set; }

        public CompTerrainCurvature Curvature { get; private set; }

        /// <summary>
        /// Terrains which have their quadtree connected with this component.
        /// Used internally to track edge updates caused by other near terrains.
        /// </summary>
        internal List<CompTerrain> AdjacentTerrains { get; private set; }

        #region LOD

        /// <summary>
        /// True while a new LOD tree is loading and tree updates have been suspended
        /// </summary>
        internal bool IsProcessingNewLOD { get; private set; }

        /// <summary>
        /// True if the last computed LOD does not reach the required tessellation (and another LOD must be immediately computed when the current one is ready)
        /// </summary>
        internal bool IsLodIncomplete { get; private set; }

        /// <summary>
        /// True if a valid LOD for this terrain is available.
        /// </summary>
        public bool IsAnyLODAvailable { get; private set; }

        internal void BeginUpdatingLOD()
        {
            if (IsProcessingNewLOD)
                throw new InvalidOperationException("This terrain is still processing the previous LOD!");
            
            // start processing a new LOD
            UpdateTessellation();
            PrepareTileEdges();
            IsProcessingNewLOD = true;
        }
        
        internal bool IsNewLODReady()
        {
            if (DataSource.IsLoading)
                return false;

            if (Tessellator.LoadingRequired)
                return false;

            foreach (IQuadTreeNode<CompTerrainTile> tileNode in Tiles.Tree.Leaves)
            {
                if (tileNode.Value.NeededUpdates != UpdateType.None)
                    return false;
                if (!tileNode.Value.Drawable.Ready)
                    return false;
                if (tileNode.Value.Drawable.MainMaterial.NeededUpdates != UpdateType.None)
                    return false;
            }

            return true;
        }

        internal void ApplyNewLOD()
        {
            if (!IsProcessingNewLOD)
                throw new InvalidOperationException("There is no LOD to be applied! Call BeginUpdatingLOD() first!");

            // switch to new LOD
            Tiles.ResumeEvents();
            UpdateTileEdges();
            TilesOnLODChanged();
            Tiles.Tree.RemoveUnusedNodes(); // force deletion of unused higher-detail tiles from previous LOD

            IsProcessingNewLOD = false;
            IsAnyLODAvailable = true;
        }
   
        private void FlagLeavesVisibilityInPreviousLOD()
        {
            // keep track of previous leaves (i.e. visible tiles)
            foreach (IQuadTreeNode<CompTerrainTile> tileNode in Tiles.Tree.TopDown)
                tileNode.Value.WasVisibleInPreviousLOD = tileNode.IsLeaf;
        }

        /// <summary>
        /// Immediately updates the tile edges. Animations are performed where necessary.
        /// </summary>
        internal void UpdateEdgeTessellation()
        {
            FlagLeavesVisibilityInPreviousLOD();
            PrepareTileEdges();
            UpdateTileEdges();
            TilesOnLODChanged();
        }

        internal Range<float> CalcCurrentTessellationRatioRange()
        {
            IVolume cameraVolume = Context.GetModule<BaseMod>().MainPass.Camera.Volume;
            Range<float> tessellationRatioRange = new Range<float>(float.MaxValue, float.MinValue);

            // iterate the quadtree and calculate leves tessellation ratios
            foreach (IQuadTreeNode<CompTerrainTile> tileNode in Tiles.Tree.Leaves)
            {
                float tessRatio = GetRequiredTessRatio(tileNode);
                tessellationRatioRange.From = Math.Min(tessellationRatioRange.From, tessRatio);
                tessellationRatioRange.To = Math.Max(tessellationRatioRange.To, tessRatio);
            }

            return tessellationRatioRange;
        }

        #endregion // LOD
        internal void UpdateTilesVisibility()
        {
            tileVisibilityTask.UpdateTilesVisibility();
        }

        private void UpdateTessellation()
        {
            IsLodIncomplete = false;
            IVolume cameraVolume = Context.GetModule<BaseMod>().MainPass.Camera.Volume;

            FlagLeavesVisibilityInPreviousLOD();

            // block events and start to updated the quadtree
            Tiles.SuspendEvents();

            // iterate the quadtree perform all updates that do not require additional bakings
            SortedQueue<float, IQuadTreeNode<CompTerrainTile>> tileToBeDivided = new SortedQueue<float, IQuadTreeNode<CompTerrainTile>>();
            foreach (IQuadTreeNode<CompTerrainTile> tileNode in Tiles.Tree.LayeredTopDown)
            {
                float tessRatio = GetRequiredTessRatio(tileNode);

                if (tessRatio >= 1.0f)
                {
                    // required tessellation reached
                    if (!tileNode.IsLeaf && !updater.ShouldDelayLodUp(tileNode.Value))
                    {
                        tileNode.Group();
                    }
                }
                else
                {
                    // more vertices are required...

                    if (!tileNode.IsLeaf)
                        // divide now since the sub tiles are already available
                        tileNode.Divide();
                    else
                    {
                        // if the current tess is less than a quarter of the required, a single division will not be enough
                        IsLodIncomplete |= (tessRatio < 0.25f);

                        // assign a lower priority to division of tiles that are currently not visible on screen
                        if (!cameraVolume.Intersects(tileNode.Value.BoundingBox))
                        {
                            tessRatio += 1.0f;
                        }

                        // save this tile and evaluate its division later
                        tileToBeDivided.Enqueue(tileNode, tessRatio);
                    }
                }
            }

            // perform the tessellating division with highest priority first, until all are done or a limit per update is reached
            int divisionCount = 0;
            while (tileToBeDivided.Count > 0 && (divisionCount < updater.Strategy.MaxDivisionsPerUpdate || !IsAnyLODAvailable))
            {
                IQuadTreeNode<CompTerrainTile> tileNode = tileToBeDivided.Dequeue();

                // divide
                tileNode.Divide();
                divisionCount++;
            }

            IsLodIncomplete |= tileToBeDivided.Count > 0; // the lod is also incomplete if not all divisions have been carried out
        }

        /// <summary>
        /// Gived a terrain tile, returns a ratio between the current tile quad density, and the density required by the LOD strategy.
        /// A ratio less than one, means that the current tile should be more tessellated.
        /// </summary>
        private float GetRequiredTessRatio(IQuadTreeNode<CompTerrainTile> tileNode)
        {
            CompTerrainTile tile = tileNode.Value;

            // calc the tessellation level of this tile and the one required in vertices per unit
            float quadsPerTile = DataSource.TileTessellation * DataSource.TileTessellation;
            float tileTess = quadsPerTile / (tile.Area.Size.X * tile.Area.Size.Y);
            float requiredTess = updater.Strategy.GetRequiredVertexDesityFor(tile.BoundingBox, Curvature.CalcLocalInfoAtTilePos(tile.Area.Center).Normal, tile.MinDisplacementHeight, tile.MaxDisplacementHeight);

            return tileTess / requiredTess;
        }

        private TerrainEdgeTessellation CalcTileEdges(IQuadTreeNode<CompTerrainTile> tileNode)
        {
            TerrainEdgeTessellation edge;
            edge.BottomDivisor = (tileNode.Depth - (tileNode.Bottom == null ? tileNode.Depth : tileNode.Bottom.Depth)).Exp2();
            edge.LeftDivisor = (tileNode.Depth - (tileNode.Left == null ? tileNode.Depth : tileNode.Left.Depth)).Exp2();
            edge.RightDivisor = (tileNode.Depth - (tileNode.Right == null ? tileNode.Depth : tileNode.Right.Depth)).Exp2();
            edge.TopDivisor = (tileNode.Depth - (tileNode.Top == null ? tileNode.Depth : tileNode.Top.Depth)).Exp2();
            return edge;
        }

        private void PrepareTileEdges()
        {
            foreach (IQuadTreeNode<CompTerrainTile> tileNode in Tiles.Tree.Leaves)
                Tessellator.RequestEdgeTessellation(CalcTileEdges(tileNode));
        }

        private void UpdateTileEdges()
        {
            foreach (IQuadTreeNode<CompTerrainTile> tileNode in Tiles.Tree.Leaves)
                tileNode.Value.EdgeTessellation = CalcTileEdges(tileNode);
        }

        private void TilesOnLODChanged()
        {
            foreach (IQuadTreeNode<CompTerrainTile> tileNode in Tiles.Tree.Leaves)
                tileNode.Value.OnLODChanged();
        }

        public bool WireframeModeEnabled
        {
            get { return wireframeMode; }
            set
            {
                wireframeMode = value;
                foreach (IQuadTreeNode<CompTerrainTile> tile in Tiles.Tree.TopDown)
                    tile.Value.OnTerrainUpdated();
            }

        }

        private class UpdateTileVisibilityTask
        {
            private const int SplitTaskOverNFrames = 6;
            private CompTaskScheduler.ITask tileVisibilityTask;
            private int tileVisibilityLastLeafIndex;
            private CompTerrain terrain;
            private Func<TiledFloat3> getViewPosition;
            private List<CompTerrainTile> pendingVisibilityUpdates;

            public UpdateTileVisibilityTask(CompTerrain terrain, CompTaskScheduler scheduler, Func<TiledFloat3> viewPosition)
            {
                this.terrain = terrain;
                this.getViewPosition = viewPosition;
                tileVisibilityTask = scheduler.CreateTask("TerrainTilesVisibility", UpdateTilesVisibilityTask);
                pendingVisibilityUpdates = new List<CompTerrainTile>();
            }

            public void UpdateTilesVisibility()
            {
                if (tileVisibilityTask.State == CompTaskScheduler.TaskState.Completed)
                {
                    // update tiles visibility using task calculated occlusion
                    foreach (CompTerrainTile tile in pendingVisibilityUpdates)
                        tile.UpdateVisibility();

                    pendingVisibilityUpdates.Clear();
                    tileVisibilityTask.Reset();
                }
                tileVisibilityTask.QueueExecution();
            }

            private void UpdateTilesVisibilityTask()
            {
                // only update LeftCount / SplitTaskOverNFrames for each task instance...
                int tileLastIndex = tileVisibilityLastLeafIndex + terrain.Tiles.Tree.LeafCount / SplitTaskOverNFrames + 1;
                int curTileIndex = 0;

                // iterate over the tiles to be updated
                TiledFloat3 curViewPosition = getViewPosition();
                CompTerrainCurvature.LocalInfo camCurvature = terrain.Curvature.CalcLocalInfoAtWorldPos(curViewPosition);
                foreach (IQuadTreeNode<CompTerrainTile> tileNode in terrain.Tiles.Tree.Leaves)
                {
                    // update occlusion if in the update range
                    if (curTileIndex >= tileVisibilityLastLeafIndex)
                    {
                        bool occlusionChanged;
                        tileNode.Value.CacheOcclusion(curViewPosition, camCurvature, out occlusionChanged);

                        if (occlusionChanged) // save tiles for which the occlusion changed
                            pendingVisibilityUpdates.Add(tileNode.Value);
                    }

                    if (++curTileIndex >= tileLastIndex)
                        break; // exit if outside the update range
                }

                // update the range to be checked in the next task instance
                tileVisibilityLastLeafIndex = curTileIndex >= terrain.Tiles.Tree.LeafCount ? 0 : curTileIndex;
            }

        }
    }

    /// <summary>
    /// Initializer struct storing a CompTerrain config.
    /// </summary>
    public struct TerrainParams
    {
        public TiledRect3 Area;
        public CompTerrainLODUpdater LodUpdater;
        public ITerrainDataSource DataSource;
        public ITerrainMaterialFactory MaterialFactory;
        public bool CurvatureEnabled;
        public float CurvatureRadius;
        internal TiledFloat3 CurvatureCenter;
        internal bool ExplicitCurvatureCenter;
    }
}
