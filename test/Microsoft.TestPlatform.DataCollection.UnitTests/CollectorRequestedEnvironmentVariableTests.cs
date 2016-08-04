// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.DataCollection.UnitTests.Implementations
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CollectorRequestedEnvironmentVariableTests
    {
        [TestMethod]
        public void AddRequestingDataCollectorShouldAddDataCollectorName()
        {
            var friendlyName = "DataCollectorFriendlyName";
            var kv = new KeyValuePair<string, string>("key", "value");

            var collectorRequestedEnvironmentVariable = new CollectorRequestedEnvironmentVariable(kv, friendlyName);

            Assert.AreEqual("key", collectorRequestedEnvironmentVariable.Name);
            Assert.AreEqual("value", collectorRequestedEnvironmentVariable.Value);
            Assert.AreEqual("key", collectorRequestedEnvironmentVariable.Name);
            Assert.AreEqual("value", collectorRequestedEnvironmentVariable.Value);
            Assert.AreEqual(friendlyName, collectorRequestedEnvironmentVariable.FirstDataCollectorThatRequested);
        }

        [TestMethod]
        public void FirstDataCollectorThatRequestedShouldReturnTheNameOfFirstRequestingDataCollector()
        {
            var friendlyName = "DataCollectorFriendlyName";
            var kv = new KeyValuePair<string, string>("key", "value");

            var collectorRequestedEnvironmentVariable = new CollectorRequestedEnvironmentVariable(kv, friendlyName);
            collectorRequestedEnvironmentVariable.AddRequestingDataCollector("DataCollectorFriendlyName1");

            Assert.AreEqual("key", collectorRequestedEnvironmentVariable.Name);
            Assert.AreEqual("value", collectorRequestedEnvironmentVariable.Value);
            Assert.AreEqual("key", collectorRequestedEnvironmentVariable.Name);
            Assert.AreEqual("value", collectorRequestedEnvironmentVariable.Value);
            Assert.AreEqual(friendlyName, collectorRequestedEnvironmentVariable.FirstDataCollectorThatRequested);
        }
    }
}