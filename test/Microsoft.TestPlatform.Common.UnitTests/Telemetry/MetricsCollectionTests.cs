// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Telemetry
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MetricsCollectionTests
    {
        private IMetricsCollection metricsCollection;

        public MetricsCollectionTests()
        {
            this.metricsCollection = new MetricsCollection();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.metricsCollection.Clear();
        }

        [TestMethod]
        public void AddShouldAddMetric()
        {
            this.metricsCollection.Add("DummyMessage", "DummyValue");

            string value;
            Assert.AreEqual(this.metricsCollection.Metrics.TryGetValue("DummyMessage", out value), true);
            Assert.AreEqual(value, "DummyValue");
        }

        [TestMethod]
        public void AddShouldUpdateMetricIfSameKeyIsPresentAlready()
        {
            this.metricsCollection.Add("DummyMessage", "DummyValue");

            string value;
            Assert.AreEqual(this.metricsCollection.Metrics.TryGetValue("DummyMessage", out value), true);
            Assert.AreEqual(value, "DummyValue");

            this.metricsCollection.Add("DummyMessage", "newValue");

            string newValue;
            Assert.AreEqual(this.metricsCollection.Metrics.TryGetValue("DummyMessage", out newValue), true);
            Assert.AreEqual(newValue, "newValue");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MetricsShouldThrowNullExceptionIfValueIsNull()
        {
            this.metricsCollection.Add("DummyMessage", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MetricsShouldThrowNullExceptionIfKeyIsNull()
        {
            this.metricsCollection.Add(null, "DummyMessage");
        }

        [TestMethod]
        public void MetricsShouldReturnValidMetricsIfValidItemsAreThere()
        {
            this.metricsCollection.Add("DummyMessage", "DummyValue");
            this.metricsCollection.Add("DummyMessage2", "DummyValue");

            Assert.AreEqual(this.metricsCollection.Metrics.Count, 2);
            Assert.AreEqual(this.metricsCollection.Metrics.ContainsKey("DummyMessage"), true);
            Assert.AreEqual(this.metricsCollection.Metrics.ContainsKey("DummyMessage2"), true);
        }

        [TestMethod]
        public void MetricsShouldReturnEmptyDictionaryIfMetricsIsEmpty()
        {
            Assert.AreEqual(this.metricsCollection.Metrics.Count, 0);
        }

        [TestMethod]
        public void MetricsShouldReturnEmptyDictionaryIfMetricsIsNull()
        {
            Assert.AreEqual(this.metricsCollection.Metrics.Count, 0);
        }

        [TestMethod]
        public void ClearMetricsShouldClearMetrics()
        {
            this.metricsCollection.Add("DummyMessage", "DummyValue");
            this.metricsCollection.Add("DummyMessage2", "DummyValue");

            Assert.AreEqual(this.metricsCollection.Metrics.Count, 2);

            this.metricsCollection.Clear();

            Assert.AreEqual(this.metricsCollection.Metrics.Count, 0);
        }
    }
}
