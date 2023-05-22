using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Dragonfly.Utils
{
    public static class StringEx
    {
        public static float ParseInvariantFloat(this string floatString)
        {
            return float.Parse(floatString, CultureInfo.InvariantCulture.NumberFormat);
        }

        public static int ParseInvariantInt(this string intString)
        {
            return int.Parse(intString, CultureInfo.InvariantCulture.NumberFormat);
        }

        public static void AppendFormatLine(this StringBuilder strBuilder, string format, params object[] args)
        {
            strBuilder.AppendFormat(format, args);
            strBuilder.AppendLine();
        }

        /// <summary>
        /// Replace the first occurrence of a given string with a new one.
        /// </summary>
        public static string ReplaceFirst(this string str, string oldValue, string newValue)
        {
            Regex regex = new Regex(Regex.Escape(oldValue));
            return regex.Replace(str, newValue, 1);
        }

        /// <summary>
        /// Read an ascii string from a binary stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static string ReadASCIILine(this BinaryReader reader)
        {
            // read bytes until a new line character is found
            List<byte> lineBytes = new List<byte>();
            for (; reader.PeekChar() != '\n'; lineBytes.Add(reader.ReadByte())) ;
            reader.ReadByte(); // advance past the end of line

            // decode and return the string
            return Encoding.ASCII.GetString(lineBytes.ToArray());
        }

        /// <summary>
        /// Writes a string to a binary string as ascii characters.
        /// </summary>
        public static void WriteASCIILine(this BinaryWriter writer, string value)
        {
            byte[] strBytes = Encoding.ASCII.GetBytes(value);
            writer.Write(strBytes);
            writer.Write('\n');
        }

        /// <summary>
        /// Returns the specified string, or a default if null.
        /// </summary>
        public static string DefaultIfNull(this string str, string defaultStr)
        {
            if (str != null) return str;
            return defaultStr;
        }

        /// <summary>
        /// Returns a description of the specified dictionary as a string.
        /// </summary>
        public static string ToKeyValueString(this IDictionary d)
        {
            StringBuilder kvString = new StringBuilder("{");
            foreach (object key in d.Keys)
            {
                kvString.Append(string.Format("{0}={1}, ", key, d[key]));
            }
            if (d.Count > 0)
                kvString.Length -= 2; 
            kvString.Append('}');
            return kvString.ToString();
        }

    }
}
