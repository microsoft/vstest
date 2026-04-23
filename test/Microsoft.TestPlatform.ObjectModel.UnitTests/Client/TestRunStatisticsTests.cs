// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.ObjectModel.UnitTests.Client;

[TestClass]
public class TestRunStatisticsTests
{
    [TestMethod]
    public void IndexerShouldReturnZeroWhenStatsIsNull()
    {
        var stats = new TestRunStatistics(null);

        Assert.AreEqual(0L, stats[TestOutcome.Passed]);
        Assert.AreEqual(0L, stats[TestOutcome.Failed]);
        Assert.AreEqual(0L, stats[TestOutcome.Skipped]);
    }

    [TestMethod]
    public void IndexerShouldReturnZeroWhenOutcomeNotInStats()
    {
        var statsDict = new Dictionary<TestOutcome, long>
        {
            { TestOutcome.Passed, 10 }
        };
        var stats = new TestRunStatistics(statsDict);

        Assert.AreEqual(0L, stats[TestOutcome.Failed]);
        Assert.AreEqual(0L, stats[TestOutcome.Skipped]);
    }

    [TestMethod]
    public void IndexerShouldReturnCountWhenOutcomeIsInStats()
    {
        var statsDict = new Dictionary<TestOutcome, long>
        {
            { TestOutcome.Passed, 5 },
            { TestOutcome.Failed, 3 },
            { TestOutcome.Skipped, 1 }
        };
        var stats = new TestRunStatistics(statsDict);

        Assert.AreEqual(5L, stats[TestOutcome.Passed]);
        Assert.AreEqual(3L, stats[TestOutcome.Failed]);
        Assert.AreEqual(1L, stats[TestOutcome.Skipped]);
    }

    [TestMethod]
    public void ExecutedTestsShouldBeSettableAndGettable()
    {
        var stats = new TestRunStatistics(executedTests: 42, stats: null);

        Assert.AreEqual(42L, stats.ExecutedTests);
    }
}
