using Dragonfly.Graphics.Math;
using System.Collections.Generic;
using System;

namespace Dragonfly.BaseModule
{
    public class Primitives
    {
        public static void Quad(IObject3D outMesh, Float3 vert1, Float3 vert2, Float3 vert3, Float2 texCoord1, Float2 texCoord2, Float2 texCoord3)
        {
            int baseIndex = outMesh.VertexCount;

            // vertices
            outMesh.AddVertex(vert1);
            outMesh.AddVertex(vert2);
            outMesh.AddVertex(vert3);
            outMesh.AddVertex(vert1 + vert3 - vert2);

            // normals
            Float3 normal = (vert2 - vert1).Cross(vert3 - vert1).Normal();
            for (int i = 0; i < 4; i++) outMesh.AddNormal(normal);

            // tex coords
            outMesh.AddTexCoord(texCoord1);
            outMesh.AddTexCoord(texCoord2);
            outMesh.AddTexCoord(texCoord3);
            outMesh.AddTexCoord(texCoord1 + texCoord3 - texCoord2);

            // indices
            outMesh.AddIndex((ushort)(baseIndex + 1));
            outMesh.AddIndex((ushort)(baseIndex + 2));
            outMesh.AddIndex((ushort)(baseIndex + 0));

            outMesh.AddIndex((ushort)(baseIndex + 0));
            outMesh.AddIndex((ushort)(baseIndex + 2));
            outMesh.AddIndex((ushort)(baseIndex + 3));

            outMesh.UpdateGeometry();
        }

        /// <summary>
        /// Create a rectangle mesh, given three consecutive vertices.
        /// </summary>
        public static void Quad(IObject3D outMesh, Float3 vert1, Float3 vert2, Float3 vert3)
        {
            Quad(outMesh, vert1, vert2, vert3, Float2.Zero, Float2.UnitX, Float2.One);
            outMesh.UpdateGeometry();
        }

        /// <summary>
        /// Create a rectangle mesh that fills the screen.
        /// </summary>
        public static void ScreenQuad(IObject3D outMesh)
        {
            ScreenQuad(outMesh, Float2.Zero, Float2.One);
        }

        /// <summary>
        /// Create a rectangle mesh that fills the screen.
        /// </summary>
        public static void ScreenQuad(IObject3D outMesh, Float2 topLeftCoords, Float2 bottomRightCoords)
        {
            Quad(outMesh, new Float3(-1.0f, 1.0f, 0), new Float3(1.0f, 1.0f, 0), new Float3(1.0f, -1.0f, 0), topLeftCoords, new Float2(bottomRightCoords.X, topLeftCoords.Y), bottomRightCoords);
            outMesh.UpdateGeometry();
        }

        public static void ScreenQuad(IObject3D outMesh, Float2 topLeftCoords, Float2 bottomRightCoords, Float2 screenTopLeft, Float2 screenBottomRight)
        {
            Quad(outMesh, screenTopLeft.ToFloat3(0), new Float3(screenBottomRight.X, screenTopLeft.Y, 0), screenBottomRight.ToFloat3(0), topLeftCoords, new Float2(bottomRightCoords.X, topLeftCoords.Y), bottomRightCoords);
            outMesh.UpdateGeometry();
        }

