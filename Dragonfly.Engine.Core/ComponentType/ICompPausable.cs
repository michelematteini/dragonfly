
namespace Dragonfly.Engine.Core
{
    public interface ICompPausable : IComponent
    {
        void Pause();

        void Resume();
    }
}
