using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Math
{
    public static class GraphicInt
    {
        public static float ToSatFloat(this int value)
        {
            return value < 0 ? 0 : (value > 255 ? 1 : (float)value / 255f);
        }

        public static int ToInt(this bool value)
        {
            return value ? 1 : 0;
        }

        /// <summary>
        /// Round up to the nearest power of 2.
        /// </summary>
        public static int CeilPower2(this int value)
        {
            int ceil = 1;
            while (ceil < value)
                ceil *= 2;

            return ceil;
        }

        /// <summary>
        /// Round to the nearest power of 2.
        /// </summary>
        public static int RoundPower2(this int value)
        {
            int ceil2 = value.CeilPower2();
            int floor2 = ceil2 / 2;
            return (ceil2 - value) < (value - floor2) ? ceil2 : floor2;
        }

        /// <summary>
        /// Round down to the nearest power of 2.
        /// </summary>
        public static int FloorPower2(this int value)
        {
            int floor = 1;
            while (floor * 2 <= value)
                floor *= 2;

            return floor;
        }

        /// <summary>
        /// Round up to the nearest power of 2.
        /// </summary>
        public static int CeilPower2(this float value)
        {
            return CeilPower2((int)value);
        }

        /// <summary>
        /// Round to the nearest power of 2.
        /// </summary>
        public static int RoundPower2(this float value)
        {
            return RoundPower2((int)value);
        }

        /// <summary>
        /// Round down to the nearest power of 2.
        /// </summary>
        public static int FloorPower2(this float value)
        {
            return FloorPower2((int)value);
        }

        /// <summary>
        /// Returns true if the value if a power of 2
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsPowerOf2(this int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }


        /// <summary>
        /// Returns the Log2 of the value, rounded up.
        /// </summary>
        public static int CeilLog2(this int value)
        {
            int log = 0;
            for (int exp = 1; exp < value; exp *= 2, log++) ;
            return log;
        }

        public static int Exp2(this int value)
        {
            int exp = 1;
            for (int pow = 0; pow < value; pow++, exp *= 2) ;
            return exp;
        }


        public static int Clamp(this int value, int min, int max)
        {
            return System.Math.Min(System.Math.Max(min, value), max);
        }

        public static bool IsEven(this int value)
        {
            return (value & 0x01) == 0;
        }
    }
}
