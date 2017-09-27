// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Publisher
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Publisher;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MetricsPublisherTests
    {
        [TestMethod]
        public void RemoveInvalidCharactersFromKeysShouldReturnEmptyDictionaryIfMetricsIsNull()
        {
            var publishMetrics = new MetricsPublisher();

            // Act.
            var result = publishMetrics.RemoveInvalidCharactersFromProperties(null);

            // Assert.
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void RemoveInvalidCharactersFromPropertiesShouldEmptyMetricIfMetricsIsEmpty()
        {
            var publishMetrics = new MetricsPublisher();
            var dummyDictionary = new Dictionary<string, object>();

            // Act.
            var result = publishMetrics.RemoveInvalidCharactersFromProperties(dummyDictionary);

            // Assert.
            Assert.AreEqual(dummyDictionary.Count, result.Count);
        }

        [TestMethod]
        public void RemoveInvalidCharactersFromPropertiesShouldValidMetrics()
        {
            var publishMetrics = new MetricsPublisher();
            var dummyDictionary = new Dictionary<string, object>();
            dummyDictionary.Add("DummyMessage://", "DummyValue");
            dummyDictionary.Add("Dummy2", "DummyValue2");

            // Act.
            var result = publishMetrics.RemoveInvalidCharactersFromProperties(dummyDictionary);

            // Assert.
            Assert.AreEqual(dummyDictionary.Count, result.Count);
            Assert.IsTrue(result.ContainsKey("DummyMessage//"));
        }
    }
}
