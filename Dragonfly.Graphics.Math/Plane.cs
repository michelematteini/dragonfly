using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Math
{
    /// <summary>
    /// Helper class that implements 3d plane math in homogeneous coordinates.
    /// </summary>
    public static class Plane
    {
        /// <summary>
        /// Returns the plane including the specified position with the specified normal orientation.
        /// </summary>
        public static Float4 FromNormal(Float3 position, Float3 normal)
        {
            return new Float4(normal, -position.Dot(normal));
        }

        /// <summary>
        /// Returns the plane that includes the specified positions.
        /// </summary>
        public static Float4 FromPoints(Float3 p1, Float3 p2, Float3 p3)
        {
            return FromNormal(p1, (p2 - p1).Cross(p3 - p1).Normal());
        }

    }
}
