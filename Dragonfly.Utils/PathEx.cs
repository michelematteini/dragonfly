using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    public static class PathEx
    {
        public const string RESOURCE_FOLDER_NAME = "res";

        public static string NormalizePath(string path)
        {
            bool isUriPath = path.Contains(':');

            if (isUriPath) path = new Uri(path).LocalPath;

            return Path.GetFullPath(path)
              .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
              .ToUpperInvariant();
        }

        /// <summary>
        /// Returns the first directory with the specified name, searching in the current directory and the its parents
        /// </summary>
        /// <param name="folderName">Name of the directory to be found</param>
        /// <returns></returns>
        public static string GetFirstTopDir(string folderName)
        {
            for (DirectoryInfo curDir = new DirectoryInfo(Directory.GetCurrentDirectory());  curDir.Exists; curDir = curDir.Parent)
            {               
                string requiredDir = Path.Combine(curDir.FullName, folderName);
                if (Directory.Exists(requiredDir)) return requiredDir;   
            }

            return folderName;
        }

        /// <summary>
        /// Returns the first directory with the specified name, searching in the current directory and the its parents
        /// </summary>
        /// <param name="folderName">Name of the directory to be found</param>
        /// <returns></returns>
        public static string GetFirstTopDir(string folderName, string startDirectory)
        {
            for (DirectoryInfo curDir = new DirectoryInfo(startDirectory); curDir.Exists; curDir = curDir.Parent)
            {
                string requiredDir = Path.Combine(curDir.FullName, folderName);
                if (Directory.Exists(requiredDir)) return requiredDir;
            }

            return folderName;
        }

        public static string DefaultResourceFolder
        {
            get
            {
                return GetFirstTopDir(RESOURCE_FOLDER_NAME);
            }
        }

        public static string GetDefaultResorcePath(string resource)
        {
            return Path.Combine(DefaultResourceFolder, resource);
        }


    }
}
