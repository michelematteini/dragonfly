using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;

namespace Dragonfly.BaseModule
{
    public class CompTransformMouseLook : CompTransform, ICompUpdatable
    {
        private struct LookAtFrame
        {
            public Float3 Up, Side;
            public Float2 DirAngles;
        }

        private Component<Float3> up;
        private CompTimeSmoothing<LookAtFrame> dirFrame;
        private Action OnInputFocusFunc;
        private float maxVertAngle;
		
		public CompTransformMouseLook(Component parent, Float3 initialDir, Component<Float3> upDirection, float smoothingSeconds) : base(parent)
		{
            up = upDirection;
            LookSpeed = FMath.PI;
            MaxVerticalAgleRadians = (60.0f).ToRadians();
            InitializeAngles(initialDir, smoothingSeconds);
            Direction = new CompFunction<Float3>(this, GetDirection);
            OnInputFocusFunc = OnInputFocus;
        }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        /// <summary>
        /// Get or set an optional external component that provide an aspect ratio used as a correction to the mouse look movement
        /// </summary>
        public Component<float> AspectRatio { get; set; }

        public float LookSpeed { get; set; }

        public float MaxVerticalAgleRadians
        {
            get { return maxVertAngle; }
            set
            {
                maxVertAngle = value.Clamp((1.0f).ToRadians(), (89.9f).ToRadians());
            }
        }

        public void Update(UpdateType updateType)
        {
            GetComponent<CompInputFocus>().RequestInput(InputType.Mouse, OnInputFocusFunc);
        }

        private void OnInputFocus()
        {
            LookAtFrame frame = dirFrame.TargetValue;

            // check if mouse left button is pressed
            Mouse mouse = Context.Input.GetDevice<Mouse>();
            if (!mouse.IsLeftButtonPressed) return;

            Float2 deltaAngles = Float2.Zero;
            deltaAngles.X = mouse.DeltaPosition.Width.Value * (AspectRatio != null ? AspectRatio.GetValue() : 1.0f) * LookSpeed; // horizontal rotation
            deltaAngles.Y = (-mouse.DeltaPosition.Height.Value * LookSpeed); // vertical rotation

            frame.DirAngles += deltaAngles;
            frame.DirAngles.Y = frame.DirAngles.Y.Clamp(-maxVertAngle, maxVertAngle);
            dirFrame.TargetValue = frame;
        }

        public Component<Float3> Direction { get; private set; }

        private static Float3 AnglesToDirection(Float2 angles, Float3 upDir, Float3 sideDir)
        {
            float cx = FMath.Cos(angles.X), sx = FMath.Sin(angles.X);
            float cy = FMath.Cos(angles.Y), sy = FMath.Sin(angles.Y);
            Float3 dsDir = new Float3(cx * cy, sy, sx * cy);
            return dsDir.X * sideDir.Cross(upDir) + dsDir.Y * upDir + dsDir.Z * sideDir;
        }

        private static Float3 AnglesToDirection(LookAtFrame frame)
        {
            return AnglesToDirection(frame.DirAngles, frame.Up, frame.Side);
        }

        private static Float2 DirectionToAngles(Float3 dir, Float3 upDir, Float3 sideDir, float maxVertAngle)
        {
            Float2 newDirAngles = Float2.Zero;

            // calc vertical angle
            newDirAngles.Y = (float)Math.Asin(dir.Dot(upDir));
            newDirAngles.Y = newDirAngles.Y.Clamp(-maxVertAngle, maxVertAngle);
            // calc horizontal angle
            float cxcy = dir.Dot(sideDir.Cross(upDir));
            float sxcy = dir.Dot(sideDir);
            float cy = FMath.Cos(newDirAngles.Y);
            float cx = cxcy / cy, sx = sxcy / cy;
            newDirAngles.X = (float)Math.Atan2(sx, cx);

            return newDirAngles;
        }

        private Float3 GetDirection()
        {
            return AnglesToDirection(dirFrame.GetValue());
        }

        private void InitializeAngles(Float3 dir, float smoothingSeconds)
        {
            LookAtFrame initialFrame;

            dir = dir.Normal();
            initialFrame.Side = up.GetValue().Cross(dir).Normal();
            initialFrame.DirAngles = DirectionToAngles(dir, up.GetValue(), initialFrame.Side, maxVertAngle);
            initialFrame.Up = up.GetValue();

            // wrap look at frame in a time smooth component
            dirFrame = new CompTimeSmoothing<LookAtFrame>(this, smoothingSeconds, initialFrame, FrameLerp);
        }

        private static LookAtFrame FrameLerp(LookAtFrame f1, LookAtFrame f2, float amount)
        {
            f2.DirAngles = f1.DirAngles.Lerp(f2.DirAngles, amount);
            return f2;
        }

        public override TiledFloat4x4 GetLocalTransform()
        {        
            // when up dir changes: update lookat frames to the new up direction, converting angles from the previous frames
            if (up.ValueChanged)
            {
                LookAtFrame frame = dirFrame.GetValue();
                LookAtFrame targetFrame = dirFrame.TargetValue;
                // calc current direction in the previos reference frame
                Float3 targetDir = AnglesToDirection(targetFrame); // calc direction, but in previous reference frame
                Float3 curDir = AnglesToDirection(frame); // calc direction, but in previous reference frame
                // create a new frame with the new up dir
                LookAtFrame newFrame = new LookAtFrame();
                newFrame.Up = up.GetValue();
                newFrame.Side = (frame.Side - frame.Side.Dot(newFrame.Up) * newFrame.Up).Normal(); // re-orthogonalize side direction
                // update current frame
                newFrame.DirAngles = DirectionToAngles(curDir, newFrame.Up, newFrame.Side, maxVertAngle);
                dirFrame.OverrideCurrentValue(newFrame);
                // update target frame
                Float2 newAngles = DirectionToAngles(targetDir, newFrame.Up, newFrame.Side, maxVertAngle);
                newAngles.X += FMath.Round((newFrame.DirAngles.X - newAngles.X) / FMath.TWO_PI) * FMath.TWO_PI; // align equivalent angles to avoid interpolanting in the wrong direction
                newFrame.DirAngles = newAngles;
                dirFrame.TargetValue = newFrame;
            }

            return Float4x4.LookAt(Float3.Zero, Direction.GetValue(), up.GetValue());
        }


    }

}