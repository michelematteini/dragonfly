using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Dragonfly.Utils
{

    /// <summary>
    /// Implement a list of sorted elemnts with fast (logarithmic) insertion, deletion, and search.
    /// </summary>
    public class SkipList<T> : ICollection<T>
    {
        private const int MaxLevel = 20;

        private readonly Random rnd;
        private readonly Comparer<T> comparer;
        private readonly SkipListSetNode<T> head, nil;

        private int editVersion; // used to track edits to the list and detect when its content has been changed
        private int level = 0;

        public SkipList(Comparer<T> comparer)
        {
            Debug.Assert(comparer != null);
            this.comparer = comparer;
            rnd = new Random();
            head = new SkipListSetNode<T>(default(T), MaxLevel);
            nil = head;
            editVersion = 0;

            for (var i = 0; i <= MaxLevel; i++)
            {
                head.Forward[i] = nil;
            }
        }

        public void Add(T item)
        {
            Insert(item);
        }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }

        public bool Contains(T item)
        {
            return Search(item) != null;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        private SkipListSetNode<T>[] BuildUpdateList(T item, out SkipListSetNode<T> baseNode, out bool containsValue)
        {
            SkipListSetNode<T>[] updateList = new SkipListSetNode<T>[MaxLevel + 1];
            baseNode = head;
            for (int i = level; i >= 0; i--)
            {
                while (baseNode.Forward[i] != nil && comparer.Compare(baseNode.Forward[i].Key, item) < 0)
                {
                    baseNode = baseNode.Forward[i];
                }
                updateList[i] = baseNode;
            }
            baseNode = baseNode.Forward[0];
            containsValue = baseNode != nil && comparer.Compare(baseNode.Key, item) == 0;
            return updateList;
        }

        public bool Remove(T item)
        {
            bool containsValue;
            SkipListSetNode<T> node;
            SkipListSetNode<T>[] updateList = BuildUpdateList(item, out node, out containsValue);

            if (!containsValue)
                return false;

            for (var i = 0; i <= level; i++)
            {
                if (updateList[i].Forward[i] != node)
                {
                    break;
                }
                updateList[i].Forward[i] = node.Forward[i];
            }
            while (level > 0 && head.Forward[level] == nil)
            {
                level--;
            }
            Count--;
            editVersion++;
            return true;
        }

        public int Count { get; private set; }

        public bool IsReadOnly { get { return false; } }

        private IEnumerable<T> Items
        {
            get
            {
                int version = editVersion;
                SkipListSetNode<T> node = head.Forward[0];
                while (node != nil)
                {
                    if (version != editVersion)
                        throw new InvalidOperationException("Collection cannot be modified while enumerating it.");

                    yield return node.Key;
                    node = node.Forward[0];
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private SkipListSetNode<T> Search(T item)
        {
            SkipListSetNode<T> node = head;

            for (int i = level; i >= 0; i--)
            {
                while (node.Forward[i] != nil)
                {
                    int cmpResult = comparer.Compare(node.Forward[i].Key, item);
                    if (cmpResult > 0)
                    {
                        break;
                    }
                    if (cmpResult == 0)
                    {
                        return node.Forward[i];
                    }
                    node = node.Forward[i];
                }
            }

            Debug.Assert(node.Forward[0] == nil || comparer.Compare(item, node.Forward[0].Key) <= 0);
            node = node.Forward[0];

            if (node != nil && comparer.Compare(node.Key, item) == 0)
            {
                return node;
            }
            return null;
        }

        private void Insert(T item)
        {
            bool containsValue;
            SkipListSetNode<T> node;
            SkipListSetNode<T>[] updateList = BuildUpdateList(item, out node, out containsValue);

            if (containsValue)
                return;

            int newLevel = 0;
            for (; rnd.Next(0, 2) > 0 && newLevel < MaxLevel; newLevel++) ;
            if (newLevel > level)
            {
                for (int i = level + 1; i <= newLevel; i++)
                {
                    updateList[i] = head;
                }
                level = newLevel;
            }

            node = new SkipListSetNode<T>(item, newLevel);

            for (int i = 0; i <= newLevel; i++)
            {
                node.Forward[i] = updateList[i].Forward[i];
                updateList[i].Forward[i] = node;
            }
            Count++;
            editVersion++;
        }
    }

    internal class SkipListSetNode<T>
    {
        public SkipListSetNode(T key, int level)
        {
            Key = key;
            Forward = new SkipListSetNode<T>[level + 1];
        }

        public T Key { get; private set; }

        public SkipListSetNode<T>[] Forward { get; private set; }
    }
}
