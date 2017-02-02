// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Moq.Protected;

    [TestClass]
    public class DataCollectorInformationTests
    {
        private DataCollectorInformation dataCollectorInfo;

        private List<KeyValuePair<string, string>> envVarList;

        private Mock<DataCollector2> mockDataCollector;

        public DataCollectorInformationTests()
        {
            this.envVarList = new List<KeyValuePair<string, string>>();
            this.mockDataCollector = new Mock<DataCollector2>();
            this.mockDataCollector.As<ITestExecutionEnvironmentSpecifier>().Setup(x => x.GetTestExecutionEnvironmentVariables()).Returns(this.envVarList);
            this.mockDataCollector.Protected().Setup("Dispose", true);
            var mockMessageSink = new Mock<IMessageSink>();
            this.dataCollectorInfo = new DataCollectorInformation(
                this.mockDataCollector.Object,
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
            this.envVarList.Add(new KeyValuePair<string, string>("key", "value"));

            this.dataCollectorInfo.InitializeDataCollector();

            CollectionAssert.AreEqual(this.envVarList, this.dataCollectorInfo.TestExecutionEnvironmentVariables.ToList());
        }

        [TestMethod]
        public void DisposeShouldInvokeDisposeOfDatacollector()
        {
            this.dataCollectorInfo.InitializeDataCollector();
            this.dataCollectorInfo.DisposeDataCollector();

            this.mockDataCollector.Protected().Verify("Dispose", Times.Once(), true);
        }
    }
}
