// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD1_0

namespace System.IO
{
    /// <summary>
    /// Specifies whether to search the current directory, or the current directory and
    /// all subdirectories.
    /// </summary>
    public enum SearchOption
    {
        /// <summary>
        /// Includes the current directory and all its subdirectories in a search operation.
        /// This option includes reparse points such as mounted drives and symbolic links
        /// in the search.
        /// </summary>
        AllDirectories = 1,

        /// <summary>
        /// Includes only the current directory in a search operation.
        /// </summary>
        TopDirectoryOnly = 0
    }
}

#endif