﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.TestUtilities.PerfInstrumentation
{
    using System.Collections.Generic;

    /// <summary>
    /// The performance test base.
    /// </summary>
    public class PerformanceTestBase : IntegrationTestBase
    {
        private readonly PerfAnalyzer perfAnalyzer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceTestBase"/> class.
        /// </summary>
        public PerformanceTestBase()
            : base()
        {
            perfAnalyzer = new PerfAnalyzer();
        }

        /// <summary>
        /// The run execution performance tests.
        /// </summary>
        /// <param name="testAsset">
        /// The test asset.
        /// </param>
        /// <param name="testAdapterPath">
        /// The test adapter path.
        /// </param>
        /// <param name="runSettings">
        /// The run settings.
        /// </param>
        public void RunExecutionPerformanceTests(string testAsset, string testAdapterPath, string runSettings)
        {
            // Start session and listen
#if NETFRAMEWORK
            perfAnalyzer.EnableProvider();
#endif
            // Run Test
            InvokeVsTestForExecution(testAsset, testAdapterPath, ".NETFramework,Version=v4.5.1", runSettings);

            // Stop Listening
#if NETFRAMEWORK
            perfAnalyzer.DisableProvider();
#endif
        }

        /// <summary>
        /// The run discovery performance tests.
        /// </summary>
        /// <param name="testAsset">
        /// The test asset.
        /// </param>
        /// <param name="testAdapterPath">
        /// The test adapter path.
        /// </param>
        /// <param name="runSettings">
        /// The run settings.
        /// </param>
        public void RunDiscoveryPerformanceTests(string testAsset, string testAdapterPath, string runSettings)
        {
            // Start session and listen
#if NETFRAMEWORK
            perfAnalyzer.EnableProvider();
#endif
            // Run Test
            InvokeVsTestForDiscovery(testAsset, testAdapterPath, runSettings, ".NETFramework,Version=v4.5.1");

            // Stop Listening
#if NETFRAMEWORK
            perfAnalyzer.DisableProvider();
#endif
        }

        /// <summary>
        /// The analyze performance data.
        /// </summary>
        public void AnalyzePerfData()
        {
            perfAnalyzer.AnalyzeEventsData();
        }

        /// <summary>
        /// The get execution time.
        /// </summary>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public double GetExecutionTime()
        {
            return perfAnalyzer.GetElapsedTimeByTaskName(Constants.ExecutionTask);
        }

        public double GetDiscoveryTime()
        {
            return perfAnalyzer.GetElapsedTimeByTaskName(Constants.DiscoveryTask);
        }

        public double GetVsTestTime()
        {
            return perfAnalyzer.GetElapsedTimeByTaskName(Constants.VsTestConsoleTask);
        }

        public double GetTestHostTime()
        {
            return perfAnalyzer.GetElapsedTimeByTaskName(Constants.TestHostTask);
        }

        public double GetAdapterSearchTime()
        {
            return perfAnalyzer.GetElapsedTimeByTaskName(Constants.AdapterSearchTask);
        }

        public IDictionary<string,string> GetDiscoveryData()
        {
            return perfAnalyzer.GetEventDataByTaskName(Constants.AdapterDiscoveryTask);
        }

        public IDictionary<string, string> GetExecutionData()
        {
            return perfAnalyzer.GetEventDataByTaskName(Constants.AdapterExecutionTask);
        }

        public double GetAdapterExecutionTime(string executorUri)
        {
            return perfAnalyzer.GetAdapterExecutionTime(executorUri);
        }
    }
}
