// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class ProxyDataCollectionManagerTests
    {
        private Mock<IDataCollectionRequestSender> mockDataCollectionRequestSender;
        private ProxyDataCollectionManager proxyDataCollectionManager;
        private Mock<IDataCollectionLauncher> mockDataCollectionLauncher;
        private Mock<IProcessHelper> mockProcessHelper;
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;

        [TestInitialize]
        public void Initialize()
        {
            this.mockDataCollectionRequestSender = new Mock<IDataCollectionRequestSender>();
            this.mockDataCollectionLauncher = new Mock<IDataCollectionLauncher>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);
            this.proxyDataCollectionManager = new ProxyDataCollectionManager(this.mockRequestData.Object, string.Empty, this.mockDataCollectionRequestSender.Object, this.mockProcessHelper.Object, this.mockDataCollectionLauncher.Object);
        }

        [TestMethod]
        public void InitializeShouldInitializeCommunication()
        {
            this.mockDataCollectionRequestSender.Setup(x => x.WaitForRequestHandlerConnection(ProxyDataCollectionManager.DataCollectorConnectionTimeout)).Returns(true);
            this.proxyDataCollectionManager.Initialize();

            this.mockDataCollectionLauncher.Verify(x => x.LaunchDataCollector(It.IsAny<IDictionary<string, string>>(), It.IsAny<IList<string>>()), Times.Once);
            this.mockDataCollectionRequestSender.Verify(x => x.WaitForRequestHandlerConnection(ProxyDataCollectionManager.DataCollectorConnectionTimeout), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfConnectionTimeouts()
        {
            this.mockDataCollectionRequestSender.Setup( x => x.WaitForRequestHandlerConnection(ProxyDataCollectionManager.DataCollectorConnectionTimeout)).Returns(false);

            Assert.ThrowsException<TestPlatformException>(() => this.proxyDataCollectionManager.Initialize());
        }

        [TestMethod]
        public void InitializeShouldSetTimeoutBasedOnTimeoutEnvironmentVarible()
        {

            var timeout = 10;
            Environment.SetEnvironmentVariable(ProxyDataCollectionManager.TimeoutEnvironmentVaribleName, timeout.ToString());
            this.mockDataCollectionRequestSender.Setup(x => x.WaitForRequestHandlerConnection(timeout * 1000)).Returns(true);

            this.proxyDataCollectionManager.Initialize();
            Environment.SetEnvironmentVariable(ProxyDataCollectionManager.TimeoutEnvironmentVaribleName, string.Empty);

            this.mockDataCollectionRequestSender.Verify(x => x.WaitForRequestHandlerConnection(timeout * 1000), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldSetTimeoutBasedOnDebugEnvironmentVaribleName()
        {
            Environment.SetEnvironmentVariable(ProxyDataCollectionManager.DebugEnvironmentVaribleName, "1");
            this.mockDataCollectionRequestSender.Setup(x => x.WaitForRequestHandlerConnection(ProxyDataCollectionManager.DataCollectorConnectionTimeout * 5)).Returns(true);

            this.proxyDataCollectionManager.Initialize();
            Environment.SetEnvironmentVariable(ProxyDataCollectionManager.DebugEnvironmentVaribleName, string.Empty);

            this.mockDataCollectionRequestSender.Verify(x => x.WaitForRequestHandlerConnection(ProxyDataCollectionManager.DataCollectorConnectionTimeout * 5), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldPassDiagArgumentsIfDiagIsEnabled()
        {
            // Saving the EqtTrace state
#if NET451
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
                this.mockDataCollectionRequestSender.Setup(x => x.WaitForRequestHandlerConnection(ProxyDataCollectionManager.DataCollectorConnectionTimeout)).Returns(true);

                this.proxyDataCollectionManager.Initialize();

                this.mockDataCollectionLauncher.Verify(
                    x =>
                        x.LaunchDataCollector(
                            It.IsAny<IDictionary<string, string>>(),
                            It.Is<IList<string>>(list => list.Contains("--diag"))),
                    Times.Once);
                this.mockDataCollectionRequestSender.Verify(x => x.WaitForRequestHandlerConnection(ProxyDataCollectionManager.DataCollectorConnectionTimeout), Times.Once);
            }
            finally
            {
                // Restoring to initial state for EqtTrace
                EqtTrace.InitializeVerboseTrace(traceFileName);
#if NET451
                EqtTrace.TraceLevel = traceLevel;
#else
                EqtTrace.TraceLevel = (PlatformTraceLevel)traceLevel;
#endif
            }
        }

        [TestMethod]
        public void BeforeTestRunStartShouldPassRunSettingsWithExtensionsFolderUpdatedAsTestAdapterPath()
        {
            string runsettings = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            this.proxyDataCollectionManager = new ProxyDataCollectionManager(this.mockRequestData.Object, runsettings, this.mockDataCollectionRequestSender.Object, this.mockProcessHelper.Object, this.mockDataCollectionLauncher.Object);

            BeforeTestRunStartResult res = new BeforeTestRunStartResult(new Dictionary<string, string>(), 123);
            this.mockDataCollectionRequestSender.Setup(x => x.SendBeforeTestRunStartAndGetResult(It.IsAny<string>(), It.IsAny<ITestMessageEventHandler>())).Returns(res);

            var result = this.proxyDataCollectionManager.BeforeTestRunStart(true, true, null);

            var extensionsFolderPath = Path.Combine(Path.GetDirectoryName(typeof(ITestPlatform).GetTypeInfo().Assembly.Location), "Extensions");
            var expectedSettingsXML = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings><RunConfiguration><TestAdaptersPaths>{extensionsFolderPath}</TestAdaptersPaths></RunConfiguration></RunSettings>";
            this.mockDataCollectionRequestSender.Verify(
                x => x.SendBeforeTestRunStartAndGetResult(expectedSettingsXML, It.IsAny<ITestMessageEventHandler>()), Times.Once);
        }

        [TestMethod]
        public void BeforeTestRunStartShouldReturnDataCollectorParameters()
        {
            BeforeTestRunStartResult res = new BeforeTestRunStartResult(new Dictionary<string, string>(), 123);
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

            mockRunEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.IsRegex("Exception of type 'System.Exception' was thrown..*")), Times.Once);
            Assert.AreEqual(0, result.EnvironmentVariables.Count);
            Assert.AreEqual(false, result.AreTestCaseLevelEventsRequired);
            Assert.AreEqual(0, result.DataCollectionEventsPort);
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

            mockRunEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.IsRegex("Exception of type 'System.Exception' was thrown..*")), Times.Once);
        }


        [TestMethod]
        public void ProxyDataCollectionShouldLogEnabledDataCollectors()
        {
            string settings = @"<RunSettings>
              <DataCollectionRunSettings>
                <DataCollectors>
                    <DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"">
                </DataCollector>
              </DataCollectors>
             </DataCollectionRunSettings>
           </RunSettings>";

            this.mockRequestData.Setup(rd => rd.IsTelemetryOptedIn).Returns(true);

            var proxyExecutionManager = new ProxyDataCollectionManager(this.mockRequestData.Object, settings, this.mockDataCollectionRequestSender.Object, this.mockProcessHelper.Object, this.mockDataCollectionLauncher.Object);

            var resultString = "{ FriendlyName = Code Coverage, Uri = datacollector://microsoft/CodeCoverage/2.0 }";
            this.mockMetricsCollection.Verify(rd => rd.Add(TelemetryDataConstants.DataCollectorsEnabled, resultString), Times.Once);
        }
    }
}