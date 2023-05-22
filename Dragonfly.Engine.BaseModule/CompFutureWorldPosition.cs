using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.BaseModule
{

    /// <summary>
    /// Given a world position, try to predict the future value based on past frames.
    /// </summary>
    public class CompFutureWorldPosition : Component<TiledFloat3>
    {
        private CompValueHistory<TiledFloat3> posHistory;

        public CompFutureWorldPosition(Component owner, Component<TiledFloat3> worldPos, float deltaTime) : base(owner)
        {
            posHistory = new CompValueHistory<TiledFloat3>(this, worldPos, 3);
            DiscardChangesAboveTHR = 20.0f;
            DeltaTime = deltaTime;
        }

        /// <summary>
        /// If the change ratio of the position goes above this value, the prediction is considered unreliable and this component just return the current value.
        /// </summary>
        public float DiscardChangesAboveTHR { get; set; }

        /// <summary>
        /// The number of seconds into the future the predicted position should be.
        /// </summary>
        public float DeltaTime { get; private set; }

        protected override TiledFloat3 getValue()
        {
            TiledFloat3 p = posHistory.GetValueAtFrame(0);

            Float3 curSpeed = (p - posHistory.GetValueAtFrame(1)).ToFloat3() / Context.Time.LastFrameDuration;
            Float3 prevSpeed = (posHistory.GetValueAtFrame(1) - posHistory.GetValueAtFrame(2)).ToFloat3() / Context.Time.LastFrameDuration;

            // avoid using abrupt speed changes or camera cut to predict a position.
            if (!(curSpeed.Length / prevSpeed.Length).IsBetween(1.0f / DiscardChangesAboveTHR, DiscardChangesAboveTHR))
                return p;

            TiledFloat3 futureP = new TiledFloat3((p + curSpeed * DeltaTime).ToFloat3(p.Tile), p.Tile);
            return futureP;
        }
    }
}
