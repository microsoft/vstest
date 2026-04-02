// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ObjectModelTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace Microsoft.TestPlatform.Client.Async.UnitTests;

[TestClass]
public class TestRunResultTests
{
    [TestMethod]
    public void Constructor_SetsAllProperties()
    {
        var testCase = new TestCase("Test1", new Uri("executor://test"), "source.dll");
        var testResults = new List<ObjectModelTestResult>
        {
            new(testCase) { Outcome = TestOutcome.Passed },
        };
        var stats = new TestRunStatistics(1, new Dictionary<TestOutcome, long>
        {
            [TestOutcome.Passed] = 1,
        });
        var elapsed = TimeSpan.FromSeconds(2.5);

        var result = new TestRunResult(testResults, stats, isCanceled: false, isAborted: false, elapsed);

        Assert.AreEqual(1, result.TestResults.Count);
        Assert.IsNotNull(result.Statistics);
        Assert.IsFalse(result.IsCanceled);
        Assert.IsFalse(result.IsAborted);
        Assert.AreEqual(elapsed, result.ElapsedTime);
    }

    [TestMethod]
    public void Constructor_CanceledRun_SetsFlags()
    {
        var result = new TestRunResult(
            Array.Empty<ObjectModelTestResult>(),
            statistics: null,
            isCanceled: true,
            isAborted: false,
            TimeSpan.Zero);

        Assert.IsTrue(result.IsCanceled);
        Assert.IsFalse(result.IsAborted);
        Assert.IsNull(result.Statistics);
    }
}
