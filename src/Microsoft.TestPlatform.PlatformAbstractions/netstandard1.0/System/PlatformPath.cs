// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.PlatformAbstractions
{
    using System.IO;

    /// <summary>
    /// File Abstraction
    /// </summary>
    public static class PlatformPath
    {
        /// <summary>
        /// Checks if give file exists on disk
        /// </summary>
        /// <param name="filePath">input filePath</param>
        /// <param name="fileName">output fileName</param>
        public static void TryGetFileName(string filePath, out string fileName)
        {
            throw new FileNotFoundException();
        }
    }
}
