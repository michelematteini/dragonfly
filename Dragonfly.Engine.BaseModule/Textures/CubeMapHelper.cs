using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    public static class CubeMapHelper
    {
        public static readonly Float3[] FaceNormals = new Float3[] { Float3.UnitX, Float3.UnitY, Float3.UnitZ, -Float3.UnitX, -Float3.UnitY, -Float3.UnitZ };
        public static readonly Float3[] FaceRightVector = new Float3[] { Float3.UnitZ, -Float3.UnitX, -Float3.UnitX, -Float3.UnitZ, -Float3.UnitX, Float3.UnitX };
        public static readonly Float3[] FaceUpVector = new Float3[] { Float3.UnitY, -Float3.UnitZ, Float3.UnitY, Float3.UnitY, Float3.UnitZ, Float3.UnitY };

        /// <summary>
        /// Generates a set of cameras attached to a transform component that look out from the faces of a cube.
        /// </summary>
        public static void CreateFaceCameras(Component parentComponent, IList<CompCamera> outCameras, IList<CompTransformStack> outCameraTransforms)
        {
            for (int i = 0; i < FaceNormals.Length; i++)
            {
                outCameraTransforms[i] = new CompTransformStack(parentComponent);
                CompCamPerspective faceCamera = new CompCamPerspective(outCameraTransforms[i]);
                faceCamera.AutoAspectRatio = false;
                faceCamera.AspectRatio.Set(1.0f);
                float fovPadding = 0.1f; // 1% margin to avoid edges in cubemaps
                faceCamera.FOV.Set(FMath.PI_OVER_2 * (1.0f + fovPadding));
                outCameras[i] = faceCamera;
            }
        }

        /// <summary>
        /// Update the view transform of cameras created with CreateFaceCameras().
        /// </summary>
        /// <param name="outCameraTransforms">The transformation components previously created.</param>
        /// <param name="cubeCenter">The position of the center of the cube in which the face cameras should be located.</param>
        public static void SetFaceCamerasPosition(IList<CompTransformStack> outCameraTransforms, Float3 cubeCenter)
        {
            for (int i = 0; i < FaceNormals.Length; i++)
            {
                outCameraTransforms[i].Set(Float4x4.LookAt(cubeCenter, FaceNormals[i], FaceUpVector[i]));
            }
        }

        /// <summary>
        /// Validate the specified mipmap count for a cubemap which require a pixel-wide edge on each face.
        /// </summary>
        /// <param name="faceResolution">The resolution of the cubemap face edge.</param>
        /// <param name="preferredMipmapCount">The wanted mipamap count, that will be clamped to a valid value.</param>
        public static int ValidateMipmapCount(int faceResolution, int preferredMipmapCount)
        {
            int maxMipmapCount = Math.Max(0, faceResolution.CeilLog2() - 2);  // 4x4 faces is the smallest that accomodate an edge for sampling
            return preferredMipmapCount.Clamp(0, maxMipmapCount);
        }
            
    }
}
