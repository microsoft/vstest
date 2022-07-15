// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

namespace Client
{
    /// <summary>
    /// Finds the root folder by finding the closest solution (.sln) file in the directory
    /// structure, and finds files relative to that.
    /// </summary>
    internal class FileUtility
    {
        /// <summary>
        /// Finds a file relative to the root (which is the closest .sln file in the folder structure).
        /// </summary>
        /// <param name="relativePath">Path relative to the closest .sln file.</param>
        /// <returns>A path to the file or null.</returns>
        internal static string FindDll(string relativePath)
        {
            return FindFile(FindSln(), relativePath);
        }

        internal static string FindVstestConsole()
        {
            var dotnet = FindRoot("dotnet*", Directory.GetParent(typeof(object).Assembly.Location).FullName);

            var vstestConsoles = Directory.EnumerateFiles(Path.Combine(dotnet, "sdk"), "vstest.console.dll", SearchOption.AllDirectories);

            return vstestConsoles.Last();
        }


        private static string FindSln()
        {
            return FindRoot("*.sln", AppContext.BaseDirectory);
        }

        private static string FindFile(string root, string relativePath)
        {
            var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                return null;
            }

            return fullPath;
        }

        private static string FindRoot(string pattern, string startingPath)
        {
            var path = startingPath;
            do
            {
                var solutions = Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly).ToList();
                if (solutions.Any())
                    return Directory.GetParent(solutions.First()).FullName;

                path = Directory.GetParent(path).FullName;
            } while (Path.GetPathRoot(path) != path);

            throw new InvalidOperationException($"Can't find any {pattern} file in '{startingPath}' or any of its parent directories");
        }
    }
}
