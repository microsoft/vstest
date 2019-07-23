// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.Internal
{
    using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using vstest.console.Internal.Interfaces;

    /// <summary>
    /// Class for getting matching files from wild card pattern file name
    /// Microsoft.Extensions.FileSystemGlobbing methods used to get matching file names
    /// </summary>
    public class FilePatternParser
    {
        private IMatcherHelper matcherHelper;
        private char[] wildCardCharacters = { '*' };

        public FilePatternParser()
            : this(new MatcherHelper())
        {
        }

        internal FilePatternParser(IMatcherHelper matcherHelper)
        {
            this.matcherHelper = matcherHelper;
        }

        /// <summary>
        /// Used to get matching files with pattern
        /// </summary>
        public IEnumerable<string> GetMatchingFiles(string filePattern)
        {
            var matchingFiles = new List<string>();

            // If there is no wildcard, return the filename as it is.
            if(filePattern.IndexOfAny(wildCardCharacters) == -1)
            {
                matchingFiles.Add(filePattern);
                return matchingFiles;
            }

            // Split the given wildcard into search directory and pattern to be searched.
            var splitPattern = SplitFilePatternOnWildCard(filePattern);
            this.matcherHelper.AddInclude(splitPattern.Item2);

            // Execute the given pattern in the search directory.
            var matches = this.matcherHelper.Execute(new DirectoryInfoWrapper(new DirectoryInfo(splitPattern.Item1)));

            // Add all the files to the list of matching files.
            foreach(var match in matches.Files)
            {
                matchingFiles.Add(Path.Combine(splitPattern.Item1, match.Path));
            }

            return matchingFiles;
        }

        /// <summary>
        /// Splits full pattern into search directory and pattern.
        /// </summary>
        private Tuple<string, string> SplitFilePatternOnWildCard(string filePattern)
        {
            // Split the pattern based on first wildcard character found.
            var splitOnWildCardIndex = filePattern.IndexOfAny(wildCardCharacters);
            var directorySeparatorIndex = filePattern.Substring(0, splitOnWildCardIndex).LastIndexOf(Path.DirectorySeparatorChar);

            string searchDir = filePattern.Substring(0, directorySeparatorIndex);
            string pattern = filePattern.Substring(directorySeparatorIndex + 1);

            Tuple<string, string> splitPattern = new Tuple<string, string>(searchDir, pattern);
            return splitPattern;
        }
    }
}
