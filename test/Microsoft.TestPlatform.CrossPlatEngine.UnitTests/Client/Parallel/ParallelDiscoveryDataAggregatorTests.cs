﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestPlatform.CrossPlatEngine.UnitTests.Client.Parallel;

[TestClass]
public class ParallelDiscoveryDataAggregatorTests
{
    [TestMethod]
    public void AggregateShouldAggregateAbortedCorrectly()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        aggregator.Aggregate(totalTests: 5, isAborted: false, discoveredExtensions: null);
        Assert.IsFalse(aggregator.IsAborted, "Aborted must be false");

        aggregator.Aggregate(totalTests: 5, isAborted: true, discoveredExtensions: null);
        Assert.IsTrue(aggregator.IsAborted, "Aborted must be true");

        aggregator.Aggregate(totalTests: 5, isAborted: false, discoveredExtensions: null);
        Assert.IsTrue(aggregator.IsAborted, "Aborted must be true");

        Assert.AreEqual(-1, aggregator.TotalTests, "Aggregator shouldn't count tests if one host aborts");
    }

    [TestMethod]
    public void AggregateShouldAggregateTotalTestsCorrectly()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();
        aggregator.Aggregate(totalTests: 2, isAborted: false, discoveredExtensions: null);
        Assert.AreEqual(2, aggregator.TotalTests, "Aggregated totalTests count does not match");

        aggregator.Aggregate(totalTests: 5, isAborted: false, discoveredExtensions: null);
        Assert.AreEqual(7, aggregator.TotalTests, "Aggregated totalTests count does not match");

        aggregator.Aggregate(totalTests: 3, isAborted: false, discoveredExtensions: null);
        Assert.AreEqual(10, aggregator.TotalTests, "Aggregated totalTests count does not match");
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldAggregateMetricsCorrectly()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        aggregator.AggregateMetrics(null);

        var runMetrics = aggregator.GetMetrics();
        Assert.AreEqual(0, runMetrics.Count);
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldAddTotalTestsDiscovered()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var metrics = new Dictionary<string, object>
        {
            [TelemetryDataConstants.TotalTestsDiscovered] = 2,
        };

        aggregator.AggregateMetrics(metrics);
        aggregator.AggregateMetrics(metrics);

        var runMetrics = aggregator.GetMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TotalTestsDiscovered, out var value));
        Assert.AreEqual(4, Convert.ToInt32(value));
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldAddTimeTakenToDiscoverTests()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var metrics = new Dictionary<string, object>
        {
            [TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter] = .02091,
        };

        aggregator.AggregateMetrics(metrics);
        aggregator.AggregateMetrics(metrics);

        var runMetrics = aggregator.GetMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter, out var value));
        Assert.AreEqual(.04182, value);
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldAddTimeTakenByAllAdapters()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var metrics = new Dictionary<string, object>
        {
            [TelemetryDataConstants.TimeTakenInSecByAllAdapters] = .02091,
        };

        aggregator.AggregateMetrics(metrics);
        aggregator.AggregateMetrics(metrics);

        var runMetrics = aggregator.GetMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenInSecByAllAdapters, out var value));
        Assert.AreEqual(.04182, value);
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldAddTimeTakenToLoadAdapters()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var metrics = new Dictionary<string, object>
        {
            [TelemetryDataConstants.TimeTakenToLoadAdaptersInSec] = .02091,
        };

        aggregator.AggregateMetrics(metrics);
        aggregator.AggregateMetrics(metrics);

        var runMetrics = aggregator.GetMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec, out var value));
        Assert.AreEqual(.04182, value);
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldNotAggregateDiscoveryState()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var metrics = new Dictionary<string, object>
        {
            [TelemetryDataConstants.DiscoveryState] = "Completed",
        };

        aggregator.AggregateMetrics(metrics);
        aggregator.AggregateMetrics(metrics);

        var runMetrics = aggregator.GetMetrics();
        Assert.IsFalse(runMetrics.TryGetValue(TelemetryDataConstants.DiscoveryState, out _));
    }

    [TestMethod]
    public void GetAggregatedDiscoveryDataMetricsShouldReturnEmptyIfMetricAggregatorIsEmpty()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        aggregator.AggregateMetrics(new Dictionary<string, object>());
        var runMetrics = aggregator.GetMetrics();

        Assert.AreEqual(0, runMetrics.Count);
    }

    [TestMethod]
    public void GetAggregatedDiscoveryDataMetricsShouldReturnEmptyIfMetricsIsNull()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        aggregator.AggregateMetrics(null);
        var runMetrics = aggregator.GetMetrics();

        Assert.AreEqual(0, runMetrics.Count);
    }

    [TestMethod]
    public void GetDiscoveryDataMetricsShouldAddTotalAdaptersUsedIfMetricsIsNotEmpty()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var metrics = new Dictionary<string, object>
        {
            [TelemetryDataConstants.TotalTestsByAdapter] = 2,
        };

        aggregator.AggregateMetrics(metrics);
        aggregator.AggregateMetrics(metrics);

        var runMetrics = aggregator.GetMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests, out var value));
        Assert.AreEqual(1, value);
    }

    [TestMethod]
    public void GetDiscoveryDataMetricsShouldAddNumberOfAdapterDiscoveredIfMetricsIsEmpty()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var metrics = new Dictionary<string, object>
        {
            [TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter + "executor:MSTestV1"] = .02091,
            [TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter + "executor:MSTestV2"] = .02091,
        };

        aggregator.AggregateMetrics(metrics);

        var runMetrics = aggregator.GetMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery, out var value));
        Assert.AreEqual(2, value);
    }

    [TestMethod]
    public void GetDiscoveryDataMetricsShouldNotAddTotalAdaptersUsedIfMetricsIsEmpty()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        aggregator.AggregateMetrics(new Dictionary<string, object>());

        var runMetrics = aggregator.GetMetrics();
        Assert.IsFalse(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests, out _));
    }

    [TestMethod]
    public void GetDiscoveryDataMetricsShouldNotAddNumberOfAdapterDiscoveredIfMetricsIsEmpty()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        aggregator.AggregateMetrics(new Dictionary<string, object>());

        var runMetrics = aggregator.GetMetrics();
        Assert.IsFalse(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery, out _));
    }

    [TestMethod]
    public void AggregateShouldAggregateMessageSentCorrectly()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        aggregator.AggregateIsMessageSent(isMessageSent: false);
        Assert.IsFalse(aggregator.IsMessageSent, "Aborted must be false");

        aggregator.AggregateIsMessageSent(isMessageSent: true);
        Assert.IsTrue(aggregator.IsMessageSent, "Aborted must be true");

        aggregator.AggregateIsMessageSent(isMessageSent: false);
        Assert.IsTrue(aggregator.IsMessageSent, "Aborted must be true");
    }

    [DataTestMethod]
    [DataRow(DiscoveryStatus.FullyDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    [DataRow(DiscoveryStatus.NotDiscovered)]
    public void MarkSourcesWithStatusWhenSourcesIsNullDoesNothing(DiscoveryStatus discoveryStatus)
    {
        // Arrange
        var dataAggregator = new ParallelDiscoveryDataAggregator();

        // Act
        dataAggregator.MarkSourcesWithStatus(null, discoveryStatus);

        // Assert
        Assert.AreEqual(0, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);
        Assert.AreEqual(0, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
        Assert.AreEqual(0, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered).Count);
    }

    [DataTestMethod]
    [DataRow(DiscoveryStatus.FullyDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    [DataRow(DiscoveryStatus.NotDiscovered)]
    public void MarkSourcesWithStatusIgnoresNullSources(DiscoveryStatus discoveryStatus)
    {
        // Arrange
        var dataAggregator = new ParallelDiscoveryDataAggregator();
        var sources = new[] { "a", null, "b" };

        // Sanity check
        Assert.AreEqual(0, dataAggregator.GetSourcesWithStatus(discoveryStatus).Count);

        // Act
        dataAggregator.MarkSourcesWithStatus(sources, discoveryStatus);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, dataAggregator.GetSourcesWithStatus(discoveryStatus));
    }

    [DataTestMethod]
    [DataRow(DiscoveryStatus.FullyDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    public void MarkSourcesWithStatusWhenSourceAddedAndStatusDifferentFromNotDiscoveredLogsWarning(DiscoveryStatus discoveryStatus)
    {
        // Arrange
        var dataAggregator = new ParallelDiscoveryDataAggregator();

        // Act
        dataAggregator.MarkSourcesWithStatus(new[] { "a" }, discoveryStatus);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a" }, dataAggregator.GetSourcesWithStatus(discoveryStatus));
    }

    [DataTestMethod]
    [DataRow(DiscoveryStatus.NotDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    public void MarkSourcesWithStatusWhenSourceStatusWasFullyDiscoveredAndIsDowngradedLogsWarning(DiscoveryStatus discoveryStatus)
    {
        // Arrange
        var dataAggregator = new ParallelDiscoveryDataAggregator();
        dataAggregator.MarkSourcesWithStatus(new[] { "a" }, DiscoveryStatus.FullyDiscovered);

        // Act
        dataAggregator.MarkSourcesWithStatus(new[] { "a" }, discoveryStatus);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a" }, dataAggregator.GetSourcesWithStatus(discoveryStatus));
    }

    [TestMethod]
    public void MarkSourcesWithStatusWhenSourceStatusWasPartiallyDiscoveredAndIsDowngradedLogsWarning()
    {
        // Arrange
        var dataAggregator = new ParallelDiscoveryDataAggregator();
        dataAggregator.MarkSourcesWithStatus(new[] { "a" }, DiscoveryStatus.PartiallyDiscovered);

        // Act
        dataAggregator.MarkSourcesWithStatus(new[] { "a" }, DiscoveryStatus.NotDiscovered);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a" }, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered));
    }

    [TestMethod]
    public void MarkSourcesBasedOnDiscoveredTestCasesHandlesTestCasesFromMultipleSources()
    {
        // Arrange
        var dataAggregator = new ParallelDiscoveryDataAggregator();
        string? previousSource = null;
        var testCases = new TestCase[]
        {
            new() { Source = "a" },
            new() { Source = "a" },
            new() { Source = "a" },
            new() { Source = "b" },
            new() { Source = "c" },
        };

        // Act
        dataAggregator.MarkSourcesBasedOnDiscoveredTestCases(testCases, false, ref previousSource);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        CollectionAssert.AreEquivalent(new[] { "c" }, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
    }

    [TestMethod]
    public void MarkSourcesBasedOnDiscoveredTestCasesReuseLastDiscoveredSource()
    {
        // Arrange
        var dataAggregator = new ParallelDiscoveryDataAggregator();
        string? previousSource = null;
        dataAggregator.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "a" } }, false, ref previousSource);

        // Act
        dataAggregator.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "b" }, }, false, ref previousSource);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a" }, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        CollectionAssert.AreEquivalent(new[] { "b" }, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void MarkSourcesBasedOnDiscoveredTestCasesWhenCompleteMarksLastSourceAsFullyDiscovered(bool trueIsEmptyFalseIsNull)
    {
        // Arrange
        var dataAggregator = new ParallelDiscoveryDataAggregator();
        string? previousSource = null;
        dataAggregator.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "a" } }, false, ref previousSource);

        // Sanity check
        CollectionAssert.AreEquivalent(new[] { "a" }, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
        Assert.AreEqual(0, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);

        // Act
        dataAggregator.MarkSourcesBasedOnDiscoveredTestCases(trueIsEmptyFalseIsNull ? Enumerable.Empty<TestCase>() : null, true, ref previousSource);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a" }, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        Assert.AreEqual(0, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
    }

    [TestMethod]
    public void MarkSourcesBasedOnDiscoveredTestCasesWhenCompleteMarksAllSourcesAsFullyDiscovered()
    {
        // Arrange
        var dataAggregator = new ParallelDiscoveryDataAggregator();
        string? previousSource = null;
        dataAggregator.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[] { new() { Source = "a" } }, false, ref previousSource);

        // Sanity check
        CollectionAssert.AreEquivalent(new[] { "a" }, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
        Assert.AreEqual(0, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);

        // Act
        dataAggregator.MarkSourcesBasedOnDiscoveredTestCases(new TestCase[]
            {
                new() { Source = "b" },
                new() { Source = "c" },
            }, true,
            ref previousSource);

        // Assert
        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
        Assert.AreEqual(0, dataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
    }

    [DataTestMethod]
    [DataRow(DiscoveryStatus.FullyDiscovered)]
    [DataRow(DiscoveryStatus.PartiallyDiscovered)]
    [DataRow(DiscoveryStatus.NotDiscovered)]
    public void GetSourcesWithStatusWhenEmptyDictionaryReturnsEmptyList(DiscoveryStatus discoveryStatus)
    {
        var instanceSources = new ParallelDiscoveryDataAggregator().GetSourcesWithStatus(discoveryStatus);
        Assert.AreEqual(0, instanceSources.Count);
    }

    [TestMethod]
    public void GetSourcesWithStatusCorrectlyFiltersSources()
    {
        // Arrange
        var dataAggregator = new ParallelDiscoveryDataAggregator();
        var notDiscoveredSources = new[]
        {
            "not1",
            "not2",
            "not3",
        };
        var partiallyDiscoveredSources = new[]
        {
            "partially1",
            "partially2",
            "partially3",
            "partially4",
        };
        var fullyDiscoveredSources = new[]
        {
            "fully1",
            "fully2",
            "fully3",
            "fully4",
            "fully5",
        };

        dataAggregator.MarkSourcesWithStatus(notDiscoveredSources, DiscoveryStatus.NotDiscovered);

        dataAggregator.MarkSourcesWithStatus(partiallyDiscoveredSources, DiscoveryStatus.NotDiscovered);
        dataAggregator.MarkSourcesWithStatus(partiallyDiscoveredSources, DiscoveryStatus.PartiallyDiscovered);

        dataAggregator.MarkSourcesWithStatus(fullyDiscoveredSources, DiscoveryStatus.NotDiscovered);
        dataAggregator.MarkSourcesWithStatus(fullyDiscoveredSources, DiscoveryStatus.FullyDiscovered);

        // Act
        var actualFullyDiscovered = dataAggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered);
        var actualPartiallyDiscovered = dataAggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered);
        var actualNotDiscovered = dataAggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered);

        // Assert
        CollectionAssert.AreEquivalent(fullyDiscoveredSources, actualFullyDiscovered);
        CollectionAssert.AreEquivalent(partiallyDiscoveredSources, actualPartiallyDiscovered);
        CollectionAssert.AreEquivalent(notDiscoveredSources, actualNotDiscovered);
    }
}
