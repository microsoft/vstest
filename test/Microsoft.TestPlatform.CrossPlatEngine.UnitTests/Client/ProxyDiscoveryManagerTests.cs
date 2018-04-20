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

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class ProxyDiscoveryManagerTests : ProxyBaseManagerTests
    {
        //private const int CLIENTPROCESSEXITWAIT = 10 * 1000;

        private readonly DiscoveryCriteria discoveryCriteria;

        private ProxyDiscoveryManager testDiscoveryManager;

        private Mock<ITestRequestSender> mockRequestSender;

        //private Mock<IDataSerializer> mockDataSerializer;

        private Mock<IRequestData> mockRequestData;

        private Mock<IMetricsCollection> mockMetricsCollection;
        private Mock<IFileHelper> mockFileHelper;

        /// <summary>
        /// The client connection timeout in milliseconds for unit tests.
        /// </summary>
        private int testableClientConnectionTimeout = 400;

        public ProxyDiscoveryManagerTests()
        {
            this.mockRequestSender = new Mock<ITestRequestSender>();
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockRequestData.Setup(rd => rd.MetricsCollection).Returns(this.mockMetricsCollection.Object);
            this.testDiscoveryManager = new ProxyDiscoveryManager(
                                            this.mockRequestData.Object,
                                            this.mockRequestSender.Object,
                                            this.mockTestHostManager.Object,
                                            this.mockDataSerializer.Object,
                                            this.testableClientConnectionTimeout,
                                            this.mockFileHelper.Object);
            this.discoveryCriteria = new DiscoveryCriteria(new[] { "test.dll" }, 1, string.Empty);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotInitializeExtensionsOnNoExtensions()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

            this.mockRequestSender.Verify(s => s.InitializeDiscovery(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldNotInitializeExtensionsOnCommunicationFailure()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(false);

            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventHandler.Object);

            this.mockRequestSender.Verify(s => s.InitializeExecution(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldAllowRuntimeProviderToUpdateAdapterSource()
        {
            // Make sure TestPlugincache is refreshed.
            TestPluginCache.Instance = null;

            this.mockTestHostManager.Setup(hm => hm.GetTestSources(this.discoveryCriteria.Sources)).Returns(this.discoveryCriteria.Sources);
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

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
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

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
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

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

            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

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
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new[] { "c:\\e1.dll", "c:\\e2.dll" });

                this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

                // Also verify that we have waited for client connection.
                this.mockRequestSender.Verify(s => s.InitializeDiscovery(extensions), Times.Once);
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

            TestPluginCacheTests.SetupMockAdditionalPathExtensions(inputExtensions);
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
            this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns((IEnumerable<string> sources, IEnumerable<string> extensions) =>
            {
                return extensions.Select(extension => { return Path.GetFileName(extension); });
            });

            this.mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns((string extensionPath) =>
            {
                return !extensionPath.Contains("def.TestAdapter.dll");
            });

            this.mockFileHelper.Setup(fh => fh.Exists("def.TestAdapter.dll")).Returns(false);
            this.mockFileHelper.Setup(fh => fh.Exists("xyz.TestAdapter.dll")).Returns(true);

            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

            this.mockRequestSender.Verify(s => s.InitializeDiscovery(expectedOutputPaths), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldQueryTestHostManagerForExtensions()
        {
            TestPluginCache.Instance = null;
            try
            {
                TestPluginCacheTests.SetupMockAdditionalPathExtensions(new[] { "c:\\e1.dll" });
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns(new[] { "he1.dll", "c:\\e1.dll" });

                this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

                this.mockRequestSender.Verify(s => s.InitializeDiscovery(new[] { "he1.dll", "c:\\e1.dll" }), Times.Once);
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
                this.mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>())).Returns(true);
                var expectedResult = TestPluginCache.Instance.GetExtensionPaths(string.Empty);

                this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

                this.mockTestHostManager.Verify(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), expectedResult), Times.Once);
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
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

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
        public void DiscoverTestsShouldCatchExceptionAndCallHandleRawMessageOfDiscoveryComplete()
        {
            // Setup mocks.
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.DiscoveryComplete, It.IsAny<DiscoveryCompletePayload>())).Returns(MessageType.DiscoveryComplete);

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.DiscoveryComplete) ? MessageType.DiscoveryComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.DiscoveryComplete))), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCatchExceptionAndCallHandleRawMessageOfTestMessage()
        {
            // Setup mocks.
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.DiscoveryComplete, It.IsAny<DiscoveryCompletePayload>())).Returns(MessageType.DiscoveryComplete);

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.DiscoveryComplete) ? MessageType.DiscoveryComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
                };

                return message;
            });

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleRawMessage(It.Is<string>(str => str.Contains(MessageType.TestMessage))), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldCatchExceptionAndCallHandleLogMessageOfError()
        {
            // Setup mocks.
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();
            this.mockTestHostManager.Setup(tmh => tmh.LaunchTestHostAsync(It.IsAny<TestProcessStartInfo>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, mockTestDiscoveryEventsHandler.Object);

            // Verify
            mockTestDiscoveryEventsHandler.Verify(s => s.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsShouldInitiateServerDiscoveryLoop()
        {
            // Setup mocks.
            this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);

            // Act.
            this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

            // Assert.
            this.mockRequestSender.Verify(s => s.DiscoverTests(It.IsAny<DiscoveryCriteria>(), this.testDiscoveryManager), Times.Once);
        }

        [TestMethod]
        public void DiscoverTestsCloseTestHostIfRawMessageIsOfTypeDiscoveryComplete()
        {
            Mock<ITestDiscoveryEventsHandler2> mockTestDiscoveryEventsHandler = new Mock<ITestDiscoveryEventsHandler2>();

            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.TestMessage, It.IsAny<TestMessagePayload>())).Returns(MessageType.TestMessage);
            this.mockDataSerializer.Setup(ds => ds.SerializePayload(MessageType.DiscoveryComplete, It.IsAny<DiscoveryCompletePayload>())).Returns(MessageType.DiscoveryComplete);

            this.mockDataSerializer.Setup(mds => mds.DeserializeMessage(It.IsAny<string>())).Returns((string rawMessage) =>
            {
                var messageType = rawMessage.Contains(MessageType.DiscoveryComplete) ? MessageType.DiscoveryComplete : MessageType.TestMessage;
                var message = new Message
                {
                    MessageType = messageType
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
            var testCases = new List<TestCase>() { new TestCase("x.y.z", new Uri("x://y"), "x.dll") };

            this.testDiscoveryManager = this.GetProxyDiscoveryManager();
            this.SetupChannelMessage(MessageType.StartDiscovery, MessageType.TestCasesFound, testCases);

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

        private void InvokeAndVerifyDiscoverTests(bool skipDefaultAdapters)
        {
            TestPluginCache.Instance = null;
            TestPluginCache.Instance.DefaultExtensionPaths = new List<string> { "default1.dll", "default2.dll" };
            TestPluginCache.Instance.UpdateExtensions(new List<string> { "filterTestAdapter.dll" }, false);
            TestPluginCache.Instance.UpdateExtensions(new List<string> { "unfilter.dll" }, true);

            try
            {
                this.mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
                this.mockTestHostManager.Setup(th => th.GetTestPlatformExtensions(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>())).Returns((IEnumerable<string> sources, IEnumerable<string> extensions) => extensions);
                this.mockRequestSender.Setup(s => s.WaitForRequestHandlerConnection(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(true);
                var expectedResult = TestPluginCache.Instance.GetExtensionPaths(TestPlatformConstants.TestAdapterEndsWithPattern, skipDefaultAdapters);

                this.testDiscoveryManager.Initialize(skipDefaultAdapters);
                this.testDiscoveryManager.DiscoverTests(this.discoveryCriteria, null);

                this.mockRequestSender.Verify(s => s.InitializeDiscovery(expectedResult), Times.Once);
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
