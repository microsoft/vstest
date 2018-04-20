// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
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

    [TestClass]
    public class ProxyExecutionManagerTests : ProxyBaseManagerTests
    {
        private readonly Mock<ITestRequestSender> mockRequestSender;

        private readonly Mock<TestRunCriteria> mockTestRunCriteria;

        private readonly Mock<IRequestData> mockRequestData;

        private Mock<IMetricsCollection> mockMetricsCollection;

        private ProxyExecutionManager testExecutionManager;

        //private Mock<IDataSerializer> mockDataSerializer;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int clientConnectionTimeout = 400;
        public ProxyExecutionManagerTests()
        {
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockTestRunCriteria = new Mock<TestRunCriteria>(new List<string> { "source.dll" }, 10);
            //this.mockDataSerializer = new Mock<IDataSerializer>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);

            this.testExecutionManager = new ProxyExecutionManager(this.mockRequestData.Object, this.mockRequestSender.Object, this.mockTestHostManager.Object, this.mockDataSerializer.Object, this.clientConnectionTimeout);

            //this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(null)).Returns(new Message());
            //this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(string.Empty)).Returns(new Message());
        }

        [TestMethod]
        public void StartTestRunShouldNotInitializeExtensionsOnNoExtensions()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

            this.mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void StartTestRunShouldAllowRuntimeProviderToUpdateAdapterSource()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.mockTestHostManager.Setup(hm => hm.GetTestSources(this.mockTestRunCriteria.Object.Sources)).Returns(this.mockTestRunCriteria.Object.Sources);
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            this.mockTestHostManager.Verify(hm => hm.GetTestSources(this.mockTestRunCriteria.Object.Sources), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldUpdateTestCaseSourceIfTestCaseSourceDiffersFromTestHostManagerSource()
        {
            var actualSources = new List<string> { "actualSource.dll" };
            var inputSource = new List<string> { "inputPackage.appxrecipe" };

            var testRunCriteria = new TestRunCriteria(
                    new List<TestCase> { new TestCase("A.C.M", new Uri("excutor://dummy"), inputSource.FirstOrDefault()) },
                    frequencyOfRunStatsChangeEvent: 10);

            this.mockTestHostManager.Setup(hm => hm.GetTestSources(inputSource)).Returns(actualSources);
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string>());
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(testRunCriteria, mockTestRunEventsHandler.Object);

            this.mockTestHostManager.Verify(hm => hm.GetTestSources(inputSource), Times.Once);
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

            this.mockTestHostManager.Setup(hm => hm.GetTestSources(inputSource)).Returns(actualSources);
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string>());
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(testRunCriteria, mockTestRunEventsHandler.Object);

            this.mockTestHostManager.Verify(hm => hm.GetTestSources(inputSource), Times.Once);
            Assert.AreEqual(actualSources.FirstOrDefault(), testRunCriteria.Tests.FirstOrDefault().Source);
        }

        [TestMethod]
        public void StartTestRunShouldNotInitializeExtensionsOnCommunicationFailure()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            this.mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void StartTestRunShouldInitializeExtensionsIfPresent()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            try
            {
                var extensions = new List<string>() { "C:\\foo.dll" };
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                this.mockTestHostManager.Setup(x => x.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                    .Returns(extensions);

                this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

                // Also verify that we have waited for client connection.
                this.mockRequestSender.Verify(s => s.InitializeExecution(extensions), Times.Once);
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
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new[] { "he1.dll", "c:\\e1.dll" });

                this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

                this.mockRequestSender.Verify(s => s.InitializeExecution(new[] { "he1.dll", "c:\\e1.dll" }), Times.Once);
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
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                var expectedResult = TestPluginCache.Instance.GetExtensionPaths(string.Empty);

                this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

                this.mockTestHostManager.Verify(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), expectedResult), Times.Once);
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
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
            this.mockTestHostManager.Verify(thl => thl.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldNotSendStartTestRunRequestIfCommunicationFails()
        {
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback(
                    () =>
                        {
                            this.mockTestHostManager.Raise(thm => thm.HostLaunched += null, new HostProviderEventArgs(string.Empty));
                        })
                .Returns(Task.FromResult(false));

            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            this.mockRequestSender.Verify(s => s.StartTestRun(It.IsAny<TestRunCriteriaWithSources>(), It.IsAny<ITestRunEventsHandler>()), Times.Never);
        }

        [TestMethod]
        public void StartTestRunShouldInitializeExtensionsIfTestHostIsNotShared()
        {
            TestPluginCache.Instance = null;
            this.mockTestHostManager.SetupGet(th => th.Shared).Returns(false);
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new[] { "x.dll" });

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

            this.mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [TestMethod]
        public void SetupChannelShouldThrowExceptionIfClientConnectionTimeout()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            Assert.ThrowsException<TestPlatformException>(() => this.testExecutionManager.SetupChannel(new List<string> { "source.dll" }));
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleTestRunComplete()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleTestRunComplete(It.Is<TestRunCompleteEventArgs>(t => t.IsAborted == true), null, null, null));
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleRawMessageOfTestRunComplete()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>())).Returns(MessageType.ExecutionComplete);

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.ExecutionComplete) ? MessageType.ExecutionComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.ExecutionComplete))), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleRawMessageOfTestMessage()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>())).Returns(MessageType.ExecutionComplete);

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.ExecutionComplete) ? MessageType.ExecutionComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.TestMessage))));
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleLogMessageOfError()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldCatchExceptionAndCallHandleRawMessageAndHandleLogMessage()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            mockTestRunEventsHandler.Verify(s => s.HandleRawMessage(It.IsAny<string>()));
            mockTestRunEventsHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()));
        }

        [TestMethod]
        public void StartTestRunShouldInitiateTestRunForSourcesThroughTheServer()
        {
            TestRunCriteriaWithSources testRunCriteriaPassed = null;
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            this.mockRequestSender.Setup(s => s.StartTestRun(It.IsAny<TestRunCriteriaWithSources>(), this.testExecutionManager))
                .Callback(
                    (TestRunCriteriaWithSources criteria, ITestRunEventsHandler sink) =>
                        {
                            testRunCriteriaPassed = criteria;
                        });

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

            Assert.IsNotNull(testRunCriteriaPassed);
            CollectionAssert.AreEqual(this.mockTestRunCriteria.Object.AdapterSourceMap.Keys, testRunCriteriaPassed.AdapterSourceMap.Keys);
            CollectionAssert.AreEqual(this.mockTestRunCriteria.Object.AdapterSourceMap.Values, testRunCriteriaPassed.AdapterSourceMap.Values);
            Assert.AreEqual(this.mockTestRunCriteria.Object.FrequencyOfRunStatsChangeEvent, testRunCriteriaPassed.TestExecutionContext.FrequencyOfRunStatsChangeEvent);
            Assert.AreEqual(this.mockTestRunCriteria.Object.RunStatsChangeEventTimeout, testRunCriteriaPassed.TestExecutionContext.RunStatsChangeEventTimeout);
            Assert.AreEqual(this.mockTestRunCriteria.Object.TestRunSettings, testRunCriteriaPassed.RunSettings);
        }

        [TestMethod]
        public void StartTestRunShouldInitiateTestRunForTestsThroughTheServer()
        {
            TestRunCriteriaWithTests testRunCriteriaPassed = null;
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            this.mockRequestSender.Setup(s => s.StartTestRun(It.IsAny<TestRunCriteriaWithTests>(), this.testExecutionManager))
                .Callback(
                    (TestRunCriteriaWithTests criteria, ITestRunEventsHandler sink) =>
                    {
                        testRunCriteriaPassed = criteria;
                    });
            var runCriteria = new Mock<TestRunCriteria>(
                                            new List<TestCase> { new TestCase("A.C.M", new System.Uri("executor://dummy"), "source.dll") },
                                            10);

            this.testExecutionManager.StartTestRun(runCriteria.Object, null);

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
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            this.testExecutionManager.SetupChannel(new List<string> { "source.dll" });

            this.testExecutionManager.Close();

            this.mockRequestSender.Verify(s => s.EndSession(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldNotSendSignalToServerSessionEndIfTestHostWasNotLaunched()
        {
            this.testExecutionManager.Close();

            this.mockRequestSender.Verify(s => s.EndSession(), Times.Never);
        }

        [TestMethod]
        public void CloseShouldSignalServerSessionEndEachTime()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            this.testExecutionManager.SetupChannel(new List<string> { "source.dll" });

            this.testExecutionManager.Close();
            this.testExecutionManager.Close();

            this.mockRequestSender.Verify(s => s.EndSession(), Times.Exactly(2));
        }

        [TestMethod]
        public void CancelShouldNotSendSendTestRunCancelIfCommunicationFails()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            this.testExecutionManager.Cancel(It.IsAny<ITestRunEventsHandler>());

            this.mockRequestSender.Verify(s => s.SendTestRunCancel(), Times.Never);
        }

        [TestMethod]
        public void AbortShouldSendTestRunAbortIfCommunicationSuccessful()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            this.testExecutionManager.Abort(It.IsAny<ITestRunEventsHandler>());

            this.mockRequestSender.Verify(s => s.SendTestRunAbort(), Times.Once);
        }

        [TestMethod]
        public void AbortShouldNotSendTestRunAbortIfCommunicationFails()
        {
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            this.testExecutionManager.Abort(It.IsAny<ITestRunEventsHandler>());

            this.mockRequestSender.Verify(s => s.SendTestRunAbort(), Times.Never);
        }

        [TestMethod]
        public void ExecuteTestsCloseTestHostIfRawMessageIfOfTypeExecutionComplete()
        {
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();

            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.ExecutionComplete, It.IsAny<TestRunCompletePayload>())).Returns(MessageType.ExecutionComplete);

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.ExecutionComplete) ? MessageType.ExecutionComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            // Act.
            this.testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            // Verify
            this.mockTestHostManager.Verify(mthm => mthm.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void ExecuteTestsShouldNotCloseTestHostIfRawMessageIsNotOfTypeExecutionComplete()
        {
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns(() =>
            {
                var message = new Message
                {
                    MessageType = MessageType.ExecutionInitialize
                };

                return message;
            });

            // Act.
            this.testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            // Verify
            this.mockTestHostManager.Verify(mthm => mthm.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public void ExecutionManagerShouldPassOnTestRunStatsChange()
        {
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var runCriteria = new Mock<TestRunCriteria>(
                new List<TestCase> { new TestCase("A.C.M", new System.Uri("executor://dummy"), "source.dll") },
                10);
            var testRunChangedArgs = new TestRunChangedEventArgs(null, null, null);

            this.testExecutionManager = this.GetProxyExecutionManager();

            var completePayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = null,
                RunAttachments = null,
                TestRunCompleteArgs = null
            };
            var completeMessage = new Message() { MessageType = MessageType.ExecutionComplete, Payload = null };
            this.SetupChannelMessage(MessageType.StartTestExecutionWithTests, MessageType.TestRunStatsChange, testRunChangedArgs);

            mockTestRunEventsHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback(
                () =>
                {
                    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunCompletePayload>(completeMessage)).Returns(completePayload);
                    this.mockDataSerializer.Setup(ds => ds.SerializeMessage(It.IsAny<string>()))
                        .Returns(MessageType.SessionEnd);
                    this.RaiseMessageReceived(MessageType.ExecutionComplete);
                });

            var waitHandle = new AutoResetEvent(false);
            mockTestRunEventsHandler.Setup(mh => mh.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback(() => waitHandle.Set());

            // Act.
            this.testExecutionManager.StartTestRun(runCriteria.Object, mockTestRunEventsHandler.Object);
            waitHandle.WaitOne();

            // Verify
            mockTestRunEventsHandler.Verify(mtdeh => mtdeh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void ExecutionManagerShouldPassOnHandleLogMessage()
        {
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns(() =>
            {
                var message = new Message
                {
                    MessageType = MessageType.TestMessage
                };

                return message;
            });

            // Act.
            this.testExecutionManager.StartTestRun(mockTestRunCriteria.Object, mockTestRunEventsHandler.Object);

            // Verify
            mockTestRunEventsHandler.Verify(mtdeh => mtdeh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void ExecutionManagerShouldPassOnLaunchProcessWithDebuggerAttached()
        {
            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new Mock<ITestRunEventsHandler>();
            var runCriteria = new Mock<TestRunCriteria>(
                new List<TestCase> { new TestCase("A.C.M", new System.Uri("executor://dummy"), "source.dll") },
                10);
            var payload = new TestProcessStartInfo();

            this.testExecutionManager = this.GetProxyExecutionManager();

            var completePayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = null,
                RunAttachments = null,
                TestRunCompleteArgs = null
            };
            var completeMessage = new Message() { MessageType = MessageType.ExecutionComplete, Payload = null };
            this.SetupChannelMessage(MessageType.StartTestExecutionWithTests,
                MessageType.LaunchAdapterProcessWithDebuggerAttached, payload);
            mockTestRunEventsHandler.Setup(mh => mh.LaunchProcessWithDebuggerAttached(It.IsAny<TestProcessStartInfo>())).Callback(
                () =>
                {
                    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TestRunCompletePayload>(completeMessage)).Returns(completePayload);
                    this.mockDataSerializer.Setup(ds => ds.SerializeMessage(It.IsAny<string>()))
                        .Returns(MessageType.SessionEnd);
                    this.RaiseMessageReceived(MessageType.ExecutionComplete);
                });

            var waitHandle = new AutoResetEvent(false);
            mockTestRunEventsHandler.Setup(mh => mh.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>())).Callback(() => waitHandle.Set());

            this.testExecutionManager.StartTestRun(runCriteria.Object, mockTestRunEventsHandler.Object);

            waitHandle.WaitOne();

            // Verify
            mockTestRunEventsHandler.Verify(mtdeh => mtdeh.LaunchProcessWithDebuggerAttached(It.IsAny<TestProcessStartInfo>()), Times.Once);
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
                this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns((IEnumerable<string> sources, IEnumerable<string> extensions) => extensions);
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                var expectedResult = TestPluginCache.Instance.GetExtensionPaths(TestPlatformConstants.TestAdapterEndsWithPattern, skipDefaultAdapters);

                this.testExecutionManager.Initialize(skipDefaultAdapters);
                this.testExecutionManager.StartTestRun(this.mockTestRunCriteria.Object, null);

                this.mockRequestSender.Verify(s => s.InitializeExecution(expectedResult), Times.Once);
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
