// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;


    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestPlatform.CrossPlatEngine.UnitTests.DataCollection;

    using Moq;

    [TestClass]
    public class ProxyExecutionManagerWithDataCollectionTests
    {
        private ProxyExecutionManager testExecutionManager;

        private Mock<ITestHostProvider> mockTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;

        private Mock<IProxyDataCollectionManager> mockDataCollectionManager;

        private ProxyExecutionManagerWithDataCollection proxyExecutionManager;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int testableClientConnectionTimeout = 400;

        [TestInitialize]
        public void TestInit()
        {
            this.mockTestHostManager = new Mock<ITestHostProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.testExecutionManager = new ProxyExecutionManager(this.mockRequestSender.Object, this.mockTestHostManager.Object, this.testableClientConnectionTimeout);
            this.mockDataCollectionManager = new Mock<IProxyDataCollectionManager>();
            this.proxyExecutionManager = new ProxyExecutionManagerWithDataCollection(this.mockTestHostManager.Object, this.mockDataCollectionManager.Object);
        }

        [TestMethod]
        public void InitializeShouldInitializeDataCollectionProcessIfDataCollectionIsEnabled()
        {
            this.proxyExecutionManager.Initialize();

            mockDataCollectionManager.Verify(dc => dc.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>()), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfThrownByDataCollectionManager()
        {
            this.mockDataCollectionManager.Setup(x => x.Initialize()).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
            {
                this.proxyExecutionManager.Initialize();
            });
        }

        [TestMethod]
        public void InitializeShouldCallAfterTestRunIfExceptionIsThrownWhileCreatingDataCollectionProcess()
        {
            mockDataCollectionManager.Setup(dc => dc.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>())).Throws(new System.Exception("MyException"));

            Assert.ThrowsException<Exception>(() =>
            {
                this.proxyExecutionManager.Initialize();
            });

            mockDataCollectionManager.Verify(dc => dc.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>()), Times.Once);
            mockDataCollectionManager.Verify(dc => dc.AfterTestRunEnd(It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>()), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldSaveExceptionMessagesIfThrownByDataCollectionProcess()
        {
            mockDataCollectionManager.Setup(dc => dc.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>())).Throws(new System.Exception("MyException"));

            ProxyDataCollectionManager proxyDataCollectonManager = new ProxyDataCollectionManager(string.Empty, new DummyDataCollectionRequestSender(), new DummyDataCollectionLauncher());

            var proxyExecutionManager = new ProxyExecutionManagerWithDataCollection(this.mockTestHostManager.Object, proxyDataCollectonManager);
            proxyExecutionManager.Initialize();
            Assert.IsNotNull(proxyExecutionManager.DataCollectionRunEventsHandler.ExceptionMessages);
            Assert.AreEqual(1, proxyExecutionManager.DataCollectionRunEventsHandler.ExceptionMessages.Count);
        }

        [TestMethod]
        public void CancelShouldInvokeAfterTestCaseEnd()
        {
            this.proxyExecutionManager.Cancel();

            this.mockDataCollectionManager.Verify(x => x.AfterTestRunEnd(true, It.IsAny<ITestMessageEventHandler>()), Times.Once);
        }

        [TestMethod]
        public void CancelShouldThrowExceptionIfThrownByProxyDataCollectionManager()
        {
            this.mockDataCollectionManager.Setup(x => x.AfterTestRunEnd(true, It.IsAny<ITestMessageEventHandler>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
            {
                this.proxyExecutionManager.Cancel();
            });
        }

        [TestMethod]
        public void UpdateTestProcessStartInfoShouldUpdateDataCollectionPortArg()
        {
            this.mockDataCollectionManager.Setup(x => x.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>())).Returns(DataCollectionParameters.CreateDefaultParameterInstance());

            var testProcessStartInfo = new TestProcessStartInfo();
            testProcessStartInfo.Arguments = string.Empty;

            var proxyExecutionManager = new TestableProxyExecutionManagerWithDataCollection(this.mockTestHostManager.Object, this.mockDataCollectionManager.Object);
            proxyExecutionManager.UpdateTestProcessStartInfoWrapper(testProcessStartInfo);

            Assert.IsTrue(testProcessStartInfo.Arguments.Contains("--datacollectionport 0"));
        }
    }

    internal class TestableProxyExecutionManagerWithDataCollection : ProxyExecutionManagerWithDataCollection
    {
        public TestableProxyExecutionManagerWithDataCollection(ITestHostProvider testHostManager, IProxyDataCollectionManager proxyDataCollectionManager) : base(testHostManager, proxyDataCollectionManager)
        {
        }

        public TestProcessStartInfo UpdateTestProcessStartInfoWrapper(TestProcessStartInfo testProcessStartInfo)
        {
            return this.UpdateTestProcessStartInfo(testProcessStartInfo);
        }

        protected override TestProcessStartInfo UpdateTestProcessStartInfo(TestProcessStartInfo testProcessStartInfo)
        {
            return base.UpdateTestProcessStartInfo(testProcessStartInfo);
        }
    }
}