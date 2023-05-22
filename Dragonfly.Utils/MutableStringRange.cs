using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Dragonfly.Utils
{
    /// <summary>
    /// A range inside a mutable string object.
    /// </summary>
    public struct MutableStringRange : IEquatable<MutableStringRange>
    {
        public MutableString String;
        public Range<int> Range;

        public int Size
        {
            get
            {
                return Range.To - Range.From;
            }
        }

        /// <summary>
        /// Returns a string range that start from the same point of this one, but end to the first separator found if any. If no separator is found, this range is returned as is.
        /// </summary>
        public MutableStringRange SplitAt(char[] separators)
        {
            MutableStringRange result = this;
            int endIndex = String.IndexOfAny(separators, result.Range.From, Range.To);
            result.Range.To = endIndex < 0 ? result.Range.To : endIndex;
            return result;
        }


        /// <summary>
        /// Returns a string range that start from the same point of this one, but end to the first separator found if any. If no separator is found, this range is returned as is.
        /// </summary>
        /// <param name="rangeLeft">The other partition of this range, which start after the found separator and ends at the same index of this one.</param>
        public MutableStringRange SplitAt(char[] separators, out MutableStringRange rangeLeft)
        {
            MutableStringRange result = this;
            int endIndex = String.IndexOfAny(separators, result.Range.From, Range.To);
            result.Range.To = endIndex < 0 ? result.Range.To : endIndex;
            rangeLeft.String = String;
            rangeLeft.Range.From = Math.Min(result.Range.To + 1, Range.To);
            rangeLeft.Range.To = Range.To;
            return result;
        }

        public MutableStringRange Trim()
        {
            MutableStringRange result = this;

            // remove leading spaces
            while (result.Range.From < result.Range.To && char.IsWhiteSpace(String.CharArray[result.Range.From]))
                result.Range.From++;
            // remove trailing spaces
            while (result.Range.To > result.Range.From && char.IsWhiteSpace(String.CharArray[result.Range.To - 1]))
                result.Range.To--;

            return result;
        }

        /// <summary>
        /// Modify this range in the parent MutableString to lower case.
        /// </summary>
        public MutableStringRange ToLower()
        {
            for (int i = Range.From; i < Range.To; i++)
            {
                String.CharArray[i] = char.ToLowerInvariant(String.CharArray[i]);
            }
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToInt()
        {
            return String.ParseInt(Range.From);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ToFloat()
        {
            return String.ParseFloat(Range.From);
        }

        public override string ToString()
        {
            return new string(String.CharArray, Range.From, Range.To - Range.From);
        }

        public bool StartsWith(string prefix)
        {
            for (int i = 0; i < prefix.Length; i++)
                if (String.CharArray[Range.From + i] != prefix[i])
                    return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(char prefix)
        {
            return String.CharArray[Range.From] == prefix;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Reset();
            for (int i = Range.From; i < Range.To; i++)
            {
                hash.Add(String.CharArray[i]);
            }
            return hash.Resolve();
        }

        public override bool Equals(object obj)
        {
            if (obj is MutableStringRange)
            {
                MutableStringRange other = (MutableStringRange)obj;
                return Equals(other);
            }

            return false;
        }

        public bool Equals(MutableStringRange other)
        {
            int size = Size;

            if (other.Size != size)
                return false;

            for (int i = 0; i < size; i++)
            {
                if (String.CharArray[i + Range.From] != other.String.CharArray[i + other.Range.From])
                    return false;
            }

            return true;
        }
    }
}
