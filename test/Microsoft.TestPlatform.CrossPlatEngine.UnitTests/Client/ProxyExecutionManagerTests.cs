// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    using CrossPlatEngineResources = Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Resources;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;

    [TestClass]
    public class ProxyExecutionManagerTests : ProxyBaseManagerTests
    {
        private readonly Mock<ITestRequestSender> mockRequestSender;

        private readonly Mock<TestRunCriteria> mockTestRunCriteria;

        private readonly Mock<IRequestData> mockRequestData;

        private readonly Mock<IMetricsCollection> mockMetricsCollection;

        private readonly Mock<IFileHelper> mockFileHelper;

        private ProxyExecutionManager testExecutionManager;

        //private Mock<IDataSerializer> mockDataSerializer;

        public ProxyExecutionManagerTests()
        {
            mockRequestSender = new Mock<ITestRequestSender>();
            mockTestRunCriteria = new Mock<TestRunCriteria>(new List<string> { "source.dll" }, 10);
            //this.mockDataSerializer = new Mock<IDataSerializer>();
            mockRequestData = new Mock<IRequestData>();
            mockMetricsCollection = new Mock<IMetricsCollection>();
            mockFileHelper = new Mock<IFileHelper>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            testExecutionManager = new ProxyExecutionManager(mockRequestData.Object, mockRequestSender.Object, mockTestHostManager.Object, mockDataSerializer.Object, mockFileHelper.Object);

            //this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(null)).Returns(new Message());
            //this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(string.Empty)).Returns(new Message());
        }

        [TestMethod]
        public void StartTestRunShouldNotInitializeExtensionsOnNoExtensions()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

            mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void StartTestRunShouldAllowRuntimeProviderToUpdateAdapterSource()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            mockTestHostManager.Setup(hm => hm.GetTestSources(mockTestRunCriteria.Object.Sources)).Returns(mockTestRunCriteria.Object.Sources);
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestHostManager.Verify(hm => hm.GetTestSources(mockTestRunCriteria.Object.Sources), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldUpdateTestCaseSourceIfTestCaseSourceDiffersFromTestHostManagerSource()
        {
            var actualSources = new List<string> { "actualSource.dll" };
            var inputSource = new List<string> { "inputPackage.appxrecipe" };

            var testRunCriteria = new TestRunCriteria(
                    new List<TestCase> { new TestCase("A.C.M", new Uri("excutor://dummy"), inputSource.FirstOrDefault()) },
                    frequencyOfRunStatsChangeEvent: 10);

            mockTestHostManager.Setup(hm => hm.GetTestSources(inputSource)).Returns(actualSources);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string>());
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(testRunCriteria, mockTestRunEventsHandler.Object);

            mockTestHostManager.Verify(hm => hm.GetTestSources(inputSource), Times.Once);
            Assert.AreEqual(actualSources.FirstOrDefault(), testRunCriteria.Tests.FirstOrDefault().Source);
        }

        [TestMethod]
        public void StartTestRunShouldNotUpdateTestCaseSourceIfTestCaseSourceDoNotDifferFromTestHostManagerSource()
        {
            var actualSources = new List<string> { "actualSource.dll" };
            var inputSource = new List<string> { "actualSource.dll" };

            var testRunCriteria = new TestRunCriteria(
                    new List<TestCase> { new TestCase("A.C.M", new Uri("excutor://dummy"), inputSource.FirstOrDefault()) },
                    frequencyOfRunStatsChangeEvent: 10);

            mockTestHostManager.Setup(hm => hm.GetTestSources(inputSource)).Returns(actualSources);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string>());
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(testRunCriteria, mockTestRunEventsHandler.Object);

            mockTestHostManager.Verify(hm => hm.GetTestSources(inputSource), Times.Once);
            Assert.AreEqual(actualSources.FirstOrDefault(), testRunCriteria.Tests.FirstOrDefault().Source);
        }

        [TestMethod]
        public void StartTestRunShouldNotInitializeExtensionsOnCommunicationFailure()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void StartTestRunShouldInitializeExtensionsIfPresent()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            try
            {
                var extensions = new List<string>() { "C:\\foo.dll" };
                mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                mockTestHostManager.Setup(x => x.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                    .Returns(extensions);

                testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

                // Also verify that we have waited for client connection.
                mockRequestSender.Verify(s => s.InitializeExecution(extensions), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void StartTestRunShouldQueryTestHostManagerForExtensions()
        {
            TestPluginCache.Instance = null;
            try
            {
                mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new[] { "he1.dll", "c:\\e1.dll" });

                testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

                mockRequestSender.Verify(s => s.InitializeExecution(new[] { "he1.dll", "c:\\e1.dll" }), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void StartTestRunShouldPassAdapterToTestHostManagerFromTestPluginCacheExtensions()
        {
            // We are updating extension with testadapter only to make it easy to test.
            // In product code it filter out testadapter from extension
            TestPluginCache.Instance.UpdateExtensions(new List<string> { "abc.TestAdapter.dll", "xyz.TestAdapter.dll" }, false);
            try
            {
                mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
                mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                var expectedResult = TestPluginCache.Instance.GetExtensionPaths(string.Empty);

                testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

                mockTestHostManager.Verify(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), expectedResult), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void StartTestRunShouldNotInitializeDefaultAdaptersIfSkipDefaultAdaptersIsTrue()
        {
            InvokeAndVerifyStartTestRun(true);
        }

        [TestMethod]
        public void StartTestRunShouldInitializeDefaultAdaptersIfSkipDefaultAdaptersIsFalse()
        {
            InvokeAndVerifyStartTestRun(false);
        }

        [TestMethod]
        public void StartTestRunShouldIntializeTestHost()
        {
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

            mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
            mockTestHostManager.Verify(thl => thl.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotSendStartTestRunRequestIfCommunicationFails()
        {
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback(
                    () => mockTestHostManager.Raise(thm => thm.HostLaunched += null, new HostProviderEventArgs(string.Empty)))
                .Returns(Task.FromResult(false));

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockRequestSender.Verify(s => s.StartTestRun(It.IsAny<TestRunCriteriaWithSources>(), It.IsAny<ITestRunEventsHandler>()), Times.Never);
        }

        [TestMethod]
        public void StartTestRunShouldInitializeExtensionsIfTestHostIsNotShared()
        {
            TestPluginCache.Instance = null;
            mockTestHostManager.SetupGet(th => th.Shared).Returns(false);
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new[] { "x.dll" });

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

            mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldInitializeExtensionsWithExistingExtensionsOnly()
        {
            TestPluginCache.Instance = null;
            TestPluginCache.Instance.UpdateExtensions(new List<string> { "abc.TestAdapter.dll", "def.TestAdapter.dll", "xyz.TestAdapter.dll" }, false);
            var expectedOutputPaths = new[] { "abc.TestAdapter.dll", "xyz.TestAdapter.dll" };

            mockTestHostManager.SetupGet(th => th.Shared).Returns(false);
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns((IEnumerable<string> sources, IEnumerable<string> extensions) => extensions.Select(extension => Path.GetFileName(extension)));

            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns((string extensionPath) => !extensionPath.Contains("def.TestAdapter.dll"));

            mockFileHelper.Setup(fh => fh.Exists("def.TestAdapter.dll")).Returns(false);
            mockFileHelper.Setup(fh => fh.Exists("xyz.TestAdapter.dll")).Returns(true);

            var mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockRequestSender.Verify(s => s.InitializeExecution(expectedOutputPaths), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldThrowExceptionIfClientConnectionTimeout()
        {
            string runsettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            Assert.ThrowsException<TestPlatformException>(() => testExecutionManager.SetupChannel(new List<string> { "source.dll" }, runsettings));
        }

        [TestMethod]
        public void SetupChannelShouldThrowExceptionIfTestHostExitedBeforeConnectionIsEstablished()
        {
            string runsettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true)).Callback(() => mockTestHostManager.Raise(t => t.HostExited += null, new HostProviderEventArgs("I crashed!")));

            Assert.AreEqual(string.Format(CrossPlatEngineResources.Resources.TestHostExitedWithError, "I crashed!"), Assert.ThrowsException<TestPlatformException>(() => testExecutionManager.SetupChannel(new List<string> { "source.dll" }, runsettings)).Message);
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleTestRunComplete()
        {
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleTestRunComplete(It.Is<TestRunCompleteEventArgs>(t => t.IsAborted == true), null, null, null));
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleRawMessageOfTestRunComplete()
        {
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>())).Returns(MessageType.ExecutionComplete);

            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.ExecutionComplete) ? MessageType.ExecutionComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.ExecutionComplete))), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleRawMessageOfTestMessage()
        {
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>())).Returns(MessageType.ExecutionComplete);

            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.ExecutionComplete) ? MessageType.ExecutionComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.TestMessage))));
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleLogMessageOfError()
        {
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleRawMessageAndHandleLogMessage()
        {
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleRawMessage(It.IsAny<string>()));
            mockTestRunEventsHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()));
        }

        [TestMethod]
        public void StartTestRunForCancelRequestShouldHandleLogMessageWithProperErrorMessage()
        {
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();
            testExecutionManager.Cancel(mockTestRunEventsHandler.Object);

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, "Cancelling the operation as requested."));
        }

        [TestMethod]
        public void StartTestRunForAnExceptionDuringLaunchOfTestShouldHandleLogMessageWithProperErrorMessage()
        {
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Throws(new Exception("DummyException"));

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.Is<string>(str => str.StartsWith("Failed to launch testhost with error: System.Exception: DummyException"))));
        }

        [TestMethod]
        public void StartTestRunShouldInitiateTestRunForSourcesThroughTheServer()
        {
            TestRunCriteriaWithSources testRunCriteriaPassed = null;
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            mockRequestSender.Setup(s => s.StartTestRun(It.IsAny<TestRunCriteriaWithSources>(), testExecutionManager))
                .Callback(
                    (TestRunCriteriaWithSources criteria, ITestRunEventsHandler sink) => testRunCriteriaPassed = criteria);

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

            Assert.IsNotNull(testRunCriteriaPassed);
            CollectionAssert.AreEqual(mockTestRunCriteria.Object.AdapterSourceMap.Keys, testRunCriteriaPassed.AdapterSourceMap.Keys);
            CollectionAssert.AreEqual(mockTestRunCriteria.Object.AdapterSourceMap.Values, testRunCriteriaPassed.AdapterSourceMap.Values);
            Assert.AreEqual(mockTestRunCriteria.Object.FrequencyOfRunStatsChangeEvent, testRunCriteriaPassed.TestExecutionContext.FrequencyOfRunStatsChangeEvent);
            Assert.AreEqual(mockTestRunCriteria.Object.RunStatsChangeEventTimeout, testRunCriteriaPassed.TestExecutionContext.RunStatsChangeEventTimeout);
            Assert.AreEqual(mockTestRunCriteria.Object.TestRunSettings, testRunCriteriaPassed.RunSettings);
        }

        [TestMethod]
        public void StartTestRunShouldInitiateTestRunForTestsThroughTheServer()
        {
            TestRunCriteriaWithTests testRunCriteriaPassed = null;
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            mockRequestSender.Setup(s => s.StartTestRun(It.IsAny<TestRunCriteriaWithTests>(), testExecutionManager))
                .Callback(
                    (TestRunCriteriaWithTests criteria, ITestRunEventsHandler sink) => testRunCriteriaPassed = criteria);
            var runCriteria = new Mock<TestRunCriteria>(
                                            new List<TestCase> { new TestCase("A.C.M", new Uri("executor://dummy"), "source.dll") },
                                            10);

            testExecutionManager.StartTestRun(runCriteria.Object, null);

            Assert.IsNotNull(testRunCriteriaPassed);
            CollectionAssert.AreEqual(runCriteria.Object.Tests.ToList(), testRunCriteriaPassed.Tests.ToList());
            Assert.AreEqual(
                runCriteria.Object.FrequencyOfRunStatsChangeEvent,
                testRunCriteriaPassed.TestExecutionContext.FrequencyOfRunStatsChangeEvent);
            Assert.AreEqual(
                runCriteria.Object.RunStatsChangeEventTimeout,
                testRunCriteriaPassed.TestExecutionContext.RunStatsChangeEventTimeout);
            Assert.AreEqual(
                runCriteria.Object.TestRunSettings,
                testRunCriteriaPassed.RunSettings);
        }

        [TestMethod]
        public void CloseShouldSignalToServerSessionEndIfTestHostWasLaunched()
        {
            string runsettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            testExecutionManager.SetupChannel(new List<string> { "source.dll" }, runsettings);

            testExecutionManager.Close();

            mockRequestSender.Verify(s => s.EndSession(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldNotSendSignalToServerSessionEndIfTestHostWasNotLaunched()
        {
            testExecutionManager.Close();

            mockRequestSender.Verify(s => s.EndSession(), Times.Never);
        }

        [TestMethod]
        public void CloseShouldSignalServerSessionEndEachTime()
        {
            string runsettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            testExecutionManager.SetupChannel(new List<string> { "source.dll" }, runsettings);

            testExecutionManager.Close();
            testExecutionManager.Close();

            mockRequestSender.Verify(s => s.EndSession(), Times.Exactly(2));
        }

        [TestMethod]
        public void CancelShouldNotSendSendTestRunCancelIfCommunicationFails()
        {
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            testExecutionManager.Cancel(It.IsAny<ITestRunEventsHandler>());

            mockRequestSender.Verify(s => s.SendTestRunCancel(), Times.Never);
        }

        [TestMethod]
        public void AbortShouldSendTestRunAbortIfCommunicationSuccessful()
        {
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            testExecutionManager.Abort(It.IsAny<ITestRunEventsHandler>());

            mockRequestSender.Verify(s => s.SendTestRunAbort(), Times.Once);
        }

        [TestMethod]
        public void AbortShouldNotSendTestRunAbortIfCommunicationFails()
        {
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            testExecutionManager.Abort(It.IsAny<ITestRunEventsHandler>());

            mockRequestSender.Verify(s => s.SendTestRunAbort(), Times.Never);
        }

        [TestMethod]
        public void ExecuteTestsCloseTestHostIfRawMessageIfOfTypeExecutionComplete()
        {
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>())).Returns(MessageType.ExecutionComplete);

            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.ExecutionComplete) ? MessageType.ExecutionComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            // Act.
            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            // Verify
            mockTestHostManager.Verify(mthm => mthm.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void ExecuteTestsShouldNotCloseTestHostIfRawMessageIsNotOfTypeExecutionComplete()
        {
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns(() =>
            {
                var message = new Message
                {
                    MessageType = MessageType.ExecutionInitialize
                };

                return message;
            });

            // Act.
            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            // Verify
            mockTestHostManager.Verify(mthm => mthm.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public void ExecutionManagerShouldPassOnTestRunStatsChange()
        {
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();
            var runCriteria = new Mock<TestRunCriteria>(
                new List<TestCase> { new TestCase("A.C.M", new Uri("executor://dummy"), "source.dll") },
                10);
            var testRunChangedArgs = new TestRunChangedEventArgs(null, null, null);

            testExecutionManager = GetProxyExecutionManager();

            var completePayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = null,
                RunAttachments = null,
                TestRunCompleteArgs = null
            };
            var completeMessage = new Message() { MessageType = MessageType.ExecutionComplete, Payload = null };
            SetupChannelMessage(MessageType.StartTestExecutionWithTests, MessageType.TestRunStatsChange, testRunChangedArgs);

            mockTestRunEventsHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback(
                () =>
                {
                    mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunCompletePayload>(completeMessage)).Returns(completePayload);
                    mockDataSerializer.Setup(ds => ds.SerializeMessage(It.IsAny<string>()))
                        .Returns(MessageType.SessionEnd);
                    RaiseMessageReceived(MessageType.ExecutionComplete);
                });

            var waitHandle = new AutoResetEvent(false);
            mockTestRunEventsHandler.Setup(mh => mh.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback(() => waitHandle.Set());

            // Act.
            testExecutionManager.StartTestRun(runCriteria.Object, mockTestRunEventsHandler.Object);
            waitHandle.WaitOne();

            // Verify
            mockTestRunEventsHandler.Verify(mtdeh => mtdeh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void ExecutionManagerShouldPassOnHandleLogMessage()
        {
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns(() =>
            {
                var message = new Message
                {
                    MessageType = MessageType.TestMessage
                };

                return message;
            });

            // Act.
            testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            // Verify
            mockTestRunEventsHandler.Verify(mtdeh => mtdeh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void ExecutionManagerShouldPassOnLaunchProcessWithDebuggerAttached()
        {
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();
            var runCriteria = new Mock<TestRunCriteria>(
                new List<TestCase> { new TestCase("A.C.M", new Uri("executor://dummy"), "source.dll") },
                10);
            var payload = new TestProcessStartInfo();

            testExecutionManager = GetProxyExecutionManager();

            var completePayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = null,
                RunAttachments = null,
                TestRunCompleteArgs = null
            };
            var completeMessage = new Message() { MessageType = MessageType.ExecutionComplete, Payload = null };
            SetupChannelMessage(MessageType.StartTestExecutionWithTests,
                MessageType.LaunchAdapterProcessWithDebuggerAttached, payload);

            mockTestRunEventsHandler.Setup(mh => mh.LaunchProcessWithDebuggerAttached(It.IsAny<TestProcessStartInfo>())).Callback(
                () =>
                {
                    mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunCompletePayload>(completeMessage)).Returns(completePayload);
                    mockDataSerializer.Setup(ds => ds.SerializeMessage(It.IsAny<string>()))
                        .Returns(MessageType.SessionEnd);
                    RaiseMessageReceived(MessageType.ExecutionComplete);
                });

            var waitHandle = new AutoResetEvent(false);
            mockTestRunEventsHandler.Setup(mh => mh.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback(() => waitHandle.Set());

            testExecutionManager.StartTestRun(runCriteria.Object, mockTestRunEventsHandler.Object);

            waitHandle.WaitOne();

            // Verify
            mockTestRunEventsHandler.Verify(mtdeh => mtdeh.LaunchProcessWithDebuggerAttached(It.IsAny<TestProcessStartInfo>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldAttemptToTakeProxyFromPoolIfProxyIsNull()
        {
            var testSessionInfo = new TestSessionInfo();

            Func<string, ProxyExecutionManager, ProxyOperationManager>
            proxyOperationManagerCreator = (
                string source,
                ProxyExecutionManager proxyExecutionManager) =>
            {
                var proxyOperationManager = TestSessionPool.Instance.TryTakeProxy(
                    testSessionInfo,
                    source,
                    string.Empty);

                return proxyOperationManager;
            };

            var testExecutionManager = new ProxyExecutionManager(
                testSessionInfo,
                proxyOperationManagerCreator,
                false);

            var mockTestSessionPool = new Mock<TestSessionPool>();
            TestSessionPool.Instance = mockTestSessionPool.Object;

            try
            {
                var mockProxyOperationManager = new Mock<ProxyOperationManager>(
                    mockRequestData.Object,
                    mockRequestSender.Object,
                    mockTestHostManager.Object);
                mockTestSessionPool.Setup(
                    tsp => tsp.TryTakeProxy(
                        testSessionInfo,
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                    .Returns(mockProxyOperationManager.Object);

                testExecutionManager.Initialize(true);
                testExecutionManager.StartTestRun(
                    mockTestRunCriteria.Object,
                    new Mock<ITestRunEventsHandler>().Object);

                mockTestSessionPool.Verify(
                    tsp => tsp.TryTakeProxy(
                        testSessionInfo,
                        It.IsAny<string>(),
                        It.IsAny<string>()),
                    Times.Once);
            }
            finally
            {
                TestSessionPool.Instance = null;
            }
        }

        private void SignalEvent(ManualResetEvent manualResetEvent)
        {
            // Wait for the 100 ms.
            Task.Delay(200).Wait();

            manualResetEvent.Set();
        }

        private void InvokeAndVerifyStartTestRun(bool skipDefaultAdapters)
        {
            TestPluginCache.Instance = null;
            TestPluginCache.Instance.DefaultExtensionPaths = new List<string> { "default1.dll", "default2.dll" };
            TestPluginCache.Instance.UpdateExtensions(new List<string> { "filterTestAdapter.dll" }, false);
            TestPluginCache.Instance.UpdateExtensions(new List<string> { "unfilter.dll" }, true);

            try
            {
                mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
                mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns((IEnumerable<string> sources, IEnumerable<string> extensions) => extensions);
                mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                var expectedResult = TestPluginCache.Instance.GetExtensionPaths(TestPlatformConstants.TestAdapterEndsWithPattern, skipDefaultAdapters);

                testExecutionManager.Initialize(skipDefaultAdapters);
                testExecutionManager.StartTestRun(mockTestRunCriteria.Object, null);

                mockRequestSender.Verify(s => s.InitializeExecution(expectedResult), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        //private void SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize()
        //{
        //    var connectionInfo = new TestHostConnectionInfo
        //    {
        //        Endpoint = IPAddress.Loopback + ":0",
        //        Role = ConnectionRole.Client,
        //        Transport = Transport.Sockets
        //    };
        //    this.mockCommunicationEndpoint = new Mock<ICommunicationEndPoint>();
        //    this.mockDataSerializer = new Mock<IDataSerializer>();
        //    this.testRequestSender = new TestRequestSender(this.mockCommunicationEndpoint.Object, connectionInfo, this.mockDataSerializer.Object, this.protocolConfig, CLIENTPROCESSEXITWAIT);
        //    this.mockCommunicationEndpoint.Setup(mc => mc.Start(connectionInfo.Endpoint)).Returns(connectionInfo.Endpoint).Callback(() =>
        //    {
        //        this.mockCommunicationEndpoint.Raise(
        //            s => s.Connected += null,
        //            this.mockCommunicationEndpoint.Object,
        //            new ConnectedEventArgs(this.mockChannel.Object));
        //    });
        //    this.SetupChannelMessage(MessageType.VersionCheck, MessageType.VersionCheck, this.protocolConfig.Version);

        //    this.testRequestSender.InitializeCommunication();

        //    this.testExecutionManager = new ProxyExecutionManager(this.mockRequestData.Object, this.testRequestSender, this.mockTestHostManager.Object, this.mockDataSerializer.Object, this.clientConnectionTimeout);
        //}

        //private void SetupChannelMessage<TPayload>(string messageType, string returnMessageType, TPayload returnPayload)
        //{
        //    this.mockChannel.Setup(mc => mc.Send(It.Is<string>(s => s.Contains(messageType))))
        //                    .Callback(() => this.mockChannel.Raise(c => c.MessageReceived += null, this.mockChannel.Object, new MessageReceivedEventArgs { Data = messageType }));

        //    this.mockDataSerializer.Setup(ds => ds.SerializePayload(It.Is<string>(s => s.Equals(messageType)), It.IsAny<object>())).Returns(messageType);
        //    this.mockDataSerializer.Setup(ds => ds.SerializePayload(It.Is<string>(s => s.Equals(messageType)), It.IsAny<object>(), It.IsAny<int>())).Returns(messageType);
        //    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.Is<string>(s => s.Equals(messageType)))).Returns(new Message { MessageType = returnMessageType });
        //    this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TPayload>(It.Is<Message>(m => m.MessageType.Equals(messageType)))).Returns(returnPayload);
        //}
    }
}