        /// <summary>
        /// Create an UI panel that extends a texture area to the specified size, breaking it along the middle horizontal and vertical axes. Positions and border size are specified in screen-space.
        /// </summary>
        public static void ScreenResizablePanel(IObject3D outMesh, Float2 topLeftPos, Float2 bottomRightPos, Float2 topLeftCoords, Float2 bottomRightCoords, Float2 borderSize)
        {
            int baseIndex = outMesh.VertexCount;

            // correct border size to avoid invalid values
            Float2 panelSize = (topLeftPos - bottomRightPos).Abs();
            Float2 maxBorderSize = panelSize * 0.5f; // border cannot be wider than the panel
            Float2 borderOverflow = Float2.Max(Float2.Zero, borderSize - maxBorderSize);
            borderSize = borderSize - borderOverflow;

            // vertices
            Float2 innerTopLeft = new Float2(topLeftPos.X + borderSize.X, topLeftPos.Y - borderSize.Y);
            Float2 innerBottomRight = new Float2(bottomRightPos.X - borderSize.X, bottomRightPos.Y + borderSize.Y);

            outMesh.AddVertex(new Float3(topLeftPos.X, topLeftPos.Y, 0));
            outMesh.AddVertex(new Float3(innerTopLeft.X, topLeftPos.Y, 0));
            outMesh.AddVertex(new Float3(innerBottomRight.X, topLeftPos.Y, 0));
            outMesh.AddVertex(new Float3(bottomRightPos.X, topLeftPos.Y, 0));

            outMesh.AddVertex(new Float3(topLeftPos.X, innerTopLeft.Y, 0));
            outMesh.AddVertex(new Float3(innerTopLeft.X, innerTopLeft.Y, 0));
            outMesh.AddVertex(new Float3(innerBottomRight.X, innerTopLeft.Y, 0));
            outMesh.AddVertex(new Float3(bottomRightPos.X, innerTopLeft.Y, 0));

            outMesh.AddVertex(new Float3(topLeftPos.X, innerBottomRight.Y, 0));
            outMesh.AddVertex(new Float3(innerTopLeft.X, innerBottomRight.Y, 0));
            outMesh.AddVertex(new Float3(innerBottomRight.X, innerBottomRight.Y, 0));
            outMesh.AddVertex(new Float3(bottomRightPos.X, innerBottomRight.Y, 0));

            outMesh.AddVertex(new Float3(topLeftPos.X, bottomRightPos.Y, 0));
            outMesh.AddVertex(new Float3(innerTopLeft.X, bottomRightPos.Y, 0));
            outMesh.AddVertex(new Float3(innerBottomRight.X, bottomRightPos.Y, 0));
            outMesh.AddVertex(new Float3(bottomRightPos.X, bottomRightPos.Y, 0));

            // tex coords
            Float2 coordVec = bottomRightCoords - topLeftCoords;
            Float2 coordBorderSize = 0.5f * coordVec / (1 + 2.0f * borderOverflow / panelSize);
            Float2 innerTopLeftCoords = topLeftCoords + coordBorderSize;
            Float2 innerBottomRightCoords = bottomRightCoords - coordBorderSize;

            outMesh.AddTexCoord(new Float2(topLeftCoords.X, topLeftCoords.Y));
            outMesh.AddTexCoord(new Float2(innerTopLeftCoords.X, topLeftCoords.Y));
            outMesh.AddTexCoord(new Float2(innerBottomRightCoords.X, topLeftCoords.Y));
            outMesh.AddTexCoord(new Float2(bottomRightCoords.X, topLeftCoords.Y));

            outMesh.AddTexCoord(new Float2(topLeftCoords.X, innerTopLeftCoords.Y));
            outMesh.AddTexCoord(new Float2(innerTopLeftCoords.X, innerTopLeftCoords.Y));
            outMesh.AddTexCoord(new Float2(innerBottomRightCoords.X, innerTopLeftCoords.Y));
            outMesh.AddTexCoord(new Float2(bottomRightCoords.X, innerTopLeftCoords.Y));

            outMesh.AddTexCoord(new Float2(topLeftCoords.X, innerBottomRightCoords.Y));
            outMesh.AddTexCoord(new Float2(innerTopLeftCoords.X, innerBottomRightCoords.Y));
            outMesh.AddTexCoord(new Float2(innerBottomRightCoords.X, innerBottomRightCoords.Y));
            outMesh.AddTexCoord(new Float2(bottomRightCoords.X, innerBottomRightCoords.Y));

            outMesh.AddTexCoord(new Float2(topLeftCoords.X, bottomRightCoords.Y));
            outMesh.AddTexCoord(new Float2(innerTopLeftCoords.X, bottomRightCoords.Y));
            outMesh.AddTexCoord(new Float2(innerBottomRightCoords.X, bottomRightCoords.Y));
            outMesh.AddTexCoord(new Float2(bottomRightCoords.X, bottomRightCoords.Y));

            // normals
            Float3 normal = -Float3.UnitZ;
            for (int i = 0; i < 16; i++) outMesh.AddNormal(normal);

            // indices
            AddIndicesToGrid(outMesh, baseIndex, 4, 4);

            outMesh.UpdateGeometry();
        }

