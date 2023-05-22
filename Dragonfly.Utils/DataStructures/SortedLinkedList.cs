using System;
using System.Collections;
using System.Collections.Generic;

namespace Dragonfly.Utils
{
    /// <summary>
    /// A linked list where items are kept sorted with the given comparator and indexed by their hash code. Removal is O(1), insertion is O(N).
    /// This ideal for fast removals, search and iteration usage, at the cost of slow insertions.
    /// </summary>
    public class SortedLinkedList<T> : IEnumerable<T>
    {
        private LinkedList<T> innerList;
        private Dictionary<T, LinkedListNode<T>> valueToNode;
        private Comparer<T> comparer;

        public int Count => innerList.Count;

        public SortedLinkedList(Comparer<T> comparer)
        {
            this.comparer = comparer;
            innerList = new LinkedList<T>();
            valueToNode = new Dictionary<T, LinkedListNode<T>>();
        }

        public void Add(T item)
        {
            // create a new list node and add it to the index
            LinkedListNode<T> itemNode = new LinkedListNode<T>(item);
            valueToNode.Add(item, itemNode);
      
            // insert the new node to the list keeping items sorted
            LinkedListNode<T> insertionLoc = innerList.Last;
            for (; insertionLoc != null && comparer.Compare(insertionLoc.Value, item) > 0; insertionLoc = insertionLoc.Previous) ;

            if (insertionLoc != null)
                innerList.AddAfter(insertionLoc, itemNode);
            else
                innerList.AddFirst(itemNode);       
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public bool Remove(T item)
        {
            LinkedListNode<T> itemNode;
            if (!valueToNode.TryGetValue(item, out itemNode))
                return false;

            innerList.Remove(itemNode);
            valueToNode.Remove(item);
            return true;
        }

        public bool Contains(T item)
        {
            return valueToNode.ContainsKey(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<T>
        {
            private SortedLinkedList<T> parent;
            private LinkedListNode<T> currentNode;

            public Enumerator(SortedLinkedList<T> parent)
            {
                this.parent = parent;
                currentNode = null;
            }

            public T Current
            {
                get
                {
                    return currentNode.Value;
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                
            }

            public bool MoveNext()
            {
                if (currentNode == null)
                    currentNode = parent.innerList.First;
                else
                    currentNode = currentNode.Next;

                return currentNode != null;
            }

            public void Reset()
            {
                currentNode = null;
            }
        }

    }
}
