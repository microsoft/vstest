// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.TestPlatform.TestUtilities;

#nullable disable

namespace Microsoft.TestPlatform.PerformanceTests.PerfInstrumentation;

/// <summary>
/// The performance test base.
/// </summary>
public class PerformanceTestBase : IntegrationTestBase
{
    private readonly PerfAnalyzer _perfAnalyzer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceTestBase"/> class.
    /// </summary>
    public PerformanceTestBase()
        : base()
    {
#if NET
        
        throw new InvalidOperationException("Perf tests are not supported on .NET");
#else
        _perfAnalyzer = new PerfAnalyzer();
#endif
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
        _perfAnalyzer.EnableProvider();
#endif
        // Run Test
        InvokeVsTestForExecution(testAsset, testAdapterPath, ".NETFramework,Version=v4.5.1", runSettings);

        // Stop Listening
#if NETFRAMEWORK
        _perfAnalyzer.DisableProvider();
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
        _perfAnalyzer.EnableProvider();
#endif
        // Run Test
        InvokeVsTestForDiscovery(testAsset, testAdapterPath, runSettings, ".NETFramework,Version=v4.5.1");

        // Stop Listening
#if NETFRAMEWORK
        _perfAnalyzer.DisableProvider();
#endif
    }

    /// <summary>
    /// The analyze performance data.
    /// </summary>
    public void AnalyzePerfData()
    {
        _perfAnalyzer.AnalyzeEventsData();
    }

    /// <summary>
    /// The get execution time.
    /// </summary>
    /// <returns>
    /// The <see cref="double"/>.
    /// </returns>
    public TimeSpan GetExecutionTime()
    {
        return TimeSpan.FromMilliseconds(_perfAnalyzer.GetElapsedTimeByTaskName(Constants.ExecutionTask));
    }

    public TimeSpan GetDiscoveryTime()
    {
        return TimeSpan.FromMilliseconds(_perfAnalyzer.GetElapsedTimeByTaskName(Constants.DiscoveryTask));
    }

    public TimeSpan GetVsTestTime()
    {
        return TimeSpan.FromMilliseconds(_perfAnalyzer.GetElapsedTimeByTaskName(Constants.VsTestConsoleTask));
    }

    public TimeSpan GetTestHostTime()
    {
        return TimeSpan.FromMilliseconds(_perfAnalyzer.GetElapsedTimeByTaskName(Constants.TestHostTask));
    }

    public TimeSpan GetAdapterSearchTime()
    {
        return TimeSpan.FromMilliseconds(_perfAnalyzer.GetElapsedTimeByTaskName(Constants.AdapterSearchTask));
    }

    public IDictionary<string, string> GetDiscoveryData()
    {
        return _perfAnalyzer.GetEventDataByTaskName(Constants.AdapterDiscoveryTask);
    }

    public IDictionary<string, string> GetExecutionData()
    {
        return _perfAnalyzer.GetEventDataByTaskName(Constants.AdapterExecutionTask);
    }

    public TimeSpan GetAdapterExecutionTime(string executorUri)
    {
        return TimeSpan.FromMilliseconds(_perfAnalyzer.GetAdapterExecutionTime(executorUri));
    }
}
