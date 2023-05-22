using System;
using System.Collections;
using System.Collections.Generic;

namespace Dragonfly.Utils
{
    /// <summary>
    /// Given a parent list, wraps a part of it as another list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SubList<T> : IList<T>, IReadOnlyList<T>
    {
        private int startIndex;

        public SubList(IList<T> parentList, int startIndex, int count)
        {
            ParentList = parentList;
            this.startIndex = startIndex;
            Count = count;
        }

        public IList<T> ParentList { get; private set; }

        public T this[int index] 
        { 
            get
            {
                if (index >= Count || index < 0)
                    throw new ArgumentOutOfRangeException();
                return ParentList[index + startIndex];
            }
            set
            {
                if (index >= Count || index < 0)
                    throw new ArgumentOutOfRangeException();
                ParentList[index + startIndex] =  value;
            }
        }

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0, si = startIndex, di = arrayIndex; i < Count; i++, si++, di++)
                array[di] = ParentList[si];
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            this[index] = item;
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
