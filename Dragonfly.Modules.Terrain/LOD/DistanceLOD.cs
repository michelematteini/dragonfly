using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// A level of detail measure based on the 3d distance from the viewer. 
    /// </summary>
    public class DistanceLOD : ITerrainLODStrategy
    {
        public interface Modifier
        {
            float GetDensityModifierFor(AABox bb, Float3 surfaceNormal, float minHeight, float maxHeight);
        }

        private Component<TiledFloat3> position;
        private Dictionary<int, TiledFloat3> lastUpdatePositions;

        public DistanceLOD(Component<TiledFloat3> worldPosition)
        {
            this.position = worldPosition;
            lastUpdatePositions = new Dictionary<int, TiledFloat3>();
            UpdateDistanceMeters = 10.0f;
            OneMeterVertexDensity = 512.0f;
            MaxVertexDensity = 64.0f;
            MaxDivisionsPerUpdate = 5;
            DensityModifiers = new List<Modifier>();
        }

        public bool NeedsToBeUpdated(CompTerrain terrain)
        {
            TiledFloat3 lastUpdatePos;
            if (!lastUpdatePositions.TryGetValue(terrain.ID, out lastUpdatePos))
                return true;
            if ((position.GetValue() - lastUpdatePos).ToFloat3().Length > UpdateDistanceMeters)
            {
                lastUpdatePositions.Remove(terrain.ID); // invalidate when moved
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// The distance after which the LOD should be updated
        /// </summary>
        public float UpdateDistanceMeters { get; set; }

        /// <summary>
        /// The number of vertices used to tesselate a 1x1 meter surface at 1 meter distance.
        /// </summary>
        public float OneMeterVertexDensity { get; set; }

        /// <summary>
        /// Maximum number for vertices for squared meter.
        /// </summary>
        public float MaxVertexDensity { get; set; }

        public int MaxDivisionsPerUpdate { get; set; }

        public void SignalUpdateCompletion(CompTerrain terrain)
        {
            lastUpdatePositions[terrain.ID] = position.GetValue();
        }

        public List<Modifier> DensityModifiers { get; private set; }

        public float GetRequiredVertexDesityFor(AABox bb, Float3 surfaceNormal, float minHeight, float maxHeight)
        {
            // calc distance-base vertex density
            Float3 localPosition = position.GetValue().Value;
            float distance = bb.DistanceFrom(localPosition);
            float vertDensity = Math.Min(MaxVertexDensity, OneMeterVertexDensity / (distance * distance));

            // apply modifiers
            foreach (Modifier m in DensityModifiers)
            {
                vertDensity *= m.GetDensityModifierFor(bb, surfaceNormal, minHeight, maxHeight);
            }

            return vertDensity;
        }
    }

    /// <summary>
    /// A Distance LOD modifier that increase vertex density for tiles with high height drops.
    /// </summary>
    public class DistanceLODHeightModifier : DistanceLOD.Modifier
    {
        public DistanceLODHeightModifier()
        {
            HeightDensityMultiplier = 0.005f;
            MaxHeightDensityMultiplier = 8.0f;
        }

        /// <summary>
        /// Defines how much the terrain slope affects vertex density.
        /// </summary>
        public float HeightDensityMultiplier { get; set; }

        /// <summary>
        /// Defines an upper bound on how much the slope can affect vertex density.
        /// </summary>
        public float MaxHeightDensityMultiplier { get; set; }


        public float GetDensityModifierFor(AABox bb, Float3 surfaceNormal, float minHeight, float maxHeight)
        {
            float height = maxHeight - minHeight;
            return 1.0f + System.Math.Min(height * HeightDensityMultiplier, MaxHeightDensityMultiplier);
        }
    }

}
