// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD1_0

namespace Microsoft.VisualStudio.TestPlatform.Utilities.Helpers
{
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// The file helper.
    /// </summary>
    public class FileHelper : IFileHelper
    {
        private static readonly Version DefaultFileVersion = new Version(0, 0);

        /// <inheritdoc/>
        public DirectoryInfo CreateDirectory(string path)
        {
            return Directory.CreateDirectory(path);
        }

        /// <inheritdoc/>
        public string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        /// <inheritdoc/>
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        /// <inheritdoc/>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <inheritdoc/>
        public Stream GetStream(string filePath, FileMode mode, FileAccess access = FileAccess.ReadWrite)
        {
            return new FileStream(filePath, mode, access);
        }

        /// <inheritdoc/>
        public Stream GetStream(string filePath, FileMode mode, FileAccess access, FileShare share)
        {
            return new FileStream(filePath, mode, access, share);
        }


        /// <inheritdoc/>
        public IEnumerable<string> EnumerateFiles(
            string directory,
            SearchOption searchOption,
            params string[] endsWithSearchPatterns)
        {
            if (endsWithSearchPatterns == null || endsWithSearchPatterns.Length == 0)
            {
                return Enumerable.Empty<string>();
            }

            var files = Directory.EnumerateFiles(directory, "*", searchOption);

            return files.Where(
                file => endsWithSearchPatterns.Any(
                    pattern => file.EndsWith(pattern, StringComparison.OrdinalIgnoreCase)));
        }

        /// <inheritdoc/>
        public FileAttributes GetFileAttributes(string path)
        {
            return new FileInfo(path).Attributes;
        }

        /// <inheritdoc/>
        public Version GetFileVersion(string path)
        {
            var currentFileVersion = FileVersionInfo.GetVersionInfo(path)?.FileVersion;
            return Version.TryParse(currentFileVersion, out var currentVersion) ? currentVersion : DefaultFileVersion;
        }

        /// <inheritdoc/>
        public void CopyFile(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath);
        }

        /// <inheritdoc/>
        public void MoveFile(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
        }

        /// <inheritdoc/>
        public void WriteAllTextToFile(string filePath, string content)
        {
            File.WriteAllText(filePath, content);
        }

        /// <inheritdoc/>
        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        /// <inheritdoc/>
        public void DeleteEmptyDirectroy(string dirPath)
        {
            try
            {
                if (Directory.Exists(dirPath)
                    && !Directory.EnumerateFileSystemEntries(dirPath).Any())
                {
                    Directory.Delete(dirPath, true);
                }
            }
            catch
            {
                // ignored
            }
        }

        /// <inheritdoc/>
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        /// <inheritdoc/>
        public void Delete(string path)
        {
            File.Delete(path);
        }
    }
}

#endif