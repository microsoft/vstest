// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// The file helper.
    /// </summary>
    public class FileHelper : IFileHelper
    {
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
        public IEnumerable<string> EnumerateFiles(string directory, string pattern, SearchOption searchOption)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return Directory.EnumerateFiles(directory, "*", searchOption).Where(f => regex.IsMatch(f));
        }

        /// <inheritdoc/>
        public FileAttributes GetFileAttributes(string path)
        {
            return new FileInfo(path).Attributes;
        }

        /// <summary>
        /// Opens a FileStream on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">A FileMode value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
        /// <param name="access">A FileAccess value that specifies the operations that can be performed on the file.</param>
        /// <param name="share">A FileShare value specifying the type of access other threads have to the file.</param>
        /// <returns>A FileStream on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</returns>
        public FileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return File.Open(path, mode, access, share);
        }
    }
}
