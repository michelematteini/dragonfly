using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Dragonfly.Utils
{
    public struct HashCode
    {
        #region Static Utilities

        private const int HASH_SEED = 352654597;

        public static int Combine<T>(T[] values)
        {
            unchecked
            {
                int hash = HASH_SEED;
                for (int i = 0; i < values.Length; i++)
                {
                        hash = ((hash << 5) + hash + (hash >> 27)) ^ values[i].GetHashCode();
                }
                return hash;
            }
        }

        public static int Combine<T>(IList<T> values)
        {
            unchecked
            {
                int hash = HASH_SEED;
                for (int i = 0; i < values.Count; i++)
                {
                    hash = ((hash << 5) + hash + (hash >> 27)) ^ values[i].GetHashCode();
                }
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine(int hash1, int hash2)
        {
            return unchecked(((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hash2);
        }

        private static SHA256 sha256 = new SHA256Managed();

        /// <summary>
        /// Compute an hash code for the specified string that is platform-indipendent.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int HashString(string value)
        {
            int hash = HASH_SEED;
            for (int i = 0; i < value.Length; i++)
                hash = unchecked(((hash << 5) + hash + (hash >> 27)) ^ (int)value[i]);
            return hash;
        }

        #endregion

        private int hashAccumulator;
        private int bitAccumulator;
        private int bitsLeft;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(bool value)
        {
            AddBits(value ? 1 : 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(ushort value)
        {
            AddBits((int)value, 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(char value)
        {
            AddBits((int)value, 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(byte value)
        {
            AddBits((int)value, 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int value)
        {
            hashAccumulator = Combine(hashAccumulator, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(object value, int bitCount = 32)
        {
            AddBits(value.GetHashCode(), bitCount);
        }

        private void AddBits(int bits, int bitCount)
        {
            if(bitsLeft < bitCount)
            {
                hashAccumulator = Combine(hashAccumulator, bitAccumulator);
                bitsLeft = 32;
                bitAccumulator = 0;
            }

            bitAccumulator = (bitAccumulator << bitCount) + bits;
            bitsLeft -= bitCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Resolve()
        {
            return Combine(hashAccumulator, bitAccumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            hashAccumulator = HASH_SEED;
            bitAccumulator = 0;
            bitsLeft = 32;
        }

    }
}
