using System;
using System.Collections.Generic;

namespace Dragonfly.Utils
{
    public class CircularArray<T>
    {
        private T[] innerArray;
        private int curZeroIndex;

        public CircularArray(int length)
        {
            Length = length;
            innerArray = new T[length];
            curZeroIndex = 0;
        }

        public int Length { get; private set; }

        public T this[int i]
        {
            get
            {
                return innerArray[GetInnerIndex(i)];
            }
            set
            {
                innerArray[GetInnerIndex(i)] = value;
            }
        }

        private int GetInnerIndex(int i)
        {
            return (curZeroIndex + i + Length) % Length; 
        }

        /// <summary>
        /// Move this circular array zero index of the specified positions: positive numbers shift left, while negative shift right.
        /// </summary>
        public void Shift(int positions)
        {
            curZeroIndex = GetInnerIndex(positions);
        }

    }
}
