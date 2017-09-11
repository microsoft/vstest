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
            this.metricsCollector.Clear();
        }

        [TestMethod]
        public void AddShouldAddMetric()
        {
            this.metricsCollector.Add("DummyMessage", "DummyValue");

            string value;
            Assert.AreEqual(metricsCollector.Metrics().TryGetValue("DummyMessage", out value), true);
            Assert.AreEqual(value, "DummyValue");
        }

        [TestMethod]
        public void AddShouldUpdateMetricIfSameKeyIsPresentAlready()
        {
            this.metricsCollector.Add("DummyMessage", "DummyValue");

            string value;
            Assert.AreEqual(metricsCollector.Metrics().TryGetValue("DummyMessage", out value), true);
            Assert.AreEqual(value, "DummyValue");

            this.metricsCollector.Add("DummyMessage", "newValue");

            string newValue;
            Assert.AreEqual(metricsCollector.Metrics().TryGetValue("DummyMessage", out newValue), true);
            Assert.AreEqual(newValue, "newValue");
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MetricsShouldThrowNullExceptionIfValueIsNull()
        {
            this.metricsCollector.Add("DummyMessage", null);
        }

        [TestMethod]
        public void MetricsShouldReturnValidMetricsIfValidItemsAreThere()
        {
            this.metricsCollector.Add("DummyMessage", "DummyValue");
            this.metricsCollector.Add("DummyMessage2", "DummyValue");

            Assert.AreEqual(this.metricsCollector.Metrics().Count, 2);
        }

        [TestMethod]
        public void MetricsShouldReturnEmptyDictionaryIfMetricsIsEmpty()
        {
            Assert.AreEqual(this.metricsCollector.Metrics().Count, 0);
        }

        [TestMethod]
        public void ClearMetricsShouldClearMetrics()
        {
            this.metricsCollector.Add("DummyMessage", "DummyValue");
            this.metricsCollector.Add("DummyMessage2", "DummyValue");

            Assert.AreEqual(this.metricsCollector.Metrics().Count, 2);

            this.metricsCollector.Clear();

            Assert.AreEqual(this.metricsCollector.Metrics().Count, 0);
        }
    }
}
