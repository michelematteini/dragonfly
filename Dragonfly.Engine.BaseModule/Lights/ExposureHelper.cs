using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.BaseModule
{
    public static class ExposureHelper
    {
        /// <summary>
        /// Returns a lux value that will be correctly exposed at the specified EV.
        /// </summary>
        public static float EVToLux(float ev)
        {
            return FMath.Pow(2.0f, ev);
        }

        public static float LuxToEV(float lux)
        {
            return FMath.Log2(lux);
        }

        public const float LuxInDirectSunlight = 100000.0f;
        public const float LuxAtSunset = 30000.0f;

        public const float EVSunny = 15.0f;
        public const float EVPartlyCloudy = 14.0f;
        public const float EVClody = 12.5f;
        public const float EVSunset = 11.5f;

    }
}
