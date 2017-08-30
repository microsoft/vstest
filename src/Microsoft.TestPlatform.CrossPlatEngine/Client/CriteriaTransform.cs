// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using System.Linq;

    internal static class CriteriaTransform
    {

        public static void UpdateDiscoveryCriteria(DiscoveryCriteria discoveryCriteria, ITestRuntimeProvider testRuntimeProvider)
        {
            var actualTestSources = testRuntimeProvider.GetTestSources(discoveryCriteria.Sources);

            // If the actual testSources, & input test sources do differ it means that the User(IDE) actually sent a package.
            // We are limiting that only one package can be sent per session, so picking the first entry in sources
            if (discoveryCriteria.Sources.Except(actualTestSources).Any())
            {
                discoveryCriteria.Package = discoveryCriteria.Sources.FirstOrDefault();

                // Allow TestRuntimeProvider to update source map, this is required for remote scenarios.
                // If we run for specific tests, then we expect the test case object to contain correct source path for remote scenario as well
                UpdateTestSources(actualTestSources, discoveryCriteria.AdapterSourceMap);
            }
        }

        public static void UpdateTestRunCriteriaForSources(TestRunCriteria testRunCriteria, ITestRuntimeProvider testRuntimeProvider, ref List<string> testPackages)
        {
            var actualTestSources = testRuntimeProvider.GetTestSources(testPackages);

            // For netcore/fullclr both packages and sources are same thing, 
            // For UWP the actual source(exe) differs from input source(.appxrecipe) which we call package.
            // So in such models we check if they differ, then we pass this info to test host to update TestCase source with package info,
            // since this is needed by IDE's to map a TestCase to project.
            var testSourcesDiffer = testPackages.Except(actualTestSources).Any();

            if (testSourcesDiffer)
            {
                UpdateTestSources(actualTestSources, testRunCriteria.AdapterSourceMap);
            }
            else
            {
                testPackages = null;
            }
        }

        public static void UpdateTestRunCriteriaForTests(TestRunCriteria testRunCriteria, ITestRuntimeProvider testRuntimeProvider, ref List<string> testPackages)
        {
            var actualTestSources = testRuntimeProvider.GetTestSources(testPackages);

            // For netcore/fullclr both packages and sources are same thing, 
            // For UWP the actual source(exe) differs from input source(.appxrecipe) which we call package.
            // So in such models we check if they differ, then we pass this info to test host to update TestCase source with package info,
            // since this is needed by IDE's to map a TestCase to project.
            var testSourcesDiffer = testPackages.Except(actualTestSources).Any();

            // In UWP scenario TestCase object contains the package as source, which is not actual test source for adapters, 
            // so update test case before sending them.
            // We are limiting that a testhost will always run for a single package, A package can contain multiple sources
            if (testSourcesDiffer)
            {
                testRunCriteria.Tests.ToList().ForEach(tc => tc.Source = actualTestSources.FirstOrDefault());
            }
            else
            {
                testPackages = null;
            }
        }

        /// <summary>
        /// Update the AdapterSourceMap
        /// </summary>
        /// <param name="sources">actual test sources</param>
        /// <param name="adapterSourceMap">Adapter Source Map</param>
        private static void UpdateTestSources(IEnumerable<string> sources, Dictionary<string, IEnumerable<string>> adapterSourceMap)
        {
            adapterSourceMap.Clear();
            adapterSourceMap.Add(Constants.UnspecifiedAdapterPath, sources);
        }
    }
}
