// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.TestPlatform.TestUtilities
{
    public class Workspace : IDisposable
    {
        /// <summary>
        /// Creates a workspace object around the given directory path. Pass null to use a temp unique directory.
        /// </summary>
        public Workspace(string path = null)
        {
            Path = path ?? IntegrationTestBase.GetResultsDirectory();
        }

        public string Path { get; }

        public void Dispose()
        {
            IntegrationTestBase.TryRemoveDirectory(Path);
        }

        public DirectoryInfo CreateDirectory(string dir) => Directory.CreateDirectory(System.IO.Path.Combine(Path, dir));

        public void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(System.IO.Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
