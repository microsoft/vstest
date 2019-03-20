// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using CommunicationUtilitiesResources = Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources.Resources;
    using CoreUtilitiesConstants = Microsoft.VisualStudio.TestPlatform.CoreUtilities.Constants;

    using Moq;
    using System.Collections;

    [TestClass]
    public class ProxyDataCollectionManagerTests
    {
        private Mock<IDataCollectionRequestSender> mockDataCollectionRequestSender;
        private ProxyDataCollectionManager proxyDataCollectionManager;
        private Mock<IDataCollectionLauncher> mockDataCollectionLauncher;
        private Mock<IProcessHelper> mockProcessHelper;
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;
        private static readonly string TimoutErrorMessage =
            "vstest.console process failed to connect to datacollector process after 90 seconds. This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.";

        [TestInitialize]
        public void Initialize()
        {
            this.mockDataCollectionRequestSender = new Mock<IDataCollectionRequestSender>();
            this.mockDataCollectionLauncher = new Mock<IDataCollectionLauncher>();
            this.mockProcessHelper = new Mock<IProcessHelper>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);
            this.proxyDataCollectionManager = new ProxyDataCollectionManager(this.mockRequestData.Object, string.Empty, new List<string>() { "testsource1.dll" }, this.mockDataCollectionRequestSender.Object, this.mockProcessHelper.Object, this.mockDataCollectionLauncher.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, string.Empty);
            Environment.SetEnvironmentVariable(ProxyDataCollectionManager.DebugEnvironmentVaribleName, string.Empty);
        }

        [TestMethod]
        public void InitializeShouldInitializeCommunication()
        {
            this.mockDataCollectionRequestSender.Setup(x => x.WaitForRequestHandlerConnection(EnvironmentHelper.DefaultConnectionTimeout * 1000)).Returns(true);
            this.proxyDataCollectionManager.Initialize();

            this.mockDataCollectionLauncher.Verify(x => x.LaunchDataCollector(It.IsAny<IDictionary<string, string>>(), It.IsAny<IList<string>>()), Times.Once);
            this.mockDataCollectionRequestSender.Verify(x => x.WaitForRequestHandlerConnection(EnvironmentHelper.DefaultConnectionTimeout * 1000), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfConnectionTimeouts()
        {
            this.mockDataCollectionRequestSender.Setup(x => x.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            var message = Assert.ThrowsException<TestPlatformException>(() => this.proxyDataCollectionManager.Initialize()).Message;
            Assert.AreEqual(message, ProxyDataCollectionManagerTests.TimoutErrorMessage);
        }

        [TestMethod]
        public void InitializeShouldSetTimeoutBasedOnTimeoutEnvironmentVarible()
        {
            var timeout = 10;
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, timeout.ToString());
            this.mockDataCollectionRequestSender.Setup(x => x.WaitForRequestHandlerConnection(timeout * 1000)).Returns(true);

            this.proxyDataCollectionManager.Initialize();

            this.mockDataCollectionRequestSender.Verify(x => x.WaitForRequestHandlerConnection(timeout * 1000), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldSetTimeoutBasedOnDebugEnvironmentVaribleName()
        {
            Environment.SetEnvironmentVariable(ProxyDataCollectionManager.DebugEnvironmentVaribleName, "1");
            var expectedTimeout = EnvironmentHelper.DefaultConnectionTimeout * 1000 * 5;
            this.mockDataCollectionRequestSender.Setup(x => x.WaitForRequestHandlerConnection(expectedTimeout)).Returns(true);

            this.proxyDataCollectionManager.Initialize();

            this.mockDataCollectionRequestSender.Verify(x => x.WaitForRequestHandlerConnection(expectedTimeout), Times.Once);
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
                EqtTrace.InitializeTrace("mylog.txt", PlatformTraceLevel.Info);
                this.mockDataCollectionRequestSender.Setup(x => x.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

                this.proxyDataCollectionManager.Initialize();

                var expectedTraceLevel = (int)PlatformTraceLevel.Info;
                this.mockDataCollectionLauncher.Verify(
                    x =>
                        x.LaunchDataCollector(
                            It.IsAny<IDictionary<string, string>>(),
                            It.Is<IList<string>>(list => list.Contains("--diag") && list.Contains("--tracelevel") && list.Contains(expectedTraceLevel.ToString()))),
                    Times.Once);
            }
            finally
            {
                // Restoring to initial state for EqtTrace
                EqtTrace.InitializeTrace(traceFileName, PlatformTraceLevel.Verbose);
#if NET451
                EqtTrace.TraceLevel = traceLevel;
#else
                EqtTrace.TraceLevel = (PlatformTraceLevel)traceLevel;
#endif
            }
        }

        [TestMethod]
        public void SendTestHostInitiazliedShouldPassProcessIdToRequestSender()
        {
            this.proxyDataCollectionManager.TestHostLaunched(1234);

            this.mockDataCollectionRequestSender.Verify(x => x.SendTestHostLaunched(It.Is<TestHostLaunchedPayload>(y => y.ProcessId == 1234)));
        }

        [TestMethod]
        public void BeforeTestRunStartShouldPassRunSettingsWithExtensionsFolderUpdatedAsTestAdapterPath()
        {
            string runsettings = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings><RunConfiguration></RunConfiguration></RunSettings>";
            var sourceList = new List<string>() { "testsource1.dll" };
            this.proxyDataCollectionManager = new ProxyDataCollectionManager(this.mockRequestData.Object, runsettings, sourceList, this.mockDataCollectionRequestSender.Object, this.mockProcessHelper.Object, this.mockDataCollectionLauncher.Object);

            BeforeTestRunStartResult res = new BeforeTestRunStartResult(new Dictionary<string, string>(), 123);
            this.mockDataCollectionRequestSender.Setup(x => x.SendBeforeTestRunStartAndGetResult(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<ITestMessageEventHandler>())).Returns(res);

            var result = this.proxyDataCollectionManager.BeforeTestRunStart(true, true, null);

            var extensionsFolderPath = Path.Combine(Path.GetDirectoryName(typeof(ITestPlatform).GetTypeInfo().Assembly.Location), "Extensions");
            var expectedSettingsXML = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><RunSettings><RunConfiguration><TestAdaptersPaths>{extensionsFolderPath}</TestAdaptersPaths></RunConfiguration></RunSettings>";
            this.mockDataCollectionRequestSender.Verify(
                x => x.SendBeforeTestRunStartAndGetResult(expectedSettingsXML, sourceList, It.IsAny<ITestMessageEventHandler>()), Times.Once);
        }

        [TestMethod]
        public void BeforeTestRunStartShouldReturnDataCollectorParameters()
        {
            BeforeTestRunStartResult res = new BeforeTestRunStartResult(new Dictionary<string, string>(), 123);
            var sourceList = new List<string>() { "testsource1.dll" };
            this.mockDataCollectionRequestSender.Setup(x => x.SendBeforeTestRunStartAndGetResult(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<ITestMessageEventHandler>())).Returns(res);

            var result = this.proxyDataCollectionManager.BeforeTestRunStart(true, true, null);

            this.mockDataCollectionRequestSender.Verify(
                x => x.SendBeforeTestRunStartAndGetResult(It.IsAny<string>(), sourceList, It.IsAny<ITestMessageEventHandler>()), Times.Once);
            Assert.IsNotNull(result);
            Assert.AreEqual(res.DataCollectionEventsPort, result.DataCollectionEventsPort);
            Assert.AreEqual(res.EnvironmentVariables.Count, result.EnvironmentVariables.Count);
        }

        [TestMethod]
        public void BeforeTestRunStartsShouldInvokeRunEventsHandlerIfExceptionIsThrown()
        {
            var mockRunEventsHandler = new Mock<ITestMessageEventHandler>();
            this.mockDataCollectionRequestSender.Setup(
                    x => x.SendBeforeTestRunStartAndGetResult(It.IsAny<string>(), new List<string>() { "testsource1.dll" }, It.IsAny<ITestMessageEventHandler>()))
                .Throws<Exception>();

            var result = this.proxyDataCollectionManager.BeforeTestRunStart(true, true, mockRunEventsHandler.Object);

            mockRunEventsHandler.Verify(eh => eh.HandleLogMessage(TestMessageLevel.Error, It.IsRegex("Exception of type 'System.Exception' was thrown..*")), Times.Once);
            Assert.AreEqual(0, result.EnvironmentVariables.Count);
            Assert.AreEqual(false, result.AreTestCaseLevelEventsRequired);
            Assert.AreEqual(0, result.DataCollectionEventsPort);
        }

        [TestMethod]
        public void SendBeforeTestRunStartAndGetResultShouldBeInvokedWithCorrectTestSources()
        {
            var testSources = new List<string>() { "abc.dll", "efg.dll" };
            this.proxyDataCollectionManager = new ProxyDataCollectionManager(this.mockRequestData.Object, string.Empty, testSources, this.mockDataCollectionRequestSender.Object, this.mockProcessHelper.Object, this.mockDataCollectionLauncher.Object);

            BeforeTestRunStartResult res = new BeforeTestRunStartResult(new Dictionary<string, string>(), 123);
            this.mockDataCollectionRequestSender.Setup(x => x.SendBeforeTestRunStartAndGetResult(string.Empty, testSources, It.IsAny<ITestMessageEventHandler>())).Returns(res);

            var result = this.proxyDataCollectionManager.BeforeTestRunStart(true, true, null);

            this.mockDataCollectionRequestSender.Verify(
                x => x.SendBeforeTestRunStartAndGetResult(string.Empty, testSources, It.IsAny<ITestMessageEventHandler>()), Times.Once);
            Assert.IsNotNull(result);
            Assert.AreEqual(res.DataCollectionEventsPort, result.DataCollectionEventsPort);
            Assert.AreEqual(res.EnvironmentVariables.Count, result.EnvironmentVariables.Count);
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

            var testSources = new List<string>() { "abc.dll", "efg.dll" };
            this.mockRequestData.Setup(rd => rd.IsTelemetryOptedIn).Returns(true);

            var proxyExecutionManager = new ProxyDataCollectionManager(this.mockRequestData.Object, settings, testSources, this.mockDataCollectionRequestSender.Object, this.mockProcessHelper.Object, this.mockDataCollectionLauncher.Object);

            var resultString = "{ FriendlyName = Code Coverage, Uri = datacollector://microsoft/CodeCoverage/2.0 }";
            this.mockMetricsCollection.Verify(rd => rd.Add(TelemetryDataConstants.DataCollectorsEnabled, resultString), Times.Once);
        }
    }
}