using Dragonfly.BaseModule;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.Engine.Procedural
{
    internal static class ProceduralTree
    {
        private struct BranchState
        {
            public int NestingLevel;
            public Float3 GrowDirection;
            public Float3 Position;
            public float StartDistance;
        }

        private static FRandom rnd;

        public static CompMeshList Generate(Component parent, ProceduralTreeDescr tp, MaterialFactory matFactory)
        {
            rnd = new FRandom(tp.Seed);
            CompMeshList treeMesh = new CompMeshList(parent);          
            CompMesh trunk = treeMesh.AddMesh();
            trunk.Materials.Add(matFactory.CreateMaterial(tp.TrunkMaterial, trunk));


            List<IObject3D> outFoliages = new List<IObject3D>();
            if (tp.FoliageLayers != null && tp.FoliageLayers.Count > 0)
            {
                bool hasAlbedoVariations = tp.FoliageAlbedoVariations != null && tp.FoliageAlbedoVariations.Count > 0;
                int foliageMeshCount = hasAlbedoVariations ? tp.FoliageAlbedoVariations.Count : 1;
                for (int i = 0; i < foliageMeshCount; i++)
                {
                    MaterialDescription foliageMatDescr = tp.FoliageMaterial;
                    if(hasAlbedoVariations)
                        foliageMatDescr.Albedo = tp.FoliageAlbedoVariations[i];
                    CompMesh foliageMesh = treeMesh.AddMesh();
                    CompMaterial foliageMat = matFactory.CreateMaterial(foliageMatDescr, foliageMesh);
                    foliageMat.CullMode = CullMode.None;
                    foliageMesh.Materials.Add(foliageMat);
                    outFoliages.Add(foliageMesh.AsObject3D());
                }
            }

            // start generation from the main branch (which is actually the trunk)
            GenerateBranch(trunk.AsObject3D(), outFoliages, tp);
            
            return treeMesh;
        }

        private static void GenerateBranch(IObject3D outBranches, List<IObject3D> outFoliages, ProceduralTreeDescr tp)
        {
            BranchState trunkState = new BranchState();
            trunkState.Position = tp.TreeStartPosition;
            trunkState.Position.Y -= tp.TrunkRadius; // avoid an open trunk to show up above the terrain
            trunkState.StartDistance = 0;
            trunkState.NestingLevel = 0;
            trunkState.GrowDirection = Float3.UnitY;

            GenerateBranch(outBranches, outFoliages, tp, trunkState);
        }

        private static void GenerateBranch(IObject3D outBranches, List<IObject3D> outFoliages, ProceduralTreeDescr tp, BranchState state)
        {
            // calc new branch starting radius
            float endDistance = GetDistanceFromRadius(tp, tp.BranchMinRadius);
            if (state.NestingLevel > 0)
                state.StartDistance = state.StartDistance.Lerp(endDistance, rnd.NextFloat() * tp.BranchMinRadiusPercent);

            // calc new branch len
            float branchLen = endDistance - state.StartDistance;
            if (branchLen < tp.TreeMaxHeight * tp.BranchMinLenPercent) return; // skip branches that are too small
            Func<float, float> distToRadius = GetRadiusFunction(tp, state.StartDistance);
            Path3D branchPath = null;

            // gereate a random starting direction from the reference one
            state.GrowDirection = rnd.PerturbateNormal(
                state.GrowDirection,
                state.NestingLevel == 0 ? 0 : tp.BranchMinAngleRadians,
                state.NestingLevel == 0 ? tp.TrunkMaxAngleRadians : tp.BranchMaxAngleRadians
            );

            // generate this branch
            {
                int branchFeatureCount = (int)(branchLen / tp.BranchAvgFeatureDistance);
                List<Float3> branchVertices = rnd.RandomWalk(state.Position, state.Position + state.GrowDirection * branchLen, branchFeatureCount, tp.BranchMaxDirectionVariance, tp.BranchSmoothingPercent * tp.BranchAvgFeatureDistance);

                // apply ground level
                for (int i = 1; i < branchVertices.Count; i++)
                {
                    Float3 curVertex = branchVertices[i];
                    curVertex.Y = Math.Max(curVertex.Y, tp.GroundLevel);
                    branchVertices[i] = curVertex;
                }

                //apply sun search
                {
                    Float3 nodeOffset = Float3.Zero;
                    for (int i = 1; i < branchVertices.Count; i++)
                    {
                        Float3 branchSegment = branchVertices[i] - branchVertices[i - 1];
                        nodeOffset += branchSegment.Lerp(Float3.UnitY * branchSegment.Length, (float)i / branchVertices.Count * tp.SunSearchRate) - branchSegment;
                        branchVertices[i] = branchVertices[i] + nodeOffset;
                    }
                }

                // create path from vertices
                branchPath = new Path3D(branchVertices.ToArray());
                branchPath.SmoothingRadius = tp.BranchSmoothingPercent * tp.BranchAvgFeatureDistance * 0.5f;
                branchPath.Points[branchPath.Points.Count - 1] = branchPath.GetPositionAt(branchLen); // trim to size
                
                // create branch mesh
                Primitives.Pipe(outBranches, branchPath, distToRadius, tp.BranchMinTessellationDistance, 0.01f, 3.0f, false, true, tp.TrunkTexCoordMul / (2.0f * tp.TrunkRadius * FMath.PI));
            }

            // generate foliage for this branch
            if(tp.FoliageLayers != null)
            {
                foreach(ProceduralTreeFoliageParams f in tp.FoliageLayers)
                {
                    if ((f.Type & ProceduralFoliageType.Terminal) == ProceduralFoliageType.Terminal)
                    {
                        int leafInstances = f.Type == ProceduralFoliageType.Terminal ? (int)f.FoliageDensity : 1; // if terminal type is used alone, density is used to indicate the number of random instances.
                        for (int i = 0; i < leafInstances; i++)
                            GenerateLeaf(outFoliages, branchPath, branchLen, f);
                    }

                    if ((f.Type & ProceduralFoliageType.Branching) == ProceduralFoliageType.Branching)
                    {
                        float distPower = 1.0f / f.BranchFoliagePowerDistribution;
                        float leafPercentStart = FMath.Pow(state.StartDistance / tp.TreeMaxHeight, distPower);
                        float leafPercentEnd = FMath.Pow(endDistance / tp.TreeMaxHeight, distPower);
                        int leafInstances = (int)((leafPercentEnd - leafPercentStart) * f.FoliageDensity * tp.TreeMaxHeight + rnd.NextFloat());
 
                        for (int i = 0; i < leafInstances; i++)
                        {
                            float fDist = FMath.Pow(rnd.NextFloat(), distPower.Lerp(1.0f, 1.0f - branchLen / tp.TreeMaxHeight)) * branchLen;
                            if (fDist + state.StartDistance < f.HeightStartPercent * tp.TreeMaxHeight) continue;
                            GenerateLeaf(outFoliages, branchPath, fDist, f);
                        }
                    }
                }
            }

            // start sub-branches     
            if (state.NestingLevel < tp.BranchMaxNestingLevel)
            {
                float branchStep = tp.BranchAvgDistance * (1.0f - state.StartDistance / tp.TreeMaxHeight * tp.BranchDistanceDensityMul);
                for (float branchDist = branchStep; branchDist < branchLen; branchDist += branchStep)
                {
                    BranchState s = state;
                    s.StartDistance += branchDist;
                    s.Position = branchPath.GetPositionAt(branchDist);
                    s.GrowDirection = branchPath.GetDirectionAt(branchDist);
                    s.NestingLevel++;
                    GenerateBranch(outBranches, outFoliages, tp, s);
                }
            }
        }

        private static void GenerateLeaf(List<IObject3D> outFoliages, Path3D branchPath, float onBranchDistance, ProceduralTreeFoliageParams fp)
        {
            // create the leaft shaping function
            Func<Float2, Float3> leafShape = GetLeafShapeFunction(fp);

            // fill a transformation matrix for this leaf instance
            Float4x4 leafTransform;
            {
                // calculate a randomized leaf size
                Float2 leafSize = fp.Sizes * (1.0f + fp.SizeVariationPercent * rnd.NextSignedFloat());

                // build a tangent space for the current leaf
                Float3 branchDir = branchPath.GetDirectionAt(onBranchDistance);
                Float3 n = rnd.PerturbateNormal(branchDir, fp.FoliageMinAngleRadians, fp.FoliageMaxAngleRadians) * Float4x4.Rotation(branchDir, rnd.NextFloat(0, FMath.TWO_PI));
                Float3 t = n.Cross(Float3.NotParallelAxis(n, Float3.UnitY)).Normal();
                Float3 b = t.Cross(n);
                leafTransform = Float4x4.Scale(leafSize.X, leafSize.CMin() * fp.BendingPercent.Y, leafSize.Y) * new Float4x4(t, b, n);

                // translate the leaf start point to the branch location
                Float2 shapeStartCoords = fp.FoliageTexCoords.GetCoordsAt(fp.FoliageStartCoords);
                Float3 p0 = branchPath.GetPositionAt(onBranchDistance) - leafShape(shapeStartCoords) * leafTransform;
                leafTransform *= Float4x4.Translation(p0);
            }

            // output the left plane geometry
            ProcPrimitives.ShapedPlane(outFoliages[rnd.NextInt(outFoliages.Count)], leafShape, fp.FoliageTexCoords, fp.Tesselation, leafTransform);
        }

        private static Func<Float2, Float3> GetLeafShapeFunction(ProceduralTreeFoliageParams fp)
        {
            return SurfaceShapes.AddNoise(SurfaceShapes.Lerp(SurfaceShapes.Flat, SurfaceShapes.QuarterSphere, fp.BendingPercent.X), fp.Perturbation, rnd);
        }

        private static Func<float, float> GetRadiusFunction(ProceduralTreeDescr tp, float distanceOffset)
        {
            return distance =>
            {
                float d = distance + distanceOffset;
                return tp.TrunkRadius * (1 - d / tp.TreeMaxHeight).Saturate() / (d * Math.Max(0, tp.BranchThinningRate) + 1);
            };
        }

        private static float GetDistanceFromRadius(ProceduralTreeDescr tp, float radius)
        {
            float r = radius / tp.TrunkRadius;
            return tp.TreeMaxHeight * (1 - r) / (radius * tp.TreeMaxHeight * tp.BranchThinningRate + 1);
        }

    }
}
