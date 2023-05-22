using System;
using System.Collections;
using System.Collections.Generic;

namespace Dragonfly.Utils
{
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> dictionary;

        public ObservableDictionary()
        {
            dictionary = new Dictionary<TKey, TValue>();
            Changed += ObservableDictionary_Changed;
        }

        private void ObservableDictionary_Changed()
        {

        }

        public event Action Changed;

        public TValue this[TKey key]
        {
            get { return dictionary[key]; }
            set { dictionary[key] = value; Changed(); }
        }

        public ICollection<TKey> Keys
        {
            get { return dictionary.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return dictionary.Values; }
        }

        public int Count
        {
            get { return dictionary.Count; }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).IsReadOnly; }
        }

        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);
            Changed();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Add(item);
            Changed();
        }

        public void Clear()
        {
            dictionary.Clear();
            Changed();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            bool removed = dictionary.Remove(key);
            Changed();
            return removed;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            bool removed = ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(item);
            Changed();
            return removed;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)dictionary).GetEnumerator();
        }
    }
}