        public static void Cuboid(IObject3D outMesh, Float3 center, Float3 sizes)
        {
            Float3 halfSizes = sizes / 2;
            Float3 halfX = halfSizes * Float3.UnitX;
            Float3 halfY = halfSizes * Float3.UnitY;
            Float3 halfZ = halfSizes * Float3.UnitZ;


            Quad(outMesh, center - halfX + halfY + halfZ, center - halfX + halfY - halfZ, center - halfX - halfY - halfZ); // x-
            Quad(outMesh, center + halfX + halfY - halfZ, center + halfX + halfY + halfZ, center + halfX - halfY + halfZ); // x+

            Quad(outMesh, center - halfX + halfY - halfZ, center + halfX + halfY - halfZ, center + halfX - halfY - halfZ); // z-
            Quad(outMesh, center + halfX + halfY + halfZ, center - halfX + halfY + halfZ, center - halfX - halfY + halfZ); // z+

            Quad(outMesh, center - halfX - halfY - halfZ, center + halfX - halfY - halfZ, center + halfX - halfY + halfZ); // y-
            Quad(outMesh, center - halfX + halfY + halfZ, center + halfX + halfY + halfZ, center + halfX + halfY - halfZ); // y+

            outMesh.UpdateGeometry();
        }

        public static void AABB(IObject3D outMesh, AABox boundingBox)
        {
            Cuboid(outMesh, 0.5f * (boundingBox.Max + boundingBox.Min), (boundingBox.Max - boundingBox.Min).Abs());
        }

        public static void Spheroid(IObject3D outMesh, Float3 center, Float3 sizes, int maxVertexCount)
        {
            int baseIndex = outMesh.VertexCount;

            // === calc tessellation ammount
            int txyz = Math.Max(maxVertexCount - 2, 3);
            int ty = (int)(Math.Sqrt(txyz) / 2 - 0.8) * 2 + 1;
            int txz = txyz / ty;
            int vcount = txz * ty + 2;

            // === calc sphere verices
            Float3 bottom = new Float3(0, -sizes.Y / 2, 0) + center;
            Float3 top = new Float3(0, sizes.Y / 2, 0) + center;
            float xzRadStep = FMath.TWO_PI / txz;
            float yRadStep = FMath.PI / (ty + 1);

            for (int yi = 0; yi < ty; yi++)
            {
                float yrad = (yi + 1) * yRadStep;
                float y = -0.5f * (float)Math.Cos(yrad) * sizes.Y;
                float radius = (float)Math.Sin(yrad);

                for (int ri = 0; ri < txz; ri++)
                {
                    float rad = xzRadStep * ri;
                    Float2 xzDir = new Float2((float)Math.Cos(rad), (float)Math.Sin(rad));
                    Float2 xzScales = new Float2(sizes.X, sizes.Z) * 0.5f;
                    Float2 xz = xzDir * xzScales * radius;

                    Float3 posOffset = new Float3(xz.X, y, xz.Y);
                    outMesh.AddVertex(posOffset + center);
                    outMesh.AddNormal(posOffset.Normal());
                }
            }

            outMesh.AddVertex(bottom);
            outMesh.AddNormal(-Float3.UnitY);

            outMesh.AddVertex(top);
            outMesh.AddNormal(Float3.UnitY);

            // === calc indices

            // top and bottom cap
            int lastIndex = baseIndex + txz * ty + 1;
            for (int i = 0; i < txz; i++)
            {
                int curi = i;
                int nexti = (i + 1) % txz;

                // bottom
                outMesh.AddIndex((ushort)(lastIndex - 1));
                outMesh.AddIndex((ushort)(baseIndex + curi));
                outMesh.AddIndex((ushort)(baseIndex + nexti));

                // top
                outMesh.AddIndex((ushort)(lastIndex));
                outMesh.AddIndex((ushort)(lastIndex - curi - 2));
                outMesh.AddIndex((ushort)(lastIndex - nexti - 2));
            }

            // side strips
            for (int i = 1; i < ty; i++)
            {
                int baseStripIndex = (i - 1) * txz + baseIndex;
                for (int j = 0; j < txz; j++)
                {
                    int v0 = baseStripIndex + j;
                    int v1 = baseStripIndex + (j + 1) % txz;
                    outMesh.AddIndex((ushort)(v0));
                    outMesh.AddIndex((ushort)(v0 + txz));
                    outMesh.AddIndex((ushort)v1);

                    outMesh.AddIndex((ushort)(v1));
                    outMesh.AddIndex((ushort)(v0 + txz));
                    outMesh.AddIndex((ushort)(v1 + txz));
                }
            }

            outMesh.UpdateGeometry();
        }

