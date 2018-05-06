// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Collector
{
    using System.IO;
    using Coverage.Interfaces;

    /// <inheritdoc />
    internal class FileHelper : IFileHelper
    {
        /// <inheritdoc />
        public bool Exists(string path)
        {
            return File.Exists(path);
        }
    }
}