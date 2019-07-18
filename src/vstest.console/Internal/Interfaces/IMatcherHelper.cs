// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.Internal.Interfaces
{
    using Microsoft.Extensions.FileSystemGlobbing;
    using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

    /// <summary>
    /// Interface for wrapping the FileSystemGlobbing Matcher class
    /// </summary>
    internal interface IMatcherHelper
    {
        /// <summary>
        /// Executes search in the given directory
        /// </summary>
        PatternMatchingResult Execute(DirectoryInfoWrapper directoryInfo);

        /// <summary>
        /// Includes patterns to search in the matcher
        /// </summary>
        void AddInclude(string pattern);
    }
}
