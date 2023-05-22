using Dragonfly.Graphics.Math;
using System;

namespace Dragonfly.Engine.Core
{
    public interface IComponent : IDisposable
    {
        Component Parent { get; }

        /// <summary>
        /// True if the component is completely initialized and can be used, false otherwise. 
        /// If the value of this property is false, the engine will not take this component into account when rendering.
        /// Checked on each frame.
        /// </summary>
        bool Ready { get; }

        /// <summary>
        /// If set to false, the engine will not take this component into account when rendering.
        /// Checked on each frame.
        /// Differently from Ready, this is a user value that can be used to enable / disable the component.
        /// The value set to this property, will propagate to child components.
        /// </summary>
        bool Active { get; set; }

        /// <summary>
        /// A user-defined name for this component.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Returns true if this component has been disposed.
        /// </summary>
        bool Disposed { get; }

        /// <summary>
        /// Returns the transformation matrix for this component
        /// </summary>
        /// <returns></returns>
        TiledFloat4x4 GetTransform();
    }

    public interface IComponent<T> : IComponent
    {
        /// <summary>
        /// Retrive a value of type T representing this component. The values is updated each frame.
        /// </summary>
        /// <returns></returns>
        T GetValue();
    }

}