// uncomment to draw the terrain tile bounding box in wireframe
//#define DrawTileAABB

using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// A single chunk of terrain, dynamically created / rendered depending on the LOD.
    /// </summary>
    public class CompTerrainTile : Component, ICompUpdatable
    {
        private CompTerrainTileGeom geom;
        private CompTransformStack tileWorldTransform;
        private QuadTreeNodeEvent lastNodeEvent;
        private BaseMod baseMod;
        private bool isOccluded;

#if DrawTileAABB
        private CompMesh aabbMesh;
#endif

        public CompTerrainTile(CompTerrain parentTerrain, CompTerrainTile parentTile, TiledRect3 area) : base(parentTerrain)
        {
            Area = area;
            ParentTerrain = parentTerrain;
            ParentTile = parentTile;
            geom = new CompTerrainTileGeom(this, parentTerrain.Tessellator);
            EdgeTessellation = geom.EdgeTesselation;
            tileWorldTransform = new CompTransformStack(this);
            baseMod = Context.GetModule<BaseMod>();

            // save a pre-offset that move the tile roughly to the surface (improve precision making the tile correctly working with tiled world coordinates)
            // and truncate to leave some unused bits in the mantissa (prevents precision issues with the in-shader subtraction later)
            CurvatureWorldPreOffset = ParentTerrain.Curvature.CalcLocalInfoAtTilePos(Area.Center).WorldOffset;
            CurvatureWorldPreOffset = new TiledFloat3(CurvatureWorldPreOffset.Value.Trunc(), CurvatureWorldPreOffset.Tile); 
            
            // create terrain tile mesh
            Drawable = new CompMesh(tileWorldTransform, null, geom);
            OnTerrainUpdated();
            LastNodeEvent =  QuadTreeNodeEvent.Disabled; // start invisible, displayed once its node is activated.
            NeededUpdates =  UpdateType.FrameStart1;
        }

        public TiledRect3 Area { get; private set; }

        public CompMesh Drawable { get; private set; }

        public CompTerrainTile ParentTile { get; private set; }

        public CompTerrain ParentTerrain { get; private set; }

        public void OnTerrainUpdated()
        {
            if (Drawable.MainMaterial != null)
                Drawable.MainMaterial.FillMode = ParentTerrain.WireframeModeEnabled ? Graphics.FillMode.Wireframe : Graphics.FillMode.Solid;
        }

        /// <summary>
        /// This terrain tile bounding box in world space
        /// </summary>
        public AABox BoundingBox
        {
            get
            {
                Int3 curTile = Context.GetModule<BaseMod>().CurWorldTile;
                if (NeededUpdates != UpdateType.None && ParentTile != null)
                {
                    // mesh box is still not valid, calculate an approximation from parent
                    AABox parentBox = ParentTile.BoundingBox;
                    Rect3 localArea = Area.ToRect3(curTile);
                    AABox parentBoxSection = AABox.Bounding(
                        localArea.Position + (parentBox.Min - localArea.Position).ProjectTo(localArea.Normal),
                        localArea.EndCorner + (parentBox.Max - localArea.Position).ProjectTo(localArea.Normal)
                    );
                    return parentBoxSection;
                }

                return Drawable.GetBoundingBox() * tileWorldTransform.GetValue().ToFloat4x4(curTile);
            }
        }

        public UpdateType NeededUpdates { get; private set; }

        /// <summary>
        /// The curvature offset pre-applied to the tile to accomodate for the final terrain curvature, reducing the ammount of WPO needed.
        /// </summary>
        public TiledFloat3 CurvatureWorldPreOffset { get; private set; }

        public QuadTreeNodeEvent LastNodeEvent
        {
            get 
            { 
                return lastNodeEvent; 
            } 
            set
            {
                lastNodeEvent = value;
                UpdateVisibility();
            }
        }

        /// <summary>
        /// Check if this tile is occluded by the terrain curvature, and cache occlusion state.
        /// </summary>
        internal void CacheOcclusion(TiledFloat3 camPos, CompTerrainCurvature.LocalInfo curvatureAtCamPos, out bool occlusionChanged)
        {
            bool wasOccluded = isOccluded;
            isOccluded = false;
            if (!ParentTerrain.Curvature.IsFlat)
            {
                // find the tile location closest to the camera
                TiledFloat3 flatCamPos = camPos - curvatureAtCamPos.WorldOffset; // project camera down to a flat terrain space
                TiledFloat3 tileClosest = Area.GetPointClosestTo(flatCamPos);

                // calc its position at maximum elevation
                CompTerrainCurvature.LocalInfo tileCurvature = ParentTerrain.Curvature.CalcLocalInfoAtTilePos(tileClosest);
                TiledFloat3 tileHighest = tileClosest + tileCurvature.WorldOffset + tileCurvature.Normal * MaxDisplacementHeight;

                // check if a ray between the camera and this point intersect the curvature
                TiledFloat3 closestToCenter = ParentTerrain.Curvature.Center.ProjectTo(camPos, tileHighest);
                TiledFloat occludingRadius = ParentTerrain.Curvature.Radius + System.Math.Min(0, MinDisplacementHeight); // makes no sense, but avoids tiles below the sea level getting occluded, without correctly path-tracing
                isOccluded = closestToCenter.IsBetween(camPos, tileHighest) && (closestToCenter - ParentTerrain.Curvature.Center).Length < occludingRadius;
            }
            occlusionChanged = wasOccluded != isOccluded;
        }

        internal void UpdateVisibility()
        {
            bool isLeaf = LastNodeEvent == QuadTreeNodeEvent.Grouped || LastNodeEvent == QuadTreeNodeEvent.Enabled;

            bool isVisible = isLeaf && !isOccluded;

            Drawable.Active = isVisible;
#if DrawTileAABB
                if (aabbMesh != null)
                    aabbMesh.Active = isVisible;
#endif
        }

        /// <summary>
        /// Value indicating if this tile was displayed before updating the terrain to the current LOD.
        /// </summary>
        public bool WasVisibleInPreviousLOD { get; set; }

        /// <summary>
        /// Minimum terrain height in this tile, along the surface normal
        /// </summary>
        public float MinDisplacementHeight { get; private set; }

        /// <summary>
        /// Maximum terrain height in this tile, along the surface normal
        /// </summary>
        public float MaxDisplacementHeight { get; private set; }


        public void Update(UpdateType updateType)
        {
            switch (updateType)
            {
                case UpdateType.FrameStart1: // terrain data polling
                    {
                        // check that terrain curvature is ready
                        if (!ParentTerrain.Curvature.DisplacementLUT.Loaded)
                            return;

                        // request / retrieve data from the data source
                        TerrainTileData terrainData;
                        if (!ParentTerrain.DataSource.TryGetTileData(Area, ParentTerrain.Curvature, Drawable, out terrainData))
                            return;

                        TiledFloat4x4 tessToWorld;

                        // update the tile transform
                        {
                            TiledFloat4x4 uvToWorld = Area.GetLocalToWorldTransform();
                            if (!ParentTerrain.Curvature.IsFlat)
                            {
                                // move the terrain tile so that its approximately near the final position after curvature is applied;
                                // since curvature can move the tile of many Km, having the world tile set closer to its final transform
                                // will keep precision errors low.
                                uvToWorld = uvToWorld.Translate(CurvatureWorldPreOffset);
                            }
                            tessToWorld = ParentTerrain.Tessellator.TessToUVTransform * uvToWorld;
                            tileWorldTransform.Set(tessToWorld);
                        }

                        // update terrain mesh with the available data
                        Drawable.MainMaterial = ParentTerrain.MaterialFactory.CreateMaterialFromData(this, this, terrainData);
                        OnTerrainUpdated();

                        // udpate the local tile bounding box
                        MinDisplacementHeight = terrainData.DisplacementMin * terrainData.DisplacementScale + terrainData.DisplacementOffset;
                        MaxDisplacementHeight = terrainData.DisplacementMax * terrainData.DisplacementScale + terrainData.DisplacementOffset;
                        geom.BoundingBox = CalcBoundingBox(MinDisplacementHeight, MaxDisplacementHeight, tessToWorld);

#if DrawTileAABB
                        CompMeshGeometry bbGeom = new CompMeshGeometry(this);
                        Primitives.AABB(bbGeom, geom.BoundingBox);
                        aabbMesh = new CompMesh(tileWorldTransform, null, bbGeom);
                        CompMtlBasic wireframeMat = new CompMtlBasic(aabbMesh, Color.Magenta.ToFloat3());
                        wireframeMat.FillMode = Graphics.FillMode.Wireframe;
                        wireframeMat.CullMode = Graphics.CullMode.None;
                        aabbMesh.MainMaterial = wireframeMat;
#endif

                        NeededUpdates = UpdateType.None;
                    }
                    break;
                case UpdateType.FrameStart2: // tessellation swap
                    {
                        if(Drawable.MainMaterial.NeededUpdates == UpdateType.None)
                        {
                            geom.EdgeTesselation = EdgeTessellation;
                            NeededUpdates = UpdateType.None;
                        }
                    }
                    break;
            } 

        }

        private static readonly TiledFloat3[] areaCorners = new TiledFloat3[9];
        private static readonly TiledFloat3[] worldDisplacedVerts = new TiledFloat3[18];
        private static readonly Float3[] tessSpaceVerts = new Float3[18];

        private AABox CalcBoundingBox(float minDisplacementHeight, float maxDisplacementHeight, TiledFloat4x4 tessToWorld)
        {
            // initial local box
            AABox tileBB;

            if (ParentTerrain.Curvature.IsFlat)
            {
                // initial local box
                tileBB = ParentTerrain.Tessellator.FlatBoundingBox;

                // set vertical displacement range
                tileBB.Min.Y = minDisplacementHeight;
                tileBB.Max.Y = maxDisplacementHeight;
            }
            else
            {
                // === calc the aabb from back-transformed corners
                Float4x4 worldToTess = tessToWorld.Value.Invert();

                // calc the flat area vertices (center + edges + corners)
                areaCorners[0] = Area.Center;
                Area.GetCorners(areaCorners, 1);
                Area.GetEdgeMiddlePoints(areaCorners, 5);

                // calc wpo + displacement vertices
                for (int i = 0; i < areaCorners.Length; i++)
                {
                    CompTerrainCurvature.LocalInfo curvature = ParentTerrain.Curvature.CalcLocalInfoAtTilePos(areaCorners[i]);
                    worldDisplacedVerts[2 * i] = areaCorners[i] + curvature.WorldOffset + curvature.Normal * maxDisplacementHeight;
                    worldDisplacedVerts[2 * i + 1] = areaCorners[i] + curvature.WorldOffset + curvature.Normal * minDisplacementHeight;
                }

                // tranform them in tessellation space
                for (int i = 0; i < worldDisplacedVerts.Length; i++)
                    tessSpaceVerts[i] = worldDisplacedVerts[i].ToFloat3(tessToWorld.Tile) * worldToTess;

                // build bb bounding the vertices (with some tollerance, due to both precision and 5-sample based approximation)
                tileBB = AABox.Bounding(tessSpaceVerts) * 1.01f;
            }

            return tileBB;
        }
        
        public TerrainEdgeTessellation EdgeTessellation { get; set; }

        /// <summary>
        /// Offset of this tile inside the parent, in percent of the parent area.
        /// </summary>
        public Float2 OffsetToParentPercent { get; set; }

        public void OnLODChanged()
        {
            if(WasVisibleInPreviousLOD)
            {
                if (EdgeTessellation != geom.EdgeTesselation)
                {   
                    // while visible, delay geometry tessellation update of one frame to allow the material to update to a morphing state, to avoid gaps.
                    // TODO: a material swap should be performed here instead: gaps can start to form if that one frame takes too long...
                    TerrainEdgeTessellation geomEdges = EdgeTessellation;
                    geomEdges.TopDivisor = System.Math.Max(EdgeTessellation.TopDivisor, geom.EdgeTesselation.TopDivisor);
                    geomEdges.BottomDivisor = System.Math.Max(EdgeTessellation.BottomDivisor, geom.EdgeTesselation.BottomDivisor);
                    geomEdges.LeftDivisor = System.Math.Max(EdgeTessellation.LeftDivisor, geom.EdgeTesselation.LeftDivisor);
                    geomEdges.RightDivisor = System.Math.Max(EdgeTessellation.RightDivisor, geom.EdgeTesselation.RightDivisor);
                    geom.EdgeTesselation = geomEdges;
                    
                    NeededUpdates = UpdateType.FrameStart2;
                }
            }
            else
            {
                geom.EdgeTesselation = EdgeTessellation;
            }

            if(Drawable.MainMaterial is ITerrainMaterial terrainMat)
            {
                terrainMat.OnLODChanged(this);
            }
            
            OnTerrainUpdated();
        }
    }
}