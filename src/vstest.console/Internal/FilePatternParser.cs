// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.Internal
{
    using Microsoft.Extensions.FileSystemGlobbing;
    using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Class for getting matching files from wild card pattern file name
    /// Microsoft.Extensions.FileSystemGlobbing methods used to get matching file names
    /// </summary>
    public class FilePatternParser
    {
        private Matcher matcher;
        private char[] wildCardCharacters = { '*' };

        public FilePatternParser()
            : this(new Matcher())
        {
        }

        internal FilePatternParser(Matcher matcher)
        {
            this.matcher = matcher;
        }

        /// <summary>
        /// Used to get matching files with pattern
        /// </summary>
        /// <returns>If the file is a valid pattern or full path. Returns true if it is valid pattern</returns>
        public bool IsValidPattern(string filePattern, out List<string> matchingFiles)
        {
            matchingFiles = new List<string>();

            // If there is no wildcard, return false.
            if(filePattern.IndexOfAny(wildCardCharacters) == -1)
            {
                EqtTrace.Info($"FilePatternParser: The given file {filePattern} is a full path.");
                return false;
            }

            // Split the given wildcard into search directory and pattern to be searched.
            var splitPattern = SplitFilePatternOnWildCard(filePattern);
            EqtTrace.Info($"FilePatternParser: Matching file pattern '{splitPattern.Item2}' within directory '{splitPattern.Item1}'");

            this.matcher.AddInclude(splitPattern.Item2);

            // Execute the given pattern in the search directory.
            var matches = this.matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(splitPattern.Item1)));

            // Add all the files to the list of matching files.
            foreach (var match in matches.Files)
            {
                matchingFiles.Add(Path.Combine(splitPattern.Item1, match.Path));
            }

            return true;
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
