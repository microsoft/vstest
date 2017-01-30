// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.ComponentTests
{
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;

    using Moq;

    [TestClass]
    public class CommunicationLayerIntegrationTests
    {
        private string defaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";
        private Mock<ObjectModel.Client.ITestMessageEventHandler> mockTestMessageEventHandler;

        [TestInitialize]
        public void Init()
        {
            mockTestMessageEventHandler = new Mock<ObjectModel.Client.ITestMessageEventHandler>();
        }

        [TestMethod]
        public void BeforeTestRunStartShouldSendEventToDataCollectorProcess()
        {
            System.Diagnostics.Debugger.Launch();

            var dataCollectorSettings = string.Format("<DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom/datacollector\" assemblyQualifiedName=\"{0}\" codebase=\"{1}\" />", typeof(CustomDataCollector).AssemblyQualifiedName, typeof(CustomDataCollector).GetTypeInfo().Assembly.Location);

            var runSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            var dataCollectionRequestSender = new DataCollectionRequestSender();
            var dataCollectionLauncher = new DataCollectionLauncher();
            var proxyDataCollectionManager = new ProxyDataCollectionManager(ObjectModel.Architecture.AnyCPU, runSettings, dataCollectionRequestSender, dataCollectionLauncher);

            var result = proxyDataCollectionManager.BeforeTestRunStart(true, true, mockTestMessageEventHandler.Object);

            Assert.AreEqual(1, result.EnvironmentVariables.Count);
        }
    }
}
