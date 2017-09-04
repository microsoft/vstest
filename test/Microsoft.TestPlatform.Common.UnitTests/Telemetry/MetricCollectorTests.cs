// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Telemetry
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;

    [TestClass]
    public class MetricCollectorTests
    {
        private IMetricsCollector metricsCollector;

        [TestInitialize]
        public void TestInitialize()
        {
            this.metricsCollector = new MetricsCollector();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.metricsCollector.FlushMetrics();
        }

        [TestMethod]
        public void AddMetricShouldAddMetric()
        {
            this.metricsCollector.AddMetric("DummyMessage","DummyValue");

            string value;
            Assert.AreEqual(metricsCollector.GetMetrics().TryGetValue("DummyMessage",out value), true);
            Assert.AreEqual(value, "DummyValue");
        }

        [TestMethod]
        public void AdddMetricShouldNotUpdateMetricIfSameKeyIsPresentAlready()
        {
            this.metricsCollector.AddMetric("DummyMessage", "DummyValue");

            string value;
            Assert.AreEqual(metricsCollector.GetMetrics().TryGetValue("DummyMessage", out value), true);
            Assert.AreEqual(value, "DummyValue");

            this.metricsCollector.AddMetric("DummyMessage", "newValue");

            string newValue;
            Assert.AreEqual(metricsCollector.GetMetrics().TryGetValue("DummyMessage", out newValue), true);
            Assert.AreEqual(newValue, "DummyValue");
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetMetricShouldThrowNullExceptionIfValueIsNull()
        {
            this.metricsCollector.AddMetric("DummyMessage", null);
        }

        [TestMethod]
        public void GetMetricShouldReturnValidMetricsIfValidItemsAreThere()
        {
            this.metricsCollector.AddMetric("DummyMessage", "DummyValue");
            this.metricsCollector.AddMetric("DummyMessage2", "DummyValue");

            Assert.AreEqual(this.metricsCollector.GetMetrics().Count, 2);
        }

        [TestMethod]
        public void GetMetricShouldReturnEmptyDictionaryIfMetricsIsEmpty()
        {
            Assert.AreEqual(this.metricsCollector.GetMetrics().Count, 0);
        }

        [TestMethod]
        public void FlushMetricsShouldClearMetrics()
        {
            this.metricsCollector.AddMetric("DummyMessage", "DummyValue");
            this.metricsCollector.AddMetric("DummyMessage2", "DummyValue");

            Assert.AreEqual(this.metricsCollector.GetMetrics().Count, 2);

            this.metricsCollector.FlushMetrics();

            Assert.AreEqual(this.metricsCollector.GetMetrics().Count, 0);
        }
    }
}
