using Dragonfly.Engine.Core;
using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.BaseModule
{
    public class CompCumulativeMouseWheel : Component<float>, ICompUpdatable
    {
        private float cumulativeValue;

        public CompCumulativeMouseWheel(Component parent, float initialValue) : base(parent)
        {
            cumulativeValue = initialValue;
            Multiplier = 1.5f;
        }

        /// <summary>
        /// When the mouse wheel is rotated by one tick, the value will be multiplied by this multiplier when moved forward, and divided when rotated backwards.
        /// </summary>
        public float Multiplier { get; set; }

        public UpdateType NeededUpdates => Context.Input.GetDevice<Mouse>().WheelDelta != 0 ? UpdateType.FrameStart1 : UpdateType.None;

        public void Update(UpdateType updateType)
        {
            int wheelDelta = Context.Input.GetDevice<Mouse>().WheelDelta;
            cumulativeValue *= FMath.Pow(Multiplier, wheelDelta);
        }

        protected override float getValue()
        {
            return cumulativeValue;
        }
    }
}
