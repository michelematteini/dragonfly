using System;
using System.Collections.Generic;

namespace Dragonfly.Utils
{
    /// <summary>
    /// An array-based list with fast removal that just replace the removed element with its default value. An unused index in the array is then re-used on new addition.
    /// Adding an element will return its actual index, this index never changes since removals wont move elements around.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IndexedList<T>
    {
        private List<T> innerList;
        private Stack<int> freeSlots;

        public IndexedList()
        {
            innerList = new List<T>();
            freeSlots = new Stack<int>();
            EmptyValue = default(T);
        }

        /// <summary>
        /// The empty value that replace current values on removal.
        /// </summary>
        public T EmptyValue { get; set; }

        public int Add(T item)
        {
            if (EqualityComparer<T>.Default.Equals(item, EmptyValue))
            {
                throw new InvalidOperationException("Adding empty items is not supported by this data structure!");
            }

            // recycle existing free slot if available
            if (freeSlots.Count > 0)
            {
                int index = freeSlots.Pop();
                innerList[index] = item;
                return index;
            }

            innerList.Add(item);
            return innerList.Count - 1;
        }

        public void RemoveAt(int index)
        {
            if (!EqualityComparer<T>.Default.Equals(innerList[index], EmptyValue))
            {
                innerList[index] = EmptyValue;
                freeSlots.Push(index);
            }
        }

        /// <summary>
        /// The total number of indexable items to iterate this list. 
        /// Also include empty slots and it's up to the user to test for them.
        /// </summary>
        public int Size
        {
            get
            {
                return innerList.Count;
            }
        }

        /// <summary>
        /// Number of non-empty elements in this list.
        /// </summary>
        public int Count
        {
            get
            {
                return innerList.Count - freeSlots.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                return innerList[index];
            }
        }


    }
}
