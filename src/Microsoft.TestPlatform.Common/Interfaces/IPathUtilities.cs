// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Common.Interfaces
{
    using System.Collections.Generic;

    /// <summary>
    /// The utilities for file paths.
    /// </summary>
    public interface IPathUtilities
    {
        /// <summary>
        /// Removes duplicate and invalid paths from parameter sourcePaths
        /// </summary>
        /// <param name="sourcePaths"> The source Paths. </param>
        /// <returns> The list of unique, valid extension paths. </returns>
        HashSet<string> GetUniqueValidPaths(IEnumerable<string> sourcePaths);
    }
}