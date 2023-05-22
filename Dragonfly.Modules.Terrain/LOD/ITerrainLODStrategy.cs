using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// Define the LOD distribution over the terrain.
    /// </summary>
    public interface ITerrainLODStrategy
    {
        /// <summary>
        /// Returns true when the terrain LOD should be updated.
        /// </summary>
        bool NeedsToBeUpdated(CompTerrain terrain);

        /// <summary>
        /// Notify this strategy that the terrain LOD has just been updated
        /// </summary>
        void SignalUpdateCompletion(CompTerrain terrain);

        /// <summary>
        /// Returns the quads per squared meter ideally required by this LOD strategy for the specific tile
        /// </summary>
        /// <param name="bb">The bounding box of the tile.</param>
        /// <param name="surfaceNormal">The base normal of the .</param>
        /// <param name="minHeight">The base normal of the .</param>
        /// <param name="maxHeight">The base normal of the .</param>
        /// <returns></returns>
        float GetRequiredVertexDesityFor(AABox bb, Float3 surfaceNormal, float minHeight, float maxHeight);

        /// <summary>
        /// Returns the maximum number of division that are allowed per LOD update. 
        /// <para/> Lower values allow the terrain to respond quickly to position changes but mutltiple LOD updated may be required for the LOD con become stable.
        /// </summary>
        int MaxDivisionsPerUpdate { get; }
    }
}
