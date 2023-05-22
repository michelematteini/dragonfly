using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    /// <summary>
    /// Defines a range over an array and related utility accessors. Ranges are not checked!
    /// </summary>
    public struct ArrayRange<T>
    {
        public T[] Buffer;
        public int StartIndex;
        public int Count;

        public ArrayRange(int Capacity)
        {
            Buffer = new T[Capacity];
            StartIndex = 0;
            Count = 0;
        }

        public void Add(T value)
        {
            Buffer[StartIndex + Count] = value;     
            Count++;
        }

        public T this[int index]
        {
            get
            {
                return Buffer[index + StartIndex];
            }
            set
            {
                Buffer[index + StartIndex] = value;
            }
        }

        public void CopyTo(T[] destArray, int destIndex)
        {
            Array.Copy(Buffer, StartIndex, destArray, destIndex, Count);
        }

    }
}
