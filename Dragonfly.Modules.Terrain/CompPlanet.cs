using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Dragonfly.Terrain
{
    /// <summary>
    /// A drawable planet, based on curved CompTerrain.
    /// </summary>
    public class CompPlanet : Component
    {
        public CompPlanet(Component parent, PlanetParams planetParams) : base(parent)
        {
            // create a terrain for each side of a cube, then add a curvature to round them to a sphere
            CompTerrain[] sides = new CompTerrain[6];
            float terrainSize = (2.0f * planetParams.Radius / FMath.SQRT_3).FloorPower2(); // must be a power of 2 for precision when dividing tiles

            TerrainParams tparams;
            tparams.DataSource = planetParams.DataSource;
            tparams.LodUpdater = planetParams.LodUpdater;
            tparams.MaterialFactory = planetParams.MaterialFactory;
            tparams.CurvatureEnabled = true;
            tparams.CurvatureRadius = planetParams.Radius;
            tparams.ExplicitCurvatureCenter = true;
            tparams.CurvatureCenter = planetParams.Center;

            for (int i = 0; i < 6; i++)
            {
                Float3 xdir = CubeMapHelper.FaceRightVector[i];
                Float3 ydir = CubeMapHelper.FaceUpVector[i];
                Float3 normal = CubeMapHelper.FaceNormals[i];
                tparams.Area = new TiledRect3(planetParams.Center +  0.5f * terrainSize * (normal - xdir - ydir), xdir, ydir, (Float2)terrainSize);
                sides[i] = new CompTerrain(this, tparams);
            }

            // connect adjacent terrains
            for (int i = 0; i < 6; i++)
            {
                ConnectTerrain(sides, i, QuadTreeSide.Top);
                ConnectTerrain(sides, i, QuadTreeSide.Bottom);
                ConnectTerrain(sides, i, QuadTreeSide.Left);
                ConnectTerrain(sides, i, QuadTreeSide.Right);
            }

            Terrains = sides;

            // create a default atmosphere
            Atmosphere = new CompAtmosphere(this, planetParams.Center, planetParams.Radius);

            // create a camera collider
            float shadowColliderOffset = Context.GetModule<BaseMod>().Settings.Shadows.MaxOccluderDistance;
            new CompFunction<ShadowCameraCollider>(this, () => GetShadowCollider(shadowColliderOffset));
        }

        private ShadowCameraCollider GetShadowCollider(float surfaceOffset)
        {
            ShadowCameraCollider collider = new ShadowCameraCollider();
            collider.Center = (TiledFloat4x4.Translation(Center) * GetTransform()).Position;
            collider.Radius = Radius.ToFloat() + surfaceOffset;
            return collider;
        }

        public IReadOnlyList<CompTerrain> Terrains { get; private set; }

        private void ConnectTerrain(CompTerrain[] terrainList, int terrainIndex, QuadTreeSide side)
        {
            // search adjacent terrain in the specified direction
            float adjDir = (side == QuadTreeSide.Bottom || side == QuadTreeSide.Right) ? 1.0f : -1.0f;
            Float3[] adjAxis = (side == QuadTreeSide.Bottom || side == QuadTreeSide.Top) ? CubeMapHelper.FaceUpVector : CubeMapHelper.FaceRightVector;
            int adjIndex = Array.FindIndex<CompTerrain>(terrainList, t => t.Area.Normal == (adjDir * adjAxis[terrainIndex]));

            if (adjIndex < terrainIndex)
                return; // skip half of the matches, to avoid connecting two permutations of the same terrain pair

            // find adjacent connecting side
            Float3 sideDir = (side == QuadTreeSide.Bottom || side == QuadTreeSide.Top) ? CubeMapHelper.FaceRightVector[terrainIndex] : CubeMapHelper.FaceUpVector[terrainIndex];
            QuadTreeSide adjSide = QuadTreeSide.Top;
            if (CubeMapHelper.FaceUpVector[adjIndex] == CubeMapHelper.FaceNormals[terrainIndex])
                adjSide = QuadTreeSide.Bottom;
            else if (CubeMapHelper.FaceRightVector[adjIndex] == CubeMapHelper.FaceNormals[terrainIndex])
                adjSide = QuadTreeSide.Right;
            else if (-CubeMapHelper.FaceRightVector[adjIndex] == CubeMapHelper.FaceNormals[terrainIndex])
                adjSide = QuadTreeSide.Left;

            // check if connecting side direction is flipped
            bool flipped = (adjSide == QuadTreeSide.Top || adjSide == QuadTreeSide.Bottom ? CubeMapHelper.FaceRightVector : CubeMapHelper.FaceUpVector)[adjIndex] != sideDir;

            // connect them
            ConnectTerrains(terrainList[terrainIndex], side, terrainList[adjIndex], adjSide, flipped);
        }

        private void ConnectTerrains(CompTerrain t1, QuadTreeSide side1, CompTerrain t2, QuadTreeSide side2, bool flipConnection)
        {
            QuadTree<CompTerrainTile>.Connect(t1.Tiles.Tree, side1, t2.Tiles.Tree, side2, flipConnection);
            t1.AdjacentTerrains.Add(t2);
            t2.AdjacentTerrains.Add(t1);
        }

        public TiledFloat3 Center
        {
            get
            {
                return Terrains[0].Curvature.Center;
            }
        }

        public TiledFloat Radius
        {
            get
            {
                return Terrains[0].Curvature.Radius;
            }
        }

        public CompAtmosphere Atmosphere { get; private set; }

    }

    /// <summary>
    /// Initializer struct storing a CompPlanet config.
    /// </summary>
    public struct PlanetParams
    {
        public CompTerrainLODUpdater LodUpdater;
        public ITerrainDataSource DataSource;
        public ITerrainMaterialFactory MaterialFactory;
        public TiledFloat3 Center;
        public float Radius;
    }
}
