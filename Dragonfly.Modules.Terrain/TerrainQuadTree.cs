using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System.Collections.Generic;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// The quad-tree specialization for the terrain, that dynamically allocate, enable / disable terrain tiles.
    /// Tile locations are Left = -XSideDir, Right = +XSideDir, Top = -YSideDir, Bottom = +YSideDir
    /// </summary>
    internal class TerrainQuadTree : IQuadTreeManager<CompTerrainTile>
    {
        private CompTerrain terrain;
        private List<QuadTreeNodeEventArgs<CompTerrainTile>> pendingEvents;
        private bool suspended;

        public TerrainQuadTree(CompTerrain parentTerrain)
        {
            terrain = parentTerrain;
            SuspendEvents(); // starts suspended, to delay the propagation of the Enabled event to the root
            pendingEvents = new List<QuadTreeNodeEventArgs<CompTerrainTile>>();
            Tree = new QuadTree<CompTerrainTile>(this);
        }

        public QuadTree<CompTerrainTile> Tree { get; private set; }

        /// <summary>
        /// Stop event propagation from the terrain quadtree to the tiles, that are kept static even if the tree layout changes.
        /// </summary>
        public void SuspendEvents()
        {
            suspended = true;
        }

        /// <summary>
        /// Resume updates, suspended by SuspendEvents(), and perform all the pending udpates.
        /// </summary>
        public void ResumeEvents()
        {
            if (!suspended)
                return; // not suspended

            suspended = false;

            // perform all pending events
            foreach (QuadTreeNodeEventArgs<CompTerrainTile> eventArgs in pendingEvents)
                OnNodeEvent(eventArgs);

            pendingEvents.Clear();
        }

        public CompTerrainTile CreateRoot()
        {
            CompTerrainTile tile = new CompTerrainTile(terrain, null, terrain.Area);
            return tile;
        }

        public CompTerrainTile CreateBottomLeft(CompTerrainTile parent)
        {
            return CreateSubTile(parent, Float2.UnitY);
        }

        public CompTerrainTile CreateBottomRight(CompTerrainTile parent)
        {
            return CreateSubTile(parent, Float2.One);
        }

        public CompTerrainTile CreateTopLeft(CompTerrainTile parent)
        {
            return CreateSubTile(parent, Float2.Zero);
        }

        public CompTerrainTile CreateTopRight(CompTerrainTile parent)
        {
            return CreateSubTile(parent, Float2.UnitX);
        }

        CompTerrainTile CreateSubTile(CompTerrainTile parent, Float2 startOffsetPercent)
        {
            // calc base sub area
            TiledRect3 subArea = parent.Area;
            subArea.Size *= 0.5f;
            subArea.Position += startOffsetPercent.X * subArea.XSideDir * subArea.Size.X + startOffsetPercent.Y * subArea.YSideDir * subArea.Size.Y;

            return new CompTerrainTile(terrain, parent, subArea) { OffsetToParentPercent = 0.5f * startOffsetPercent };
        }

        public void OnNodeEvent(QuadTreeNodeEventArgs<CompTerrainTile> args)
        {
            if(suspended)
            {
                pendingEvents.Add(args);
                return;
            }

            args.NodeValue.LastNodeEvent = args.Type;

            if (args.Type == QuadTreeNodeEvent.Deleted)
            {
                terrain.DataSource.DeleteTileData(args.NodeValue.Area);
                args.NodeValue.Dispose();
            }
        }
    }
}
