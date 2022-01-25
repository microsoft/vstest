// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ProxyDiscoveryManagerTests : ProxyBaseManagerTests
    {
        //private const int CLIENTPROCESSEXITWAIT = 10 * 1000;

        private readonly DiscoveryCriteria discoveryCriteria;

        private ProxyDiscoveryManager testDiscoveryManager;

        private readonly Mock<ITestRequestSender> mockRequestSender;

        //private Mock<IDataSerializer> mockDataSerializer;

        private readonly Mock<IRequestData> mockRequestData;

        private readonly Mock<IMetricsCollection> mockMetricsCollection;
        private readonly Mock<IFileHelper> mockFileHelper;


        public ProxyDiscoveryManagerTests()
        {
            mockRequestSender = new Mock<ITestRequestSender>();
            mockRequestData = new Mock<IRequestData>();
            mockMetricsCollection = new Mock<IMetricsCollection>();
            mockFileHelper = new Mock<IFileHelper>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);
            testDiscoveryManager = new ProxyDiscoveryManager(
                                            mockRequestData.Object,
                                            mockRequestSender.Object,
                                            mockTestHostManager.Object,
                                            mockDataSerializer.Object,
                                            mockFileHelper.Object);
            discoveryCriteria = new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotInitializeExtensionsOnNoExtensions()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            testDiscoveryManager.DiscoverTests(discoveryCriteria, null);

            mockRequestSender.Verify(s => s.InitializeDiscovery(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotInitializeExtensionsOnCommunicationFailure()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new();

            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventHandler.Object);

            mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldAllowRuntimeProviderToUpdateAdapterSource()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            mockTestHostManager.Setup(hm => hm.GetTestSources(discoveryCriteria.Sources)).Returns(discoveryCriteria.Sources);
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new();

            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventHandler.Object);

            mockTestHostManager.Verify(hm => hm.GetTestSources(discoveryCriteria.Sources), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldUpdateTestSourcesIfSourceDiffersFromTestHostManagerSource()
        {
            var actualSources = new List<string> { "actualSource.dll" };
            var inputSource = new List<string> { "inputPackage.appxrecipe" };

            var localDiscoveryCriteria = new DiscoveryCriteria(inputSource, 1, string.Empty);

            mockTestHostManager.Setup(hm => hm.GetTestSources(localDiscoveryCriteria.Sources)).Returns(actualSources);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string>());
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new();

            testDiscoveryManager.DiscoverTests(localDiscoveryCriteria, mockTestDiscoveryEventHandler.Object);

            Assert.IsNotNull(localDiscoveryCriteria.Package);
            // AdapterSourceMap should contain updated testSources.
            Assert.AreEqual(actualSources.FirstOrDefault(), localDiscoveryCriteria.AdapterSourceMap.FirstOrDefault().Value.FirstOrDefault());
            Assert.AreEqual(inputSource.FirstOrDefault(), localDiscoveryCriteria.Package);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotUpdateTestSourcesIfSourceDoNotDifferFromTestHostManagerSource()
        {
            var actualSources = new List<string> { "actualSource.dll" };
            var inputSource = new List<string> { "actualSource.dll" };

            var localDiscoveryCriteria = new DiscoveryCriteria(inputSource, 1, string.Empty);

            mockTestHostManager.Setup(hm => hm.GetTestSources(localDiscoveryCriteria.Sources)).Returns(actualSources);
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string>());
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new();

            testDiscoveryManager.DiscoverTests(localDiscoveryCriteria, mockTestDiscoveryEventHandler.Object);

            Assert.IsNull(localDiscoveryCriteria.Package);
            // AdapterSourceMap should contain updated testSources.
            Assert.AreEqual(actualSources.FirstOrDefault(), localDiscoveryCriteria.AdapterSourceMap.FirstOrDefault().Value.FirstOrDefault());
        }

        [TestMethod]
        public void DiscoverTestsShouldNotSendDiscoveryRequestIfCommunicationFails()
        {
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback(
                    () => mockTestHostManager.Raise(thm => thm.HostLaunched += null, new HostProviderEventArgs(string.Empty)))
                .Returns(Task.FromResult(false));

            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new();

            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventHandler.Object);

            mockRequestSender.Verify(s => s.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldInitializeExtensionsIfPresent()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            try
            {
                var extensions = new[] { "c:\\e1.dll", "c:\\e2.dll" };

                // Setup Mocks.
                TestPluginCacheHelper.SetupMockAdditionalPathExtensions(extensions);
                mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
                mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new[] { "c:\\e1.dll", "c:\\e2.dll" });

                testDiscoveryManager.DiscoverTests(discoveryCriteria, null);

                // Also verify that we have waited for client connection.
                mockRequestSender.Verify(s => s.InitializeDiscovery(extensions), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void DiscoverTestsShouldInitializeExtensionsWithExistingExtensionsOnly()
        {
            var inputExtensions = new[] { "abc.TestAdapter.dll", "def.TestAdapter.dll", "xyz.TestAdapter.dll" };
            var expectedOutputPaths = new[] { "abc.TestAdapter.dll", "xyz.TestAdapter.dll" };

            TestPluginCacheHelper.SetupMockAdditionalPathExtensions(inputExtensions);
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
            mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns((IEnumerable<string> sources, IEnumerable<string> extensions) => extensions.Select(extension => Path.GetFileName(extension)));

            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns((string extensionPath) => !extensionPath.Contains("def.TestAdapter.dll"));

            mockFileHelper.Setup(fh => fh.Exists("def.TestAdapter.dll")).Returns(false);
            mockFileHelper.Setup(fh => fh.Exists("xyz.TestAdapter.dll")).Returns(true);

            var mockTestDiscoveryEventHandler = new Mock<ITestDiscoveryEventsHandler2>();
            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventHandler.Object);

            mockRequestSender.Verify(s => s.InitializeDiscovery(expectedOutputPaths), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldQueryTestHostManagerForExtensions()
        {
            TestPluginCache.Instance = null;
            try
            {
                TestPluginCacheHelper.SetupMockAdditionalPathExtensions(new[] { "c:\\e1.dll" });
                mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
                mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new[] { "he1.dll", "c:\\e1.dll" });

                testDiscoveryManager.DiscoverTests(discoveryCriteria, null);

                mockRequestSender.Verify(s => s.InitializeDiscovery(new[] { "he1.dll", "c:\\e1.dll" }), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void DiscoverTestsShouldPassAdapterToTestHostManagerFromTestPluginCacheExtensions()
        {
            // We are updating extension with test adapter only to make it easy to test.
            // In product code it filter out test adapter from extension
            TestPluginCache.Instance.UpdateExtensions(new List<string> { "abc.TestAdapter.dll", "xyz.TestAdapter.dll" }, false);
            try
            {
                mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
                mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                var expectedResult = TestPluginCache.Instance.GetExtensionPaths(string.Empty);

                testDiscoveryManager.DiscoverTests(discoveryCriteria, null);

                mockTestHostManager.Verify(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), expectedResult), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void DiscoverTestsShouldNotInitializeDefaultAdaptersIfSkipDefaultAdaptersIsTrue()
        {
            InvokeAndVerifyDiscoverTests(true);
        }

        [TestMethod]
        public void DiscoverTestsShouldInitializeDefaultAdaptersIfSkipDefaultAdaptersIsFalse()
        {
            InvokeAndVerifyDiscoverTests(false);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotIntializeTestHost()
        {
            // Setup mocks.
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            // Act.
            testDiscoveryManager.DiscoverTests(discoveryCriteria, null);

            mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
            mockTestHostManager.Verify(thl => thl.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCatchExceptionAndCallHandleDiscoveryComplete()
        {
            // Setup mocks.
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new();
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            // Act.
            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), It.IsAny<IEnumerable<TestCase>>()));
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleRawMessage(It.IsAny<string>()));
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()));
        }

        [TestMethod]
        public void DiscoverTestsShouldCatchExceptionAndCallHandleRawMessageOfDiscoveryComplete()
        {
            // Setup mocks.
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new();
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.DiscoveryComplete, It.IsAny<DiscoveryCompletePayload>())).Returns(MessageType.DiscoveryComplete);

            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.DiscoveryComplete) ? MessageType.DiscoveryComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            // Act.
            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.DiscoveryComplete))), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCatchExceptionAndCallHandleRawMessageOfTestMessage()
        {
            // Setup mocks.
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new();
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.DiscoveryComplete, It.IsAny<DiscoveryCompletePayload>())).Returns(MessageType.DiscoveryComplete);

            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.DiscoveryComplete) ? MessageType.DiscoveryComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            // Act.
            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.TestMessage))), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCatchExceptionAndCallHandleLogMessageOfError()
        {
            // Setup mocks.
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new();
            mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            // Act.
            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldInitiateServerDiscoveryLoop()
        {
            // Setup mocks.
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            // Act.
            testDiscoveryManager.DiscoverTests(discoveryCriteria, null);

            // Assert.
            mockRequestSender.Verify(s => s.DiscoverTests(It.IsAny<DiscoveryCriteria>(), testDiscoveryManager), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsCloseTestHostIfRawMessageIsOfTypeDiscoveryComplete()
        {
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new();

            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.DiscoveryComplete, It.IsAny<DiscoveryCompletePayload>())).Returns(MessageType.DiscoveryComplete);

            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.DiscoveryComplete) ? MessageType.DiscoveryComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            // Act.
            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestHostManager.Verify(mthm => mthm.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotCloseTestHostIfRawMessageIsNotOfTypeDiscoveryComplete()
        {
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new();

            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns(() =>
            {
                var message = new Message
                {
                    MessageType = MessageType.DiscoveryInitialize
                };

                return message;
            });

            // Act.
            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestHostManager.Verify(mthm => mthm.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public void DiscoveryManagerShouldPassOnHandleDiscoveredTests()
        {
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new();
            var testCases = new List<TestCase>() { new TestCase("x.y.z", new Uri("x://y"), "x.dll") };

            testDiscoveryManager = GetProxyDiscoveryManager();
            SetupChannelMessage(MessageType.StartDiscovery, MessageType.TestCasesFound, testCases);

            var completePayload = new DiscoveryCompletePayload()
            {
                IsAborted = false,
                LastDiscoveredTests = null,
                TotalTests = 1
            };
            var completeMessage = new Message() { MessageType = MessageType.DiscoveryComplete, Payload = null };
            mockTestDiscoveryEventsHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback(
                () =>
                {
                    mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    mockDataSerializer.Setup(ds => ds.DeserializePayload<DiscoveryCompletePayload>(completeMessage)).Returns(completePayload);
                });

            // Act.
            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(mtdeh => mtdeh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void DiscoveryManagerShouldPassOnHandleLogMessage()
        {
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new();

            mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns(() =>
            {
                var message = new Message
                {
                    MessageType = MessageType.TestMessage
                };

                return message;
            });

            // Act.
            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(mtdeh => mtdeh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void AbortShouldSendTestDiscoveryCancelIfCommunicationSuccessful()
        {
            var mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            Mock<ITestRunEventsHandler> mockTestRunEventsHandler = new();

            testDiscoveryManager.DiscoverTests(discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            testDiscoveryManager.Abort();

            mockRequestSender.Verify(s => s.EndSession(), Times.Once);
        }

        [TestMethod]
        public void StartTestRunShouldAttemptToTakeProxyFromPoolIfProxyIsNull()
        {
            var testSessionInfo = new TestSessionInfo();

            Func<string, ProxyDiscoveryManager, ProxyOperationManager>
            proxyOperationManagerCreator = (
                string source,
                ProxyDiscoveryManager proxyDiscoveryManager) =>
            {
                var proxyOperationManager = TestSessionPool.Instance.TryTakeProxy(
                    testSessionInfo,
                    source,
                    discoveryCriteria.RunSettings);

                return proxyOperationManager;
            };

            var testDiscoveryManager = new ProxyDiscoveryManager(
                testSessionInfo,
                proxyOperationManagerCreator);

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

                testDiscoveryManager.Initialize(true);
                testDiscoveryManager.DiscoverTests(
                    discoveryCriteria,
                    new Mock<ITestDiscoveryEventsHandler2>().Object);

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

        private void InvokeAndVerifyDiscoverTests(bool skipDefaultAdapters)
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

                testDiscoveryManager.Initialize(skipDefaultAdapters);
                testDiscoveryManager.DiscoverTests(discoveryCriteria, null);

                mockRequestSender.Verify(s => s.InitializeDiscovery(expectedResult), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        //private void SetupAndInitializeTestRequestSender()
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
        //        {
        //            this.mockCommunicationEndpoint.Raise(
        //                s => s.Connected += null,
        //                this.mockCommunicationEndpoint.Object,
        //                new ConnectedEventArgs(this.mockChannel.Object));
        //        });
        //    this.SetupChannelMessage(MessageType.VersionCheck, MessageType.VersionCheck, this.protocolConfig.Version);
        //    this.testRequestSender.InitializeCommunication();

        //    this.testDiscoveryManager = new ProxyDiscoveryManager(
        //                                    this.mockRequestData.Object,
        //                                    this.testRequestSender,
        //                                    this.mockTestHostManager.Object,
        //                                    this.mockDataSerializer.Object,
        //                                    this.testableClientConnectionTimeout);
        //}

        //private void SetupChannelMessage<TPayload>(string messageType, string returnMessageType, TPayload returnPayload)
        //{
        //    this.mockChannel.Setup(mc => mc.Send(It.Is<string>(s => s.Contains(messageType))))
        //                    .Callback(() => this.mockChannel.Raise(c => c.MessageReceived += null,  this.mockChannel.Object, new MessageReceivedEventArgs { Data = messageType }));

        //    this.mockDataSerializer.Setup(ds => ds.SerializePayload(It.Is<string>(s => s.Equals(messageType)), It.IsAny<object>())).Returns(messageType);
        //    this.mockDataSerializer.Setup(ds => ds.SerializePayload(It.Is<string>(s => s.Equals(messageType)), It.IsAny<object>(), It.IsAny<int>())).Returns(messageType);
        //    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.Is<string>(s => s.Equals(messageType))))
        //        .Returns(new Message { MessageType = returnMessageType });
        //        this.mockDataSerializer.Setup(ds => ds.DeserializePayload<TPayload>(It.Is<Message>(m => m.MessageType.Equals(messageType))))
        //        .Returns(returnPayload);
        //}
    }
}
