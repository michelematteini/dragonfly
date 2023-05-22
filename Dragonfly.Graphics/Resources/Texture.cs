using Dragonfly.Graphics.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Graphics.Resources
{
    public abstract class Texture : GraphicSurface
    {
        protected internal Texture(GraphicResourceID resID, int width, int height, SurfaceFormat format)
            : base(resID)
        {
            this.Width = width;
            this.Height = height;
            this.Format = format;
        }

        public override int Width { get; protected set; }

        public override int Height { get; protected set; }

        public override SurfaceFormat Format { get; protected set; }

        public abstract void SetData<T>(T[] srcBuffer) where T : struct;
    }
}
