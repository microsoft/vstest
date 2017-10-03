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
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.Common.UnitTests.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

    [TestClass]
    public class ProxyDiscoveryManagerTests
    {
        private readonly DiscoveryCriteria discoveryCriteria;

        private ProxyDiscoveryManager testDiscoveryManager;

        private Mock<ITestRuntimeProvider> mockTestHostManager;

        private Mock<ITestRequestSender> mockRequestSender;

        private Mock<IDataSerializer> mockDataSerializer;

        private Mock<IRequestData> mockRequestData;

        private Mock<IMetricsCollection> mockMetricsCollection;

        private ITestRequestSender testRequestSender;
        private Mock<ICommunicationManager> mockCommunicationManager;

        ProtocolConfig protocolConfig = new ProtocolConfig { Version = 2 };

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int testableClientConnectionTimeout = 400;

        public ProxyDiscoveryManagerTests()
        {
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(null)).Returns(new Message());
            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(string.Empty)).Returns(new Message());

            this.testDiscoveryManager = new ProxyDiscoveryManager(
                                            this.mockRequestData.Object, 
                                            this.mockRequestSender.Object,
                                            this.mockTestHostManager.Object,
                                            this.mockDataSerializer.Object,
                                            this.testableClientConnectionTimeout);
            this.discoveryCriteria = new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty);

            // Default setup test host manager as shared (desktop)
            this.mockTestHostManager.SetupGet(th => th.Shared).Returns(true);
            this.mockTestHostManager.Setup(
                    m => m.GetTestHostProcessStartInfo(
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<TestRunnerConnectionInfo>()))
                .Returns(new TestProcessStartInfo());
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback(
                    () =>
                        {
                            this.mockTestHostManager.Raise(thm => thm.HostLaunched += null, new HostProviderEventArgs(string.Empty));
                        })
                .Returns(Task.FromResult(true));
        }

        [TestMethod]
        public void DiscoverTestsShouldNotInitializeExtensionsOnNoExtensions()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

            this.mockRequestSender.Verify(s => s.InitializeDiscovery(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotInitializeExtensionsOnCommunicationFailure()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(false);

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventHandler.Object);

            this.mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldAllowRuntimeProviderToUpdateAdapterSource()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.mockTestHostManager.Setup(hm => hm.GetTestSources(this.discoveryCriteria.Sources)).Returns(this.discoveryCriteria.Sources);
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventHandler.Object);

            this.mockTestHostManager.Verify(hm => hm.GetTestSources(this.discoveryCriteria.Sources), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldUpdateTestSourcesIfSourceDiffersFromTestHostManagerSource()
        {
            var actualSources = new List<string> { "actualSource.dll" };
            var inputSource = new List<string> { "inputPackage.appxrecipe" };

            var localDiscoveryCriteria = new DiscoveryCriteria(inputSource, 1, string.Empty);

            this.mockTestHostManager.Setup(hm => hm.GetTestSources(localDiscoveryCriteria.Sources)).Returns(actualSources);
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string>());
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.testDiscoveryManager.DiscoverTests(localDiscoveryCriteria, mockTestDiscoveryEventHandler.Object);

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

            this.mockTestHostManager.Setup(hm => hm.GetTestSources(localDiscoveryCriteria.Sources)).Returns(actualSources);
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new List<string>());
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.testDiscoveryManager.DiscoverTests(localDiscoveryCriteria, mockTestDiscoveryEventHandler.Object);

            Assert.IsNull(localDiscoveryCriteria.Package);
            // AdapterSourceMap should contain updated testSources.
            Assert.AreEqual(actualSources.FirstOrDefault(), localDiscoveryCriteria.AdapterSourceMap.FirstOrDefault().Value.FirstOrDefault());
        }

        [TestMethod]
        public void DiscoverTestsShouldNotSendDiscoveryRequestIfCommunicationFails()
        {
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .Callback(
                    () =>
                        {
                            this.mockTestHostManager.Raise(thm => thm.HostLaunched += null, new HostProviderEventArgs(string.Empty));
                        })
                .Returns(Task.FromResult(false));

            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventHandler.Object);

            this.mockRequestSender.Verify(s => s.DiscoverTests(It.IsAny<DiscoveryCriteria>(), It.IsAny<ITestDiscoveryEventsHandler2>()), Times.Never);
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
                TestPluginCacheTests.SetupMockAdditionalPathExtensions(extensions);
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
                this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new[] { "c:\\e1.dll", "c:\\e2.dll" });

                this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

                // Also verify that we have waited for client connection.
                this.mockRequestSender.Verify(s => s.InitializeDiscovery(extensions, true), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void DiscoverTestsShouldQueryTestHostManagerForExtensions()
        {
            TestPluginCache.Instance = null;
            try
            {
                TestPluginCacheTests.SetupMockAdditionalPathExtensions(new[] { "c:\\e1.dll" });
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
                this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new[] { "he1.dll", "c:\\e1.dll" });

                this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

                this.mockRequestSender.Verify(s => s.InitializeDiscovery(new[] { "he1.dll", "c:\\e1.dll" }, true), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void DiscoverTestsShouldPassAdapterToTestHostManagerFromTestPluginCacheExtensions()
        {
            // We are updating extension with testadapter only to make it easy to test.
            // In product code it filter out testadapter from extension
            TestPluginCache.Instance.UpdateExtensions(new List<string> { "abc.TestAdapter.dll", "xyz.TestAdapter.dll" }, false);
            try
            {
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

                var expectedResult = new List<string>();
                expectedResult.AddRange(TestPluginCache.Instance.PathToExtensions);
                expectedResult.AddRange(TestPluginCache.Instance.DefaultExtensionPaths);

                this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

                this.mockTestHostManager.Verify(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), expectedResult), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void DiscoverTestsShouldNotIntializeTestHost()
        {
            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

            this.mockRequestSender.Verify(s => s.InitializeCommunication(), Times.Once);
            this.mockTestHostManager.Verify(thl => thl.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCatchExceptionAndCallHandleDiscoveryComplete()
        {
            // Setup mocks.
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), It.IsAny<IEnumerable<TestCase>>()));
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleRawMessage(It.IsAny<string>()));
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()));
        }

        [TestMethod]
        public void DiscoverTestsShouldInitiateServerDiscoveryLoop()
        {
            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

            // Assert.
            this.mockRequestSender.Verify(s => s.DiscoverTests(It.IsAny<DiscoveryCriteria>(), this.testDiscoveryManager), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCollectMetrics()
        {
            TestPluginCache.Instance = null;
            try
            {
                var mockMetricsCollector = new Mock<IMetricsCollection>();
                this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollector.Object);

                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);

                // Act.
                this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

                mockMetricsCollector.Verify(rd => rd.Add(TelemetryDataConstants.TimeTakenInSecToStartDiscoveryEngine, It.IsAny<double>()), Times.Once);
            }
            finally
            {
                TestPluginCache.Instance = null;
            }
        }

        [TestMethod]
        public void DiscoverTestsCloseTestHostIfRawMessageIsOfTypeDiscoveryComplete()
        {
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns( () =>
            {
                var message = new Message
                {
                    MessageType = MessageType.DiscoveryComplete
                };

                return message;
            });

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            this.mockTestHostManager.Verify(mthm => mthm.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotCloseTestHostIfRawMessageIsNotOfTypeDiscoveryComplete()
        {
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns(() =>
            {
                var message = new Message
                {
                    MessageType = MessageType.DiscoveryInitialize
                };

                return message;
            });

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            this.mockTestHostManager.Verify(mthm => mthm.CleanTestHostAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public void DiscoveryManagerShouldPassOnHandleDiscoveredTests()
        {
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            var testCases = new List<TestCase>() { new TestCase("x.y.z", new Uri("x://y"), "x.dll") };
            var rawMessage = "OnDiscoveredTests";
            var message = new Message() { MessageType = MessageType.TestCasesFound, Payload = null };
            this.SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(rawMessage, message);

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
                    this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(It.IsAny<string>())).Returns(completeMessage);
                    this.mockDataSerializer.Setup(ds => ds.DeserializePayload<DiscoveryCompletePayload>(completeMessage)).Returns(completePayload);
                });


            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(mtdeh => mtdeh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void DiscoveryManagerShouldPassOnHandleLogMessage()
        {
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns(() =>
            {
                var message = new Message
                {
                    MessageType = MessageType.TestMessage
                };

                return message;
            });

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(mtdeh => mtdeh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once);
        }

        private void SetupReceiveRawMessageAsyncAndDeserializeMessageAndInitialize(string rawMessage, Message message)
        {
            TestHostConnectionInfo connectionInfo;
            connectionInfo = new TestHostConnectionInfo
            {
                Endpoint = IPAddress.Loopback + ":0",
                Role = ConnectionRole.Client,
                Transport = Transport.Sockets
            };
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.mockDataSerializer = new Mock<IDataSerializer>();
            this.testRequestSender = new TestRequestSender(this.mockCommunicationManager.Object, connectionInfo, this.mockDataSerializer.Object, this.protocolConfig);
            this.mockCommunicationManager.Setup(mc => mc.HostServer(It.IsAny<IPEndPoint>())).Returns(new IPEndPoint(IPAddress.Loopback, 0));
            this.mockCommunicationManager.Setup(mc => mc.WaitForClientConnection(It.IsAny<int>())).Returns(true);
            this.testRequestSender.InitializeCommunication();
            this.mockCommunicationManager.Setup(mc => mc.ReceiveRawMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(rawMessage));
            this.mockDataSerializer.Setup(ds => ds.DeserializeMessage(rawMessage)).Returns(message);

            this.testDiscoveryManager = new ProxyDiscoveryManager(
                                            this.mockRequestData.Object,
                                            this.testRequestSender,
                                            this.mockTestHostManager.Object,
                                            this.mockDataSerializer.Object,
                                            this.testableClientConnectionTimeout);

            this.CheckAndSetProtocolVersion();
        }

        private void CheckAndSetProtocolVersion()
        {
            var message = new Message() { MessageType = MessageType.VersionCheck, Payload = this.protocolConfig.Version };
            this.mockCommunicationManager.Setup(mc => mc.ReceiveMessage()).Returns(message);
            this.mockDataSerializer.Setup(ds => ds.DeserializePayload<int>(It.IsAny<Message>())).Returns(this.protocolConfig.Version);
            this.testRequestSender.CheckVersionWithTestHost();
        }
    }
}
