// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Utilities
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Test sources determiner utility class
    /// </summary>
    internal class TestSourceDeterminer
    {
        /// <summary>
        /// Gets test sources from adapter source map
        /// </summary>
        /// <param name="adapterSourceMap"> The test list. </param>
        /// <returns> List of test Sources </returns>
        internal static IEnumerable<string> GetSources(Dictionary<string, IEnumerable<string>> adapterSourceMap)
        {
            IEnumerable<string> sources = new List<string>();
            return adapterSourceMap?.Values.Aggregate(sources, (current, enumerable) => current.Concat(enumerable));
        }

        /// <summary>
        /// Gets test sources from test case list
        /// </summary>
        /// <param name="tests"> The test list. </param>
        /// <returns> List of test Sources </returns>
        internal static IEnumerable<string> GetSources(IEnumerable<TestCase> tests)
        {
            return tests.Select(tc => tc.Source).Distinct();
        }

        /// <summary>
        /// Gets default code base path for inproc collector from test sources
        /// </summary>
        /// <param name="adapterSourceMap"> The test list. </param>
        /// <returns> List of test Sources </returns>
        internal static string GetDefaultCodebasePath(Dictionary<string, IEnumerable<string>> adapterSourceMap)
        {
            return Path.GetDirectoryName(GetSources(adapterSourceMap).FirstOrDefault());
        }

        /// <summary>
        /// Gets default code base path for inproc collector from test sources
        /// </summary>
        /// <param name="tests"> The test list. </param>
        /// <returns> List of test Sources </returns>
        internal static string GetDefaultCodebasePath(IEnumerable<TestCase> tests)
        {
            return Path.GetDirectoryName(GetSources(tests).FirstOrDefault());
        }
    }
}
