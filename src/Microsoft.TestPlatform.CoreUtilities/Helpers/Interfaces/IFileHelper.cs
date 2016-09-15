// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces
{
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
    }
}
