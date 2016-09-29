// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces
{
    using System.IO;

    /// <summary>
    /// The FileHelper interface.
    /// </summary>
    public interface IFileHelper
    {
        /// <summary>
        /// Exists utility to check if file exists
        /// </summary>
        /// <param name="path"> The path of file. </param>
        /// <returns> True if file exists <see cref="bool"/>. </returns>
        bool Exists(string path);

        /// <summary>
        /// Gets a stream for the file.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="mode"><see cref="FileMode"/> for file operations.</param>
        /// <returns>A <see cref="Stream"/> that supports read/write on the file.</returns>
        Stream GetStream(string filePath, FileMode mode);
    }
}
