// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities
{
    using ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using TestPlatform.Utilities.Helpers;
    using TestPlatform.Utilities.Helpers.Interfaces;
    using Resources = Resources;

    /// <summary>
    /// Utility class for creating Adapter source map.
    /// </summary>
    public static class AdapterSourceMapUtilities
    {
        /// <summary>
        /// Returns the dictionary of TestAdapterPaths and AssemblyPaths
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        public static Dictionary<string, IEnumerable<string>> GetTestRunnerAndAssemblyInfo(IEnumerable<string> sources)
        {
            var resultDictionary = new Dictionary<string, IEnumerable<string>>();

            foreach (var source in sources)
            {
                string assemblyPath = source;
                string testRunnerPath = Constants.UnspecifiedAdapterPath;
                IEnumerable<string> assemblySources;
                if (resultDictionary.TryGetValue(testRunnerPath, out assemblySources))
                {
                    assemblySources = assemblySources.Concat(new List<string> { assemblyPath });
                    resultDictionary[testRunnerPath] = assemblySources;
                }
                else
                {
                    resultDictionary.Add(testRunnerPath, new List<string> { assemblyPath });
                }
            }
            return resultDictionary;
        }                
    }
}
