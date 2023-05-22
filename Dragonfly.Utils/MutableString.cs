using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    /// <summary>
    /// A string made of an array of chars, with a fixed maximum length.
    /// Supports conversion of primitive types and append / insertion operation without creating any additional refernces.
    /// </summary>
    public class MutableString
    {
        private static readonly string digitToChar = "0123456789";
        private static ThreadLocal<char[]> convBuffer = new ThreadLocal<char[]>(() => new char[32], false);

        private Action<MutableString> onChanged;

        public char[] CharArray;

        public int Length { get; private set; }

        public MutableString(string initialValue = "", Action<MutableString> onChanged = null)
        {
            CharArray = new char[Math.Max(8, initialValue.Length)];
            Append(initialValue);
            this.onChanged = onChanged;
        }

        public void NotifyChanges()
        {
            if (onChanged != null)
                onChanged(this);
        }

        public static implicit operator MutableString(string str)
        {
            return new MutableString(str);
        }

        private void PrepareBuffer(int requiredLength)
        {
            if (CharArray.Length < requiredLength)
            {
                char[] newCharArray = new char[Math.Max(requiredLength, 2 * CharArray.Length)];
                Array.Copy(CharArray, newCharArray, Length);
                CharArray = newCharArray;
            }
        }

        /// <summary>
        /// Write the specified string at the given index in this mutable string.
        /// </summary>
        public MutableString Insert(int startIndex, string str)
        {
            int finalLen = Math.Max(Length, startIndex + str.Length);
            PrepareBuffer(finalLen);
            str.CopyTo(0, CharArray, startIndex, str.Length);
            Length = finalLen;
            NotifyChanges();
            return this;
        }

        /// <summary>
        /// Set the content of this mutable string to the specified value.
        /// </summary>
        public MutableString Set(string text)
        {
            return Insert(0, text);
        }

        public MutableString Insert(int index, char c)
        {
            PrepareBuffer(index + 1);
            CharArray[index] = c;
            Length = Math.Max(Length, index + 1);
            NotifyChanges();
            return this;
        }

        /// <summary>
        /// Append a string to the current mutable string.
        /// </summary>
        /// <param name="str"></param>
        public MutableString Append(string str)
        {
            return Insert(Length, str);
        }


        /// <summary>
        /// Append a string to the current mutable string, and returns it as a range.
        /// </summary>
        public MutableStringRange AppendAsRange(string str)
        {
            MutableStringRange appended;
            appended.String = this;
            appended.Range.From = Length;
            Append(str);
            appended.Range.To = Length;
            return appended;
        }

        public MutableString AppendLine()
        {
            return Append(Environment.NewLine);
        }

        /// <summary>
        /// Convert a signed number to a reversed string in the convBuffer and returns its length.
        /// </summary>
        private static int ConvertToBuffer(long number, bool addNegativeSign, int startIndex, int minDigitCount = -1)
        {
            bool isNeg = number < 0 || addNegativeSign;
            number = Math.Abs(number);
            int numLen = 0;
            char[] localConvBuffer = convBuffer.Value;
            for (; number > 0 || numLen == 0; numLen++)
            {
                long rem = number % 10;
                localConvBuffer[startIndex + numLen] = digitToChar[(int)rem];
                number = number / 10;
            }

            for (; numLen < minDigitCount; numLen++)
            {
                localConvBuffer[startIndex + numLen] = '0';
            }

            if (isNeg)
                localConvBuffer[startIndex + numLen++] = '-';
            return numLen;
        }

        private static int ConvertToBuffer(float number, int decimalPlaces)
        {
            bool isNeg = number < 0;
            number = Math.Abs(number);

            int intPart = (int)number;
            int numLen = 0;

            if (decimalPlaces > 0)
            {
                // fractional part
                float frac = number - intPart;
                for (int i = 0; i < decimalPlaces; i++)
                    frac *= 10;
                int fracPart = (int)frac;
                numLen += ConvertToBuffer(fracPart, false, 0, decimalPlaces);

                // decimal separator
                convBuffer.Value[numLen] = '.';
                numLen++;
            }

            // integer part
            numLen += ConvertToBuffer(intPart, isNeg, numLen);

            return numLen;
        }

        private MutableString InsertConverted(int startIndex, int length)
        {
            int requiredLen = startIndex + length;
            PrepareBuffer(requiredLen);
            char[] localConvBuffer = convBuffer.Value;
            for (int i = length - 1; i >= 0; i--)
                CharArray[startIndex++] = localConvBuffer[i];
            Length = Math.Max(Length, requiredLen);
            NotifyChanges();
            return this;
        }

        /// <summary>
        /// Convert the specified number to a string that is inserted at the give start index.
        /// </summary>
        public MutableString Insert(int startIndex, long number)
        {
            int numLen = ConvertToBuffer(number, false, 0);
            return InsertConverted(startIndex, numLen);   
        }

        public MutableString InsertLeft(int startIndex, int rangeSize, long number, char clearChar = ' ')
        {
            int numLen = Math.Min(ConvertToBuffer(number, false, 0), rangeSize);
            int unusedSlots = rangeSize - numLen;
            InsertConverted(startIndex + unusedSlots, numLen);
            Clear(startIndex, unusedSlots, clearChar);
            return this;
        }

        public MutableString Append(long number)
        {
            return Insert(Length, number);
        }

        /// <summary>
        /// Convert the specified floating point number to a string that is inserted at the given start index.
        /// </summary>
        public MutableString Insert(int startIndex, float number, int decimalPlaces)
        {
            int numLen = ConvertToBuffer(number, decimalPlaces);
            return InsertConverted(startIndex, numLen);
        }

        public MutableString InsertLeft(int startIndex, int rangeSize, float number, int decimalPlaces, char clearChar = ' ')
        {
            int numLen = Math.Min(ConvertToBuffer(number, decimalPlaces), rangeSize);
            int unusedSlots = rangeSize - numLen;
            InsertConverted(startIndex + unusedSlots, numLen);
            Clear(startIndex, unusedSlots, clearChar);
            return this;
        }

        public MutableString Append(float number, int decimalPlaces)
        {
            return Insert(Length, number, decimalPlaces);
        }

        /// <summary>
        /// Clear the given range of the string with the specified value. Won't change its length.
        /// </summary>
        public MutableString Clear(int fromInclusive, int count, char value = ' ')
        {
            int toExclusive = fromInclusive + count;
            for (int i = fromInclusive; i < toExclusive; i++)
                CharArray[i] = value;
            return this;
        }

        public static MutableString operator +(MutableString ms, string str)
        {
            return ms.Append(str);
        }

        public static MutableString operator +(MutableString ms, long number)
        {
            return ms.Append(number);
        }

        public static MutableString operator +(MutableString ms, float number)
        {
            return ms.Append(number, 3);
        }

        public int IndexOfAny(char[] anyOf, int fromIndex, int toIndex)
        {
            for (int i = fromIndex; i < toIndex; i++)
            {
                for (int ci = 0; ci < anyOf.Length; ci++)
                {
                    if (CharArray[i] == anyOf[ci])
                        return i;
                }
            }

            return -1;
        }

        private int ParseIntInternal(ref int fromIndex, out int sign)
        {
            int number = 0;
            sign = 1;
            if (CharArray[fromIndex] == '-')
            {
                sign = -1;
                fromIndex++;
            }

            for (; CharArray[fromIndex] >= '0' && CharArray[fromIndex] <= '9'; fromIndex++)
            {
                number = 10 * number + CharArray[fromIndex] - '0';
            }

            return number;
        }

        public int ParseInt(int fromIndex)
        {
            int sign;
            int value = ParseIntInternal(ref fromIndex, out sign);
            return sign * value;
        }

        public float ParseFloat(int fromIndex)
        {
            int sign;
            float number = ParseIntInternal(ref fromIndex, out sign);

            if (CharArray[fromIndex] == '.')
            {
                // parse deimals
                fromIndex++;
                float multiplier = 0.1f;
                for (; CharArray[fromIndex] >= '0' && CharArray[fromIndex] <= '9'; fromIndex++, multiplier *= 0.1f)
                {
                    number += multiplier * (CharArray[fromIndex] - '0');
                }
            }

            return sign * number;
        }

        public MutableStringRange FullRange
        {
            get
            {
                MutableStringRange fullRange;
                fullRange.String = this;
                fullRange.Range.From = 0;
                fullRange.Range.To = Length;
                return fullRange;
            }
        }

        public override string ToString()
        {
            return new string(CharArray, 0, Length);
        }

    }
}
