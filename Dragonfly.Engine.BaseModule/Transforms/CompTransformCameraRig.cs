using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    public class CompTransformCameraRig : CompTransform, ICompUpdatable
    {
        private enum CutType
        {
            StaticShot, // still camera
            PanShot, // camera keep direction but follow a path
            TrackingShot, // camera keep direction and position aligned to a path
            ArcShort, // fixed target and moving camera
            DynamicShot // a path for both position and target
        }

        private struct Cut
        {
            public CutType Type;
            public Path3D CamPositionPath;
            public Path3D CamTargetPath;
            public Float3 CamFixedPos, CamFixedDir, CamFixedTarget;
            public float ShotSeconds;
            public float Speed, TargetSpeed;
        }

        private List<Cut> cuts;
        private int displayedCut;
        private CompTimeSeconds timeline;

        // current transform
        private CompValue<Float3> curPosition, curDirection;
        private bool curDirectionIsTarget;

        public CompTransformCameraRig(Component owner, CompTimeSeconds timeline) : base(owner)
        {
            cuts = new List<Cut>();
            displayedCut = -1;
            this.timeline = timeline;
            curPosition = new CompValue<Float3>(this, Float3.Zero);
            curDirection = new CompValue<Float3>(this, Float3.UnitZ);
            UpVector = new CompValue<Float3>(this, Float3.UnitY);
        }

        public UpdateType NeededUpdates { get { return (cuts.Count > 0) ? UpdateType.FrameStart1 : UpdateType.None; } }

        public CompValue<Float3> UpVector { get; private set; }

        public void AddStaticShot(float durationSeconds, Float3 position, Float3 target)
        {
            Cut c = new Cut();
            c.Type = CutType.StaticShot;
            c.CamFixedPos = position;
            c.CamFixedTarget = target;
            c.ShotSeconds = durationSeconds;
            cuts.Add(c);
        }

        public void AddPanShot(float durationSeconds, Path3D track, float panningSpeed, Float3 cameraDir)
        {
            Cut c = new Cut();
            c.Type = CutType.PanShot;
            c.CamPositionPath = track;
            c.CamFixedDir = cameraDir;
            c.ShotSeconds = durationSeconds;
            c.Speed = panningSpeed;
            cuts.Add(c);
        }

        public void AddTrackingShot(float durationSeconds, Path3D track, float trackingSpeed)
        {
            Cut c = new Cut();
            c.Type = CutType.TrackingShot;
            c.CamPositionPath = track;
            c.ShotSeconds = durationSeconds;
            c.Speed = trackingSpeed;
            cuts.Add(c);
        }

        public void AddArcShort(float durationSeconds, Path3D track, float cameraSpeed,  Float3 target)
        {
            Cut c = new Cut();
            c.Type = CutType.ArcShort;
            c.CamPositionPath = track;
            c.CamFixedTarget = target;
            c.ShotSeconds = durationSeconds;
            c.Speed = cameraSpeed;
            cuts.Add(c);
        }

        public void AddDynamicShot(float durationSeconds, Path3D track, float cameraSpeed, Path3D movingTarget, float targetSpeed)
        {
            Cut c = new Cut();
            c.Type = CutType.DynamicShot;
            c.CamPositionPath = track;
            c.CamTargetPath = movingTarget;
            c.ShotSeconds = durationSeconds;
            c.Speed = cameraSpeed;
            c.TargetSpeed = targetSpeed;
            cuts.Add(c);
        }

        public void Update(UpdateType updateType)
        {      
            // retrieve current cut info
            Cut c; // the current cut
            float relativeTime = timeline.GetValue(); // time relative to the start of the current cut
            PreciseFloat activationTime; // seconds from start at which the current cut started
            {
                int cutIndex = 0;
                while (relativeTime > cuts[cutIndex].ShotSeconds && cutIndex < cuts.Count - 1)
                {
                    relativeTime -= cuts[cutIndex].ShotSeconds;
                    cutIndex++;
                }

                if (cutIndex == displayedCut)
                    return; // already displaying the correct cut
                displayedCut = cutIndex;

                c = cuts[cutIndex];
                activationTime = Context.Time.SecondsFromStart - relativeTime;
            }

            // switch the components controllig the transformation, based on the current cut
            switch (c.Type)
            {
                case CutType.StaticShot:
                    curPosition.Set(c.CamFixedPos);
                    curDirection.Set(c.CamFixedTarget);
                    curDirectionIsTarget = true;
                    break;

                case CutType.PanShot:
                    curPosition.Set(new CompPathWalker(this, c.CamPositionPath, c.Speed, activationTime));
                    curDirection.Set(c.CamFixedDir);
                    curDirectionIsTarget = false;
                    break;

                case CutType.TrackingShot:
                    curPosition.Set(new CompPathWalker(this, c.CamPositionPath, c.Speed, activationTime));
                    curDirection.Set(new CompPathWalker(this, c.CamPositionPath, c.Speed, activationTime).Tangent());
                    curDirectionIsTarget = false;
                    break;

                case CutType.ArcShort:
                    curPosition.Set(new CompPathWalker(this, c.CamPositionPath, c.Speed, activationTime));
                    curDirection.Set(c.CamFixedTarget);  
                    curDirectionIsTarget = true;
                    break;

                case CutType.DynamicShot:
                    curPosition.Set(new CompPathWalker(this, c.CamPositionPath, c.Speed, activationTime));
                    curDirection.Set(new CompPathWalker(this, c.CamTargetPath, c.TargetSpeed, activationTime));
                    curDirectionIsTarget = true;
                    break;
            }
          
        }

        public override TiledFloat4x4 GetLocalTransform()
        {
            Float3 dir = curDirectionIsTarget ? curDirection.GetValue() - curPosition.GetValue() : curDirection.GetValue();
            return Float4x4.LookAt(curPosition.GetValue(), dir, UpVector.GetValue());
        }

    }



}
