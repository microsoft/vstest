// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client.Parallel
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ParallelDiscoveryDataAggregatorTests
    {
        [TestMethod]
        public void AggregateShouldAggregateAbortedCorrectly()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            aggregator.Aggregate(totalTests: 5, isAborted: false);
            Assert.IsFalse(aggregator.IsAborted, "Aborted must be false");

            aggregator.Aggregate(totalTests: 5, isAborted: true);
            Assert.IsTrue(aggregator.IsAborted, "Aborted must be true");

            aggregator.Aggregate(totalTests: 5, isAborted: false);
            Assert.IsTrue(aggregator.IsAborted, "Aborted must be true");

            Assert.AreEqual(-1, aggregator.TotalTests, "Aggregator shouldn't count tests if one host aborts");
        }

        [TestMethod]
        public void AggregateShouldAggregateTotalTestsCorrectly()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();
            aggregator.Aggregate(totalTests: 2, isAborted: false);
            Assert.AreEqual(2, aggregator.TotalTests, "Aggregated totalTests count does not match");

            aggregator.Aggregate(totalTests: 5, isAborted: false);
            Assert.AreEqual(7, aggregator.TotalTests, "Aggregated totalTests count does not match");

            aggregator.Aggregate(totalTests: 3, isAborted: false);
            Assert.AreEqual(10, aggregator.TotalTests, "Aggregated totalTests count does not match");
        }

        [TestMethod]
        public void AggregateDiscoveryDataMetricsShouldAggregateMetricsCorrectly()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            aggregator.AggregateDiscoveryDataMetrics(null);

            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();
            Assert.AreEqual(runMetrics.Count, 0);
        }

        [TestMethod]
        public void AggregateDiscoveryDataMetricsShouldAddTotalTestsDiscovered()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TotalTestsDiscovered, "2");

            aggregator.AggregateDiscoveryDataMetrics(dict);
            aggregator.AggregateDiscoveryDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.TotalTestsDiscovered, out value), true);
            Assert.AreEqual(value, "4");
        }

        [TestMethod]
        public void AggregateDiscoveryDataMetricsShouldAddTimeTakenToDiscoverTests()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter, ".02091");

            aggregator.AggregateDiscoveryDataMetrics(dict);
            aggregator.AggregateDiscoveryDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter, out value), true);
            Assert.AreEqual(value, (.04182).ToString());
        }

        [TestMethod]
        public void AggregateDiscoveryDataMetricsShouldAddTimeTakenByAllAdapters()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TimeTakenInSecByAllAdapters, ".02091");

            aggregator.AggregateDiscoveryDataMetrics(dict);
            aggregator.AggregateDiscoveryDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenInSecByAllAdapters, out value), true);
            Assert.AreEqual(value, (.04182).ToString());
        }

        [TestMethod]
        public void AggregateDiscoveryDataMetricsShouldAddTimeTakenToLoadAdapters()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec, ".02091");

            aggregator.AggregateDiscoveryDataMetrics(dict);
            aggregator.AggregateDiscoveryDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.TimeTakenToLoadAdaptersInSec, out value), true);
            Assert.AreEqual(value, (.04182).ToString());
        }

        [TestMethod]
        public void AggregateDiscoveryDataMetricsShouldNotAggregateDiscoveryState()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.DiscoveryState, "Completed");

            aggregator.AggregateDiscoveryDataMetrics(dict);
            aggregator.AggregateDiscoveryDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.DiscoveryState, out value), false);
        }

        [TestMethod]
        public void GetAggregatedDiscoveryDataMetricsShouldReturnEmptyIfMetricAggregatorIsEmpty()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            var dict = new Dictionary<string, string>();

            aggregator.AggregateDiscoveryDataMetrics(dict);
            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            Assert.AreEqual(runMetrics.Count, 0);
        }

        [TestMethod]
        public void GetAggregatedDiscoveryDataMetricsShouldReturnEmptyIfMetricsIsNull()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            var dict = new Dictionary<string, string>();

            aggregator.AggregateDiscoveryDataMetrics(null);
            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            Assert.AreEqual(runMetrics.Count, 0);
        }

        [TestMethod]
        public void GetDiscoveryDataMetricsShouldAddTotalAdaptersUsedIfMetricsIsNotEmpty()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TotalTestsByAdapter, "2");

            aggregator.AggregateDiscoveryDataMetrics(dict);
            aggregator.AggregateDiscoveryDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests, out value), true);
            Assert.AreEqual(value, "1");
        }

        [TestMethod]
        public void GetDiscoveryDataMetricsShouldAddNumberOfAdapterDiscoveredIfMetricsIsEmpty()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();

            var dict = new Dictionary<string, string>();
            dict.Add(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter + "executor:MSTestV1", ".02091");
            dict.Add(TelemetryDataConstants.TimeTakenToDiscoverTestsByAnAdapter + "executor:MSTestV2", ".02091");

            aggregator.AggregateDiscoveryDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery, out value), true);
            Assert.AreEqual(value, "2");
        }

        [TestMethod]
        public void GetDiscoveryDataMetricsShouldNotAddTotalAdaptersUsedIfMetricsIsEmpty()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();
            var dict = new Dictionary<string, string>();

            aggregator.AggregateDiscoveryDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterUsedToDiscoverTests, out value), false);
        }

        [TestMethod]
        public void GetDiscoveryDataMetricsShouldNotAddNumberOfAdapterDiscoveredIfMetricsIsEmpty()
        {
            var aggregator = new ParallelDiscoveryDataAggregator();
            var dict = new Dictionary<string, string>();

            aggregator.AggregateDiscoveryDataMetrics(dict);

            var runMetrics = aggregator.GetAggregatedDiscoveryDataMetrics();

            string value;
            Assert.AreEqual(runMetrics.TryGetValue(TelemetryDataConstants.NumberOfAdapterDiscoveredDuringDiscovery, out value), false);
        }
    }
}
