// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.Telemetry
{
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MetricsCollectionTests
    {
        private readonly IMetricsCollection metricsCollection;

        public MetricsCollectionTests()
        {
            metricsCollection = new MetricsCollection();
        }

        [TestMethod]
        public void AddShouldAddMetric()
        {
            metricsCollection.Add("DummyMessage", "DummyValue");

            Assert.IsTrue(metricsCollection.Metrics.TryGetValue("DummyMessage", out var value));
            Assert.AreEqual("DummyValue", value);
        }

        [TestMethod]
        public void AddShouldUpdateMetricIfSameKeyIsPresentAlready()
        {
            metricsCollection.Add("DummyMessage", "DummyValue");

            Assert.IsTrue(metricsCollection.Metrics.TryGetValue("DummyMessage", out var value));
            Assert.AreEqual("DummyValue", value);

            metricsCollection.Add("DummyMessage", "newValue");

            Assert.IsTrue(metricsCollection.Metrics.TryGetValue("DummyMessage", out var newValue));
            Assert.AreEqual("newValue", newValue);
        }

        [TestMethod]
        public void MetricsShouldReturnValidMetricsIfValidItemsAreThere()
        {
            metricsCollection.Add("DummyMessage", "DummyValue");
            metricsCollection.Add("DummyMessage2", "DummyValue");

            Assert.AreEqual(2, metricsCollection.Metrics.Count);
            Assert.IsTrue(metricsCollection.Metrics.ContainsKey("DummyMessage"));
            Assert.IsTrue(metricsCollection.Metrics.ContainsKey("DummyMessage2"));
        }

        [TestMethod]
        public void MetricsShouldReturnEmptyDictionaryIfMetricsIsEmpty()
        {
            Assert.AreEqual(0, metricsCollection.Metrics.Count);
        }
    }
}
