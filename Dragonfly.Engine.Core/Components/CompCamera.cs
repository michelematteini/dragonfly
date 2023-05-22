using Dragonfly.Graphics.Math;
using System;
using System.Runtime.CompilerServices;

namespace Dragonfly.Engine.Core
{
    public abstract class CompCamera : Component<Float4x4>
    {
        private struct CameraCache
        {
            public TiledFloat3 Position;
            public Float3 Direction, Up;
            public ViewFrustum Volume;
        }

        private class CompCameraCache : Component<CameraCache>
        {
            private Component<Float4x4> camera;

            public CompCameraCache(Component<Float4x4> camera) : base(camera)
            {
                this.camera = camera;
            }

            protected override CameraCache getValue()
            {
                TiledFloat4x4 transform = GetTransform();
                return new CameraCache() {
                    Direction = transform.Value.GetZDirection(),
                    Position = new TiledFloat3(transform.Value.Origin, transform.Tile),
                    Volume = new ViewFrustum(transform.Value * camera.GetValue()),
                    Up = transform.Value.GetYDirection()
                };
            }
        }

        private class DefaultVolume : IVolume
        {
            public ViewFrustum ViewFrustum;

            public bool Contains(Float3 point)
            {
                return ViewFrustum.Contains(point);
            }

            public bool Contains(Sphere s)
            {
                return ViewFrustum.Contains(s);
            }

            public bool Contains(AABox b)
            {
                return ViewFrustum.Contains(b);
            }

            public bool Intersects(Sphere s)
            {
                return ViewFrustum.Intersects(s);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Intersects(AABox b)
            {
                return ViewFrustum.Intersects(b);
            }
        }

        private CompCameraCache cameraCache;
        private DefaultVolume defaultVolume;

        protected CompCamera(Component owner) : base(owner)
        {
            cameraCache = new CompCameraCache(this);
            Viewport = AARect.Bounding(Float2.Zero, Float2.One);
            defaultVolume = new DefaultVolume();
            StatsLock = new object();
            StatsFrameID = -1;
        }

        /// <summary>
        /// Returns the position of this camera relative to the current world tile, based on this component transformation.
        /// </summary>
        public Float3 LocalPosition => cameraCache.GetValue().Position.Value;

        /// <summary>
        /// Returns the world position of this camera, based on this component transformation.
        /// </summary>
        public TiledFloat3 Position => cameraCache.GetValue().Position;

        /// <summary>
        /// Returns the direction of this camera based on this component transformation.
        /// </summary>
        public Float3 Direction => cameraCache.GetValue().Direction;

        /// <summary>
        /// Returns the up direction for this camera based on this component transformation.
        /// </summary>
        public Float3 UpDirection => cameraCache.GetValue().Up;

        /// <summary>
        /// The portion of the target that will be used to render with this camera, specified in texture coords ([0; 1] range)
        /// </summary>
        public AARect Viewport { get; set; }

        /// <summary>
        /// Render stats for this camera on the last frame.
        /// </summary>
        public RenderStats Stats { get; internal set; }

        internal object StatsLock;

        internal int StatsFrameID;

        /// <summary>
        /// Returns the volume of this camera, based by default on this component view frustum.
        /// Used by the engine to test for visibility.
        /// </summary>
        public virtual IVolume Volume
        {
            get
            {
                defaultVolume.ViewFrustum = ViewFrustum;
                return defaultVolume;
            }
        }

        /// <summary>
        /// Returns the view frustum of this camera relative to the current world tile, based on this component transformation.
        /// </summary>
        public ViewFrustum ViewFrustum => cameraCache.GetValue().Volume;

    }
}
