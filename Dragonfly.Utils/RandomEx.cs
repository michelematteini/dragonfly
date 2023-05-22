using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    public static class RandomEx
    {
        private static readonly byte[] buffer = new byte[8];

        public static ulong NextUlong(this Random rnd)
        {
            rnd.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public static uint NextUInt(this Random rnd)
        {
            rnd.NextBytes(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        /// <summary>
        /// Returns an hash with no conflicts of the specified integer.
        /// https://stackoverflow.com/questions/664014/what-integer-hash-function-are-good-that-accepts-an-integer-hash-key
        /// </summary>
        public static uint HashUint(uint x)
        {
            unchecked
            {
                x = ((x >> 16) ^ x) * 0x45d9f3b;
                x = ((x >> 16) ^ x) * 0x45d9f3b;
                x = (x >> 16) ^ x;
            }
            return x;
        }

    }
}
