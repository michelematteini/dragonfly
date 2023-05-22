using System;
using System.Collections.Generic;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;


namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A camera used to rendering view frustum slices during shadowmaps rendering.
    /// </summary>
    internal class CompCamCascade : CompCamera
    {
        private Component<CameraState> croppingLogic;
        private float prevBoundingSphereRadius; // radius of the last calculated bounding sphere, used to stabilize a slice.
        private Float3[] frustumCorners;
        private bool explicitBoundingSphere; // if true, the slice bounding sphere is specified by the user, and do not need to be calculated
        private Sphere boundingSphere;
        private CompTransformStack sliceTransform; // the view part of the slice projection

        /// <summary>
        /// Create a new view frustum rendering camera.
        /// </summary>
        /// <param name="parentTransform">A transformation node used as parent, this will be automatically updated by this camera to create the final slice projection.</param>
        public CompCamCascade(CompTransformStack parentTransform) : base(parentTransform)
        {
            sliceTransform = parentTransform;
            frustumCorners = new Float3[8];
            Viewport = AARect.Bounding(Float2.Zero, Float2.One);
            LightDirection = new CompValue<Float3>(this, -Float3.UnitZ);
            DistanceFromPoints = new CompValue<float>(this, 1000.0f);
            croppingLogic = new CompFunction<CameraState>(this, CalcViewSpaceCrop);
        }

        public CompValue<Float3> LightDirection { get; private set; }

        public CompValue<float> DistanceFromPoints { get; private set; }

        public CompCamCascade PreviousSlice { get; set; }

        /// <summary>
        /// Direction of the viewer, along which this frustum slice is cut
        /// </summary>
        public Float3 ViewDirection { get; set; }

        /// <summary>
        /// The world tile where the viewer is currently located.
        /// </summary>
        public Int3 ViewTile { get; set; }

        /// <summary>
        /// If set to a positive value, the camera will move in steps of WorldCropSize / SnappingResolution to avoid flickering.
        /// </summary>
        public int SnappingResolution { get; set; }

        /// <summary>
        /// The egde size for the current slice in meters.
        /// </summary>
        public float SliceSize
        {
            get { return croppingLogic.GetValue().WorldCropSize; }
        }

        public void UpdateView(ViewFrustum viewFrustum, float fromDepth, float toDepth)
        {
            // calc split points for this slice
            viewFrustum.GetScreenCornersAt(fromDepth, frustumCorners, 0);
            viewFrustum.GetScreenCornersAt(toDepth, frustumCorners, 4);
            explicitBoundingSphere = false;
            UpdateSliceTransform();
        }

        public void UpdateView(Sphere boundingSphere)
        {
            this.boundingSphere = boundingSphere;
            explicitBoundingSphere = true;
            UpdateSliceTransform();
        }

        private void UpdateSliceTransform()
        {
            sliceTransform.Set(new TiledFloat4x4() { Tile = ViewTile, Value = croppingLogic.GetValue().ViewMatrix });
        }

        private CameraState CalcViewSpaceCrop()
        {
            CameraState s;

            // calc camera axes
            s.Up = Float3.NotParallelAxis(LightDirection.GetValue(), Float3.UnitY);
            Float3 ZAxis = LightDirection.GetValue().Normal();

            if (explicitBoundingSphere)
            {
                s.BoundingSphere = boundingSphere;
            }
            else
            {
                // calc a transform stable bounding sphere for the included points
                s.BoundingSphere = Sphere.Bounding(frustumCorners);
                s.BoundingSphere.Radius *= 1.001f; // add a little play to avoid border and precision issues and allow the frustum to be moved back

                // sphere radius stabilization
                {
                    float rateOfChange = (s.BoundingSphere.Radius - prevBoundingSphereRadius) / s.BoundingSphere.Radius;
                    if (rateOfChange.IsBetween(-0.1f, 0.0f)) // only stabilize small negative changes
                    {
                        s.BoundingSphere.Radius = prevBoundingSphereRadius;
                    }
                    prevBoundingSphereRadius = s.BoundingSphere.Radius;
                }
            }
            
            // calc projection box from the bounding sphere
            s.Target = s.BoundingSphere.Center + ZAxis * s.BoundingSphere.Radius;
            s.WorldCropSize = 2.0f * s.BoundingSphere.Radius;
            s.Position = s.Target - ZAxis * System.Math.Max(2.0f * s.BoundingSphere.Radius, DistanceFromPoints.GetValue());
            s.ViewMatrix = Float4x4.LookAt(s.Position, LightDirection.GetValue(), s.Up);
            s.Projection = Float4x4.Orthographic(s.WorldCropSize, s.WorldCropSize, 0, (s.Target - s.Position).Length);
            

            if (!explicitBoundingSphere)
            {
                //try to move the split box as forward as possible
                Float3 forwardShift = Float3.Zero;
                {
                    Float4x4 cameraMatrix = s.ViewMatrix * s.Projection;
                    ViewFrustum splitFrustum = new ViewFrustum(cameraMatrix);
                    Float3 moveStep = ViewDirection * s.BoundingSphere.Radius * 0.5f;
                    for (int i = 0; i < 5; i++, moveStep *= 0.5f)
                    {
                        bool stillContained = true;
                        for (int pi = 0; pi < frustumCorners.Length && stillContained; pi++)
                            stillContained = splitFrustum.Contains(frustumCorners[pi] - forwardShift - moveStep);

                        if (stillContained)
                            forwardShift += moveStep;
                    }
                }

                // adjust split using the found shift
                s.Target += forwardShift;
                s.Position += forwardShift;

                // move the shadow camera position out of colliders
                foreach (IComponent<ShadowCameraCollider> compShadowCollider in GetComponents<IComponent<ShadowCameraCollider>>())
                {
                    Sphere collider = compShadowCollider.GetValue().ToSphere(ViewTile);
                    if (collider.Contains(s.Position))
                    {
                        // use the intersection point with the collider as the new camera position
                        FMath.RaySphereIntersection(s.Position, -ZAxis, collider.Center, collider.Radius, out _, out s.Position);
                    }
                }

                // calc split view and projection 
                s.ViewMatrix = Float4x4.LookAt(s.Position, LightDirection.GetValue(), s.Up);
                s.Projection = Float4x4.Orthographic(s.WorldCropSize, s.WorldCropSize, 0, (s.Target - s.Position).Length);
            }

            // snap the camera to its texel grid, to avoid shadowmap texels creating flickering shadows
            if (SnappingResolution > 0)
            {
                Float4x4 cameraMatrix = s.ViewMatrix * s.Projection;
                Float3 snapOffset = new Float3(cameraMatrix.Position.XY % (2.0f / SnappingResolution), 0);
                s.Projection *= Float4x4.Translation(-snapOffset);
            }

            return s;
        }

        protected override Float4x4 getValue()
        {
            CameraState s = croppingLogic.GetValue();
            return s.Projection;
        }

    }

    internal struct CameraState
    {
        public Float3 Up, Target, Position;
        public float WorldCropSize;
        public Float4x4 Projection;
        public Float4x4 ViewMatrix;
        public Sphere BoundingSphere;
    }

}
