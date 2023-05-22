using System;
using System.IO;

namespace Dragonfly.Utils
{
    /// <summary>
    /// Utility functions for console program.
    /// </summary>
    public static class ConsoleUtils
    {
        /// <summary>
        /// Print a formatted console error in red.
        /// </summary>
        public static void PrintError(string errorMsg, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(string.Format(errorMsg, args));
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Ask the user for an input and output file, with the given extensions.
        /// </summary>
        /// <returns>Returns false if the specified path for input or output are invalid.</returns>
        public static bool AskInOutPath(string inputExtension, string outputExtension, out string inputPath, out string outputPath)
        {
            inputPath = outputPath = string.Empty;

            // read input path
            Console.WriteLine("Insert the path of the input '{0}' file:", inputExtension);
            inputPath = Console.ReadLine();

            if (!File.Exists(inputPath))
            {
                PrintError("Invalid input file.");
                return false;
            }

            if (Path.GetExtension(inputPath) != inputExtension)
            {
                PrintError("Invalid file extension.");
                return false;
            }

            // read output path
            Console.WriteLine("Insert the output '{0}' file path:", outputExtension);
            outputPath = Console.ReadLine();
            if (!outputPath.EndsWith(outputExtension))
                outputPath += outputExtension;

            return true;
        }

        /// <summary>
        /// Ask the user for an output file path, with the given extensions.
        /// </summary>
        /// <returns>Returns false if the specified path is invalid.</returns>
        public static bool AskOutPath(string outputExtension, out string outputPath)
        {
            // read output path
            Console.WriteLine("Insert the output '{0}' file path:", outputExtension);
            outputPath = Console.ReadLine();
            if (!outputPath.EndsWith(outputExtension))
                outputPath += outputExtension;
            return true;
        }

        public static float AskFloat(string message, float defaultIfNoInput)
        {
            Console.WriteLine(message + "[default = " + defaultIfNoInput + "]");

            while (true)
            {
                string valueStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(valueStr))
                    return defaultIfNoInput;
                try
                {
                    return valueStr.ParseInvariantFloat();
                }
                catch
                {
                    Console.WriteLine("Invalid value, insert a number using '.' as decimal separator:");
                }
            }
        }

        public static int AskInt(string message, int defaultIfNoInput)
        {
            Console.WriteLine(message + "[default = " + defaultIfNoInput + "]");

            while (true)
            {
                string valueStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(valueStr))
                    return defaultIfNoInput;
                try
                {
                    return valueStr.ParseInvariantInt();
                }
                catch
                {
                    Console.WriteLine("Invalid value, insert an integer number:");
                }
            }
        }

        public static bool AskYesNo(string message)
        {
            while (true)
            {
                Console.WriteLine(message + "[Y/N]");
                string valueStr = Console.ReadLine();

                if (valueStr == "Y" || valueStr == "y")
                    return true;

                if (valueStr == "N" || valueStr == "n")
                    return false;
            }
        }

        public static T AskEnum<T>(string message, T defaultIfNoInput) where T : Enum
        {
            Array enumValues = Enum.GetValues(typeof(T));
            string valueDescr = "[";
            foreach(object enumValue in enumValues)
                valueDescr += " " + enumValue + " = " + (int)enumValue + ",";
            valueDescr += " default = " + defaultIfNoInput + "]";
            Console.WriteLine(message + valueDescr);

            while (true)
            {
                string valueStr = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(valueStr))
                    return defaultIfNoInput;
                try
                {
                    return (T)(object)valueStr.ParseInvariantInt();
                }
                catch
                {
                    Console.WriteLine("Invalid value, insert a number using '.' as decimal separator:");
                }
            }
        }
    }
}
