// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Moq.Protected;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    [TestClass]
    public class DataCollectorInformationTests
    {
        private const string TargetPlatformSetting = "<TargetPlatform>X86</TargetPlatform>";
        private const string TargetFrameworkSetting = "<TargetFrameworkVersion>.NETCoreApp,Version=v2.0</TargetFrameworkVersion>";
        private readonly static string DefaultRunSettings = $@"<RunSettings>
                                                      <RunConfiguration>
                                                        {DataCollectorInformationTests.TargetFrameworkSetting}
                                                        {DataCollectorInformationTests.TargetPlatformSetting}
                                                      </RunConfiguration >
                                                   </RunSettings >";
        private DataCollectorInformation dataCollectorInfo;

        private List<KeyValuePair<string, string>> envVarList;

        private Mock<DataCollector2> mockDataCollector;

        private string dataCollectorReceivedConfig;


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
                mockMessageSink.Object,
                DataCollectorInformationTests.DefaultRunSettings);
        }

        [TestMethod]
        public void InitializeDataCollectorShouldInitializeDataCollector()
        {
            this.envVarList.Add(new KeyValuePair<string, string>("key", "value"));

            this.dataCollectorInfo.InitializeDataCollector();
            this.dataCollectorInfo.SetTestExecutionEnvironmentVariables();

            CollectionAssert.AreEqual(this.envVarList, this.dataCollectorInfo.TestExecutionEnvironmentVariables.ToList());
        }

        [TestMethod]
        public void DisposeShouldInvokeDisposeOfDatacollector()
        {
            this.dataCollectorInfo.InitializeDataCollector();
            this.dataCollectorInfo.DisposeDataCollector();

            this.mockDataCollector.Protected().Verify("Dispose", Times.Once(), true);
        }

        [TestMethod]
        public void InitializeDataCollectorShouldPassTargetPlatformtoDataCollectorConfig()
        {
            this.SetupDataCollectorInitialize();

            this.dataCollectorInfo.InitializeDataCollector();

            StringAssert.Contains(this.dataCollectorReceivedConfig, DataCollectorInformationTests.TargetPlatformSetting);
        }

        [TestMethod]
        public void InitializeDataCollectorShouldPassTargetFrameworktoDataCollectorConfig()
        {
            this.SetupDataCollectorInitialize();

            this.dataCollectorInfo.InitializeDataCollector();

            StringAssert.Contains(this.dataCollectorReceivedConfig, "<Framework>.NETCoreApp,Version=v2.0</Framework>");
        }

        private void SetupDataCollectorInitialize()
        {
            this.mockDataCollector.Setup(d => d.Initialize(
                    It.IsAny<XmlElement>(),
                    It.IsAny<DataCollectionEvents>(),
                    It.IsAny<DataCollectionSink>(),
                    It.IsAny<DataCollectionLogger>(),
                    It.IsAny<DataCollectionEnvironmentContext>()))
                .Callback<XmlElement, DataCollectionEvents, DataCollectionSink, DataCollectionLogger,
                    DataCollectionEnvironmentContext>(
                    (xmlEle, events, sink, logger, envContext) => { this.dataCollectorReceivedConfig = xmlEle.InnerXml; });
        }
    }
}
