// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Utilities.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// The file helper.
    /// </summary>
    public class FileHelper : IFileHelper
    {
        /// <summary>
        /// Exists utility to check if file exists
        /// </summary>
        /// <param name="path"> The path of file. </param>
        /// <returns> True if file exists <see cref="bool"/>. </returns>
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        /// <inheritdoc/>
        public Stream GetStream(string filePath, FileMode mode)
        {
            return new FileStream(filePath, mode);
        }

        /// <inheritdoc/>
        IEnumerable<string> IFileHelper.EnumerateFiles(string directory, string pattern, SearchOption searchOption)
        {
            return Directory.EnumerateFiles(directory, pattern, searchOption);
        }
    }
}
