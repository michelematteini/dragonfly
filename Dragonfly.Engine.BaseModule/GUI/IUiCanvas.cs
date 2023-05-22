using Dragonfly.Graphics.Math;

namespace Dragonfly.BaseModule
{
    public interface IUiCanvas
    {
        /// <summary>
        /// Return the size of this canvas in pixels
        /// </summary>
        Int2 PixelSize { get; }

        /// <summary>
        /// Class name of the pass in which the UI will be rendered
        /// </summary>
        string MaterialClass { get; }

        /// <summary>
        /// Cooardinate reference on the surface of this canvas
        /// </summary>
        CoordContext Coords { get; }
    }
}