        public static void Ellipse(IObject3D outMesh, Float3 center, Float3 normal, Float2 sizes, int maxVertexCount)
        {
            Ellipse_Internal(outMesh, center, normal, sizes, 0, maxVertexCount);
        }

        internal static void Ellipse_Internal(IObject3D outMesh, Float3 center, Float3 normal, Float2 sizes, float centerHeightOffset, int maxVertexCount)
        {
            int baseIndex = outMesh.VertexCount;

            // === calc tessellation ammount
            int sliceCount = Math.Max(3, maxVertexCount - 1);

            // === calc ellipse verices
            {
                Float2x3 to3d = Float2x2.Scale(sizes * 0.5f) * Float2x3.PlanarMapping(normal);

                // loop all the ellipse triangular slices
                float xzRadStep = FMath.TWO_PI / sliceCount;

                for (int ri = 0; ri < sliceCount; ri++)
                {
                    float rad = xzRadStep * ri;

                    // external vertex
                    Float2 xzDir = Float2.FromAngle(rad);
                    Float3 posOffset = xzDir * to3d;
                    outMesh.AddVertex(posOffset + center);
                    outMesh.AddTexCoord(xzDir * 0.5f + (Float2)0.5f);
                    outMesh.AddNormal((normal * posOffset.Length + posOffset.Normal() * centerHeightOffset).Normal());
                }

                // central vertex
                outMesh.AddVertex(center + normal * centerHeightOffset);
                outMesh.AddTexCoord((Float2)0.5f);
                outMesh.AddNormal(Float3.Zero); // create a singularity but once normalized creates the correct normal interpolation
            }

            // === calc indices
            {
                for (int i = 0; i < sliceCount; i++)
                {
                    int curi = baseIndex + i;
                    int nexti = baseIndex + (i + 1) % sliceCount;

                    outMesh.AddIndex((ushort)(baseIndex + sliceCount)/*the ellipse center index*/);
                    outMesh.AddIndex((ushort)curi);
                    outMesh.AddIndex((ushort)nexti);
                }
            }

            outMesh.UpdateGeometry();
        }

        public static void Cone(IObject3D outMesh, Float3 baseCenter, Float3 upDir, Float3 sizes, int maxVertexCount)
        {
            Ellipse_Internal(outMesh, baseCenter, -upDir.Normal(), sizes.XZ, 0, maxVertexCount / 2);
            Ellipse_Internal(outMesh, baseCenter, upDir.Normal(), sizes.XZ, sizes.Y, maxVertexCount / 2);

            outMesh.UpdateGeometry();
        }

