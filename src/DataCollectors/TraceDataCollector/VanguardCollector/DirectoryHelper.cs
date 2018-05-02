// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Collector
{
    using System.IO;
    using Coverage.Interfaces;

    public class DirectoryHelper : IDirectoryHelper
    {
        /// <inheritdoc />
        public void Delete(string directoryPath, bool recursive)
        {
            Directory.Delete(directoryPath, recursive);
        }

        /// <inheritdoc />
        public void CreateDirectory(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}