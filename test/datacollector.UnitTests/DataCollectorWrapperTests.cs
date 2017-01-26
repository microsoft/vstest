// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class dataCollectorInfoTests
    {
        private DataCollectorInformation dataCollectorInfo;

        [TestInitialize]
        public void Init()
        {
            var mockMessageSink = new Mock<IMessageSink>();
            this.dataCollectorInfo = new DataCollectorInformation(
                new CustomDataCollector(),
                null,
                new DataCollectorConfig(typeof(CustomDataCollector)),
                null,
                new Mock<IDataCollectionAttachmentManager>().Object,
                new TestPlatformDataCollectionEvents(),
                mockMessageSink.Object);
        }

        [TestMethod]
        public void InitializeDataCollectorShouldInitializeDataCollector()
        {
            var envVarList = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("key", "value") };
            CustomDataCollector.EnvVarList = envVarList;

            this.dataCollectorInfo.InitializeDataCollector();

            Assert.IsTrue(CustomDataCollector.IsInitialized);
            Assert.AreEqual(envVarList.First().Key, this.dataCollectorInfo.TestExecutionEnvironmentVariables.First().Key);
        }

        [TestMethod]
        public void DisposeShouldInvokeDisposeOfDatacollector()
        {
            this.dataCollectorInfo.InitializeDataCollector();
            this.dataCollectorInfo.DisposeDataCollector();

            Assert.IsTrue(CustomDataCollector.IsDisposeInvoked);
        }
    }
}