        internal static void CylinderShell_Internal(IObject3D outMesh, Float3 baseCenter, Float3 upDir, Float3 sizes, int baseTess)
        {
            int baseIndex = outMesh.VertexCount;

            AddVertexRing(outMesh, baseCenter, upDir, sizes.XZ, 0, baseTess); // base ring
            AddVertexRing(outMesh, baseCenter + upDir * sizes.Y, upDir, sizes.XZ, 1, baseTess); // top ring
            AddIndicesToJoinRings(outMesh, baseIndex, baseTess, 0);
        }

        public static void Cylinder(IObject3D outMesh, Float3 baseCenter, Float3 upDir, Float3 sizes, int maxVertexCount)
        {
            int baseTess = Math.Max(3, (maxVertexCount - 2) / 4);
            CylinderShell_Internal(outMesh, baseCenter, upDir, sizes, baseTess); // sides
            Ellipse_Internal(outMesh, baseCenter, -upDir.Normal(), sizes.XZ, 0, baseTess + 1); // base
            Ellipse_Internal(outMesh, baseCenter + upDir * sizes.Y, upDir.Normal(), sizes.XZ, 0, baseTess + 1); // top

            outMesh.UpdateGeometry();
        }

        public static void Pipe(IObject3D outMesh, Path3D pipePath, float radius, float minSplitLength, float splitDirectionThr, int sideTesselation, bool closeStart, bool closeEnd, float vCoordMul)
        {
            Pipe(outMesh, pipePath, x => radius, minSplitLength, splitDirectionThr, sideTesselation / (radius * FMath.TWO_PI), closeStart, closeEnd, vCoordMul);
        }

        public static void Pipe(IObject3D outMesh, Path3D pipePath, Func<float, float> distanceToRadius, float minSplitLength, float splitDirectionThr, float sideTessDensity, bool closeStart, bool closeEnd, float vCoordMul)
        {
            float totalDist = pipePath.TotalDistance;
            List<int> ringBaseIndex = new List<int>();
            List<int> ringTess = new List<int>();
            List<float> ringDist = new List<float>();

            // add all vertices
            int ringSkipCount = 0;
            float curVertTexCoord = 1.0f;
            for (float curDist = 0; curDist - minSplitLength < totalDist; curDist += minSplitLength)
            {
                float clampedDist = Math.Min(curDist, totalDist);

                // if this is not the start or end of the pipe, evaluate is tesselation is needed
                if (ringBaseIndex.Count > 0 && curDist < totalDist && ringSkipCount < 4)
                {
                    if (pipePath.GetAccelerationBetween(ringDist[ringDist.Count - 1], clampedDist) < splitDirectionThr)
                    {
                        ringSkipCount++;
                        continue; // tesselation ring not needed
                    }
                }

                ringSkipCount = 0;
                float radius = distanceToRadius(clampedDist);
                int sideTesselation = 3 * (int)Math.Pow(2.0, Math.Round(Math.Max(0, Math.Log(sideTessDensity * radius * FMath.TWO_PI / 3.0f + 0.5f, 2.0))));

                ringBaseIndex.Add(outMesh.VertexCount);
                ringTess.Add(sideTesselation);

                if (ringDist.Count > 0)
                {
                    float prevDist = ringDist[ringDist.Count - 1];
                    curVertTexCoord -= (clampedDist - prevDist) * vCoordMul / (radius + distanceToRadius(prevDist)) * 2.0f;
                }

                ringDist.Add(clampedDist);

                AddVertexRing(outMesh, pipePath.GetPositionAt(clampedDist), pipePath.GetDirectionAt(clampedDist), (Float2)(2 * radius), curVertTexCoord, sideTesselation);
            }

            // tesselate all sides
            for (int i = 1; i < ringBaseIndex.Count; i++)
            {
                if (ringTess[i - 1] == ringTess[i])
                    AddIndicesToJoinRings(outMesh, ringBaseIndex[i - 1], ringTess[i - 1], 0);
                else if (ringTess[i - 1] / 2 == ringTess[i])
                    AddIndicesToJoinRings2To1(outMesh, ringBaseIndex[i - 1], ringTess[i - 1]);
                else
                {
                    // TODO unmanaged tesselation change
                }
            }

            if (closeStart)
                Ellipse_Internal(outMesh, pipePath.GetPositionAt(0), -pipePath.GetDirectionAt(0), (Float2)distanceToRadius(0) * 2, 0, ringTess[0] + 1); // base

            if (closeEnd)
                Ellipse_Internal(outMesh, pipePath.GetPositionAt(totalDist), pipePath.GetDirectionAt(totalDist), (Float2)distanceToRadius(totalDist) * 2, 0, ringTess[ringTess.Count - 1] + 1); // top

            outMesh.UpdateGeometry();
        }

