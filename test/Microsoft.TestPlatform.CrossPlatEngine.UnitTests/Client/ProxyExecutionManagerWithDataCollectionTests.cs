// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using TestPlatform.CrossPlatEngine.UnitTests.DataCollection;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProxyExecutionManagerWithDataCollectionTests
    {
        private ProxyExecutionManager testExecutionManager;

        private Mock<ITestHostManager> mockTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;

        private Mock<IProxyDataCollectionManager> mockDataCollectionClient;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int testableClientConnectionTimeout = 400;

        [TestInitialize]
        public void TestInit()
        {
            this.mockTestHostManager = new Mock<ITestHostManager>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.testExecutionManager = new ProxyExecutionManager(this.mockRequestSender.Object, this.mockTestHostManager.Object, this.testableClientConnectionTimeout);
            this.mockDataCollectionClient = new Mock<IProxyDataCollectionManager>();
        }

        [TestMethod]
        public void InitializeShouldInitializeDataCollectionProcessIfDataCollectionIsEnabled()
        {
            var proxyExecutionManager = new ProxyExecutionManagerWithDataCollection(this.mockDataCollectionClient.Object);
            proxyExecutionManager.Initialize(this.mockTestHostManager.Object);

            mockDataCollectionClient.Verify(dc => dc.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>()), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldCallAfterTestRunIfExceptionIsThrownWhileCreatingDataCollectionProcess()
        {
            mockDataCollectionClient.Setup(dc => dc.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>())).Throws(new System.Exception("MyException"));
            mockDataCollectionClient.Setup(dc => dc.AfterTestRunEnd(It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>()));

            var proxyExecutionManager = new ProxyExecutionManagerWithDataCollection(this.mockDataCollectionClient.Object);
            proxyExecutionManager.Initialize(this.mockTestHostManager.Object);

            mockDataCollectionClient.Verify(dc => dc.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>()), Times.Once);
            mockDataCollectionClient.Verify(dc => dc.AfterTestRunEnd(It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>()), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldSaveExceptionMessagesIfThrownByDataCollectionProcess()
        {
            mockDataCollectionClient.Setup(dc => dc.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>())).Throws(new System.Exception("MyException"));
            mockDataCollectionClient.Setup(dc => dc.AfterTestRunEnd(It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>()));

            ProxyDataCollectionManager proxyDataCollectonManager = new ProxyDataCollectionManager(Architecture.AnyCPU, string.Empty, new DummyDataCollectionRequestSender(), new DummyDataCollectionLauncher());

            var proxyExecutionManager = new ProxyExecutionManagerWithDataCollection(proxyDataCollectonManager);
            proxyExecutionManager.Initialize(this.mockTestHostManager.Object);
            Assert.IsNotNull(proxyExecutionManager.DataCollectionRunEventsHandler.ExceptionMessages);
            Assert.AreEqual(1, proxyExecutionManager.DataCollectionRunEventsHandler.ExceptionMessages.Count);
        }
    }
}