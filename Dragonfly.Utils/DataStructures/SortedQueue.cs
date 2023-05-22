using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    /// <summary>
    /// A queue data structure where elements are keep sorted, and the order of extraction depends on its current sorted state.
    /// </summary>
    public class SortedQueue<TOrder, TValue>
    {
        private LinkedList<KeyValuePair<TOrder, TValue>> elements;

        public SortedQueue()
        {
            elements = new LinkedList<KeyValuePair<TOrder, TValue>>();
        }

        public void Enqueue(TValue value, TOrder order)
        {
            KeyValuePair<TOrder, TValue> elem = new KeyValuePair<TOrder, TValue>(order, value);

            // move along the linked list to find the correctly sorted insertion position
            Comparer<TOrder> orderComp = Comparer<TOrder>.Default;
            LinkedListNode<KeyValuePair<TOrder, TValue>> curNode;
            for (curNode = elements.First; curNode != null && orderComp.Compare(order, curNode.Value.Key) > 0; curNode = curNode.Next) ;

            // add the new element in the correct location
            if (curNode == null)
                elements.AddLast(elem);
            else
                elements.AddBefore(curNode, elem);
        }

        public TValue Dequeue()
        {
            TValue firstValue = elements.First.Value.Value;
            elements.RemoveFirst();
            return firstValue;
        }

        public int Count
        {
            get
            {
                return elements.Count;
            }
        }

    }
}
