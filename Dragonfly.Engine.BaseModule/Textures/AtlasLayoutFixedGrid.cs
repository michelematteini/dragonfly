
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// An atlas layout made by a simple grid, where cells have a fixed size
    /// </summary>
    public class AtlasLayoutFixedGrid : AtlasLayout
    {
        private readonly Int2 subTextureResolution;
        private readonly Int2 gridSize;
        private GridCell[,] grid;
        private readonly Float2 cellUVSize;

        private struct GridCell
        {
            public AARect TexArea;
            public bool Allocated;
        }

        public AtlasLayoutFixedGrid(Int2 subTextureResolution, Int2 gridSize)
        {
            this.subTextureResolution = subTextureResolution;
            this.gridSize = gridSize;
            grid = new GridCell[gridSize.X, gridSize.Y];
            cellUVSize = 1.0f / (Float2)gridSize;
            for (int x = 0; x < gridSize.Width; x++)
            {
                for (int y = 0; y < gridSize.Height; y++)
                {
                    grid[x, y].Allocated = false;
                    grid[x, y].TexArea = new AARect(x * cellUVSize.X, y * cellUVSize.Y, cellUVSize.X, cellUVSize.Y);
                }
            }
        }

        public override Int2 Resolution => subTextureResolution * gridSize;

        public override bool TryAllocateSubTexture(Int2 preferredSize, out SubTextureReference subTexture)
        {
#if DEBUG
            if (preferredSize.X > subTextureResolution.X || preferredSize.Y > subTextureResolution.Y)
                throw new ArgumentException("Requested texture is larger than the cell size!");
#endif
            // find a free cell
            for (int x = 0; x < gridSize.Width; x++)
            {
                for (int y = 0; y < gridSize.Height; y++)
                {
                    if (!grid[x, y].Allocated)
                    {
                        // free cell found
                        subTexture = new SubTextureReference(grid[x, y].TexArea, this);
                        return true;
                    }
                }
            }

            // no slot is available
            subTexture = new SubTextureReference(new AARect(), this);
            return false;
        }

        public override void ReleaseSubTexture(SubTextureReference subTexture)
        {
            Int2 cellCoords = (Int2)(subTexture.Area.Min / cellUVSize);
            grid[cellCoords.X, cellCoords.Y].Allocated = false;
        }


    }
}
