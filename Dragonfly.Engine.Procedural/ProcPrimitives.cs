using Dragonfly.BaseModule;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.Engine.Procedural
{
    internal static class ProcPrimitives
    {
        public static void ShapedPlane(IObject3D outMesh, Func<Float2, Float3> surfaceShape, Rect texCoords, Int2 tesselation, Float4x4 transform)
        {
            int baseIndex = outMesh.VertexCount;

            Float2 oneOverTess = 1.0f / (Float2)tesselation;
            Float3[,] vertices = new Float3[tesselation.X + 1, tesselation.Y + 1];
            for (Int2 index = Int2.Zero; index.Y < vertices.GetLength(1); index.Y++)
            {
                for (index.X = 0; index.X < vertices.GetLength(0); index.X++)
                {
                    Float2 surfaceCoords= index * oneOverTess;
                    vertices[index.X, index.Y] = surfaceShape(surfaceCoords) * transform;
                }
            }

            Primitives.Grid(outMesh, vertices, texCoords);
        }

    }

    internal static class SurfaceShapes
    {
        public static Float3 Flat(Float2 coords)
        {
            return new Float3(coords.X, 0, coords.Y);
        }

        public static Float3 QuarterSphere(Float2 coords)
        {
            coords.X = 2.0f * (coords.X - 0.5f);
            float angle = coords.Length * FMath.PI_OVER_2;

            Float3 sphereCoords = new Float3();
            sphereCoords.XZ = coords.Normal() * FMath.Sin(angle);
            sphereCoords.Y = FMath.Cos(angle) - 1.0f;
            return sphereCoords;
        }

        public static Func<Float2, Float3> AddNoise(Func<Float2, Float3> shape, float noiseAmplitude, FRandom rnd)
        {
            return (Float2 coords) => shape(coords) + rnd.NextSignedFloat() * noiseAmplitude;
        }

        public static Func<Float2, Float3> Lerp(Func<Float2, Float3> shape1, Func<Float2, Float3> shape2, float ammount)
        {
            return (Float2 coords) => shape1(coords).Lerp(shape2(coords), ammount);          
        }

    }

}
