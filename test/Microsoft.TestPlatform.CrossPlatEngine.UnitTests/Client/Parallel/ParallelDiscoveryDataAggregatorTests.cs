// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
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

        aggregator.Aggregate(new(totalTests: 5, isAborted: false), discoveredExtensions: null);
        Assert.IsFalse(aggregator.IsAborted, "Aborted must be false");

        aggregator.Aggregate(new(totalTests: 5, isAborted: true), discoveredExtensions: null);
        Assert.IsTrue(aggregator.IsAborted, "Aborted must be true");

        aggregator.Aggregate(new(totalTests: 5, isAborted: false), discoveredExtensions: null);
        Assert.IsTrue(aggregator.IsAborted, "Aborted must be true");

        Assert.AreEqual(-1, aggregator.TotalTests, "Aggregator shouldn't count tests if one host aborts");
    }

    [TestMethod]
    public void AggregateShouldAggregateTotalTestsCorrectly()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();
        aggregator.Aggregate(new(totalTests: 2, isAborted: false), discoveredExtensions: null);
        Assert.AreEqual(2, aggregator.TotalTests, "Aggregated totalTests count does not match");

        aggregator.Aggregate(new(totalTests: 5, isAborted: false), discoveredExtensions: null);
        Assert.AreEqual(7, aggregator.TotalTests, "Aggregated totalTests count does not match");

        aggregator.Aggregate(new(totalTests: 3, isAborted: false), discoveredExtensions: null);
        Assert.AreEqual(10, aggregator.TotalTests, "Aggregated totalTests count does not match");
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldAggregateMetricsCorrectly()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        aggregator.Aggregate(new(0, false) { Metrics = null });

        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();
        Assert.AreEqual(0, runMetrics.Count);
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldAddTotalTestsDiscovered()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = new Dictionary<string, object>
            {
                { TelemetryDataConstants.TotalTestsDiscovered, 2 },
            }
        };

        aggregator.Aggregate(args);
        aggregator.Aggregate(args);

        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TotalTestsDiscovered, out var value));
        Assert.AreEqual(4, Convert.ToInt32(value));
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldAddTimeTakenToDiscoverTests()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = new Dictionary<string, object>
            {
                { TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter, .02091 }
            },
        };

        aggregator.Aggregate(args);
        aggregator.Aggregate(args);

        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter, out var value));
        Assert.AreEqual(.04182, value);
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldAddTimeTakenByAllAdapters()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = new Dictionary<string, object>
            {
                { TelemetryDataConstants.TimeTakenInSecByAllAdapters, .02091 }
            },
        };

        aggregator.Aggregate(args);
        aggregator.Aggregate(args);

        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenInSecByAllAdapters, out var value));
        Assert.AreEqual(.04182, value);
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldAddTimeTakenToLoadAdapters()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = new Dictionary<string, object>
            {
                { TelemetryDataConstants.TimeTakenToLoadAdaptersInSec, .02091 }
            },
        };

        aggregator.Aggregate(args);
        aggregator.Aggregate(args);

        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec, out var value));
        Assert.AreEqual(.04182, value);
    }

    [TestMethod]
    public void AggregateDiscoveryDataMetricsShouldNotAggregateDiscoveryState()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = new Dictionary<string, object>
            {
                { TelemetryDataConstants.DiscoveryState, "Completed" }
            },
        };

        aggregator.Aggregate(args);
        aggregator.Aggregate(args);

        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();
        Assert.IsFalse(runMetrics.TryGetValue(TelemetryDataConstants.DiscoveryState, out _));
    }

    [TestMethod]
    public void GetAggregatedDiscoveryDataMetricsShouldReturnEmptyIfMetricAggregatorIsEmpty()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = new Dictionary<string, object>(),
        };

        aggregator.Aggregate(args);
        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

        Assert.AreEqual(0, runMetrics.Count);
    }

    [TestMethod]
    public void GetAggregatedDiscoveryDataMetricsShouldReturnEmptyIfMetricsIsNull()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = null,
        };

        aggregator.Aggregate(args);
        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

        Assert.AreEqual(0, runMetrics.Count);
    }

    [TestMethod]
    public void GetDiscoveryDataMetricsShouldAddTotalAdaptersUsedIfMetricsIsNotEmpty()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = new Dictionary<string, object>
            {
                { TelemetryDataConstants.TotalTestsByAdapter, 2 }
            },
        };

        aggregator.Aggregate(args);
        aggregator.Aggregate(args);

        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests, out var value));
        Assert.AreEqual(1, value);
    }

    [TestMethod]
    public void GetDiscoveryDataMetricsShouldAddNumberOfAdapterDiscoveredIfMetricsIsEmpty()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = new Dictionary<string, object>
            {
                { TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter + "executor:MSTestV1", .02091 },
                { TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter + "executor:MSTestV2", .02091 },
            },
        };

        aggregator.Aggregate(args);

        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

        Assert.IsTrue(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery, out var value));
        Assert.AreEqual(2, value);
    }

    [TestMethod]
    public void GetDiscoveryDataMetricsShouldNotAddTotalAdaptersUsedIfMetricsIsEmpty()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();

        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = new Dictionary<string, object>(),
        };

        aggregator.Aggregate(args);

        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();
        Assert.IsFalse(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests, out _));
    }

    [TestMethod]
    public void GetDiscoveryDataMetricsShouldNotAddNumberOfAdapterDiscoveredIfMetricsIsEmpty()
    {
        var aggregator = new ParallelDiscoveryDataAggregator();
        var args = new DiscoveryCompleteEventArgs(0, false)
        {
            Metrics = new Dictionary<string, object>(),
        };

        aggregator.Aggregate(args);

        var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();
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

    [TestMethod]
    public void AggregateDiscoveryStatusChangeStatusOfCorrectSource()
    {
        // Arrange
        var aggregator = new ParallelDiscoveryDataAggregator();
        var sources = new List<string> { "a", };

        // Act
        aggregator.Aggregate(new(0, false)
        {
            NotDiscoveredSources = sources,
        });

        // Assert
        Assert.AreEqual(1, aggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered).Count);
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);

        // Act
        aggregator.Aggregate(new(0, false)
        {
            PartiallyDiscoveredSources = sources,
        });

        // Assert
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered).Count);
        Assert.AreEqual(1, aggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);

        // Act
        aggregator.Aggregate(new(0, false)
        {
            FullyDiscoveredSources = sources,
        });

        // Assert
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered).Count);
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
        Assert.AreEqual(1, aggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);
    }

    [TestMethod]
    public void AggregateDiscoveryStatusAggregatesNotDiscoveredSources()
    {
        // Arrange
        var aggregator = new ParallelDiscoveryDataAggregator();
        aggregator.Aggregate(new(0, false)
        {
            NotDiscoveredSources = new List<string> { "a", "b", "d" },
        });

        // Act
        aggregator.Aggregate(new(0, false)
        {
            NotDiscoveredSources = new List<string> { "a", "c", "d" },
        });

        // Assert
        CollectionAssert.AreEquivalent(
            new List<string> { "a", "b", "c", "d" },
            aggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered));
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered).Count);
    }

    [TestMethod]
    public void AggregateDiscoveryStatusAggregatesPartiallyDiscoveredSources()
    {
        // Arrange
        var aggregator = new ParallelDiscoveryDataAggregator();
        aggregator.Aggregate(new(0, false)
        {
            NotDiscoveredSources = new List<string> { "a", "d" },
            FullyDiscoveredSources = new List<string> { "b" },
        });

        // Act
        aggregator.Aggregate(new(0, false)
        {
            PartiallyDiscoveredSources = new List<string> { "a", "b", "c", "d" },
        });

        // Assert
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered).Count);
        CollectionAssert.AreEquivalent(
            new List<string> { "a", "c", "d" },
            aggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered));
        CollectionAssert.AreEquivalent(
            new List<string> { "b" },
            aggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
    }

    [TestMethod]
    public void AggregateDiscoveryStatusAggregatesFullyDiscoveredSources()
    {
        // Arrange
        var aggregator = new ParallelDiscoveryDataAggregator();
        aggregator.Aggregate(new(0, false)
        {
            NotDiscoveredSources = new List<string> { "a", "d" },
            PartiallyDiscoveredSources = new List<string> { "b" },
            FullyDiscoveredSources = new List<string> { "e" },
        });

        // Act
        aggregator.Aggregate(new(0, false)
        {
            FullyDiscoveredSources = new List<string> { "a", "b", "c", "d", "e" },
        });

        // Assert
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.NotDiscovered).Count);
        Assert.AreEqual(0, aggregator.GetSourcesWithStatus(DiscoveryStatus.PartiallyDiscovered).Count);
        CollectionAssert.AreEquivalent(
            new List<string> { "a", "b", "c", "d", "e" },
            aggregator.GetSourcesWithStatus(DiscoveryStatus.FullyDiscovered));
    }
}
