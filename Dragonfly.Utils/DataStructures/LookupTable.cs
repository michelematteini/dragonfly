using System;

namespace Dragonfly.Utils
{
    public class LookupTable<T>
    {
        private int width;
        private int height;
        private Func<T, T, float, T> lerp;
        private int xmax, ymax; // cached max index values

        public LookupTable(int width, int height, Func<T, T, float, T> lerpFunction)
        {
            this.width = width;
            this.height = height;
            this.lerp = lerpFunction;
            xmax = width - 1;
            ymax = height - 1;
            Buffer = new T[width * height];
        }
        public T[] Buffer { get; private set; }

        public T this[int x, int y]
        {
            get
            {
                x = x < 0 ? 0 : (x > xmax ? xmax : x);
                y = y < 0 ? 0 : (y > ymax ? ymax : y);
                int i = x + width * y;
                return Buffer[i];
            }
            set
            {
                x = x < 0 ? 0 : (x > xmax ? xmax : x);
                y = y < 0 ? 0 : (y > ymax ? ymax : y);
                int i = x + width * y;
                Buffer[i] = value;
            }
        }

        /// <summary>
        /// Samples this LUT with x, y coordinates between 0 and 1.
        /// </summary>
        public T SampleBilinear(float xCoord, float yCoord)
        {
            int x0 = (int)(xCoord * xmax);
            int y0 = (int)(yCoord * ymax);
            float xa = xCoord * xmax - x0;
            float ya = yCoord * ymax - y0;

            T y0Value = lerp(this[x0, y0], this[x0 + 1, y0], xa);
            T y1Value = lerp(this[x0, y0 + 1], this[x0 + 1, y0 + 1], xa);
            return lerp(y0Value, y1Value, ya);
        }
    }
}
