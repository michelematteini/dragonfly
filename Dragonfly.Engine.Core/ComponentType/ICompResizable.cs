
namespace Dragonfly.Engine.Core
{
    public interface ICompResizable : IComponent
    {
        void ScreenResized(int width, int height);
    }
}
