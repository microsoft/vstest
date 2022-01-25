﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        private readonly string defaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";
        private readonly Mock<ITestMessageEventHandler> mockTestMessageEventHandler;
        private readonly string dataCollectorSettings, runSettings;
        private readonly IDataCollectionLauncher dataCollectionLauncher;
        private readonly IProcessHelper processHelper;
        private readonly Mock<IRequestData> mockRequestData;
        private readonly Mock<IMetricsCollection> mockMetricsCollection;
        private readonly List<string> testSources;

        public CommunicationLayerIntegrationTests()
        {
            mockTestMessageEventHandler = new Mock<ITestMessageEventHandler>();
            mockRequestData = new Mock<IRequestData>();
            mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);
            dataCollectorSettings = string.Format("<DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom/datacollector\" assemblyQualifiedName=\"{0}\" codebase=\"{1}\" />", typeof(CustomDataCollector).AssemblyQualifiedName, typeof(CustomDataCollector).GetTypeInfo().Assembly.Location);
            runSettings = string.Format(defaultRunSettings, dataCollectorSettings);
            testSources = new List<string>() { "testsource1.dll" };
            processHelper = new ProcessHelper();
            dataCollectionLauncher = DataCollectionLauncherFactory.GetDataCollectorLauncher(processHelper, runSettings);
        }

        [TestMethod]
        public void BeforeTestRunStartShouldGetEnviornmentVariables()
        {
            var dataCollectionRequestSender = new DataCollectionRequestSender();

            using var proxyDataCollectionManager = new ProxyDataCollectionManager(mockRequestData.Object, runSettings, testSources, dataCollectionRequestSender, processHelper, dataCollectionLauncher);
            proxyDataCollectionManager.Initialize();

            var result = proxyDataCollectionManager.BeforeTestRunStart(true, true, mockTestMessageEventHandler.Object);

            Assert.AreEqual(1, result.EnvironmentVariables.Count);
        }

        [TestMethod]
        public void AfterTestRunShouldSendGetAttachments()
        {
            var dataCollectionRequestSender = new DataCollectionRequestSender();

            using var proxyDataCollectionManager = new ProxyDataCollectionManager(mockRequestData.Object, runSettings, testSources, dataCollectionRequestSender, processHelper, dataCollectionLauncher);
            proxyDataCollectionManager.Initialize();

            proxyDataCollectionManager.BeforeTestRunStart(true, true, mockTestMessageEventHandler.Object);

            var dataCollectionResult = proxyDataCollectionManager.AfterTestRunEnd(false, mockTestMessageEventHandler.Object);

            Assert.AreEqual("CustomDataCollector", dataCollectionResult.Attachments[0].DisplayName);
            Assert.AreEqual("my://custom/datacollector", dataCollectionResult.Attachments[0].Uri.ToString());
            Assert.IsTrue(dataCollectionResult.Attachments[0].Attachments[0].Uri.ToString().Contains("filename.txt"));
        }

        [TestMethod]
        public void AfterTestRunShouldHandleSocketFailureGracefully()
        {
            var socketCommManager = new SocketCommunicationManager();
            var dataCollectionRequestSender = new DataCollectionRequestSender(socketCommManager, JsonDataSerializer.Instance);
            var dataCollectionLauncher = DataCollectionLauncherFactory.GetDataCollectorLauncher(processHelper, runSettings);

            using var proxyDataCollectionManager = new ProxyDataCollectionManager(mockRequestData.Object, runSettings, testSources, dataCollectionRequestSender, processHelper, dataCollectionLauncher);
            proxyDataCollectionManager.Initialize();
            proxyDataCollectionManager.BeforeTestRunStart(true, true, mockTestMessageEventHandler.Object);

            var result = Process.GetProcessById(dataCollectionLauncher.DataCollectorProcessId);
            Assert.IsNotNull(result);

            socketCommManager.StopClient();

            var attachments = proxyDataCollectionManager.AfterTestRunEnd(false, mockTestMessageEventHandler.Object);

            Assert.IsNull(attachments);

            // Give time to datacollector process to exit.
            Assert.IsTrue(result.WaitForExit(500));
        }
    }
}