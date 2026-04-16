// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace Microsoft.TestPlatform.Client.Async;

/// <summary>
/// Result of a test run operation.
/// </summary>
public class TestRunResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestRunResult"/> class.
    /// </summary>
    public TestRunResult(
        IReadOnlyList<TestResult> testResults,
        ITestRunStatistics? statistics,
        bool isCanceled,
        bool isAborted,
        TimeSpan elapsedTime)
    {
        TestResults = testResults;
        Statistics = statistics;
        IsCanceled = isCanceled;
        IsAborted = isAborted;
        ElapsedTime = elapsedTime;
    }

    /// <summary>
    /// The test results collected during the run.
    /// </summary>
    public IReadOnlyList<TestResult> TestResults { get; }

    /// <summary>
    /// Test run statistics (pass/fail/skip counts).
    /// </summary>
    public ITestRunStatistics? Statistics { get; }

    /// <summary>
    /// Whether the test run was canceled.
    /// </summary>
    public bool IsCanceled { get; }

    /// <summary>
    /// Whether the test run was aborted.
    /// </summary>
    public bool IsAborted { get; }

    /// <summary>
    /// Total elapsed time for the test run.
    /// </summary>
    public TimeSpan ElapsedTime { get; }
}
