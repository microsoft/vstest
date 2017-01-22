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
    public class DataCollectorWrapperTests
    {
        private DataCollectorWrapper dataCollectorWrapper;

        [TestInitialize]
        public void Init()
        {
            this.dataCollectorWrapper = new DataCollectorWrapper(
                new CustomDataCollector(),
                null,
                new DataCollectorConfig(typeof(CustomDataCollector)),
                null,
                new Mock<IDataCollectionAttachmentManager>().Object,
                new TestPlatformDataCollectionEvents(),
                new DummyMessageSink());
        }

        [TestMethod]
        public void InitializeDataCollectorShouldInitializeDataCollector()
        {
            var envVarList = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("key", "value") };
            CustomDataCollector.EnvVarList = envVarList;

            this.dataCollectorWrapper.InitializeDataCollector();

            Assert.IsTrue(CustomDataCollector.IsInitialized);
            Assert.AreEqual(envVarList.First().Key, this.dataCollectorWrapper.TestExecutionEnvironmentVariables.First().Key);
        }

        [TestMethod]
        public void DisposeShouldInvokeDisposeOfDatacollector()
        {
            this.dataCollectorWrapper.InitializeDataCollector();
            this.dataCollectorWrapper.DisposeDataCollector();

            Assert.IsTrue(CustomDataCollector.IsDisposeInvoked);
        }
    }
}
