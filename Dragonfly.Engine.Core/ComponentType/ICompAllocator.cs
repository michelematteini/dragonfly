
namespace Dragonfly.Engine.Core
{
    public interface ICompAllocator : IComponent
    {
        void LoadGraphicResources(EngineResourceAllocator g);

        void ReleaseGraphicResources();

        bool LoadingRequired { get; }
    }
}
