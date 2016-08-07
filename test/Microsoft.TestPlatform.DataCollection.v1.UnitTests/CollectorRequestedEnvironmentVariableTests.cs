// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.DataCollection.V1.UnitTests
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.DataCollection.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CollectorRequestedEnvironmentVariableTests
    {
        [TestInitialize]
        public void Init()
        {

        }

        [TestMethod]
        public void AddRequestingDataCollectorShouldAddDataCollectorName()
        {
            var friendlyName = "DataCollectorFriendlyName";
            var kvpair = new KeyValuePair<string, string>("key", "value");

            var collectorRequestedEnvironmentVariable = new CollectorRequestedEnvironmentVariable(kvpair, friendlyName);

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
            var kvpair = new KeyValuePair<string, string>("key", "value");

            var collectorRequestedEnvironmentVariable = new CollectorRequestedEnvironmentVariable(kvpair, friendlyName);
            collectorRequestedEnvironmentVariable.AddRequestingDataCollector("DataCollectorFriendlyName1");

            Assert.AreEqual("key", collectorRequestedEnvironmentVariable.Name);
            Assert.AreEqual("value", collectorRequestedEnvironmentVariable.Value);
            Assert.AreEqual("key", collectorRequestedEnvironmentVariable.Name);
            Assert.AreEqual("value", collectorRequestedEnvironmentVariable.Value);
            Assert.AreEqual(friendlyName, collectorRequestedEnvironmentVariable.FirstDataCollectorThatRequested);
        }
    }
}