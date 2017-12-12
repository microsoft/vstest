// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProxyExecutionManagerWithDataCollectionTests
    {
        private ProxyExecutionManager testExecutionManager;

        private Mock<ITestRuntimeProvider> mockTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;

        private Mock<IProxyDataCollectionManager> mockDataCollectionManager;

        private Mock<IProcessHelper> mockProcessHelper;

        private ProxyExecutionManagerWithDataCollection proxyExecutionManager;

        private Mock<IDataSerializer> mockDataSerializer;

        private Mock<IRequestData> mockRequestData;

        private Mock<IMetricsCollection> mockMetricsCollection;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int testableClientConnectionTimeout = 400;

        [TestInitialize]
        public void TestInit()
        {
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);
            this.testExecutionManager = new ProxyExecutionManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.mockDataSerializer.Object, this.testableClientConnectionTimeout);
            this.mockDataCollectionManager = new Mock<IProxyDataCollectionManager>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.proxyExecutionManager = new ProxyExecutionManagerWithDataCollection(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.mockDataCollectionManager.Object);
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
            mockDataCollectionManager.Setup(dc => dc.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>())).Throws(new Exception("MyException"));

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
            var mockRequestSender = new Mock<IDataCollectionRequestSender>();
            mockRequestSender.Setup(x => x.SendBeforeTestRunStartAndGetResult(It.IsAny<string>(), It.IsAny<ITestMessageEventHandler>())).Throws(new Exception("MyException"));

            var mockDataCollectionLauncher = new Mock<IDataCollectionLauncher>();
            var proxyDataCollectonManager = new ProxyDataCollectionManager(this.mockRequestData.Object, string.Empty, mockRequestSender.Object, this.mockProcessHelper.Object, mockDataCollectionLauncher.Object);

            var proxyExecutionManager = new ProxyExecutionManagerWithDataCollection(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, proxyDataCollectonManager);
            proxyExecutionManager.Initialize();
            Assert.IsNotNull(proxyExecutionManager.DataCollectionRunEventsHandler.Messages);
            Assert.AreEqual(TestMessageLevel.Error, proxyExecutionManager.DataCollectionRunEventsHandler.Messages[0].Item1);
            StringAssert.Contains(proxyExecutionManager.DataCollectionRunEventsHandler.Messages[0].Item2, "MyException");
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

            var proxyExecutionManager = new TestableProxyExecutionManagerWithDataCollection(this.mockRequestSender.Object, this.mockTestHostManager.Object, this.mockDataCollectionManager.Object);
            proxyExecutionManager.UpdateTestProcessStartInfoWrapper(testProcessStartInfo);

            Assert.IsTrue(testProcessStartInfo.Arguments.Contains("--datacollectionport 0"));
        }

        [TestMethod]
        public void UpdateTestProcessStartInfoShouldUpdateTelemetryOptedInArgTrueIfTelemetryOptedIn()
        {
            var mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.IsTelemetryOptedIn).Returns(true);

            this.mockDataCollectionManager.Setup(x => x.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>())).Returns(DataCollectionParameters.CreateDefaultParameterInstance());

            var testProcessStartInfo = new TestProcessStartInfo();
            testProcessStartInfo.Arguments = string.Empty;

            var proxyExecutionManager = new TestableProxyExecutionManagerWithDataCollection(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.mockDataCollectionManager.Object);

            // Act.
            proxyExecutionManager.UpdateTestProcessStartInfoWrapper(testProcessStartInfo);

            // Verify.
            Assert.IsTrue(testProcessStartInfo.Arguments.Contains("--telemetryoptedin true"));
        }

        [TestMethod]
        public void UpdateTestProcessStartInfoShouldUpdateTelemetryOptedInArgFalseIfTelemetryOptedOut()
        {
            var mockRequestData = new Mock<IRequestData>();
            this.mockRequestData.Setup(rd => rd.IsTelemetryOptedIn).Returns(false);

            this.mockDataCollectionManager.Setup(x => x.BeforeTestRunStart(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<ITestMessageEventHandler>())).Returns(DataCollectionParameters.CreateDefaultParameterInstance());

            var testProcessStartInfo = new TestProcessStartInfo();
            testProcessStartInfo.Arguments = string.Empty;

            var proxyExecutionManager = new TestableProxyExecutionManagerWithDataCollection(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.mockDataCollectionManager.Object);

            // Act.
            proxyExecutionManager.UpdateTestProcessStartInfoWrapper(testProcessStartInfo);

            // Verify.
            Assert.IsTrue(testProcessStartInfo.Arguments.Contains("--telemetryoptedin false"));
        }
    }

    internal class TestableProxyExecutionManagerWithDataCollection : ProxyExecutionManagerWithDataCollection
    {
        public TestableProxyExecutionManagerWithDataCollection(ITestRequestSender testRequestSender, ITestRuntimeProvider testHostManager, IProxyDataCollectionManager proxyDataCollectionManager) : base(new RequestData{MetricsCollection = new NoOpMetricsCollection()}, testRequestSender, testHostManager, proxyDataCollectionManager)
        {
        }

        public TestableProxyExecutionManagerWithDataCollection(IRequestData requestData, ITestRequestSender testRequestSender, ITestRuntimeProvider testHostManager, IProxyDataCollectionManager proxyDataCollectionManager) : base(requestData, testRequestSender, testHostManager, proxyDataCollectionManager)
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