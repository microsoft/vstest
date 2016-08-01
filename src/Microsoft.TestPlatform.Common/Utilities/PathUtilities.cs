// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Utilities
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    
    /// <summary>
    /// The utilities for file paths.
    /// </summary>
    internal class PathUtilities : IPathUtilities
    {
        /// <summary>
        /// Removes duplicate and invalid paths from parameter sourcePaths
        /// </summary>
        /// <param name="paths"> The Paths. </param>
        /// <returns> The list of unique, valid extension paths. </returns>
        public HashSet<string> GetUniqueValidPaths(IEnumerable<string> paths)
        {
            var result = new HashSet<string>();

            if (paths == null)
            {
                return result;
            }

            foreach (var sourcePath in paths)
            {
                var fullPath = Path.GetFullPath(sourcePath);

                if (File.Exists(fullPath))
                {
                    result.Add(fullPath);
                }
                else
                {
                    EqtTrace.Verbose("TestPluginCache: Ignoring extension {0} as it does not exist.", result);
                }
            }

            return result;
        }
    }
}
