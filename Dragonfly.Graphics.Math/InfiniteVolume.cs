using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Math
{
    public class InfiniteVolume : IVolume
    {
        public InfiniteVolume()
        {

        }

        public bool Contains(Float3 point)
        {
            return true;
        }

        public bool Contains(Sphere s)
        {
            return true;
        }

        public bool Contains(AABox b)
        {
            return true;
        }

        public bool Intersects(Sphere s)
        {
            return true;
        }

        public bool Intersects(AABox b)
        {
            return true;
        }
    }
}
