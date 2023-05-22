using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    public static class SerializationUtils
    {       
        public static void WriteDictionary<T>(Dictionary<string, T> d, BinaryWriter writer, Action<T> valueWriter)
        {
            writer.Write(d.Count);
            foreach (string key in d.Keys)
            {
                writer.Write(key);
                valueWriter(d[key]);
            }
        }

        public static Dictionary<string, T> ReadDictionary<T>(BinaryReader reader, Func<T> valueReader)
        {
            Dictionary<string, T> d = new Dictionary<string, T>();
            int recordCount = reader.ReadInt32();
            for (int i = 0; i < recordCount; i++)
            {
                string key = reader.ReadString();
                d[key] = valueReader();
            }
            return d;
        }

        public static void WriteCollection<T>(ICollection<T> collection, BinaryWriter writer, Action<T> valueWriter)
        {
            writer.Write(collection.Count);
            foreach (T value in collection)
                valueWriter(value);
        }

        public static List<T> ReadCollection<T>(BinaryReader reader, Func<T> valueReader)
        {
            List<T> collection = new List<T>();
            int recordCount = reader.ReadInt32();
            for (int i = 0; i < recordCount; i++)
                collection.Add(valueReader());
            return collection;
        }

        public static void WriteSet<T>(HashSet<T> set, BinaryWriter writer, Action<T> valueWriter)
        {
            WriteCollection<T>(set.ToArray(), writer, valueWriter);
        }

        public static HashSet<T> ReadSet<T>(BinaryReader reader, Func<T> valueReader)
        {
            HashSet<T> set = new HashSet<T>();
            int recordCount = reader.ReadInt32();
            for (int i = 0; i < recordCount; i++)
                set.Add(valueReader());
            return set;
        }

        public static void WriteBytes(byte[] bytes, BinaryWriter writer)
        {
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        public static byte[] ReadBytes(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            return reader.ReadBytes(count);
        }

    }

    public class SerializableDictionaryProxy<TKey, TValue>
    {
        private Dictionary<TKey, TValue> innerValue;

        private SerializableDictionaryProxy() { }

        public SerializableDictionaryProxy(Dictionary<TKey, TValue> innerValue)
        {
            this.innerValue = innerValue;
        }

        public List<Pair<TKey, TValue>> RecordList
        {
            get
            {
                List<Pair<TKey, TValue>> recordList = new List<Pair<TKey, TValue>>();
                foreach (TKey key in innerValue.Keys)
                    recordList.Add(new Pair<TKey, TValue>(key, innerValue[key]));
                return recordList;
            }
            set
            {
                innerValue.Clear();
                foreach (Pair<TKey, TValue> record in value)
                {
                    innerValue[record.First] = record.Second;
                }
            }
        }
    }

    public class SerializableHashSetProxy<T>
    {
        private HashSet<T> innerValue;

        private SerializableHashSetProxy() { }

        public SerializableHashSetProxy(HashSet<T> innerValue)
        {
            this.innerValue = innerValue;
        }

        public List<T> RecordList
        {
            get
            {
                List<T> recordList = new List<T>();
                foreach (T key in innerValue.ToArray())
                    recordList.Add(key);
                return recordList;
            }
            set
            {
                innerValue.Clear();
                foreach (T record in value)
                {
                    innerValue.Add(record);
                }
            }
        }
    }
}
