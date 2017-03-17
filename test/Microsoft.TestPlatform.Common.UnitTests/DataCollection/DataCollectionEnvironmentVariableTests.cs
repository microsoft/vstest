// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Common.UnitTests.DataCollection
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataCollectionEnvironmentVariableTests
    {
        [TestMethod]
        public void ConstructorShouldThrowExceptionIfKeyValueIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        var envvariable = new DataCollectionEnvironmentVariable(default(KeyValuePair<string, string>), null);
                    });
        }

        [TestMethod]
        public void FirstDataCollectorThatRequestedShouldReturnTheFirstdataCollectorRequestingThatEnvVariable()
        {
            var envValPair = new KeyValuePair<string, string>("key", "value");
            var envvariable = new DataCollectionEnvironmentVariable(envValPair, "datacollector");
            envvariable.AddRequestingDataCollector("datacollector1");

            Assert.AreEqual("datacollector", envvariable.FirstDataCollectorThatRequested);
        }

        [TestMethod]
        public void FirstDataCollectorThatRequestedShouldSetNameAndValue()
        {
            var envValPair = new KeyValuePair<string, string>("key", "value");
            var envvariable = new DataCollectionEnvironmentVariable(envValPair, "datacollector");
            envvariable.AddRequestingDataCollector("datacollector1");

            Assert.AreEqual("key", envvariable.Name);
            Assert.AreEqual("value", envvariable.Value);
        }
    }
}