        private static void AddVertexRing(IObject3D outMesh, Float3 center, Float3 upDir, Float2 sizes, float texCoordV, int vertexCount)
        {
            int baseIndex = outMesh.VertexCount;

            // loop all the polygonal edges and add top and bottom vertices
            float radStep = FMath.TWO_PI / vertexCount;
            Float2x3 to3d = Float2x2.Scale(sizes * 0.5f) * Float2x3.PlanarMapping(upDir);
            Float2x3 toNormal = Float2x2.Scale(2.0f / sizes) * Float2x3.PlanarMapping(upDir);

            for (int i = 0; i <= vertexCount; i++)
            {
                float rad = radStep * i;

                Float2 xzDir = Float2.FromAngle(rad);
                Float3 posOffset = xzDir * to3d;
                Float3 normal = (xzDir * toNormal).Normal();
                Float2 texCoords = new Float2((float)i / vertexCount, texCoordV);

                // add vertex
                outMesh.AddVertex(posOffset + center);
                outMesh.AddTexCoord(texCoords);
                outMesh.AddNormal(normal);
            }
        }

        private static void AddIndicesToJoinRings(IObject3D outMesh, int baseIndex, int ringSideCount, int secondRingOffset)
        {
            int rlen = ringSideCount + 1;
            for (int i = 0; i < ringSideCount; i++)
            {
                int basei0 = baseIndex + i;
                int basei1 = basei0 + 1;
                int topi0 = baseIndex + rlen + (i + secondRingOffset) % rlen;
                int topi1 = baseIndex + rlen + (i + 1 + secondRingOffset) % rlen;

                outMesh.AddIndex((ushort)(topi0));
                outMesh.AddIndex((ushort)(basei0));
                outMesh.AddIndex((ushort)(basei1));
                outMesh.AddIndex((ushort)(topi0));
                outMesh.AddIndex((ushort)(basei1));
                outMesh.AddIndex((ushort)(topi1));
            }
        }

        private static void AddIndicesToJoinRings2To1(IObject3D outMesh, int baseIndex, int ringSideCount)
        {
            for (int i = 0; i < ringSideCount / 2; i++)
            {
                int basei = baseIndex + 2 * i;
                int topi = baseIndex + ringSideCount + 1 + i;

                outMesh.AddIndex((ushort)(topi));
                outMesh.AddIndex((ushort)(basei));
                outMesh.AddIndex((ushort)(basei + 1));

                outMesh.AddIndex((ushort)(topi));
                outMesh.AddIndex((ushort)(basei + 1));
                outMesh.AddIndex((ushort)(topi + 1));

                outMesh.AddIndex((ushort)(topi + 1));
                outMesh.AddIndex((ushort)(basei + 1));
                outMesh.AddIndex((ushort)(basei + 2));
            }
        }

