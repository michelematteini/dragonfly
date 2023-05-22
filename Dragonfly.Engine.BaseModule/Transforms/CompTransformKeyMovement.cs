using System;
using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using Dragonfly.Utils;

namespace Dragonfly.BaseModule
{
    public class CompTransformKeyMovement : CompTransform, ICompUpdatable
    {
        private Component<Float3> up;
        private CompTimeSmoothing<TiledFloat3> position;
	
		public CompTransformKeyMovement(Component parent, TiledFloat3 initialPosition, Component<Float3> upDirection, float smoothingSecods) : base(parent)		
		{
            SpeedMps = new CompValue<float>(this, 15.0f);
            Direction = new CompValue<Float3>(this, Float3.UnitZ);
            position = new CompTimeSmoothing<TiledFloat3>(this, smoothingSecods, initialPosition, TiledFloat3.Lerp);
            up = upDirection;

			ForwardKey = VKey.K_W;
			BackwardKey = VKey.K_S;
			LeftKey = VKey.K_A;
			RightKey = VKey.K_D;
            FastMovementModfierKey = VKey.VK_SHIFT;
            FastMovementSpeedMul = 10.0f;
            MaxFrameTimeSeconds = 1.0f;
		}

        /// <summary>
        /// Input component used to specify the movement forward direction
        /// </summary>
        public CompValue<Float3> Direction { get; private set; }

        /// <summary>
        /// Returns the position tracked by this component.
        /// </summary>
        public Component<TiledFloat3> Position
        {
            get
            {
                return position;
            }
        }

        public CompValue<float> SpeedMps { get; private set; }

		public VKey ForwardKey { get; set; }
		
		public VKey BackwardKey { get; set; }
		
		public VKey LeftKey { get; set; }
		
		public VKey RightKey { get; set; }

        public VKey FastMovementModfierKey { get; set; }

        public float FastMovementSpeedMul { get; set; }

        public UpdateType NeededUpdates => UpdateType.FrameStart1;

        /// <summary>
        /// If a frame duretion exceed this value, its contribution to the movement is rejected, to avoid ghosting / jumps.
        /// </summary>
        public float MaxFrameTimeSeconds { get; set; }

        public void Update(UpdateType updateType)
        {
            if (Context.Time.LastFrameDuration > MaxFrameTimeSeconds)
                return; // reject input lag

            Keyboard keyboard = Context.Input.GetDevice<Keyboard>();
            Float3 camDir = Direction.GetValue();
            Float3 sideDir = up.GetValue().Cross(camDir).Normal();

            float kf = keyboard.IsKeyDown(ForwardKey).ToFloat(), kb = keyboard.IsKeyDown(BackwardKey).ToFloat();
            float kl = keyboard.IsKeyDown(LeftKey).ToFloat(), kr = keyboard.IsKeyDown(RightKey).ToFloat();

            Float3 moveDir = camDir * (kf - kb) + sideDir * (kr - kl);
            float moveDist = (Math.Abs(kf - kb) + Math.Abs(kr - kl)).Saturate() * SpeedMps.GetValue() * Context.Time.LastFrameDuration;
            if (keyboard.IsKeyDown(FastMovementModfierKey))
                moveDist *= FastMovementSpeedMul;

            position.TargetValue += moveDir.Normal() * moveDist;
        }


        public override TiledFloat4x4 GetLocalTransform()
        {
            return TiledFloat4x4.Translation(position.GetValue());
        }
    }
}