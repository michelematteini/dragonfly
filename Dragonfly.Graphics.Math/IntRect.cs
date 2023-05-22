using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Math
{
    public struct IntRect
    {
        public int X, Y;
        public int Width, Height;

        public Float2 Position
        {
            get
            {
                return new Float2(X, Y);
            }
        }
    }
}
