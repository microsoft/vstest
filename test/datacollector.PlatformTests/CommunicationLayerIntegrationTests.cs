// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.PlatformTests
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    [Ignore]    // Tests are flaky
    public class CommunicationLayerIntegrationTests
    {
        private string defaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";
        private Mock<ObjectModel.Client.ITestMessageEventHandler> mockTestMessageEventHandler;
        private string dataCollectorSettings, runSettings;
        private IDataCollectionLauncher dataCollectionLauncher;
        private IProcessHelper processHelper;
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;
        private List<string> testSources;

        public CommunicationLayerIntegrationTests()
        {
            this.mockTestMessageEventHandler = new Mock<ObjectModel.Client.ITestMessageEventHandler>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);
            this.dataCollectorSettings = string.Format("<DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom/datacollector\" assemblyQualifiedName=\"{0}\" codebase=\"{1}\" />", typeof(CustomDataCollector).AssemblyQualifiedName, typeof(CustomDataCollector).GetTypeInfo().Assembly.Location);
            this.runSettings = string.Format(this.defaultRunSettings, this.dataCollectorSettings);
            this.testSources = new List<string>() { "testsource1.dll" };
            this.processHelper = new ProcessHelper();
            this.dataCollectionLauncher = DataCollectionLauncherFactory.GetDataCollectorLauncher(this.processHelper, this.runSettings);
        }

        [TestMethod]
        public void BeforeTestRunStartShouldGetEnviornmentVariables()
        {
            var dataCollectionRequestSender = new DataCollectionRequestSender();

            using (var proxyDataCollectionManager = new ProxyDataCollectionManager(this.mockRequestData.Object, this.runSettings, this.testSources, dataCollectionRequestSender, this.processHelper, this.dataCollectionLauncher))
            {
                proxyDataCollectionManager.Initialize();

                var result = proxyDataCollectionManager.BeforeTestRunStart(true, true, this.mockTestMessageEventHandler.Object);

                Assert.AreEqual(1, result.EnvironmentVariables.Count);
            }
        }

        [TestMethod]
        public void AfterTestRunShouldSendGetAttachments()
        {
            var dataCollectionRequestSender = new DataCollectionRequestSender();

            using (var proxyDataCollectionManager = new ProxyDataCollectionManager(this.mockRequestData.Object, this.runSettings, this.testSources, dataCollectionRequestSender, this.processHelper, this.dataCollectionLauncher))
            {
                proxyDataCollectionManager.Initialize();

                proxyDataCollectionManager.BeforeTestRunStart(true, true, this.mockTestMessageEventHandler.Object);

                var attachments = proxyDataCollectionManager.AfterTestRunEnd(false, this.mockTestMessageEventHandler.Object);

                Assert.AreEqual("CustomDataCollector", attachments[0].DisplayName);
                Assert.AreEqual("my://custom/datacollector", attachments[0].Uri.ToString());
                Assert.IsTrue(attachments[0].Attachments[0].Uri.ToString().Contains("filename.txt"));
            }
        }

        [TestMethod]
        public void AfterTestRunShouldHandleSocketFailureGracefully()
        {
            var socketCommManager = new SocketCommunicationManager();
            var dataCollectionRequestSender = new DataCollectionRequestSender(socketCommManager, JsonDataSerializer.Instance);
            var dataCollectionLauncher = DataCollectionLauncherFactory.GetDataCollectorLauncher(this.processHelper, this.runSettings);

            using (var proxyDataCollectionManager = new ProxyDataCollectionManager(this.mockRequestData.Object, this.runSettings, this.testSources, dataCollectionRequestSender, this.processHelper, dataCollectionLauncher))
            {
                proxyDataCollectionManager.Initialize();
                proxyDataCollectionManager.BeforeTestRunStart(true, true, this.mockTestMessageEventHandler.Object);

                var result = Process.GetProcessById(dataCollectionLauncher.DataCollectorProcessId);
                Assert.IsNotNull(result);

                socketCommManager.StopClient();

                var attachments = proxyDataCollectionManager.AfterTestRunEnd(false, this.mockTestMessageEventHandler.Object);

                Assert.IsNull(attachments);

                // Give time to datacollector process to exit.
                Assert.IsTrue(result.WaitForExit(500));
            }
        }
    }
}