        /// <summary>
        /// Tesselate a grid of vertices, calculating texture coords, normals, and indices.
        /// </summary>
        public static void Grid(IObject3D outMesh, Float3[,] vertices, Rect texCoordsRegion)
        {
            int width = vertices.GetLength(0), height = vertices.GetLength(1);
            Func<int, int, Float3> positionAt = (int x, int y) =>
            {
                if (x < 0) x = 0;
                if (x >= width) x = width - 1;
                if (y < 0) y = 0;
                if (y >= height) y = height - 1;
                return vertices[x, y];
            };
            Grid(outMesh, new Int2(width, height), texCoordsRegion, positionAt);
        }

        /// <summary>
        /// Tesselate a grid of vertices, calculating texture coords, normals, and indices.
        /// </summary>
        public static void Grid(IObject3D outMesh, Rect3 area, Rect texCoordsRegion, Int2 tessellation)
        {
            Grid(outMesh, tessellation, texCoordsRegion, (int x, int y) => area.GetPositionAt((new Float2(x, y) / (tessellation - 1)).Saturate()));
        }

        public static void Grid(IObject3D outMesh, Int2 tessellation, Rect texCoordsRegion, Func<int, int, Float3> positionAt)
        {
            int baseIndex = outMesh.VertexCount;

            // calculate grid geometry
            Float2x2 texCoordStep = new Float2x2(texCoordsRegion.WidthVector / (tessellation.X - 1), texCoordsRegion.HeightVector / (tessellation.Y - 1));
            for (int y = 0; y < tessellation.Y; y++)
            {
                for (int x = 0; x < tessellation.X; x++)
                {
                    // position
                    outMesh.AddVertex(positionAt(x, y));

                    // tex coord
                    outMesh.AddTexCoord(texCoordsRegion.Position + new Float2(x, y) * texCoordStep);

                    // normal
                    Float3 ddx = positionAt(x + 1, y) - positionAt(x - 1, y);
                    Float3 ddy = positionAt(x, y + 1) - positionAt(x, y - 1);
                    outMesh.AddNormal(ddy.Cross(ddx).Normal());
                }
            }

            // tesselate grid
            AddIndicesToGrid(outMesh, baseIndex, tessellation.X, tessellation.Y);

            outMesh.UpdateGeometry();
        }


