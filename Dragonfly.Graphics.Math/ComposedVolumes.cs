using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Math
{
    /// <summary>
    /// Describes a volume made by a boolean union of two others.
    /// </summary>
    public class VolumeUnion : IVolume
    {
        private IVolume v1, v2;

        public VolumeUnion(IVolume v1, IVolume v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }

        public bool Contains(Float3 point)
        {
            return v1.Contains(point) || v2.Contains(point);
        }

        public bool Contains(Sphere s)
        {
            return v1.Contains(s) || v2.Contains(s);
        }

        public bool Contains(AABox b)
        {
            return v1.Contains(b) || v2.Contains(b);
        }

        public bool Intersects(Sphere s)
        {
            return v1.Intersects(s) || v2.Intersects(s);
        }

        public bool Intersects(AABox b)
        {
            return v1.Intersects(b) || v2.Intersects(b);
        }
    }

    /// <summary>
    /// Describes a volume made by a boolean difference of two others.
    /// </summary>
    public class VolumeDifference : IVolume
    {
        private IVolume v1, v2;

        public VolumeDifference(IVolume v1, IVolume v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }

        public bool Contains(Float3 point)
        {
            return v1.Contains(point) && !v2.Contains(point);
        }

        public bool Contains(Sphere s)
        {
            return v1.Contains(s) && !v2.Intersects(s);
        }

        public bool Contains(AABox b)
        {
            return v1.Contains(b) && !v2.Intersects(b);
        }

        public bool Intersects(Sphere s)
        {
            return v1.Intersects(s) && !v2.Contains(s);
        }

        public bool Intersects(AABox b)
        {
            return v1.Intersects(b) && !v2.Contains(b);
        }
    }

}
