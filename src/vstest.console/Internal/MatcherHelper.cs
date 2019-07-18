// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal
{
    using Microsoft.Extensions.FileSystemGlobbing;
    using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
    using vstest.console.Internal.Interfaces;

    /// <summary>
    /// Class for implementing the FileSystemGlobbing Matcher class methods
    /// </summary>
    internal class MatcherHelper: IMatcherHelper
    {
        private Matcher matcher;

        public MatcherHelper()
        {
            this.matcher = new Matcher();
        }

        /// <summary>
        /// Executes search in the given directory
        /// </summary>
        public PatternMatchingResult Execute(DirectoryInfoWrapper directoryInfo)
        {
            return this.matcher.Execute(directoryInfo);
        }

        /// <summary>
        /// Includes patterns to search in the matcher
        /// </summary>
        public void AddInclude(string pattern)
        {
            this.matcher.AddInclude(pattern);
        }
    }
}
