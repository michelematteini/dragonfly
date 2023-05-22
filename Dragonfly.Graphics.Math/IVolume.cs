
namespace Dragonfly.Graphics.Math
{
    public interface IVolume
    {
        bool Contains(Float3 point);

        bool Contains(Sphere s);

        bool Contains(AABox b);

        bool Intersects(Sphere s);

        bool Intersects(AABox b);
    }
}
