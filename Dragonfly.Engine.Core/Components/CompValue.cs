
using System;

namespace Dragonfly.Engine.Core
{
    /// <summary>
    /// A generic typed component, that switch between a static value and a component input.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CompValue<T> : Component<T>
    {
        private bool useExplicitValue;
        private T value;
        private Component<T> component;

        public CompValue(Component parent, T defaultValue) : base(parent)
        {
            Set(defaultValue);
        }

        public void Set(T value)
        {
            this.value = value;
            useExplicitValue = true;
            component = null;
        }

        public void Set(Component<T> value)
        {
            component = value;
            useExplicitValue = false;
        }

        public void Set(CompValue<T> other)
        {         
            useExplicitValue = other.useExplicitValue;
            value = other.value;
            component = other.component;
        }

        /// <summary>
        /// If the value is a component, try to cast it to the specified type and returns it.
        /// Returns null if a simple value is currently set, or the component is not of the specified type.
        /// </summary>
        public TDest As<TDest>() where TDest : Component
        {
            if (useExplicitValue)
                return null;
            return component as TDest;
        }

        public CompValue<T> Clone(Component cloneParent)
        {
            CompValue<T> clone = new CompValue<T>(cloneParent, value);
            clone.useExplicitValue = useExplicitValue;
            clone.component = component;
            return clone;
        }

        protected override T getValue()
        {
            return useExplicitValue ? value : component.GetValue();
        }
    }
}
