using System;
using System.Collections.Generic;
using Dragonfly.BaseModule;
using Dragonfly.Graphics.Math;

namespace Dragonfly.Engine.Procedural
{
    public class ProceduralTreeDescr : ProceduralMeshDescription
    {
        #region Global tree parameters

        /// <summary>
        /// Material used for the tree trunk
        /// </summary>
        public MaterialDescription TrunkMaterial;
        /// <summary>
        /// The maximum trunk radius at the base of the tree.
        /// </summary>
        public float TrunkRadius;
        /// <summary>
        /// Determine how long a branch can grow before thinning out to 0 radius.
        /// </summary>
        public float TreeMaxHeight;
        /// <summary>
        /// The 3d location from which the tree trunk start.
        /// </summary>
        public Float3 TreeStartPosition;
        /// <summary>
        /// Higher values makes the tree more likely to grow crooked from the start. 
        /// </summary>
        public float TrunkMaxAngleRadians;
        /// <summary>
        /// Vertical height that is considered ground level. The tree won't grow below this level. 
        /// </summary>
        public float GroundLevel;
        /// <summary>
        /// Multiplier for the vertical tiling of the tree texture coords. 
        /// </summary>
        public float TrunkTexCoordMul;

        #endregion

        #region Branch Parameters

        /// <summary>
        /// Determine how fast branches and trunk thin in radius.
        /// </summary>
        public float BranchThinningRate;
        /// <summary>
        /// The minimum tesselation distance for a branch along its length.
        /// </summary>
        public float BranchMinTessellationDistance;
        /// <summary>
        /// Minimum angle between the branches and the trunk. 
        /// </summary>
        public float BranchMinAngleRadians;
        /// <summary>
        /// Maximum angle between the branches and the trunk. 
        /// </summary>
        public float BranchMaxAngleRadians;
        /// <summary>
        /// The branch radius after which its length stops.
        /// </summary>
        public float BranchMinRadius;
        /// <summary>
        /// The vertical distance between direction changes on a branch.
        /// </summary>
        public float BranchAvgFeatureDistance;
        /// <summary>
        /// The percent of feature distance over which direction changes are smoothed.
        /// </summary>
        public float BranchSmoothingPercent;
        /// <summary>
        /// How much a branch can change its direction along its path.
        /// </summary>
        public float BranchMaxDirectionVariance;
        /// <summary>
        /// Average distance of two branches starting from the same trunk.
        /// </summary>
        public float BranchAvgDistance;
        /// <summary>
        /// When a new branch is spawn, the new branch radius will not be smaller than the original branch radius times this value.
        /// </summary>
        public float BranchMinRadiusPercent;
        /// <summary>
        /// When a new branch is spawn, its discarded if its length would be smaller that a percentage of the three height indicated by this value.
        /// </summary>
        public float BranchMinLenPercent;
        /// <summary>
        /// How fast branches bends towards the sun (up).
        /// </summary>
        public float SunSearchRate;
        /// <summary>
        /// Higher values increases the number of branches starting from another while the latter gets thinner.
        /// </summary>
        public float BranchDistanceDensityMul;
        /// <summary>
        /// The number of parents a branch can have.
        /// </summary>
        public float BranchMaxNestingLevel;

        #endregion

        #region Foliage Parameters

        /// <summary>
        /// List of generative layers for this tree foliage.
        /// </summary>
        public List<ProceduralTreeFoliageParams> FoliageLayers;
        /// <summary>
        /// Material used for the foliage
        /// </summary>
        public MaterialDescription FoliageMaterial;
        /// <summary>
        /// Material used for the foliage
        /// </summary>
        public List<Float3> FoliageAlbedoVariations;

        #endregion

