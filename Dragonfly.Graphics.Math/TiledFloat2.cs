
namespace Dragonfly.Graphics.Math
{
    public struct TiledFloat2
    {
        public TiledFloat X, Y;

        public TiledFloat2(Float2 value, Int2 tile)
        {
            X = new TiledFloat() { Value = value.X, Tile = tile.X };
            Y = new TiledFloat() { Value = value.Y, Tile = tile.Y };
        }

    }
}
