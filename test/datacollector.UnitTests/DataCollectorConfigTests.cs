// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataCollectorConfigTests
    {
        [TestMethod]
        public void ConstructorShouldSetCorrectFriendlyNameAndUri()
        {
            var dataCollectorConfig = new DataCollectorConfig(typeof(CustomDataCollector));

            Assert.AreEqual("CustomDataCollector", dataCollectorConfig.FriendlyName);
            Assert.AreEqual("my://custom/datacollector", dataCollectorConfig.TypeUri.ToString());
        }

        [TestMethod]
        public void ConstructorShouldThrowExceptionIfTypeIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(
                () =>
                    {
                        new DataCollectorConfig(null);
                    });
        }
    }
}
