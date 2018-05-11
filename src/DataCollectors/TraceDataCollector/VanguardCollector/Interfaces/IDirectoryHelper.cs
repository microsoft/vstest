// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage.Interfaces
{
    /// <summary>
    /// Interface to Abstraction System.IO.Directory funcationalities for mocking Directory functionalities
    /// in unit tests.
    /// </summary>
    internal interface IDirectoryHelper
    {
        /// <summary>
        ///  Deletes the specified directory and, if indicated, any subdirectories and files in the directory.
        /// </summary>
        /// <param name="path">The name of the directory to remove.</param>
        /// <param name="recursive">true to remove directories, subdirectories, and files in path; otherwise, false.</param>
        void Delete(string path, bool recursive);

        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        /// <param name="path">The directory to create.</param>
        void CreateDirectory(string path);

        /// <summary>
        /// Determines whether the given path refers to an existing directory on disk.
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <returns>true if path refers to an existing directory;
        /// false if the directory does not exist or an error occurs when trying to determine if the specified directory exists.</returns>
        bool Exists(string path);
    }
}