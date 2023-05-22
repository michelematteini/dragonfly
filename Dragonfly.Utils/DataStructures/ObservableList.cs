using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    /// <summary>
    /// A generic list where item addition and removal can be observed by listening to specific events.
    /// </summary>
    /// <typeparam name="T">The type of the elements in this list.</typeparam>
    public class ObservableList<T> : IList<T>
    {
        private List<T> innerList;

        public ObservableList()
        {
            innerList = new List<T>();
        }

        public event Action<T> ItemAdded, ItemRemoved;

        public T this[int index] 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return innerList[index];
            }
            set
            {
                T prevItem = innerList[index];
                innerList[index] = value;
                if (ItemRemoved != null)
                    ItemRemoved.Invoke(prevItem);
                if (ItemAdded != null)
                    ItemAdded.Invoke(value);
            }
        }

        public int Count => innerList.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            innerList.Add(item);
            if (ItemAdded != null)
                ItemAdded.Invoke(item);
        }

        public void Clear()
        {
            T[] remItems = innerList.ToArray();
            innerList.Clear();
            if (ItemRemoved != null)
                foreach (T item in remItems)
                    ItemRemoved(item);
        }

        public bool Contains(T item)
        {
            return innerList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            innerList.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return innerList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            innerList.Insert(index, item);
            if (ItemAdded != null)
                ItemAdded.Invoke(item);
        }

        public bool Remove(T item)
        {
            bool removed = innerList.Remove(item);
            if (removed && ItemRemoved != null)
                ItemRemoved.Invoke(item);
            return removed;
        }

        public void RemoveAt(int index)
        {
            T remItem = innerList[index];
            innerList.RemoveAt(index);
            if (ItemRemoved != null)
                ItemRemoved.Invoke(remItem);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerList.GetEnumerator();
        }
    }
}
