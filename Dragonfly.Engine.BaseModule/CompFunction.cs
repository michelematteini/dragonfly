using Dragonfly.Engine.Core;
using System;

namespace Dragonfly.BaseModule
{
    /// <summary>
    /// A generic component, that compute its value based on a function without state or using external state.
    /// </summary>
    public class CompFunction<T> : Component<T>
    {
        private Func<T> computeValue;

        public CompFunction(Component parent, Func<T> valueComputation) : base(parent)
        {
            computeValue = valueComputation;
        }

        protected override T getValue()
        {
            return computeValue();
        }
    }
}
