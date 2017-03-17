// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProxyDataCollectionManagerTests
    {
        private Mock<IDataCollectionRequestSender> mockDataCollectionRequestSender;
        private ProxyDataCollectionManager proxyDataCollectionManager;
        private Mock<IDataCollectionLauncher> mockDataCollectionLauncher;
        private Mock<IProcessHelper> mockProcessHelper;

        [TestInitialize]
        public void Initialize()
        {
            this.mockDataCollectionRequestSender = new Mock<IDataCollectionRequestSender>();
            this.mockDataCollectionLauncher = new Mock<IDataCollectionLauncher>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.proxyDataCollectionManager = new ProxyDataCollectionManager(string.Empty, this.mockDataCollectionRequestSender.Object, this.mockProcessHelper.Object, this.mockDataCollectionLauncher.Object);
        }

        [TestMethod]
        public void InitializeShouldInitializeCommunication()
        {
            this.proxyDataCollectionManager.Initialize();

            this.mockDataCollectionLauncher.Verify(x => x.LaunchDataCollector(It.IsAny<IDictionary<string, string>>(), It.IsAny<IList<string>>()), Times.Once);
            this.mockDataCollectionRequestSender.Verify(x => x.WaitForRequestHandlerConnection(5000), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldPassDiagArgumentsIfDiagIsEnabled()
        {
            // Saving the EqtTrace state
#if NET46
            var traceLevel = EqtTrace.TraceLevel;
            EqtTrace.TraceLevel = TraceLevel.Off;
#else
            var traceLevel = (TraceLevel)EqtTrace.TraceLevel;
            EqtTrace.TraceLevel = (PlatformTraceLevel)TraceLevel.Off;
#endif
            var traceFileName = EqtTrace.LogFile;

            try
            {
                EqtTrace.InitializeVerboseTrace("mylog.txt");

                this.proxyDataCollectionManager.Initialize();

                this.mockDataCollectionLauncher.Verify(
                    x =>
                        x.LaunchDataCollector(
                            It.IsAny<IDictionary<string, string>>(),
                            It.Is<IList<string>>(list => list.Contains("--diag"))),
                    Times.Once);
                this.mockDataCollectionRequestSender.Verify(x => x.WaitForRequestHandlerConnection(5000), Times.Once);
            }
            finally
            {
                // Restoring to initial state for EqtTrace
                EqtTrace.InitializeVerboseTrace(traceFileName);
#if NET46
                EqtTrace.TraceLevel = traceLevel;
#else
                EqtTrace.TraceLevel = (PlatformTraceLevel)traceLevel;
#endif
            }
        }

        [TestMethod]
        public void BeforeTestRunStartShouldReturnDataCollectorParameters()
        {
            var res = new DataCollectionParameters(new Dictionary<string, string>(), 123);
            this.mockDataCollectionRequestSender.Setup(x => x.SendBeforeTestRunStartAndGetResult(It.IsAny<string>(), It.IsAny<ITestMessageEventHandler>())).Returns(res);

            var result = this.proxyDataCollectionManager.BeforeTestRunStart(true, true, null);

            this.mockDataCollectionRequestSender.Verify(
                x => x.SendBeforeTestRunStartAndGetResult(It.IsAny<string>(), It.IsAny<ITestMessageEventHandler>()), Times.Once);
            Assert.IsNotNull(result);
            Assert.AreEqual(res.DataCollectionEventsPort, result.DataCollectionEventsPort);
            Assert.AreEqual(res.EnvironmentVariables.Count, result.EnvironmentVariables.Count);
        }

        [TestMethod]
        public void BeforeTestRunStartsShouldInvokeRunEventsHandlerIfExceptionIsThrown()
        {
            var mockRunEventsHandler = new Mock<ITestMessageEventHandler>();
            this.mockDataCollectionRequestSender.Setup(
                    x => x.SendBeforeTestRunStartAndGetResult(It.IsAny<string>(), It.IsAny<ITestMessageEventHandler>()))
                .Throws<Exception>();

            var result = this.proxyDataCollectionManager.BeforeTestRunStart(true, true, mockRunEventsHandler.Object);

            mockRunEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, "Exception of type 'System.Exception' was thrown."), Times.Once);
            Assert.AreEqual(result, null);
        }

        [TestMethod]
        public void AfterTestRunEndShouldReturnAttachments()
        {
            var attachments = new Collection<AttachmentSet>();
            var dispName = "MockAttachments";
            var uri = new Uri("Mock://Attachments");
            var attachmentSet = new AttachmentSet(uri, dispName);
            attachments.Add(attachmentSet);

            this.mockDataCollectionRequestSender.Setup(x => x.SendAfterTestRunStartAndGetResult(It.IsAny<ITestRunEventsHandler>(), It.IsAny<bool>())).Returns(attachments);

            var result = this.proxyDataCollectionManager.AfterTestRunEnd(false, null);

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
            this.mockDataCollectionRequestSender.Setup(
                    x => x.SendAfterTestRunStartAndGetResult(It.IsAny<ITestMessageEventHandler>(), It.IsAny<bool>()))
                .Throws<Exception>();

            var result = this.proxyDataCollectionManager.AfterTestRunEnd(false, mockRunEventsHandler.Object);

            mockRunEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, "Exception of type 'System.Exception' was thrown."), Times.Once);
        }
    }
}