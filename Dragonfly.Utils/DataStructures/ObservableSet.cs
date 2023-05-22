using System;
using System.Collections;
using System.Collections.Generic;

namespace Dragonfly.Utils
{
    public class ObservableSet<T> : ISet<T>
    {
        private HashSet<T> innerSet;

        public ObservableSet()
        {
            innerSet = new HashSet<T>();
        }

        public event Action Changed;

        public int Count => innerSet.Count;

        public bool IsReadOnly => false;

        public bool Add(T item)
        {
            bool added = innerSet.Add(item);
            if (added && Changed != null) Changed();
            return added;
        }

        public void Clear()
        {
            innerSet.Clear();
            if (Changed != null) Changed();
        }

        public bool Contains(T item)
        {
            return innerSet.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            innerSet.CopyTo(array, arrayIndex);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            innerSet.ExceptWith(other);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return innerSet.GetEnumerator();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            innerSet.IntersectWith(other);
            if (Changed != null) Changed();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return innerSet.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return innerSet.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return innerSet.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return innerSet.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return innerSet.Overlaps(other);
        }

        public bool Remove(T item)
        {
            bool removed = innerSet.Remove(item);
            if (removed && Changed != null) Changed();
            return removed;
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return innerSet.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            innerSet.SymmetricExceptWith(other);
            if (Changed != null) Changed();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            innerSet.UnionWith(other);
            if (Changed != null) Changed();
        }

        void ICollection<T>.Add(T item)
        {
            innerSet.Add(item);
            if (Changed != null) Changed();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerSet.GetEnumerator();
        }
    }
}