        private static void AddIndicesToGrid(IObject3D outMesh, int baseIndex, int width, int height)
        {
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int startIndex = y * width + x + baseIndex;

                    outMesh.AddIndex((ushort)(startIndex));
                    outMesh.AddIndex((ushort)(startIndex + width));
                    outMesh.AddIndex((ushort)(startIndex + width + 1));

                    outMesh.AddIndex((ushort)(startIndex));
                    outMesh.AddIndex((ushort)(startIndex + width + 1));
                    outMesh.AddIndex((ushort)(startIndex + 1));
                }
            }
        }

        public static ushort[] GridIndices(int baseIndex, int width, int height)
        {
            ushort[] indices = new ushort[(width - 1) * (height - 1) * 6];
            FillGridIndices(indices, 0, 0, width, height);
            return indices;
        }

        private static void FillGridIndices(IList<ushort> destBuffer, int destBufferStartIndex, int baseIndex, int width, int height)
        {
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int startIndex = y * width + x + baseIndex;

                    destBuffer[destBufferStartIndex++] = (ushort)(startIndex);
                    destBuffer[destBufferStartIndex++] = (ushort)(startIndex + width);
                    destBuffer[destBufferStartIndex++] = (ushort)(startIndex + width + 1);
                    
                    destBuffer[destBufferStartIndex++] = (ushort)(startIndex);
                    destBuffer[destBufferStartIndex++] = (ushort)(startIndex + width + 1);
                    destBuffer[destBufferStartIndex++] = (ushort)(startIndex + 1);
                }
            }
        }

        public static void Terrain(IObject3D outMesh, Float3 center, float[,] srcHeights, float terrainWidth, float terrainHeight, float texCoordRepetitions)
        {
            IntRect srcRect = new IntRect() { Width = srcHeights.GetLength(0), Height = srcHeights.GetLength(1) };
            Terrain(outMesh, center, srcHeights, srcRect, terrainWidth, terrainHeight, texCoordRepetitions);
        }

        public static void Terrain(IObject3D outMesh, Float3 center, float[,] srcHeights, IntRect srcRect, float terrainWidth, float terrainHeight, float texCoordRepetitions)
        {
            int baseIndex = outMesh.VertexCount;
            Float2 size = new Float2(terrainWidth, terrainHeight);
            Float2 start2d = center.XZ - size / 2.0f;

            int width = srcHeights.GetLength(0), height = srcHeights.GetLength(1);
            Float2 vspacing = size / new Float2(width - 1, height - 1);
            start2d += vspacing * srcRect.Position;

            // calculate vertices
            Float3[,] vertices = new Float3[srcRect.Width, srcRect.Height];
            for (int y = 0; y < srcRect.Height; y++)
            {
                for (int x = 0; x < srcRect.Width; x++)
                {
                    // position
                    Float2 floatIndex = new Float2(x, y);
                    vertices[x, y].XZ = start2d + vspacing * floatIndex;
                    vertices[x, y].Y = srcHeights[x + srcRect.X, y + srcRect.Y];
                }
            }


            Grid(outMesh, vertices, new Rect(Float2.Zero, texCoordRepetitions));
        }

    }

    public interface IObject3D
    {
        void AddVertex(Float3 position);

        void AddNormal(Float3 normal);

        void AddTexCoord(Float2 coords);

        void AddIndex(ushort index);

        int VertexCount { get; }

        void UpdateGeometry();

        void ClearGeometry();
    }

    public class Object3D : IObject3D
    {
        public Object3D()
        {
            IndicesEnabled = true;
            TexCoordsEnabled = true;
            NormalsEnabled = true;
            ClearGeometry();
        }

        public IList<Float3> Vertices { get; private set; }

        public IList<Float3> Normals { get; private set; }

        public IList<Float2> TexCoords { get; private set; }

        public IList<ushort> Indices { get; private set; }

        public int VertexCount
        {
            get
            {
                return Vertices.Count;
            }
        }

        public bool IndicesEnabled { get; set; }

        public bool NormalsEnabled { get; set; }

        public bool TexCoordsEnabled { get; set; }

        public void AddVertex(Float3 position)
        {
            Vertices.Add(position);
        }

        public void AddNormal(Float3 normal)
        {
            if (NormalsEnabled)
                Normals.Add(normal);
        }

        public void AddTexCoord(Float2 coords)
        {
            if (TexCoordsEnabled)
                TexCoords.Add(coords);
        }

        public void AddIndex(ushort index)
        {
            if (IndicesEnabled)
                Indices.Add(index);
        }

        public void UpdateGeometry() { }

        public List<VertexTexNorm> ToMeshVertices()
        {
            List<VertexTexNorm> meshVertices = new List<VertexTexNorm>();
            for (int i = 0; i < VertexCount; i++) 
                meshVertices.Add(new VertexTexNorm(Vertices[i], TexCoords[i], Normals[i]));
            return meshVertices;
        }

        public List<VertexPosition> ToPositionVertices()
        {
            List<VertexPosition> posVertices = new List<VertexPosition>();
            for (int i = 0; i < VertexCount; i++) 
                posVertices.Add(new VertexPosition(Vertices[i]));
            return posVertices;
        }

        public void ClearGeometry()
        {
            Vertices = new List<Float3>();
            Normals = new List<Float3>();
            TexCoords = new List<Float2>();
            Indices = new List<ushort>();
        }
    }

}
