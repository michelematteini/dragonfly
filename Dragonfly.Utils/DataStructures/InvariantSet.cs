using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    /// <summary>
    /// A set of values indexed by their hash code. Quick addition and removal, indexed access.
    /// The accessed Values won't change once retrieved (a new copy is create at each access if element are modified).
    /// </summary>
    public class InvariantSet<T> : IInvariantSet
    {
        private HashSet<T> innerMap;
        private T[] invariantValues;
        private bool valuesInvalidated;

        public InvariantSet()
        {
            innerMap = new HashSet<T>();
            valuesInvalidated = true;
        }

        public void Add(T value)
        {
            innerMap.Add(value);
            valuesInvalidated = true;
        }

        public void AddIfTypeIsCompatible(object value)
        {
            if(value is T typedValue)
                Add(typedValue);
        }

        public void Remove(T value)
        {
            if (innerMap.Remove(value))
                valuesInvalidated = true;
        }

        public void Remove(object value)
        {
            if (value is T typedValue)
                Remove(typedValue);
        }

        public IReadOnlyList<T> Values
        {
            get
            {
                if (valuesInvalidated)
                {
                    if (invariantValues == null || invariantValues.Length != innerMap.Count)
                        invariantValues = new T[innerMap.Count];
                    innerMap.CopyTo(invariantValues, 0);
                    valuesInvalidated = false;
                }

                return invariantValues;
            }
        }

    }

    public interface IInvariantSet
    {
        void AddIfTypeIsCompatible(object value);

        void Remove(object value);
    }

}
