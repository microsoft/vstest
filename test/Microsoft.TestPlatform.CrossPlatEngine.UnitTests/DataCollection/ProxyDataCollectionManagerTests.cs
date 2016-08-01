// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;

    [TestClass]
    public class ProxyDataCollectionManagerTests
    {
        private DummyDataCollectionRequestSender mockDataCollectionRequestSender;
        private ProxyDataCollectionManager proxyDataCollectionManager;
        private DummyDataCollectionLauncher mockDataCollectionLauncher;

        [TestInitialize]
        public void Initialize()
        {
            this.mockDataCollectionRequestSender = new DummyDataCollectionRequestSender();
            this.mockDataCollectionLauncher = new DummyDataCollectionLauncher();
            this.proxyDataCollectionManager = new ProxyDataCollectionManager(Architecture.AnyCPU, string.Empty, this.mockDataCollectionRequestSender, this.mockDataCollectionLauncher);
        }

        [TestMethod]
        public void InitializeSocketCommunicationShouldInitializeCommunication()
        {
            this.proxyDataCollectionManager.InitializeSocketCommunication(Architecture.X86);

            Assert.IsTrue(this.mockDataCollectionLauncher.isInitialized);
            Assert.IsTrue(this.mockDataCollectionLauncher.dataCollectorLaunched);
            Assert.IsTrue(this.mockDataCollectionRequestSender.waitForRequestHandlerConnection);
            Assert.AreEqual(5000, this.mockDataCollectionRequestSender.connectionTimeout);
        }

        [TestMethod]
        public void BeforeTestRunStartShouldReturnDataCollectorParameters()
        {
            BeforeTestRunStartResult res = new BeforeTestRunStartResult(new Dictionary<string, string>(), true, 123);
            this.mockDataCollectionRequestSender.BeforeTestRunStartResult = res;

            var result = proxyDataCollectionManager.BeforeTestRunStart(true, true, null);

            Assert.IsTrue(this.mockDataCollectionRequestSender.sendBeforeTestRunStartAndGetResult);
            Assert.IsNotNull(result);
            Assert.AreEqual(res.DataCollectionEventsPort, result.DataCollectionEventsPort);
            Assert.AreEqual(res.EnvironmentVariables.Count, result.EnvironmentVariables.Count);
            Assert.AreEqual(res.AreTestCaseLevelEventsRequired, result.AreTestCaseLevelEventsRequired);
        }

        [TestMethod]
        public void BeforeTestRunStartsShouldInvokeRunEventsHandlerIfExceptionIsThrown()
        {
            var mockRunEventsHandler = new Mock<ITestMessageEventHandler>();
            this.mockDataCollectionRequestSender.sendBeforeTestRunStartAndGetResultThrowException = true;

            mockRunEventsHandler.Setup(eh => eh.HandleLogMessage(TestMessageLevel.Error, "SocketException"));

            var result = this.proxyDataCollectionManager.BeforeTestRunStart(true, true, mockRunEventsHandler.Object);

            mockRunEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, "SocketException"), Times.Once);
            Assert.AreEqual(result.IsDataCollectionStarted, false);
            Assert.AreEqual(result.EnvironmentVariables, null);
            Assert.AreEqual(result.AreTestCaseLevelEventsRequired, false);
            Assert.AreEqual(result.DataCollectionEventsPort, 0);
        }

        [TestMethod]
        public void AfterTestRunEndShouldReturnAttachments()
        {
            var attachments = new Collection<AttachmentSet>();
            var dispName = "MockAttachments";
            var uri = new Uri("Mock://Attachments");
            var attachmentSet = new AttachmentSet(uri, dispName);
            attachments.Add(attachmentSet);

            this.mockDataCollectionRequestSender.Attachments = attachments;

            var result = this.proxyDataCollectionManager.AfterTestRunEnd(false, null);

            Assert.IsTrue(this.mockDataCollectionRequestSender.sendAfterTestRunStartAndGetResult);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Count, 1);
            Assert.IsNotNull(result[0]);
            Assert.AreEqual(result[0].DisplayName, dispName);
            Assert.AreEqual(uri, result[0].Uri);
        }

        [TestMethod]
        public void AfterTestRunEndShouldInvokeRunEventsHandlerIfExceptionIsThrown()
        {
            var mockRunEventsHandler = new Mock<ITestMessageEventHandler>();
            mockRunEventsHandler.Setup(eh => eh.HandleLogMessage(TestMessageLevel.Error, "SocketException"));
            this.mockDataCollectionRequestSender.sendAfterTestRunStartAndGetResultThrowException = true;

            var result = this.proxyDataCollectionManager.AfterTestRunEnd(false, mockRunEventsHandler.Object);

            mockRunEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, "SocketException"), Times.Once);
        }
    }

    internal class DummyDataCollectionRequestSender : IDataCollectionRequestSender
    {
        public bool waitForRequestHandlerConnection;
        public int connectionTimeout;
        public bool sendBeforeTestRunStartAndGetResult;
        public bool sendAfterTestRunStartAndGetResult;
        public bool sendAfterTestRunStartAndGetResultThrowException;
        public bool sendBeforeTestRunStartAndGetResultThrowException;

        public BeforeTestRunStartResult BeforeTestRunStartResult { get; set; }

        public Collection<AttachmentSet> Attachments { get; set; }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public int InitializeCommunication()
        {
            return 1;
        }

        public Collection<AttachmentSet> SendAfterTestRunStartAndGetResult()
        {
            if (sendAfterTestRunStartAndGetResultThrowException)
            {
                throw new Exception("SocketException");
            }

            this.sendAfterTestRunStartAndGetResult = true;
            return Attachments;
        }

        public BeforeTestRunStartResult SendBeforeTestRunStartAndGetResult(string settingXml)
        {
            if (this.sendBeforeTestRunStartAndGetResultThrowException)
            {
                throw new Exception("SocketException");

            }

            this.sendBeforeTestRunStartAndGetResult = true;
            return BeforeTestRunStartResult;
        }

        public bool WaitForRequestHandlerConnection(int connectionTimeout)
        {
            this.connectionTimeout = connectionTimeout;
            this.waitForRequestHandlerConnection = true;
            return true;
        }
    }

    internal class DummyDataCollectionLauncher : IDataCollectionLauncher
    {
        public bool isInitialized;
        public bool dataCollectorLaunched;
        public void Initialize(Architecture architecture)
        {
            this.isInitialized = true;
        }

        public int LaunchDataCollector(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            this.dataCollectorLaunched = true;
            return 1;
        }
    }
}