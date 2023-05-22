using Dragonfly.Graphics.Math;

namespace Dragonfly.Graphics.Resources
{
    public abstract class GraphicSurface : GraphicResource
    {
        internal GraphicSurface(GraphicResourceID id) : base(id)
        {
        }


        public abstract SurfaceFormat Format { get; protected set; }

        public abstract int Width { get; protected set; }

        public abstract int Height { get; protected set; }

        public Int2 Resolution { get { return new Int2(Width, Height); } }
    }
}
