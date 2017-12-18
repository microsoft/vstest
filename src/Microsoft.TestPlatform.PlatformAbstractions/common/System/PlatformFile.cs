// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System.IO;

    /// <summary>
    /// File Abstraction
    /// </summary>
    public static class PlatformFile
    {
        /// <summary>
        /// Checks if give file exists on disk
        /// </summary>
        /// <param name="filePath">input filePath</param>
        /// <returns>True if file Exists</returns>
        public static bool Exists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}
