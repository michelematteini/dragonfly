using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// Coordinates LOD updates for all the available terrains in the current scene.
    /// </summary>
    public class CompTerrainLODUpdater : Component, ICompUpdatable
    {
        private PreciseFloat lastLodUpdateTime; // real time at which the last lod update took place
        private CompTerrain curTerrain;
        private Dictionary<int, int> delayedTileLodups; // tiled id -> number of frame that it grouping has been delayed

        public CompTerrainLODUpdater(Component parent, ITerrainLODStrategy strategy) : base(parent)
        {
            Strategy = strategy;
            lastLodUpdateTime = PreciseFloat.Zero;
            LodUpDelay = 2;
            delayedTileLodups = new Dictionary<int, int>();
        }

        /// <summary>
        /// All terrain LODs will not be updated while this value is set to true.
        /// </summary>
        public bool FreezeLOD { get; set; }

        /// <summary>
        /// How many updates should be performed before an over-detailed terrain tile is decreased in LOD.
        /// Higher values avoid LOD flickering and sudden detail loss, but make the terrain LOD less responsive.
        /// </summary>
        public int LodUpDelay { get; set; }

        internal bool ShouldDelayLodUp(CompTerrainTile tile)
        {
            // required tessellation reached
            int delayedUpdates, tileID = tile.ID;
            if (!delayedTileLodups.TryGetValue(tileID, out delayedUpdates))
                delayedUpdates = 0;
            
            if (delayedUpdates < LodUpDelay)
            {
                // don't discard for now, wait next updates to avoid LOD flickering
                delayedTileLodups[tileID] = delayedUpdates + 1;
                return true;
            }
            else
            {
                // allow lod-up
                delayedTileLodups.Remove(tileID);
                return false;
            }
        }

        public UpdateType NeededUpdates => UpdateType.FrameStart2;

        public ITerrainLODStrategy Strategy { get; private set; }

        private CompTerrain GetNextTerrainForLODUpdates(IReadOnlyList<CompTerrain> terrainList)
        {
            CompTerrain toBeUpdated = null;
            float maxTessRatio = 1.0f;

            // search for the next terrain to be updated, picking the one with the maximum discrepancy between the needed and current tessellation
            for (int i = 0; i < terrainList.Count; i++)
            {
                if (!Strategy.NeedsToBeUpdated(terrainList[i]))
                    continue;
                Range<float> tessRatioRange = terrainList[i].CalcCurrentTessellationRatioRange();
                float absTessRatio = Math.Max(tessRatioRange.From < 1.0f ? 1.0f / tessRatioRange.From : tessRatioRange.From, tessRatioRange.To < 1.0f ? 1.0f / tessRatioRange.To : tessRatioRange.To);
                if (absTessRatio > maxTessRatio)
                {
                    maxTessRatio = absTessRatio;
                    toBeUpdated = terrainList[i];
                }
            }

            return toBeUpdated;
        }

        private bool IsLodTransitionTimeElapsed(CompTerrain terrain)
        {
            if (lastLodUpdateTime == PreciseFloat.Zero || !terrain.IsAnyLODAvailable)
                return true;

            return (Context.Time.RealSecondsFromStart - lastLodUpdateTime) > terrain.DataSource.MinLodSwitchTimeSeconds;
        }

        public void Update(UpdateType updateType)
        {
            IReadOnlyList<CompTerrain> terrainList = GetComponents<CompTerrain>();
            if (terrainList.Count == 0)
                return;

            // LOD
            {
                // give priority to terrains that are still not in a drawable state, process all of them in parallel
                bool newTerrainLoading = false;
                for (int i = 0; i < terrainList.Count; i++)
                {
                    if (!terrainList[i].IsAnyLODAvailable && terrainList[i].IsProcessingNewLOD)
                    {
                        newTerrainLoading = true;
                        if (terrainList[i].IsNewLODReady())
                            terrainList[i].ApplyNewLOD();
                    }
                }

                if (!newTerrainLoading)
                {
                    if (curTerrain != null)
                    {
                        // check current terrain update state
                        if (curTerrain.IsNewLODReady() && IsLodTransitionTimeElapsed(curTerrain))
                        {
                            // update completed commit tessellation and update adjacent terrain edges
                            if (!curTerrain.IsLodIncomplete && curTerrain.IsAnyLODAvailable)
                                Strategy.SignalUpdateCompletion(curTerrain);
                            curTerrain.ApplyNewLOD();
                            foreach (CompTerrain adjTerrain in curTerrain.AdjacentTerrains)
                                adjTerrain.UpdateEdgeTessellation();
                            lastLodUpdateTime = Context.Time.RealSecondsFromStart;
                            curTerrain = null;
                        }
                    }

                    if (!FreezeLOD)
                    {
                        // search for the next terrain to be updated
                        if (curTerrain == null)
                        {
                            curTerrain = GetNextTerrainForLODUpdates(terrainList);

                            // start updating new  LOD ( round-robin )
                            if (curTerrain != null && !curTerrain.IsProcessingNewLOD/* skip begin for the first update that is already running*/)
                                curTerrain.BeginUpdatingLOD();
                        }
                    }
                }
            }

            // Tiles visibility ( internally terrains already distribute the task asynchronosly )
            for (int i = 0; i < terrainList.Count; i++)
            {
                if (terrainList[i].IsAnyLODAvailable)
                    terrainList[i].UpdateTilesVisibility();
            }
        }
    }
}
