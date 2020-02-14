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
        private IMetricsCollection metricsCollection;

        public MetricsCollectionTests()
        {
            this.metricsCollection = new MetricsCollection();
        }

        [TestMethod]
        public void AddShouldAddMetric()
        {
            this.metricsCollection.Add("DummyMessage", "DummyValue");

            object value;
            Assert.IsTrue(this.metricsCollection.Metrics.TryGetValue("DummyMessage", out value));
            Assert.AreEqual("DummyValue", value);
        }

        [TestMethod]
        public void AddShouldUpdateMetricIfSameKeyIsPresentAlready()
        {
            this.metricsCollection.Add("DummyMessage", "DummyValue");

            object value;
            Assert.IsTrue(this.metricsCollection.Metrics.TryGetValue("DummyMessage", out value));
            Assert.AreEqual("DummyValue", value);

            this.metricsCollection.Add("DummyMessage", "newValue");

            object newValue;
            Assert.IsTrue(this.metricsCollection.Metrics.TryGetValue("DummyMessage", out newValue));
            Assert.AreEqual("newValue", newValue);
        }

        [TestMethod]
        public void MetricsShouldReturnValidMetricsIfValidItemsAreThere()
        {
            this.metricsCollection.Add("DummyMessage", "DummyValue");
            this.metricsCollection.Add("DummyMessage2", "DummyValue");

            Assert.AreEqual(2, this.metricsCollection.Metrics.Count);
            Assert.IsTrue(this.metricsCollection.Metrics.ContainsKey("DummyMessage"));
            Assert.IsTrue(this.metricsCollection.Metrics.ContainsKey("DummyMessage2"));
        }

        [TestMethod]
        public void MetricsShouldReturnEmptyDictionaryIfMetricsIsEmpty()
        {
            Assert.AreEqual(0, this.metricsCollection.Metrics.Count);
        }
    }
}
