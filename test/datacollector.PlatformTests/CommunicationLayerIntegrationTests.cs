// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.PlatformTests
{
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using System;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;

    [TestClass]
    public class CommunicationLayerIntegrationTests
    {
        private string defaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";
        private Mock<ObjectModel.Client.ITestMessageEventHandler> mockTestMessageEventHandler;
        private string dataCollectorSettings, runSettings;
        private IDataCollectionLauncher dataCollectionLauncher;

        public CommunicationLayerIntegrationTests()
        {
            this.mockTestMessageEventHandler = new Mock<ObjectModel.Client.ITestMessageEventHandler>();
            this.dataCollectorSettings = string.Format("<DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom/datacollector\" assemblyQualifiedName=\"{0}\" codebase=\"{1}\" />", typeof(CustomDataCollector).AssemblyQualifiedName, typeof(CustomDataCollector).GetTypeInfo().Assembly.Location);
            this.runSettings = string.Format(this.defaultRunSettings, this.dataCollectorSettings);
#if NET46
            this.dataCollectionLauncher = new DataCollectionLauncher();
#else
            this.dataCollectionLauncher = new DotnetDataCollectionLauncher();
#endif
        }

        [TestMethod]
        public void BeforeTestRunStartShouldGetEnviornmentVariables()
        {
            var dataCollectionRequestSender = new DataCollectionRequestSender();
            var proxyDataCollectionManager = new ProxyDataCollectionManager(ObjectModel.Architecture.AnyCPU, this.runSettings, dataCollectionRequestSender, this.dataCollectionLauncher);

            var result = proxyDataCollectionManager.BeforeTestRunStart(true, true, this.mockTestMessageEventHandler.Object);

            Assert.AreEqual(1, result.EnvironmentVariables.Count);

            proxyDataCollectionManager.Dispose();
        }

        [TestMethod]
        public void AfterTestRunShouldSendGetAttachments()
        {
            var dataCollectionRequestSender = new DataCollectionRequestSender();
            var proxyDataCollectionManager = new ProxyDataCollectionManager(ObjectModel.Architecture.AnyCPU, this.runSettings, dataCollectionRequestSender, this.dataCollectionLauncher);

            proxyDataCollectionManager.BeforeTestRunStart(true, true, this.mockTestMessageEventHandler.Object);

            var attachments = proxyDataCollectionManager.AfterTestRunEnd(false, this.mockTestMessageEventHandler.Object);

            Assert.AreEqual("CustomDataCollector", attachments[0].DisplayName);
            Assert.AreEqual("my://custom/datacollector", attachments[0].Uri.ToString());
            Assert.IsTrue(attachments[0].Attachments[0].Uri.ToString().Contains("filename.txt"));

            proxyDataCollectionManager.Dispose();
        }

        [TestMethod]
        public void AfterTestRunShouldHandleSocketFailureGracefully()
        {
            var socketCommManager = new SocketCommunicationManager();
            var dataCollectionRequestSender = new DataCollectionRequestSender(socketCommManager, JsonDataSerializer.Instance);
#if NET46
            var dataCollectionLauncher = new DataCollectionLauncher();
#else
            var dataCollectionLauncher = new DotnetDataCollectionLauncher();
#endif
            var proxyDataCollectionManager = new ProxyDataCollectionManager(ObjectModel.Architecture.AnyCPU, this.runSettings, dataCollectionRequestSender, dataCollectionLauncher);

            proxyDataCollectionManager.BeforeTestRunStart(true, true, this.mockTestMessageEventHandler.Object);

            var result = Process.GetProcessById(dataCollectionLauncher.DataCollectorProcess.Id);
            Assert.IsNotNull(result);

            socketCommManager.StopClient();

            var attachments = proxyDataCollectionManager.AfterTestRunEnd(false, this.mockTestMessageEventHandler.Object);

            Assert.IsNull(attachments);

            // Give time to datacollector process to exit.
            Thread.Sleep(500);

            // Throws argument exception because process with the given id is not running.
            Assert.ThrowsException<ArgumentException>(() =>
            {
                result = Process.GetProcessById(dataCollectionLauncher.DataCollectorProcess.Id);
            });
        }
    }
}
