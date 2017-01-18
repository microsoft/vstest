// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// The FileHelper interface.
    /// </summary>
    public interface IFileHelper
    {
        /// <summary>
        /// Creates a directory.
        /// </summary>
        /// <param name="path">Path of the directory.</param>
        /// <returns><see cref="DirectoryInfo"/> for the created directory.</returns>
        DirectoryInfo CreateDirectory(string path);

        /// <summary>
        /// Exists utility to check if file exists (case sensitive).
        /// </summary>
        /// <param name="path"> The path of file. </param>
        /// <returns>True if file exists <see cref="bool"/>.</returns>
        bool Exists(string path);

        /// <summary>
        /// Exists utility to check if directory exists (case sensitive).
        /// </summary>
        /// <param name="path"> The path of file. </param>
        /// <returns>True if directory exists <see cref="bool"/>.</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Gets a stream for the file.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="mode"><see cref="FileMode"/> for file operations.</param>
        /// <param name="access"><see cref="FileAccess"/> for file operations.</param>
        /// <returns>A <see cref="Stream"/> that supports read/write on the file.</returns>
        Stream GetStream(string filePath, FileMode mode, FileAccess access = FileAccess.ReadWrite);

        /// <summary>
        /// Enumerates files which match a pattern (case insensitive) in a directory.
        /// </summary>
        /// <param name="directory">Parent directory to search.</param>
        /// <param name="pattern">Search pattern.</param>
        /// <param name="searchOption"><see cref="SearchOption"/> for directory.</param>
        /// <returns>List of files matching the pattern.</returns>
        IEnumerable<string> EnumerateFiles(string directory, string pattern, SearchOption searchOption);

        /// <summary>
        /// Gets attributes of a file.
        /// </summary>
        /// <param name="path">Full path of the file.</param>
        /// <returns>Attributes of the file.</returns>
        FileAttributes GetFileAttributes(string path);

        /// <summary>
        /// Check if file path is rooted.
        /// </summary>
        /// <param name="path">Full path of the file.</param>
        /// <returns>Is file path rooted.</returns>
        bool IsRootedPath(string path);

        /// <summary>
        /// Create full file path.
        /// </summary>
        /// <param name="path">relative path of the file.</param>
        /// <returns>Full file path, based on current working directory.</returns>
        string CombinePath(string source);
    }
}
