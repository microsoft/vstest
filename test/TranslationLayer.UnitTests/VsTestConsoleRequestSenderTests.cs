// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
    using Payloads = Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
    using NuGet.Frameworks;

    [TestClass]
    public class VsTestConsoleRequestSenderTests
    {
        private readonly ITranslationLayerRequestSender requestSender;

        private readonly Mock<ICommunicationManager> mockCommunicationManager;

        private readonly int WaitTimeout = 2000;

        private int protocolVersion = 5;
        private IDataSerializer serializer = JsonDataSerializer.Instance;

        public VsTestConsoleRequestSenderTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.requestSender = new VsTestConsoleRequestSender(
                this.mockCommunicationManager.Object,
                JsonDataSerializer.Instance,
                new Mock<ITestPlatformEventSource>().Object);
        }

        #region Communication Tests

        [TestMethod]
        public void InitializeCommunicationShouldSucceed()
        {
            this.InitializeCommunication();

            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Exactly(2));
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once);
        }

        [TestMethod]
        public async Task InitializeCommunicationAsyncShouldSucceed()
        {
            await this.InitializeCommunicationAsync();

            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldReturnInvalidPortNumberIfHostServerFails()
        {
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Throws(new Exception("Fail"));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false));

            var portOutput = this.requestSender.InitializeCommunication();
            Assert.IsTrue(portOutput < 0, "Negative port number must be returned if Hosting Server fails.");

            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsFalse(connectionSuccess, "Connection must fail as server failed to host.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Never);
            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Never);
            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Never);
        }

        [TestMethod]
        public async Task InitializeCommunicationAsyncShouldReturnInvalidPortNumberIfHostServerFails()
        {
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Throws(new Exception("Fail"));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false));

            var portOutput = await this.requestSender.InitializeCommunicationAsync(this.WaitTimeout);
            Assert.IsTrue(portOutput < 0, "Negative port number must be returned if Hosting Server fails.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Never);
            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public void InitializeCommunicationShouldFailConnectionIfMessageReceiveFailed()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
                .Callback((int timeout) => Task.Delay(200).Wait());
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Throws(new Exception("Fail"));

            var portOutput = this.requestSender.InitializeCommunication();

            // Hosting server didn't server, so port number should still be valid
            Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");

            // Connection must not succeed as handshake failed
            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsFalse(connectionSuccess, "Connection must fail if handshake failed.");
            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);
        }

        [TestMethod]
        public async Task InitializeCommunicationAsyncShouldFailConnectionIfMessageReceiveFailed()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Fail"));

            var portOutput = await this.requestSender.InitializeCommunicationAsync(this.WaitTimeout);

            // Connection must not succeed as handshake failed
            Assert.AreEqual(-1, portOutput, "Connection must fail if handshake failed.");
            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldFailConnectionIfSessionConnectedDidNotComeFirst()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
                .Callback((int timeout) => Task.Delay(200).Wait());

            var discoveryMessage = new Message() { MessageType = MessageType.StartDiscovery };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(discoveryMessage);

            var portOutput = this.requestSender.InitializeCommunication();
            Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsFalse(connectionSuccess, "Connection must fail if version check failed.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Never);
        }

        [TestMethod]
        public async Task InitializeCommunicationAsyncShouldFailConnectionIfSessionConnectedDidNotComeFirst()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

            var discoveryMessage = new Message() { MessageType = MessageType.StartDiscovery };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryMessage));

            var portOutput = await this.requestSender.InitializeCommunicationAsync(this.WaitTimeout);
            Assert.AreEqual(-1, portOutput, "Connection must fail if version check failed.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Never);
        }

        [TestMethod]
        public void InitializeCommunicationShouldFailConnectionIfSendMessageFailed()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
                .Callback((int timeout) => Task.Delay(200).Wait());

            var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(sessionConnected);
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion)).Throws(new Exception("Fail"));

            var portOutput = this.requestSender.InitializeCommunication();
            Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsFalse(connectionSuccess, "Connection must fail if version check failed.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once);
        }

        [TestMethod]
        public async Task InitializeCommunicationAsyncShouldFailConnectionIfSendMessageFailed()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

            var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(sessionConnected));
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion)).Throws(new Exception("Fail"));

            var portOutput = await this.requestSender.InitializeCommunicationAsync(this.WaitTimeout);
            Assert.AreEqual(-1, portOutput, "Connection must fail if version check failed.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldFailConnectionIfProtocolIsNotCompatible()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

            this.mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
                .Callback((int timeout) => Task.Delay(200).Wait());

            var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };

            // Give wrong version
            var protocolError = new Message()
            {
                MessageType = MessageType.ProtocolError,
                Payload = null
            };

            Action changedMessage =
                () => { this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(protocolError); };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(sessionConnected);
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck)).Callback(changedMessage);

            var portOutput = this.requestSender.InitializeCommunication();
            Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsFalse(connectionSuccess, "Connection must fail if version check failed.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Exactly(2));
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once);
        }

        [TestMethod]
        public async Task InitializeCommunicationAsyncShouldFailConnectionIfProtocolIsNotCompatible()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

            var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };

            // Give wrong version
            var protocolError = new Message()
            {
                MessageType = MessageType.ProtocolError,
                Payload = null
            };

            Action changedMessage =
                () => { this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(protocolError)); };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(sessionConnected));
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck)).Callback(changedMessage);

            var portOutput = await this.requestSender.InitializeCommunicationAsync(this.WaitTimeout);
            Assert.AreEqual(-1, portOutput, "Connection must fail if version check failed.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once);
        }

        #endregion

        #region Discovery Tests

        [TestMethod]
        public void DiscoverTestsShouldCompleteWithZeroTests()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

            var payload = new DiscoveryCompletePayload() { TotalTests = 0, LastDiscoveredTests = null, IsAborted = false };
            var discoveryComplete = new Message()
            {
                MessageType = MessageType.DiscoveryComplete,
                Payload = JToken.FromObject(payload)
            };
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete));

            this.requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Never, "DiscoveredTests must not be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldCompleteWithZeroTests()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

            var payload = new DiscoveryCompletePayload() { TotalTests = 0, LastDiscoveredTests = null, IsAborted = false };
            var discoveryComplete = new Message()
            {
                MessageType = MessageType.DiscoveryComplete,
                Payload = JToken.FromObject(payload)
            };
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete));

            await this.requestSender.DiscoverTestsAsync(new List<string>() { "1.dll" }, null, null, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Never, "DiscoveredTests must not be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public void DiscoverTestsShouldCompleteWithSingleTest()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testCaseList = new List<TestCase>() { testCase };
            var testsFound = new Message()
            {
                MessageType = MessageType.TestCasesFound,
                Payload = JToken.FromObject(testCaseList)
            };

            var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = null, IsAborted = false };
            var discoveryComplete = new Message()
            {
                MessageType = MessageType.DiscoveryComplete,
                Payload = JToken.FromObject(payload)
            };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
            mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback(
                () => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete)));

            this.requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Once, "DiscoveredTests must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldCompleteWithSingleTest()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testCaseList = new List<TestCase>() { testCase };
            var testsFound = new Message()
            {
                MessageType = MessageType.TestCasesFound,
                Payload = JToken.FromObject(testCaseList)
            };

            var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = null, IsAborted = false };
            var discoveryComplete = new Message()
            {
                MessageType = MessageType.DiscoveryComplete,
                Payload = JToken.FromObject(payload)
            };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
            mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback(
                () => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete)));

            await this.requestSender.DiscoverTestsAsync(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Once, "DiscoveredTests must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public void DiscoverTestsShouldReportBackTestsWithTraitsInTestsFoundMessage()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            testCase.Traits.Add(new Trait("a", "b"));

            List<TestCase> receivedTestCases = null;
            var testCaseList = new List<TestCase>() { testCase };
            var testsFound = CreateMessage(MessageType.TestCasesFound, testCaseList);

            var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = null, IsAborted = false };
            var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
            mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()))
                .Callback(
                    (IEnumerable<TestCase> tests) =>
                    {
                        receivedTestCases = tests?.ToList();
                        this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult((discoveryComplete)));
                    });

            this.requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            Assert.IsNotNull(receivedTestCases);
            Assert.AreEqual(1, receivedTestCases.Count);

            // Verify that the traits are passed through properly.
            var traits = receivedTestCases.ToArray()[0].Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual("a", traits.ToArray()[0].Name);
            Assert.AreEqual("b", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldReportBackTestsWithTraitsInTestsFoundMessage()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            testCase.Traits.Add(new Trait("a", "b"));

            List<TestCase> receivedTestCases = null;
            var testCaseList = new List<TestCase>() { testCase };
            var testsFound = CreateMessage(MessageType.TestCasesFound, testCaseList);

            var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = null, IsAborted = false };
            var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
            mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()))
                .Callback(
                    (IEnumerable<TestCase> tests) =>
                    {
                        receivedTestCases = tests?.ToList();
                        this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult((discoveryComplete)));
                    });

            await this.requestSender.DiscoverTestsAsync(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            Assert.IsNotNull(receivedTestCases);
            Assert.AreEqual(1, receivedTestCases.Count);

            // Verify that the traits are passed through properly.
            var traits = receivedTestCases.ToArray()[0].Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual("a", traits.ToArray()[0].Name);
            Assert.AreEqual("b", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public void DiscoverTestsShouldReportBackTestsWithTraitsInDiscoveryCompleteMessage()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            testCase.Traits.Add(new Trait("a", "b"));

            List<TestCase> receivedTestCases = null;
            var testCaseList = new List<TestCase>() { testCase };

            var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = testCaseList, IsAborted = false };
            var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete));
            mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), It.IsAny<IEnumerable<TestCase>>()))
                .Callback(
                    (DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> tests) =>
                    {
                        receivedTestCases = tests?.ToList();
                    });

            this.requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            Assert.IsNotNull(receivedTestCases);
            Assert.AreEqual(1, receivedTestCases.Count);

            // Verify that the traits are passed through properly.
            var traits = receivedTestCases.ToArray()[0].Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual("a", traits.ToArray()[0].Name);
            Assert.AreEqual("b", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldReportBackTestsWithTraitsInDiscoveryCompleteMessage()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            testCase.Traits.Add(new Trait("a", "b"));

            List<TestCase> receivedTestCases = null;
            var testCaseList = new List<TestCase>() { testCase };

            var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = testCaseList, IsAborted = false };
            var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete));
            mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), It.IsAny<IEnumerable<TestCase>>()))
                .Callback(
                    (DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> tests) =>
                    {
                        receivedTestCases = tests?.ToList();
                    });

            await this.requestSender.DiscoverTestsAsync(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            Assert.IsNotNull(receivedTestCases);
            Assert.AreEqual(1, receivedTestCases.Count);

            // Verify that the traits are passed through properly.
            var traits = receivedTestCases.ToArray()[0].Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual("a", traits.ToArray()[0].Name);
            Assert.AreEqual("b", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public void DiscoverTestsShouldCompleteWithTestMessage()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testCaseList = new List<TestCase>() { testCase };
            var testsFound = CreateMessage(MessageType.TestCasesFound, testCaseList);

            var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = null, IsAborted = false };
            var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
            mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback(
                () => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message)));
            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete)));

            this.requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Once, "DiscoveredTests must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Once, "TestMessage event must be called");
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldCompleteWithTestMessage()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testCaseList = new List<TestCase>() { testCase };
            var testsFound = CreateMessage(MessageType.TestCasesFound, testCaseList);

            var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = null, IsAborted = false };
            var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
            mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback(
                () => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message)));
            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete)));

            await this.requestSender.DiscoverTestsAsync(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Once, "DiscoveredTests must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Once, "TestMessage event must be called");
        }

        [TestMethod]
        public void DiscoverTestsShouldAbortOnExceptionInSendMessage()
        {
            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();
            var sources = new List<string> { "1.dll" };
            var payload = new DiscoveryRequestPayload { Sources = sources, RunSettings = null };
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.StartDiscovery, payload)).Throws(new IOException());

            this.requestSender.DiscoverTests(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            this.mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldAbortOnExceptionInSendMessage()
        {
            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();
            var sources = new List<string> { "1.dll" };
            var payload = new DiscoveryRequestPayload { Sources = sources, RunSettings = null };
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.StartDiscovery, payload)).Throws(new IOException());

            await this.requestSender.DiscoverTestsAsync(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            this.mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldLogErrorWhenProcessExited()
        {
            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();
            var sources = new List<string> { "1.dll" };
            var payload = new DiscoveryRequestPayload { Sources = sources, RunSettings = null };
            var manualEvent = new ManualResetEvent(false);

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testCaseList = new List<TestCase>() { testCase };
            var testsFound = CreateMessage(MessageType.TestCasesFound, testCaseList);
            this.requestSender.InitializeCommunication();
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Callback(
                (CancellationToken c) =>
                {
                    Task.Run(() => this.requestSender.OnProcessExited()).Wait();

                    Assert.IsTrue(c.IsCancellationRequested);
                }).Returns(Task.FromResult((Message)null));

            mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null)).Callback(() => manualEvent.Set());

            this.requestSender.DiscoverTests(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

            manualEvent.WaitOne();
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task DiscoverTestsAsyncShouldLogErrorWhenProcessExited()
        {
            var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();
            var sources = new List<string> { "1.dll" };
            var payload = new DiscoveryRequestPayload { Sources = sources, RunSettings = null };

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testCaseList = new List<TestCase>() { testCase };
            var testsFound = CreateMessage(MessageType.TestCasesFound, testCaseList);
            await this.requestSender.InitializeCommunicationAsync(this.WaitTimeout);
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Callback(
                (CancellationToken c) =>
                {
                    Task.Run(() => this.requestSender.OnProcessExited()).Wait();

                    Assert.IsTrue(c.IsCancellationRequested);
                }).Returns(Task.FromResult((Message)null));

            await this.requestSender.DiscoverTestsAsync(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region RunTests

        [TestMethod]
        public void StartTestRunShouldCompleteWithZeroTests()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };

            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

            this.requestSender.StartTestRun(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Never, "RunChangedArgs must not be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public async Task StartTestRunAsyncShouldCompleteWithZeroTests()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };

            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

            await this.requestSender.StartTestRunAsync(new List<string>() { "1.dll" }, null, null, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Never, "RunChangedArgs must not be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public void StartTestRunShouldCompleteWithSingleTestAndMessage()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(null,
                new List<TestResult>() { testResult }, null);

            var testsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };

            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload));

            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
                (testRunChangedArgs) =>
                {
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
                });

            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () =>
                {
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

            this.requestSender.StartTestRun(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        }

        [TestMethod]
        public async Task StartTestRunAsyncShouldCompleteWithSingleTestAndMessage()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(null,
                new List<TestResult>() { testResult }, null);

            var testsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };

            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload));

            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
                (testRunChangedArgs) =>
                {
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
                });

            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () =>
                {
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

            await this.requestSender.StartTestRunAsync(new List<string>() { "1.dll" }, null, null, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        }

        [TestMethod]
        public void StartTestRunShouldNotThrowIfTestPlatformOptionsIsNull()
        {
            // Arrange.
            var sources = new List<string>() { "1.dll" };
            TestRunRequestPayload receivedRequest = null;

            var mockHandler = new Mock<ITestRunEventsHandler>();

            this.SetupMockCommunicationForRunRequest(mockHandler);
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.TestRunAllSourcesWithDefaultHost, It.IsAny<TestRunRequestPayload>(), It.IsAny<int>())).
                Callback((string msg, object requestpayload, int protocol) => { receivedRequest = (TestRunRequestPayload)requestpayload; });

            // Act.
            this.requestSender.StartTestRun(sources, null, null, null, mockHandler.Object);

            // Assert.
            Assert.IsNotNull(receivedRequest);
            CollectionAssert.AreEqual(sources, receivedRequest.Sources);
            Assert.IsNull(receivedRequest.TestPlatformOptions, "The run request message should include a null test case filter");
        }

        [TestMethod]
        public void StartTestRunShouldIncludeFilterInRequestPayload()
        {
            // Arrange.
            var sources = new List<string>() { "1.dll" };
            var filter = "GivingCampaign";
            TestRunRequestPayload receivedRequest = null;

            var mockHandler = new Mock<ITestRunEventsHandler>();

            this.SetupMockCommunicationForRunRequest(mockHandler);
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.TestRunAllSourcesWithDefaultHost, It.IsAny<TestRunRequestPayload>(), It.IsAny<int>())).
                Callback((string msg, object requestpayload, int protocol) => { receivedRequest = (TestRunRequestPayload)requestpayload; });

            // Act.
            this.requestSender.StartTestRun(sources, null, new TestPlatformOptions() { TestCaseFilter = filter }, null, mockHandler.Object);

            // Assert.
            Assert.IsNotNull(receivedRequest);
            CollectionAssert.AreEqual(sources, receivedRequest.Sources);
            Assert.AreEqual(filter, receivedRequest.TestPlatformOptions.TestCaseFilter, "The run request message should include test case filter");
        }

        [TestMethod]
        public void StartTestRunWithCustomHostShouldComplete()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(null, new List<TestResult>() { testResult }, null);
            var testsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            var runprocessInfoPayload = new Message()
            {
                MessageType = MessageType.CustomTestHostLaunch,
                Payload = JToken.FromObject(new TestProcessStartInfo())
            };


            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
                (testRunChangedArgs) =>
                {
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
                });

            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () =>
                {
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

            var mockLauncher = new Mock<ITestHostLauncher>();
            mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Callback
                (() => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload)));

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

            this.requestSender.StartTestRunWithCustomHost(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object, mockLauncher.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            mockLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once, "Custom TestHostLauncher must be called");
        }

        [TestMethod]
        public async Task StartTestRunAsyncWithCustomHostShouldComplete()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(null, new List<TestResult>() { testResult }, null);
            var testsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            var runprocessInfoPayload = new Message()
            {
                MessageType = MessageType.CustomTestHostLaunch,
                Payload = JToken.FromObject(new TestProcessStartInfo())
            };


            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
                (testRunChangedArgs) =>
                {
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
                });

            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () =>
                {
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

            var mockLauncher = new Mock<ITestHostLauncher>();
            mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Callback
                (() => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload)));

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

            await this.requestSender.StartTestRunWithCustomHostAsync(new List<string>() { "1.dll" }, null, null, null, mockHandler.Object, mockLauncher.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            mockLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once, "Custom TestHostLauncher must be called");
        }

        [TestMethod]
        public void StartTestRunWithCustomHostShouldNotAbortAndSendErrorToVstestConsoleInErrorScenario()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(null, new List<TestResult>() { testResult }, null);
            var testsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            Message runprocessInfoPayload = new VersionedMessage()
            {
                MessageType = MessageType.CustomTestHostLaunch,
                Payload = JToken.FromObject(new TestProcessStartInfo())
            };

            var mockLauncher = new Mock<ITestHostLauncher>();
            mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Throws(new Exception("BadError"));

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>(), this.protocolVersion)).
                Callback(() => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

            this.requestSender.StartTestRunWithCustomHost(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object, mockLauncher.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        }

        [TestMethod]
        public async Task StartTestRunAsyncWithCustomHostShouldNotAbortAndSendErrorToVstestConsoleInErrorScenario()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(null, new List<TestResult>() { testResult }, null);
            var testsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            Message runprocessInfoPayload = new VersionedMessage()
            {
                MessageType = MessageType.CustomTestHostLaunch,
                Payload = JToken.FromObject(new TestProcessStartInfo())
            };

            var mockLauncher = new Mock<ITestHostLauncher>();
            mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Throws(new Exception("BadError"));

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>(), this.protocolVersion)).
                Callback(() => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

            await this.requestSender.StartTestRunWithCustomHostAsync(new List<string>() { "1.dll" }, null, null, null, mockHandler.Object, mockLauncher.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        }

        [TestMethod]
        public void StartTestRunWithCustomHostShouldNotThrowIfTestPlatformOptionsIsNull()
        {
            // Arrange.
            var sources = new List<string>() { "1.dll" };
            TestRunRequestPayload receivedRequest = null;

            var mockHandler = new Mock<ITestRunEventsHandler>();

            this.SetupMockCommunicationForRunRequest(mockHandler);
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.GetTestRunnerProcessStartInfoForRunAll, It.IsAny<TestRunRequestPayload>(), It.IsAny<int>())).
                Callback((string msg, object requestpayload, int protocol) => { receivedRequest = (TestRunRequestPayload)requestpayload; });

            // Act.
            this.requestSender.StartTestRunWithCustomHost(sources, null, null, null, mockHandler.Object, new Mock<ITestHostLauncher>().Object);

            // Assert.
            Assert.IsNotNull(receivedRequest);
            CollectionAssert.AreEqual(sources, receivedRequest.Sources);
            Assert.IsNull(receivedRequest.TestPlatformOptions, "The run request message should include a null test case filter");
        }

        [TestMethod]
        public void StartTestRunWithCustomHostShouldIncludeFilterInRequestPayload()
        {
            // Arrange.
            var sources = new List<string>() { "1.dll" };
            var filter = "GivingCampaign";
            TestRunRequestPayload receivedRequest = null;

            var mockHandler = new Mock<ITestRunEventsHandler>();

            this.SetupMockCommunicationForRunRequest(mockHandler);
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.GetTestRunnerProcessStartInfoForRunAll, It.IsAny<TestRunRequestPayload>(), It.IsAny<int>())).
                Callback((string msg, object requestpayload, int protocol) => { receivedRequest = (TestRunRequestPayload)requestpayload; });

            // Act.
            this.requestSender.StartTestRunWithCustomHost(sources, null, new TestPlatformOptions() { TestCaseFilter = filter }, null, mockHandler.Object, new Mock<ITestHostLauncher>().Object);

            // Assert.
            Assert.IsNotNull(receivedRequest);
            CollectionAssert.AreEqual(sources, receivedRequest.Sources);
            Assert.AreEqual(filter, receivedRequest.TestPlatformOptions.TestCaseFilter, "The run request message should include test case filter");
        }

        [TestMethod]
        public void StartTestRunWithSelectedTestsShouldCompleteWithZeroTests()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

            this.requestSender.StartTestRun(new List<TestCase>(), null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Never, "RunChangedArgs must not be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public async Task StartTestRunAsyncWithSelectedTestsShouldCompleteWithZeroTests()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunEventsHandler>();
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

            await this.requestSender.StartTestRunAsync(new List<TestCase>(), null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Never, "RunChangedArgs must not be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public void StartTestRunWithSelectedTestsShouldCompleteWithSingleTestAndMessage()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var testCaseList = new List<TestCase>() { testCase };

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(null, new List<TestResult>() { testResult }, null);
            var testsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload));

            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
                (testRunChangedArgs) =>
                {
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
                });

            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () =>
                {
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

            this.requestSender.StartTestRun(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        }

        [TestMethod]
        public async Task StartTestRunAsyncWithSelectedTestsShouldCompleteWithSingleTestAndMessage()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var testCaseList = new List<TestCase>() { testCase };

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(null, new List<TestResult>() { testResult }, null);
            var testsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload));

            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
                (testRunChangedArgs) =>
                {
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
                });

            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () =>
                {
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

            await this.requestSender.StartTestRunAsync(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        }

        [TestMethod]
        public void StartTestRunWithSelectedTestsHavingTraitsShouldReturnTestRunCompleteWithTraitsIntact()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            testCase.Traits.Add(new Trait("a", "b"));

            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var testCaseList = new List<TestCase>() { testCase };

            TestRunChangedEventArgs receivedChangeEventArgs = null;
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, new List<TestResult> { testResult }, null);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

            mockHandler.Setup(mh => mh.HandleTestRunComplete(
                    It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()))
                .Callback(
                    (TestRunCompleteEventArgs complete,
                     TestRunChangedEventArgs stats,
                     ICollection<AttachmentSet> attachments,
                     ICollection<string> executorUris) =>
                    {
                        receivedChangeEventArgs = stats;
                    });

            this.requestSender.StartTestRun(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

            Assert.IsNotNull(receivedChangeEventArgs);
            Assert.IsTrue(receivedChangeEventArgs.NewTestResults.Any());

            // Verify that the traits are passed through properly.
            var traits = receivedChangeEventArgs.NewTestResults.ToArray()[0].TestCase.Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual("a", traits.ToArray()[0].Name);
            Assert.AreEqual("b", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public async Task StartTestRunAsyncWithSelectedTestsHavingTraitsShouldReturnTestRunCompleteWithTraitsIntact()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            testCase.Traits.Add(new Trait("a", "b"));

            var testResult = new TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var testCaseList = new List<TestCase>() { testCase };

            TestRunChangedEventArgs receivedChangeEventArgs = null;
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, new List<TestResult> { testResult }, null);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

            mockHandler.Setup(mh => mh.HandleTestRunComplete(
                    It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()))
                .Callback(
                    (TestRunCompleteEventArgs complete,
                     TestRunChangedEventArgs stats,
                     ICollection<AttachmentSet> attachments,
                     ICollection<string> executorUris) =>
                    {
                        receivedChangeEventArgs = stats;
                    });

            await this.requestSender.StartTestRunAsync(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

            Assert.IsNotNull(receivedChangeEventArgs);
            Assert.IsTrue(receivedChangeEventArgs.NewTestResults.Any());

            // Verify that the traits are passed through properly.
            var traits = receivedChangeEventArgs.NewTestResults.ToArray()[0].TestCase.Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual("a", traits.ToArray()[0].Name);
            Assert.AreEqual("b", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public void StartTestRunWithSelectedTestsHavingTraitsShouldReturnTestRunStatsWithTraitsIntact()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            testCase.Traits.Add(new Trait("a", "b"));

            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var testCaseList = new List<TestCase>() { testCase };

            TestRunChangedEventArgs receivedChangeEventArgs = null;
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(
                null,
                new List<TestResult>() { testResult },
                null);
            var testsRunStatsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var testRunCompletepayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, testRunCompletepayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsRunStatsPayload));

            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(
                    It.IsAny<TestRunChangedEventArgs>()))
                .Callback(
                    (TestRunChangedEventArgs stats) =>
                    {
                        receivedChangeEventArgs = stats;
                        this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                    });

            this.requestSender.StartTestRun(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

            Assert.IsNotNull(receivedChangeEventArgs);
            Assert.IsTrue(receivedChangeEventArgs.NewTestResults.Any());

            // Verify that the traits are passed through properly.
            var traits = receivedChangeEventArgs.NewTestResults.ToArray()[0].TestCase.Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual("a", traits.ToArray()[0].Name);
            Assert.AreEqual("b", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public async Task StartTestRunAsyncWithSelectedTestsHavingTraitsShouldReturnTestRunStatsWithTraitsIntact()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            testCase.Traits.Add(new Trait("a", "b"));

            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var testCaseList = new List<TestCase>() { testCase };

            TestRunChangedEventArgs receivedChangeEventArgs = null;
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(
                null,
                new List<TestResult>() { testResult },
                null);
            var testsRunStatsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var testRunCompletepayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, testRunCompletepayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsRunStatsPayload));

            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(
                    It.IsAny<TestRunChangedEventArgs>()))
                .Callback(
                    (TestRunChangedEventArgs stats) =>
                    {
                        receivedChangeEventArgs = stats;
                        this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                    });

            await this.requestSender.StartTestRunAsync(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

            Assert.IsNotNull(receivedChangeEventArgs);
            Assert.IsTrue(receivedChangeEventArgs.NewTestResults.Any());

            // Verify that the traits are passed through properly.
            var traits = receivedChangeEventArgs.NewTestResults.ToArray()[0].TestCase.Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual("a", traits.ToArray()[0].Name);
            Assert.AreEqual("b", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public void StartTestRunWithCustomHostWithSelectedTestsComplete()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var testCaseList = new List<TestCase>() { testCase };

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(null, new List<TestResult>() { testResult }, null);

            var testsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };

            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);
            var runprocessInfoPayload = CreateMessage(MessageType.CustomTestHostLaunch, new TestProcessStartInfo());

            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
                (testRunChangedArgs) =>
                {
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
                });

            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () =>
                {
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

            var mockLauncher = new Mock<ITestHostLauncher>();
            mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Callback
                (() => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload)));

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

            this.requestSender.StartTestRunWithCustomHost(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object, mockLauncher.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            mockLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once, "Custom TestHostLauncher must be called");
        }

        [TestMethod]
        public async Task StartTestRunWithCustomHostAsyncWithSelectedTestsShouldComplete()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var testCaseList = new List<TestCase>() { testCase };

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var testsChangedArgs = new TestRunChangedEventArgs(null, new List<TestResult>() { testResult }, null);

            var testsPayload = CreateMessage(MessageType.TestRunStatsChange, testsChangedArgs);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };

            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);
            var runprocessInfoPayload = CreateMessage(MessageType.CustomTestHostLaunch, new TestProcessStartInfo());

            mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
                (testRunChangedArgs) =>
                {
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
                });

            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () =>
                {
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

            var mockLauncher = new Mock<ITestHostLauncher>();
            mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Callback
                (() => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload)));

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

            await this.requestSender.StartTestRunWithCustomHostAsync(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object, mockLauncher.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            mockLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once, "Custom TestHostLauncher must be called");
        }

        [TestMethod]
        public void StartTestRunWithCustomHostInParallelShouldCallCustomHostMultipleTimes()
        {
            var mockLauncher = new Mock<ITestHostLauncher>();
            var mockHandler = new Mock<ITestRunEventsHandler>();
            IEnumerable<string> sources = new List<string> { "1.dll" };
            var p1 = new TestProcessStartInfo() { FileName = "X" };
            var p2 = new TestProcessStartInfo() { FileName = "Y" };
            var message1 = CreateMessage(MessageType.CustomTestHostLaunch, p1);
            var message2 = CreateMessage(MessageType.CustomTestHostLaunch, p2);
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var completepayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = null,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, completepayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message1));
            mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()))
                .Callback<TestProcessStartInfo>((startInfo) =>
                {
                    if (startInfo.FileName.Equals(p1.FileName))
                    {
                        this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message2));
                    }
                    else if (startInfo.FileName.Equals(p2.FileName))
                    {
                        this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                    }
                });
            this.requestSender.InitializeCommunication();
            this.requestSender.StartTestRunWithCustomHost(sources, null, new TestPlatformOptions(), null, mockHandler.Object, mockLauncher.Object);

            mockLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task StartTestRunWithCustomHostAsyncInParallelShouldCallCustomHostMultipleTimes()
        {
            var mockLauncher = new Mock<ITestHostLauncher>();
            var mockHandler = new Mock<ITestRunEventsHandler>();
            IEnumerable<string> sources = new List<string> { "1.dll" };
            var p1 = new TestProcessStartInfo() { FileName = "X" };
            var p2 = new TestProcessStartInfo() { FileName = "Y" };
            var message1 = CreateMessage(MessageType.CustomTestHostLaunch, p1);
            var message2 = CreateMessage(MessageType.CustomTestHostLaunch, p2);
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var completepayload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = null,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            var runComplete = CreateMessage(MessageType.ExecutionComplete, completepayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message1));
            mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()))
                .Callback<TestProcessStartInfo>((startInfo) =>
                {
                    if (startInfo.FileName.Equals(p1.FileName))
                    {
                        this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message2));
                    }
                    else if (startInfo.FileName.Equals(p2.FileName))
                    {
                        this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                    }
                });

            await this.requestSender.InitializeCommunicationAsync(this.WaitTimeout);
            await this.requestSender.StartTestRunWithCustomHostAsync(sources, null, null, null, mockHandler.Object, mockLauncher.Object);

            mockLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Exactly(2));
        }

        [TestMethod]
        public void StartTestRunShouldAbortOnExceptionInSendMessage()
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var sources = new List<string> { "1.dll" };
            var payload = new TestRunRequestPayload { Sources = sources, RunSettings = null };
            var exception = new IOException();

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.TestRunAllSourcesWithDefaultHost, payload, this.protocolVersion)).Throws(exception);

            this.requestSender.StartTestRun(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null), Times.Once, "Test Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            this.mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
        }

        [TestMethod]
        public async Task StartTestRunAsyncShouldAbortOnExceptionInSendMessage()
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var sources = new List<string> { "1.dll" };
            var payload = new TestRunRequestPayload { Sources = sources, RunSettings = null };
            var exception = new IOException();

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.TestRunAllSourcesWithDefaultHost, payload, this.protocolVersion)).Throws(exception);

            await this.requestSender.StartTestRunAsync(sources, null, null, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null), Times.Once, "Test Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            this.mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
        }

        [TestMethod]
        public void StartTestRunShouldLogErrorOnProcessExited()
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var manualEvent = new ManualResetEvent(false);
            var sources = new List<string> { "1.dll" };
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);
            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            this.requestSender.InitializeCommunication();
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken c) =>
                {
                    Task.Run(() => this.requestSender.OnProcessExited()).Wait();

                    Assert.IsTrue(c.IsCancellationRequested);
                }).Returns(Task.FromResult((Message)null));

            mockHandler.Setup(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null)).Callback(() => manualEvent.Set());

            this.requestSender.StartTestRun(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

            manualEvent.WaitOne();
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task StartTestRunAsyncShouldLogErrorOnProcessExited()
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var sources = new List<string> { "1.dll" };
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);
            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };
            await this.requestSender.InitializeCommunicationAsync(this.WaitTimeout);
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Callback((CancellationToken c) =>
                {
                    Task.Run(() => this.requestSender.OnProcessExited()).Wait();

                    Assert.IsTrue(c.IsCancellationRequested);
                }).Returns(Task.FromResult((Message)null));

            await this.requestSender.StartTestRunAsync(sources, null, null, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region Attachments Processing Tests

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsyncShouldCompleteWithZeroAttachments()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();

            var payload = new TestRunAttachmentsProcessingCompletePayload()
            {
                AttachmentsProcessingCompleteEventArgs = new TestRunAttachmentsProcessingCompleteEventArgs(false, null),
                Attachments = new AttachmentSet[0]
            };

            var attachmentsProcessingComplete = new Message()
            {
                MessageType = MessageType.TestRunAttachmentsProcessingComplete,
                Payload = JToken.FromObject(payload)
            };
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete));
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
            {
                Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
                Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
            });

            await this.requestSender.ProcessTestRunAttachmentsAsync(
                new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
                new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
                Constants.EmptyRunSettings,
                true,
                mockHandler.Object,
                CancellationToken.None);

            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel), Times.Never);
            mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.Is<TestRunAttachmentsProcessingCompleteEventArgs>(a => !a.IsCanceled && a.Error == null), It.Is<ICollection<AttachmentSet>>(a => a.Count == 0)), Times.Once, "Attachments Processing Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsyncShouldCompleteWithOneAttachment()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();

            var payload = new TestRunAttachmentsProcessingCompletePayload()
            {
                AttachmentsProcessingCompleteEventArgs = new TestRunAttachmentsProcessingCompleteEventArgs(true, new Exception("msg")),
                Attachments = new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "out") }
            };
            var attachmentsProcessingComplete = new Message()
            {
                MessageType = MessageType.TestRunAttachmentsProcessingComplete,
                Payload = JToken.FromObject(payload)
            };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete));
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
            {
                Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
                Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
            });

            await this.requestSender.ProcessTestRunAttachmentsAsync(
                new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
                new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
                Constants.EmptyRunSettings,
                true,
                mockHandler.Object,
                CancellationToken.None);

            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel), Times.Never);
            mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.Is<TestRunAttachmentsProcessingCompleteEventArgs>(a => a.IsCanceled && a.Error != null), It.Is<ICollection<AttachmentSet>>(a => a.Count == 1)), Times.Once, "Attachments Processing Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsyncShouldCompleteWithOneAttachmentAndTestMessage()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();

            var payload = new TestRunAttachmentsProcessingCompletePayload()
            {
                AttachmentsProcessingCompleteEventArgs = new TestRunAttachmentsProcessingCompleteEventArgs(false, null),
                Attachments = new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "out") }
            };

            var attachmentsProcessingComplete = new Message()
            {
                MessageType = MessageType.TestRunAttachmentsProcessingComplete,
                Payload = JToken.FromObject(payload)
            };

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
            {
                Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
                Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
            });
            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete)));

            await this.requestSender.ProcessTestRunAttachmentsAsync(
                new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
                new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
                Constants.EmptyRunSettings,
                false,
                mockHandler.Object,
                CancellationToken.None);

            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel), Times.Never);
            mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.IsAny<TestRunAttachmentsProcessingCompleteEventArgs>(), It.Is<ICollection<AttachmentSet>>(a => a.Count == 1)), Times.Once, "Attachments Processing Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Once, "TestMessage event must be called");
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsyncShouldCompleteWithOneAttachmentAndProgressMessage()
        {
            await this.InitializeCommunicationAsync();

            var mockHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();

            var completePayload = new TestRunAttachmentsProcessingCompletePayload()
            {
                AttachmentsProcessingCompleteEventArgs = new TestRunAttachmentsProcessingCompleteEventArgs(false, null),
                Attachments = new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "out") }
            };

            var attachmentsProcessingComplete = new Message()
            {
                MessageType = MessageType.TestRunAttachmentsProcessingComplete,
                Payload = JToken.FromObject(completePayload)
            };

            var progressPayload = new TestRunAttachmentsProcessingProgressPayload()
            {
                AttachmentsProcessingProgressEventArgs = new TestRunAttachmentsProcessingProgressEventArgs(1, new[] { new Uri("http://www.bing.com/") }, 50, 2)
            };

            var attachmentsProcessingProgress = new Message()
            {
                MessageType = MessageType.TestRunAttachmentsProcessingProgress,
                Payload = JToken.FromObject(progressPayload)
            };
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingProgress));
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
            {
                Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
                Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
            });

            mockHandler.Setup(mh => mh.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>())).Callback(
                () => this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete)));

            await this.requestSender.ProcessTestRunAttachmentsAsync(
                new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
                new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
                Constants.EmptyRunSettings,
                false,
                mockHandler.Object,
                CancellationToken.None);

            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel), Times.Never);
            mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.IsAny<TestRunAttachmentsProcessingCompleteEventArgs>(), It.Is<ICollection<AttachmentSet>>(a => a.Count == 1)), Times.Once, "Attachments Processing Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => a.CurrentAttachmentProcessorIndex == 1 && a.CurrentAttachmentProcessorUris.First() == new Uri("http://www.bing.com/") && a.CurrentAttachmentProcessorProgress == 50 && a.AttachmentProcessorsCount == 2)), Times.Once, "Attachments processing Progress must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Never);
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsyncShouldSendCancelMessageIfCancellationTokenCancelled()
        {
            await this.InitializeCommunicationAsync();

            var cts = new CancellationTokenSource();

            var mockHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();

            var payload = new TestRunAttachmentsProcessingCompletePayload()
            {
                Attachments = new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "out") }
            };
            var attachmentsProcessingComplete = new Message()
            {
                MessageType = MessageType.TestRunAttachmentsProcessingComplete,
                Payload = JToken.FromObject(payload)
            };

            var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
            var message = CreateMessage(MessageType.TestMessage, mpayload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
            {
                Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
                Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
            });
            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(() =>
            {
                cts.Cancel();
                this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete));
            });

            await this.requestSender.ProcessTestRunAttachmentsAsync(
                new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
                new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
                Constants.EmptyRunSettings,
                false,
                mockHandler.Object,
                cts.Token);

            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel));
            mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.IsAny<TestRunAttachmentsProcessingCompleteEventArgs>(), It.Is<ICollection<AttachmentSet>>(a => a.Count == 1)), Times.Once, "Attachments Processing Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Once, "TestMessage event must be called");
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsyncShouldSendCancelMessageIfCancellationTokenCancelledAtTheBeginning()
        {
            await this.InitializeCommunicationAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var mockHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();

            var payload = new TestRunAttachmentsProcessingCompletePayload()
            {
                Attachments = new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "out") }
            };
            var attachmentsProcessingComplete = new Message()
            {
                MessageType = MessageType.TestRunAttachmentsProcessingComplete,
                Payload = JToken.FromObject(payload)
            };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete));
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
            {
                Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
                Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
            });

            await this.requestSender.ProcessTestRunAttachmentsAsync(
                new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
                new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
                Constants.EmptyRunSettings,
                true,
                mockHandler.Object,
                cts.Token);

            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
            mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel));
            mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.IsAny<TestRunAttachmentsProcessingCompleteEventArgs>(), It.Is<ICollection<AttachmentSet>>(a => a.Count == 1)), Times.Once, "Attachments Processing Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Never, "TestMessage event must be called");
        }

        [TestMethod]
        public async Task ProcessTestRunAttachmentsAsyncShouldAbortOnExceptionInSendMessage()
        {
            var mockHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>())).Throws(new IOException());

            await this.requestSender.ProcessTestRunAttachmentsAsync(new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "out") }, new List<InvokedDataCollector>(), Constants.EmptyRunSettings, false, mockHandler.Object, CancellationToken.None);

            mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.Is<TestRunAttachmentsProcessingCompleteEventArgs>(a => !a.IsCanceled && a.Error is IOException), null), Times.Once, "Attachments Processing Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            this.mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
        }

        #endregion

        #region Sessions API
        private const int MinimumProtocolVersionWithTestSessionSupport = 5;
        private const int TesthostPid = 5000;

        [TestMethod]
        public void StartTestSessionShouldFailIfWrongProtocolVersionIsNegotiated()
        {
            this.InitializeCommunication(MinimumProtocolVersionWithTestSessionSupport - 1);

            var mockHandler = new Mock<ITestSessionEventsHandler>();
            mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });

            Assert.IsNull(this.requestSender.StartTestSession(
                new List<string>() { "DummyTestAssembly.dll" },
                string.Empty,
                null,
                mockHandler.Object,
                null));

            mockHandler.Verify(mh => mh.HandleStartTestSessionComplete(null), Times.Once);
        }

        [TestMethod]
        public async Task StartTestSessionAsyncShouldFailIfWrongProtocolVersionIsNegotiated()
        {
            await this.InitializeCommunicationAsync(MinimumProtocolVersionWithTestSessionSupport - 1).ConfigureAwait(false);

            var mockHandler = new Mock<ITestSessionEventsHandler>();
            mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });

            Assert.IsNull(await this.requestSender.StartTestSessionAsync(
                new List<string>() { "DummyTestAssembly.dll" },
                string.Empty,
                null,
                mockHandler.Object,
                null).ConfigureAwait(false));

            mockHandler.Verify(mh => mh.HandleStartTestSessionComplete(null), Times.Once);
        }

        [TestMethod]
        public void StartTestSessionShouldSucceed()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestSessionEventsHandler>();
            mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });

            var testSessionInfo = new TestSessionInfo();
            var ackPayload = new Payloads.StartTestSessionAckPayload()
            {
                TestSessionInfo = testSessionInfo
            };
            var message = this.CreateMessage<Payloads.StartTestSessionAckPayload>(
                MessageType.StartTestSessionCallback,
                ackPayload);

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(
                MessageType.StartTestSession,
                It.IsAny<Payloads.StartTestSessionPayload>(),
                this.protocolVersion)).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(message));

            Assert.AreEqual(
                testSessionInfo,
                this.requestSender.StartTestSession(
                    new List<string>() { "DummyTestAssembly.dll" },
                    string.Empty,
                    null,
                    mockHandler.Object,
                    null));
            mockHandler.Verify(mh => mh.HandleStartTestSessionComplete(testSessionInfo), Times.Once);
        }

        [TestMethod]
        public async Task StartTestSessionAsyncShouldSucceed()
        {
            await this.InitializeCommunicationAsync().ConfigureAwait(false);

            var mockHandler = new Mock<ITestSessionEventsHandler>();
            mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });

            var testSessionInfo = new TestSessionInfo();
            var ackPayload = new Payloads.StartTestSessionAckPayload()
            {
                TestSessionInfo = testSessionInfo
            };
            var message = this.CreateMessage<Payloads.StartTestSessionAckPayload>(
                MessageType.StartTestSessionCallback,
                ackPayload);

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(
                MessageType.StartTestSession,
                It.IsAny<Payloads.StartTestSessionPayload>(),
                this.protocolVersion)).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(message));

            Assert.AreEqual(
                testSessionInfo,
                await this.requestSender.StartTestSessionAsync(
                    new List<string>() { "DummyTestAssembly.dll" },
                    string.Empty,
                    null,
                    mockHandler.Object,
                    null).ConfigureAwait(false));
        }

        [TestMethod]
        public void StartTestSessionWithTesthostLauncherShouldSucceed()
        {
            this.InitializeCommunication();

            // Setup
            var mockHandler = new Mock<ITestSessionEventsHandler>();
            mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });
            var mockTesthostLauncher = new Mock<ITestHostLauncher>();
            mockTesthostLauncher.Setup(tl => tl.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Returns(TesthostPid);

            var launchInfo = new TestProcessStartInfo();
            var launchMessage = this.CreateMessage<TestProcessStartInfo>(
                MessageType.CustomTestHostLaunch,
                launchInfo);

            var testSessionInfo = new TestSessionInfo();
            var ackPayload = new Payloads.StartTestSessionAckPayload()
            {
                TestSessionInfo = testSessionInfo
            };
            var ackMessage = this.CreateMessage<Payloads.StartTestSessionAckPayload>(
                MessageType.StartTestSessionCallback,
                ackPayload);

            Action reconfigureAction = () =>
            {
                this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(ackMessage));
            };

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(
                MessageType.StartTestSession,
                It.IsAny<Payloads.StartTestSessionPayload>(),
                this.protocolVersion)).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(
                    MessageType.CustomTestHostLaunchCallback,
                    It.IsAny<CustomHostLaunchAckPayload>(),
                    this.protocolVersion))
                .Callback((string messageType, object payload, int version) =>
                {
                    Assert.AreEqual(((CustomHostLaunchAckPayload)payload).HostProcessId, TesthostPid);
                });
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(launchMessage))
                .Callback(reconfigureAction);

            // Act
            Assert.AreEqual(
                testSessionInfo,
                this.requestSender.StartTestSession(
                    new List<string>() { "DummyTestAssembly.dll" },
                    string.Empty,
                    null,
                    mockHandler.Object,
                    mockTesthostLauncher.Object));

            // Verify
            mockTesthostLauncher.Verify(tl => tl.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once);
            mockHandler.Verify(mh => mh.HandleStartTestSessionComplete(testSessionInfo), Times.Once);
        }

        [TestMethod]
        public async Task StartTestSessionAsyncWithTesthostLauncherShouldSucceed()
        {
            await this.InitializeCommunicationAsync().ConfigureAwait(false);

            // Setup
            var mockHandler = new Mock<ITestSessionEventsHandler>();
            mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });
            var mockTesthostLauncher = new Mock<ITestHostLauncher>();
            mockTesthostLauncher.Setup(tl => tl.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Returns(TesthostPid);

            var launchInfo = new TestProcessStartInfo();
            var launchMessage = this.CreateMessage<TestProcessStartInfo>(
                MessageType.CustomTestHostLaunch,
                launchInfo);

            var testSessionInfo = new TestSessionInfo();
            var ackPayload = new Payloads.StartTestSessionAckPayload()
            {
                TestSessionInfo = testSessionInfo
            };
            var ackMessage = this.CreateMessage<Payloads.StartTestSessionAckPayload>(
                MessageType.StartTestSessionCallback,
                ackPayload);

            Action reconfigureAction = () =>
            {
                this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(ackMessage));
            };

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(
                MessageType.StartTestSession,
                It.IsAny<Payloads.StartTestSessionPayload>(),
                this.protocolVersion)).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(
                    MessageType.CustomTestHostLaunchCallback,
                    It.IsAny<CustomHostLaunchAckPayload>(),
                    this.protocolVersion))
                .Callback((string messageType, object payload, int version) =>
                {
                    Assert.AreEqual(((CustomHostLaunchAckPayload)payload).HostProcessId, TesthostPid);
                });
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(launchMessage))
                .Callback(reconfigureAction);

            // Act
            Assert.AreEqual(
                testSessionInfo,
                await this.requestSender.StartTestSessionAsync(
                    new List<string>() { "DummyTestAssembly.dll" },
                    string.Empty,
                    null,
                    mockHandler.Object,
                    mockTesthostLauncher.Object).ConfigureAwait(false));

            // Verify
            mockTesthostLauncher.Verify(tl => tl.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once);
            mockHandler.Verify(mh => mh.HandleStartTestSessionComplete(testSessionInfo), Times.Once);
        }

        [TestMethod]
        public void StartTestSessionWithTesthostLauncherAttachingToProcessShouldSucceed()
        {
            this.InitializeCommunication();

            // Setup
            var mockHandler = new Mock<ITestSessionEventsHandler>();
            mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });
            var mockTesthostLauncher = new Mock<ITestHostLauncher2>();
            mockTesthostLauncher.Setup(tl => tl.AttachDebuggerToProcess(TesthostPid)).Returns(true);

            var launchMessage = this.CreateMessage<int>(
                MessageType.EditorAttachDebugger,
                TesthostPid);

            var testSessionInfo = new TestSessionInfo();
            var ackPayload = new Payloads.StartTestSessionAckPayload()
            {
                TestSessionInfo = testSessionInfo
            };
            var ackMessage = this.CreateMessage<Payloads.StartTestSessionAckPayload>(
                MessageType.StartTestSessionCallback,
                ackPayload);

            Action reconfigureAction = () =>
            {
                this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(ackMessage));
            };

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(
                MessageType.StartTestSession,
                It.IsAny<Payloads.StartTestSessionPayload>(),
                this.protocolVersion)).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(
                    MessageType.EditorAttachDebuggerCallback,
                    It.IsAny<EditorAttachDebuggerAckPayload>(),
                    this.protocolVersion))
                .Callback((string messageType, object payload, int version) =>
                {
                    Assert.IsTrue(((EditorAttachDebuggerAckPayload)payload).Attached);
                });
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(launchMessage))
                .Callback(reconfigureAction);

            // Act
            Assert.AreEqual(
                testSessionInfo,
                this.requestSender.StartTestSession(
                    new List<string>() { "DummyTestAssembly.dll" },
                    string.Empty,
                    null,
                    mockHandler.Object,
                    mockTesthostLauncher.Object));

            // Verify
            mockTesthostLauncher.Verify(tl => tl.AttachDebuggerToProcess(TesthostPid), Times.Once);
            mockHandler.Verify(mh => mh.HandleStartTestSessionComplete(testSessionInfo), Times.Once);
        }

        [TestMethod]
        public async Task StartTestSessionAsyncWithTesthostLauncherAttachingToProcessShouldSucceed()
        {
            await this.InitializeCommunicationAsync().ConfigureAwait(false);

            // Setup
            var mockHandler = new Mock<ITestSessionEventsHandler>();
            mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });
            var mockTesthostLauncher = new Mock<ITestHostLauncher2>();
            mockTesthostLauncher.Setup(tl => tl.AttachDebuggerToProcess(TesthostPid)).Returns(true);

            var launchMessage = this.CreateMessage<int>(
                MessageType.EditorAttachDebugger,
                TesthostPid);

            var testSessionInfo = new TestSessionInfo();
            var ackPayload = new Payloads.StartTestSessionAckPayload()
            {
                TestSessionInfo = testSessionInfo
            };
            var ackMessage = this.CreateMessage<Payloads.StartTestSessionAckPayload>(
                MessageType.StartTestSessionCallback,
                ackPayload);

            Action reconfigureAction = () =>
            {
                this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(ackMessage));
            };

            this.mockCommunicationManager.Setup(cm => cm.SendMessage(
                MessageType.StartTestSession,
                It.IsAny<Payloads.StartTestSessionPayload>(),
                this.protocolVersion)).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(
                    MessageType.EditorAttachDebuggerCallback,
                    It.IsAny<EditorAttachDebuggerAckPayload>(),
                    this.protocolVersion))
                .Callback((string messageType, object payload, int version) =>
                {
                    Assert.IsTrue(((EditorAttachDebuggerAckPayload)payload).Attached);
                });
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(launchMessage))
                .Callback(reconfigureAction);

            // Act
            Assert.AreEqual(
                testSessionInfo,
                await this.requestSender.StartTestSessionAsync(
                    new List<string>() { "DummyTestAssembly.dll" },
                    string.Empty,
                    null,
                    mockHandler.Object,
                    mockTesthostLauncher.Object).ConfigureAwait(false));

            // Verify
            mockTesthostLauncher.Verify(tl => tl.AttachDebuggerToProcess(TesthostPid), Times.Once);
            mockHandler.Verify(mh => mh.HandleStartTestSessionComplete(testSessionInfo), Times.Once);
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Serialize and Deserialize message as it would happen for real.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageType"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private Message CreateMessage<T>(string messageType, T payload)
        {
            return this.serializer.DeserializeMessage(this.serializer.SerializePayload(messageType, payload, this.protocolVersion));
        }

        private void InitializeCommunication()
        {
            this.InitializeCommunication(this.protocolVersion);
        }

        private void InitializeCommunication(int protocolVersion)
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

            this.mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
                .Callback((int timeout) => Task.Delay(200).Wait());

            var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };
            var versionCheck = new Message() { MessageType = MessageType.VersionCheck, Payload = protocolVersion };

            Action changedMessage = () =>
            {
                this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(versionCheck);
            };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(sessionConnected);
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, It.IsAny<int>())).Callback(changedMessage);

            var portOutput = this.requestSender.InitializeCommunication();
            Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsTrue(connectionSuccess, "Connection must succeed.");
        }

        private void SetupMockCommunicationForRunRequest(Mock<ITestRunEventsHandler> mockHandler)
        {
            this.InitializeCommunication();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, null, TimeSpan.FromMilliseconds(1));
            var dummyLastRunArgs = new TestRunChangedEventArgs(null, null, null);

            var payload = new TestRunCompletePayload()
            {
                ExecutorUris = null,
                LastRunTests = dummyLastRunArgs,
                RunAttachments = null,
                TestRunCompleteArgs = dummyCompleteArgs
            };

            var runComplete = CreateMessage(MessageType.ExecutionComplete, payload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
        }

        private async Task InitializeCommunicationAsync()
        {
            await this.InitializeCommunicationAsync(this.protocolVersion);
        }

        private async Task InitializeCommunicationAsync(int protocolVersion)
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

            var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };
            var versionCheck = new Message() { MessageType = MessageType.VersionCheck, Payload = protocolVersion };

            Action changedMessage = () =>
            {
                this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(versionCheck));
            };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(sessionConnected));
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, It.IsAny<int>())).Callback(changedMessage);

            var portOutput = await this.requestSender.InitializeCommunicationAsync(this.WaitTimeout);
            Assert.AreEqual(dummyPortInput, portOutput, "Connection must succeed.");
        }

        #endregion
    }
}
