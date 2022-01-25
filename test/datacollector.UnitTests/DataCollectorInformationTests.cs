// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Moq.Protected;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    [TestClass]
    public class DataCollectorInformationTests
    {
        private readonly DataCollectorInformation dataCollectorInfo;

        private readonly List<KeyValuePair<string, string>> envVarList;

        private readonly Mock<DataCollector2> mockDataCollector;

        public DataCollectorInformationTests()
        {
            envVarList = new List<KeyValuePair<string, string>>();
            mockDataCollector = new Mock<DataCollector2>();
            mockDataCollector.As<ITestExecutionEnvironmentSpecifier>().Setup(x => x.GetTestExecutionEnvironmentVariables()).Returns(envVarList);
            mockDataCollector.Protected().Setup("Dispose", true);
            var mockMessageSink = new Mock<IMessageSink>();
            dataCollectorInfo = new DataCollectorInformation(
                mockDataCollector.Object,
                null,
                new DataCollectorConfig(typeof(CustomDataCollector)),
                null,
                new Mock<IDataCollectionAttachmentManager>().Object,
                new TestPlatformDataCollectionEvents(),
                mockMessageSink.Object,
                string.Empty);
        }

        [TestMethod]
        public void InitializeDataCollectorShouldInitializeDataCollector()
        {
            envVarList.Add(new KeyValuePair<string, string>("key", "value"));

            dataCollectorInfo.InitializeDataCollector();
            dataCollectorInfo.SetTestExecutionEnvironmentVariables();

            CollectionAssert.AreEqual(envVarList, dataCollectorInfo.TestExecutionEnvironmentVariables.ToList());
        }

        [TestMethod]
        public void DisposeShouldInvokeDisposeOfDatacollector()
        {
            dataCollectorInfo.InitializeDataCollector();
            dataCollectorInfo.DisposeDataCollector();

            mockDataCollector.Protected().Verify("Dispose", Times.Once(), true);
        }
    }
}
