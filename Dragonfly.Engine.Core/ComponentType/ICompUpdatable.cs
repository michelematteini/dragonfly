
using System;

namespace Dragonfly.Engine.Core
{
    public interface ICompUpdatable : IComponent
    {
        void Update(UpdateType updateType);

        UpdateType NeededUpdates { get; }
    }

    [Flags]
    public enum UpdateType
    {
        None = 0,
        FrameStart1 = 1 << 0,
        FrameStart2 = 1 << 1,
        ResourceLoaded = 1 << 2
    }

}