        public ProceduralTreeDescr()
        {
            TrunkRadius = 1.0f;
            BranchThinningRate = 0.25f;
            TreeMaxHeight = 15.0f;
            TreeStartPosition = Float3.Zero;
            TrunkMaxAngleRadians = 0.5f;
            BranchMinAngleRadians = 0.3f;
            BranchMaxAngleRadians = 1.2f;
            BranchMinRadius = 0.015f;
            BranchAvgFeatureDistance = 1.5f;
            BranchSmoothingPercent = 0.5f;
            BranchMaxDirectionVariance = 0.5f;
            BranchAvgDistance = 4.0f;
            SunSearchRate = 0.3f;
            BranchDistanceDensityMul = 0.8f;
            BranchMaxNestingLevel = 8;
            GroundLevel = 0.02f;
            TrunkTexCoordMul = 1.0f;
            BranchMinRadiusPercent = 0.5f;
            TrunkMaterial = MaterialDescription.Default;
            FoliageMaterial = MaterialDescription.Default;
            BranchMinTessellationDistance = 0.1f;
            BranchMinLenPercent = 0.005f;
        }

        public void AddFoliage(ProceduralTreeFoliageParams foliageInstance)
        {
            if (FoliageLayers == null)
                FoliageLayers = new List<ProceduralTreeFoliageParams>();
            FoliageLayers.Add(foliageInstance);
        }
    }


    public struct ProceduralTreeFoliageParams
    {
        /// <summary>
        /// Define the geometry type and the positioning of this foliage instance
        /// </summary>
        public ProceduralFoliageType Type;
        /// <summary>
        /// The coordinates of the texture are used by this foliage instance.
        /// </summary>
        public Rect FoliageTexCoords;
        /// <summary>
        /// The sizes of the mesh used for the foliage.
        /// </summary>
        public Float2 Sizes;
        /// <summary>
        /// Texture coords of the location that should be positioned on the branch.
        /// </summary>
        public Float2 FoliageStartCoords;
        /// <summary>
        /// The number of vertex per meter used in each direction to tesselate the leaf.
        /// </summary>
        public Int2 Tesselation;
        /// <summary>
        /// Specify how much the leaf geometry is bend in each direction in the normal direction.
        /// </summary>
        public Float2 BendingPercent;
        /// <summary>
        /// Affect the total ammount of foliage created for this tree.
        /// </summary>
        public float FoliageDensity;
        /// <summary>
        /// Higher values make branching leaves more likely to spawn near the top then near the bottom of the tree.
        /// </summary>
        public float BranchFoliagePowerDistribution;
        /// <summary>
        /// Random foliage offset percent along its normal. A value of 0 disable this feature.
        /// </summary>
        public float Perturbation;
        /// <summary>
        /// Minimum angle between the branches and the foliage. 
        /// </summary>
        public float FoliageMinAngleRadians;
        /// <summary>
        /// Maximum angle between the branches and the foliage. 
        /// </summary>
        public float FoliageMaxAngleRadians;
        /// <summary>
        /// Percent of the leaf size that is variated randomly. 
        /// </summary>
        public float SizeVariationPercent;
        /// <summary>
        /// Percent of the tree height at which this foliage istance starts. 
        /// </summary>
        public float HeightStartPercent;


        public ProceduralTreeFoliageParams(Rect texCoordRegion, Float2 sizes, Float2 startCoords)
        {
            Type = ProceduralFoliageType.Terminal;
            FoliageStartCoords = startCoords;
            Sizes = sizes;
            FoliageTexCoords = texCoordRegion;
            Tesselation = new Int2(4, 4);
            BendingPercent = -Float2.One * 0.1f;
            FoliageDensity = 1.0f;
            Perturbation = 0.08f * Math.Min(sizes.X, sizes.Y);
            BranchFoliagePowerDistribution = 0.5f;
            FoliageMinAngleRadians = 0.2f;
            FoliageMaxAngleRadians = 0.9f;
            SizeVariationPercent = 0.1f;
            HeightStartPercent = 0.5f;
        }
    }

    [Flags]
    public enum ProceduralFoliageType
    {
        Terminal = 1 << 0,
        Branching = 1 << 1,
    }


}
