// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json.Linq;

using TestResult = VisualStudio.TestPlatform.ObjectModel.TestResult;
using Payloads = VisualStudio.TestPlatform.ObjectModel.Client.Payloads;

[TestClass]
public class VsTestConsoleRequestSenderTests
{
    private readonly ITranslationLayerRequestSender _requestSender;

    private readonly Mock<ICommunicationManager> _mockCommunicationManager;

    private readonly int _waitTimeout = 2000;

    private readonly int _protocolVersion = 5;
    private readonly IDataSerializer _serializer = JsonDataSerializer.Instance;

    public VsTestConsoleRequestSenderTests()
    {
        _mockCommunicationManager = new Mock<ICommunicationManager>();
        _requestSender = new VsTestConsoleRequestSender(
            _mockCommunicationManager.Object,
            JsonDataSerializer.Instance,
            new Mock<ITestPlatformEventSource>().Object);
    }

    #region Communication Tests

    [TestMethod]
    public void InitializeCommunicationShouldSucceed()
    {
        InitializeCommunication();

        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Exactly(2));
        _mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, _protocolVersion), Times.Once);
    }

    [TestMethod]
    public async Task InitializeCommunicationAsyncShouldSucceed()
    {
        await InitializeCommunicationAsync();

        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, _protocolVersion), Times.Once);
    }

    [TestMethod]
    public void InitializeCommunicationShouldReturnInvalidPortNumberIfHostServerFails()
    {
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Throws(new Exception("Fail"));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false));

        var portOutput = _requestSender.InitializeCommunication();
        Assert.IsTrue(portOutput < 0, "Negative port number must be returned if Hosting Server fails.");

        var connectionSuccess = _requestSender.WaitForRequestHandlerConnection(_waitTimeout);
        Assert.IsFalse(connectionSuccess, "Connection must fail as server failed to host.");

        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Never);
        _mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Never);
        _mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Never);
    }

    [TestMethod]
    public async Task InitializeCommunicationAsyncShouldReturnInvalidPortNumberIfHostServerFails()
    {
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Throws(new Exception("Fail"));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false));

        var portOutput = await _requestSender.InitializeCommunicationAsync(_waitTimeout);
        Assert.IsTrue(portOutput < 0, "Negative port number must be returned if Hosting Server fails.");

        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Never);
        _mockCommunicationManager.Verify(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public void InitializeCommunicationShouldFailConnectionIfMessageReceiveFailed()
    {
        var dummyPortInput = 123;
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });
        _mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
            .Callback((int timeout) => Task.Delay(200).Wait());
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Throws(new Exception("Fail"));

        var portOutput = _requestSender.InitializeCommunication();

        // Hosting server didn't server, so port number should still be valid
        Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");

        // Connection must not succeed as handshake failed
        var connectionSuccess = _requestSender.WaitForRequestHandlerConnection(_waitTimeout);
        Assert.IsFalse(connectionSuccess, "Connection must fail if handshake failed.");
        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);
    }

    [TestMethod]
    public async Task InitializeCommunicationAsyncShouldFailConnectionIfMessageReceiveFailed()
    {
        var dummyPortInput = 123;
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Fail"));

        var portOutput = await _requestSender.InitializeCommunicationAsync(_waitTimeout);

        // Connection must not succeed as handshake failed
        Assert.AreEqual(-1, portOutput, "Connection must fail if handshake failed.");
        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);
    }

    [TestMethod]
    public void InitializeCommunicationShouldFailConnectionIfSessionConnectedDidNotComeFirst()
    {
        var dummyPortInput = 123;
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });
        _mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
            .Callback((int timeout) => Task.Delay(200).Wait());

        var discoveryMessage = new Message() { MessageType = MessageType.StartDiscovery };

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(discoveryMessage);

        var portOutput = _requestSender.InitializeCommunication();
        Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
        var connectionSuccess = _requestSender.WaitForRequestHandlerConnection(_waitTimeout);
        Assert.IsFalse(connectionSuccess, "Connection must fail if version check failed.");

        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

        _mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);

        _mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, _protocolVersion), Times.Never);
    }

    [TestMethod]
    public async Task InitializeCommunicationAsyncShouldFailConnectionIfSessionConnectedDidNotComeFirst()
    {
        var dummyPortInput = 123;
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

        var discoveryMessage = new Message() { MessageType = MessageType.StartDiscovery };

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryMessage));

        var portOutput = await _requestSender.InitializeCommunicationAsync(_waitTimeout);
        Assert.AreEqual(-1, portOutput, "Connection must fail if version check failed.");

        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

        _mockCommunicationManager.Verify(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, _protocolVersion), Times.Never);
    }

    [TestMethod]
    public void InitializeCommunicationShouldFailConnectionIfSendMessageFailed()
    {
        var dummyPortInput = 123;
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });
        _mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
            .Callback((int timeout) => Task.Delay(200).Wait());

        var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(sessionConnected);
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, _protocolVersion)).Throws(new Exception("Fail"));

        var portOutput = _requestSender.InitializeCommunication();
        Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
        var connectionSuccess = _requestSender.WaitForRequestHandlerConnection(_waitTimeout);
        Assert.IsFalse(connectionSuccess, "Connection must fail if version check failed.");

        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

        _mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);

        _mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, _protocolVersion), Times.Once);
    }

    [TestMethod]
    public async Task InitializeCommunicationAsyncShouldFailConnectionIfSendMessageFailed()
    {
        var dummyPortInput = 123;
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

        var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(sessionConnected));
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, _protocolVersion)).Throws(new Exception("Fail"));

        var portOutput = await _requestSender.InitializeCommunicationAsync(_waitTimeout);
        Assert.AreEqual(-1, portOutput, "Connection must fail if version check failed.");

        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

        _mockCommunicationManager.Verify(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, _protocolVersion), Times.Once);
    }

    [TestMethod]
    public void InitializeCommunicationShouldFailConnectionIfProtocolIsNotCompatible()
    {
        var dummyPortInput = 123;
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

        _mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
            .Callback((int timeout) => Task.Delay(200).Wait());

        var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };

        // Give wrong version
        var protocolError = new Message()
        {
            MessageType = MessageType.ProtocolError,
            Payload = null
        };

        Action changedMessage =
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(protocolError);

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(sessionConnected);
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck)).Callback(changedMessage);

        var portOutput = _requestSender.InitializeCommunication();
        Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
        var connectionSuccess = _requestSender.WaitForRequestHandlerConnection(_waitTimeout);
        Assert.IsFalse(connectionSuccess, "Connection must fail if version check failed.");

        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

        _mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);

        _mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Exactly(2));
        _mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, _protocolVersion), Times.Once);
    }

    [TestMethod]
    public async Task InitializeCommunicationAsyncShouldFailConnectionIfProtocolIsNotCompatible()
    {
        var dummyPortInput = 123;
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

        var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };

        // Give wrong version
        var protocolError = new Message()
        {
            MessageType = MessageType.ProtocolError,
            Payload = null
        };

        Action changedMessage =
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(protocolError));

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(sessionConnected));
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck)).Callback(changedMessage);

        var portOutput = await _requestSender.InitializeCommunicationAsync(_waitTimeout);
        Assert.AreEqual(-1, portOutput, "Connection must fail if version check failed.");

        _mockCommunicationManager.Verify(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0)), Times.Once);
        _mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

        _mockCommunicationManager.Verify(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, _protocolVersion), Times.Once);
    }

    #endregion

    #region Discovery Tests

    [TestMethod]
    public void DiscoverTestsShouldCompleteWithZeroTests()
    {
        InitializeCommunication();

        var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

        var payload = new DiscoveryCompletePayload() { TotalTests = 0, LastDiscoveredTests = null, IsAborted = false };
        var discoveryComplete = new Message()
        {
            MessageType = MessageType.DiscoveryComplete,
            Payload = JToken.FromObject(payload)
        };
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete));

        _requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
        mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Never, "DiscoveredTests must not be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
    }

    [TestMethod]
    public async Task DiscoverTestsAsyncShouldCompleteWithZeroTests()
    {
        await InitializeCommunicationAsync();

        var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

        var payload = new DiscoveryCompletePayload() { TotalTests = 0, LastDiscoveredTests = null, IsAborted = false };
        var discoveryComplete = new Message()
        {
            MessageType = MessageType.DiscoveryComplete,
            Payload = JToken.FromObject(payload)
        };
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete));

        await _requestSender.DiscoverTestsAsync(new List<string>() { "1.dll" }, null, null, null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
        mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Never, "DiscoveredTests must not be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
    }

    [TestMethod]
    public void DiscoverTestsShouldCompleteWithSingleTest()
    {
        InitializeCommunication();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
        mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete)));

        _requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
        mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Once, "DiscoveredTests must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
    }

    [TestMethod]
    public async Task DiscoverTestsAsyncShouldCompleteWithSingleTest()
    {
        await InitializeCommunicationAsync();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
        mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete)));

        await _requestSender.DiscoverTestsAsync(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
        mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Once, "DiscoveredTests must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
    }

    [TestMethod]
    public void DiscoverTestsShouldReportBackTestsWithTraitsInTestsFoundMessage()
    {
        InitializeCommunication();

        var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

        var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
        testCase.Traits.Add(new Trait("a", "b"));

        List<TestCase> receivedTestCases = null;
        var testCaseList = new List<TestCase>() { testCase };
        var testsFound = CreateMessage(MessageType.TestCasesFound, testCaseList);

        var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = null, IsAborted = false };
        var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
        mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()))
            .Callback(
                (IEnumerable<TestCase> tests) =>
                {
                    receivedTestCases = tests?.ToList();
                    _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult((discoveryComplete)));
                });

        _requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        await InitializeCommunicationAsync();

        var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

        var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
        testCase.Traits.Add(new Trait("a", "b"));

        List<TestCase> receivedTestCases = null;
        var testCaseList = new List<TestCase>() { testCase };
        var testsFound = CreateMessage(MessageType.TestCasesFound, testCaseList);

        var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = null, IsAborted = false };
        var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
        mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()))
            .Callback(
                (IEnumerable<TestCase> tests) =>
                {
                    receivedTestCases = tests?.ToList();
                    _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult((discoveryComplete)));
                });

        await _requestSender.DiscoverTestsAsync(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        InitializeCommunication();

        var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

        var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
        testCase.Traits.Add(new Trait("a", "b"));

        List<TestCase> receivedTestCases = null;
        var testCaseList = new List<TestCase>() { testCase };

        var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = testCaseList, IsAborted = false };
        var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete));
        mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), It.IsAny<IEnumerable<TestCase>>()))
            .Callback(
                (DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> tests) => receivedTestCases = tests?.ToList());

        _requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        await InitializeCommunicationAsync();

        var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

        var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
        testCase.Traits.Add(new Trait("a", "b"));

        List<TestCase> receivedTestCases = null;
        var testCaseList = new List<TestCase>() { testCase };

        var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = testCaseList, IsAborted = false };
        var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete));
        mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), It.IsAny<IEnumerable<TestCase>>()))
            .Callback(
                (DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> tests) => receivedTestCases = tests?.ToList());

        await _requestSender.DiscoverTestsAsync(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        InitializeCommunication();

        var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

        var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
        var testCaseList = new List<TestCase>() { testCase };
        var testsFound = CreateMessage(MessageType.TestCasesFound, testCaseList);

        var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = null, IsAborted = false };
        var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

        var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
        var message = CreateMessage(MessageType.TestMessage, mpayload);

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
        mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message)));
        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete)));

        _requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
        mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Once, "DiscoveredTests must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Once, "TestMessage event must be called");
    }

    [TestMethod]
    public async Task DiscoverTestsAsyncShouldCompleteWithTestMessage()
    {
        await InitializeCommunicationAsync();

        var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();

        var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
        var testCaseList = new List<TestCase>() { testCase };
        var testsFound = CreateMessage(MessageType.TestCasesFound, testCaseList);

        var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = null, IsAborted = false };
        var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

        var mpayload = new TestMessagePayload() { MessageLevel = TestMessageLevel.Informational, Message = "Hello" };
        var message = CreateMessage(MessageType.TestMessage, mpayload);

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsFound));
        mockHandler.Setup(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message)));
        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete)));

        await _requestSender.DiscoverTestsAsync(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.StartDiscovery, payload)).Throws(new IOException());

        _requestSender.DiscoverTests(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        _mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
    }

    [TestMethod]
    public async Task DiscoverTestsAsyncShouldAbortOnExceptionInSendMessage()
    {
        var mockHandler = new Mock<ITestDiscoveryEventsHandler2>();
        var sources = new List<string> { "1.dll" };
        var payload = new DiscoveryRequestPayload { Sources = sources, RunSettings = null };
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.StartDiscovery, payload)).Throws(new IOException());

        await _requestSender.DiscoverTestsAsync(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null), Times.Once, "Discovery Complete must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        _mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
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
        _requestSender.InitializeCommunication();
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Callback(
            (CancellationToken c) =>
            {
                Task.Run(() => _requestSender.OnProcessExited(), c).Wait(c);

                Assert.IsTrue(c.IsCancellationRequested);
            }).Returns(Task.FromResult((Message)null));

        mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<DiscoveryCompleteEventArgs>(), null)).Callback(() => manualEvent.Set());

        _requestSender.DiscoverTests(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        await _requestSender.InitializeCommunicationAsync(_waitTimeout);
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Callback(
            (CancellationToken c) =>
            {
                Task.Run(() => _requestSender.OnProcessExited(), c).Wait(c);

                Assert.IsTrue(c.IsCancellationRequested);
            }).Returns(Task.FromResult((Message)null));

        await _requestSender.DiscoverTestsAsync(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region RunTests

    [TestMethod]
    public void StartTestRunShouldCompleteWithZeroTests()
    {
        InitializeCommunication();

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
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

        _requestSender.StartTestRun(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Never, "RunChangedArgs must not be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
    }

    [TestMethod]
    public async Task StartTestRunAsyncShouldCompleteWithZeroTests()
    {
        await InitializeCommunicationAsync();

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
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

        await _requestSender.StartTestRunAsync(new List<string>() { "1.dll" }, null, null, null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Never, "RunChangedArgs must not be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
    }

    [TestMethod]
    public void StartTestRunShouldCompleteWithSingleTestAndMessage()
    {
        InitializeCommunication();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload));

        mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
            (testRunChangedArgs) =>
            {
                Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
            });

        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

        _requestSender.StartTestRun(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
    }

    [TestMethod]
    public async Task StartTestRunAsyncShouldCompleteWithSingleTestAndMessage()
    {
        await InitializeCommunicationAsync();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload));

        mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
            (testRunChangedArgs) =>
            {
                Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
            });

        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

        await _requestSender.StartTestRunAsync(new List<string>() { "1.dll" }, null, null, null, mockHandler.Object);

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

        SetupMockCommunicationForRunRequest(mockHandler);
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.TestRunAllSourcesWithDefaultHost, It.IsAny<TestRunRequestPayload>(), It.IsAny<int>())).
            Callback((string msg, object requestpayload, int protocol) => receivedRequest = (TestRunRequestPayload)requestpayload);

        // Act.
        _requestSender.StartTestRun(sources, null, null, null, mockHandler.Object);

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

        SetupMockCommunicationForRunRequest(mockHandler);
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.TestRunAllSourcesWithDefaultHost, It.IsAny<TestRunRequestPayload>(), It.IsAny<int>())).
            Callback((string msg, object requestpayload, int protocol) => receivedRequest = (TestRunRequestPayload)requestpayload);

        // Act.
        _requestSender.StartTestRun(sources, null, new TestPlatformOptions() { TestCaseFilter = filter }, null, mockHandler.Object);

        // Assert.
        Assert.IsNotNull(receivedRequest);
        CollectionAssert.AreEqual(sources, receivedRequest.Sources);
        Assert.AreEqual(filter, receivedRequest.TestPlatformOptions.TestCaseFilter, "The run request message should include test case filter");
    }

    [TestMethod]
    public void StartTestRunWithCustomHostShouldComplete()
    {
        InitializeCommunication();

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
                _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
            });

        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

        var mockLauncher = new Mock<ITestHostLauncher>();
        mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Callback
            (() => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload)));

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

        _requestSender.StartTestRunWithCustomHost(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object, mockLauncher.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        mockLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once, "Custom TestHostLauncher must be called");
    }

    [TestMethod]
    public async Task StartTestRunAsyncWithCustomHostShouldComplete()
    {
        await InitializeCommunicationAsync();

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
                _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
            });

        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

        var mockLauncher = new Mock<ITestHostLauncher>();
        mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Callback
            (() => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload)));

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

        await _requestSender.StartTestRunWithCustomHostAsync(new List<string>() { "1.dll" }, null, null, null, mockHandler.Object, mockLauncher.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        mockLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once, "Custom TestHostLauncher must be called");
    }

    [TestMethod]
    public void StartTestRunWithCustomHostShouldNotAbortAndSendErrorToVstestConsoleInErrorScenario()
    {
        InitializeCommunication();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

        _mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>(), _protocolVersion)).
            Callback(() => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

        _requestSender.StartTestRunWithCustomHost(new List<string>() { "1.dll" }, null, new TestPlatformOptions(), null, mockHandler.Object, mockLauncher.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
    }

    [TestMethod]
    public async Task StartTestRunAsyncWithCustomHostShouldNotAbortAndSendErrorToVstestConsoleInErrorScenario()
    {
        await InitializeCommunicationAsync();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

        _mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>(), _protocolVersion)).
            Callback(() => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

        await _requestSender.StartTestRunWithCustomHostAsync(new List<string>() { "1.dll" }, null, null, null, mockHandler.Object, mockLauncher.Object);

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

        SetupMockCommunicationForRunRequest(mockHandler);
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.GetTestRunnerProcessStartInfoForRunAll, It.IsAny<TestRunRequestPayload>(), It.IsAny<int>())).
            Callback((string msg, object requestpayload, int protocol) => receivedRequest = (TestRunRequestPayload)requestpayload);

        // Act.
        _requestSender.StartTestRunWithCustomHost(sources, null, null, null, mockHandler.Object, new Mock<ITestHostLauncher>().Object);

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

        SetupMockCommunicationForRunRequest(mockHandler);
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.GetTestRunnerProcessStartInfoForRunAll, It.IsAny<TestRunRequestPayload>(), It.IsAny<int>())).
            Callback((string msg, object requestpayload, int protocol) => receivedRequest = (TestRunRequestPayload)requestpayload);

        // Act.
        _requestSender.StartTestRunWithCustomHost(sources, null, new TestPlatformOptions() { TestCaseFilter = filter }, null, mockHandler.Object, new Mock<ITestHostLauncher>().Object);

        // Assert.
        Assert.IsNotNull(receivedRequest);
        CollectionAssert.AreEqual(sources, receivedRequest.Sources);
        Assert.AreEqual(filter, receivedRequest.TestPlatformOptions.TestCaseFilter, "The run request message should include test case filter");
    }

    [TestMethod]
    public void StartTestRunWithSelectedTestsShouldCompleteWithZeroTests()
    {
        InitializeCommunication();

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
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

        _requestSender.StartTestRun(new List<TestCase>(), null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Never, "RunChangedArgs must not be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
    }

    [TestMethod]
    public async Task StartTestRunAsyncWithSelectedTestsShouldCompleteWithZeroTests()
    {
        await InitializeCommunicationAsync();

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
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

        await _requestSender.StartTestRunAsync(new List<TestCase>(), null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Never, "RunChangedArgs must not be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
    }

    [TestMethod]
    public void StartTestRunWithSelectedTestsShouldCompleteWithSingleTestAndMessage()
    {
        InitializeCommunication();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload));

        mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
            (testRunChangedArgs) =>
            {
                Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
            });

        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

        _requestSender.StartTestRun(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
    }

    [TestMethod]
    public async Task StartTestRunAsyncWithSelectedTestsShouldCompleteWithSingleTestAndMessage()
    {
        await InitializeCommunicationAsync();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload));

        mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
            (testRunChangedArgs) =>
            {
                Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
            });

        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

        await _requestSender.StartTestRunAsync(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
    }

    [TestMethod]
    public void StartTestRunWithSelectedTestsHavingTraitsShouldReturnTestRunCompleteWithTraitsIntact()
    {
        InitializeCommunication();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

        mockHandler.Setup(mh => mh.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>()))
            .Callback(
                (TestRunCompleteEventArgs complete,
                    TestRunChangedEventArgs stats,
                    ICollection<AttachmentSet> attachments,
                    ICollection<string> executorUris) => receivedChangeEventArgs = stats);

        _requestSender.StartTestRun(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        await InitializeCommunicationAsync();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));

        mockHandler.Setup(mh => mh.HandleTestRunComplete(
                It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(),
                It.IsAny<ICollection<AttachmentSet>>(),
                It.IsAny<ICollection<string>>()))
            .Callback(
                (TestRunCompleteEventArgs complete,
                    TestRunChangedEventArgs stats,
                    ICollection<AttachmentSet> attachments,
                    ICollection<string> executorUris) => receivedChangeEventArgs = stats);

        await _requestSender.StartTestRunAsync(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        InitializeCommunication();

        var mockHandler = new Mock<ITestRunEventsHandler>();

        var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
        testCase.Traits.Add(new Trait("a", "b"));

        var testResult = new TestResult(testCase);
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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsRunStatsPayload));

        mockHandler.Setup(mh => mh.HandleTestRunStatsChange(
                It.IsAny<TestRunChangedEventArgs>()))
            .Callback(
                (TestRunChangedEventArgs stats) =>
                {
                    receivedChangeEventArgs = stats;
                    _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

        _requestSender.StartTestRun(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        await InitializeCommunicationAsync();

        var mockHandler = new Mock<ITestRunEventsHandler>();

        var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
        testCase.Traits.Add(new Trait("a", "b"));

        var testResult = new TestResult(testCase);
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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsRunStatsPayload));

        mockHandler.Setup(mh => mh.HandleTestRunStatsChange(
                It.IsAny<TestRunChangedEventArgs>()))
            .Callback(
                (TestRunChangedEventArgs stats) =>
                {
                    receivedChangeEventArgs = stats;
                    _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

        await _requestSender.StartTestRunAsync(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        InitializeCommunication();

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
        var runprocessInfoPayload = CreateMessage(MessageType.CustomTestHostLaunch, new TestProcessStartInfo());

        mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
            (testRunChangedArgs) =>
            {
                Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
            });

        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

        var mockLauncher = new Mock<ITestHostLauncher>();
        mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Callback
            (() => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload)));

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

        _requestSender.StartTestRunWithCustomHost(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object, mockLauncher.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
            It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        mockLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Once, "Custom TestHostLauncher must be called");
    }

    [TestMethod]
    public async Task StartTestRunWithCustomHostAsyncWithSelectedTestsShouldComplete()
    {
        await InitializeCommunicationAsync();

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
        var runprocessInfoPayload = CreateMessage(MessageType.CustomTestHostLaunch, new TestProcessStartInfo());

        mockHandler.Setup(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>())).Callback<TestRunChangedEventArgs>(
            (testRunChangedArgs) =>
            {
                Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Any(), "TestResults must be passed properly");
                _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
            });

        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete)));

        var mockLauncher = new Mock<ITestHostLauncher>();
        mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Callback
            (() => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(testsPayload)));

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runprocessInfoPayload));

        await _requestSender.StartTestRunWithCustomHostAsync(testCaseList, null, new TestPlatformOptions(), null, mockHandler.Object, mockLauncher.Object);

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message1));
        mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()))
            .Callback<TestProcessStartInfo>((startInfo) =>
            {
                if (startInfo.FileName.Equals(p1.FileName))
                {
                    _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message2));
                }
                else if (startInfo.FileName.Equals(p2.FileName))
                {
                    _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                }
            });
        _requestSender.InitializeCommunication();
        _requestSender.StartTestRunWithCustomHost(sources, null, new TestPlatformOptions(), null, mockHandler.Object, mockLauncher.Object);

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message1));
        mockLauncher.Setup(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()))
            .Callback<TestProcessStartInfo>((startInfo) =>
            {
                if (startInfo.FileName.Equals(p1.FileName))
                {
                    _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message2));
                }
                else if (startInfo.FileName.Equals(p2.FileName))
                {
                    _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                }
            });

        await _requestSender.InitializeCommunicationAsync(_waitTimeout);
        await _requestSender.StartTestRunWithCustomHostAsync(sources, null, null, null, mockHandler.Object, mockLauncher.Object);

        mockLauncher.Verify(ml => ml.LaunchTestHost(It.IsAny<TestProcessStartInfo>()), Times.Exactly(2));
    }

    [TestMethod]
    public void StartTestRunShouldAbortOnExceptionInSendMessage()
    {
        var mockHandler = new Mock<ITestRunEventsHandler>();
        var sources = new List<string> { "1.dll" };
        var payload = new TestRunRequestPayload { Sources = sources, RunSettings = null };
        var exception = new IOException();

        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.TestRunAllSourcesWithDefaultHost, payload, _protocolVersion)).Throws(exception);

        _requestSender.StartTestRun(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null), Times.Once, "Test Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        _mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
    }

    [TestMethod]
    public async Task StartTestRunAsyncShouldAbortOnExceptionInSendMessage()
    {
        var mockHandler = new Mock<ITestRunEventsHandler>();
        var sources = new List<string> { "1.dll" };
        var payload = new TestRunRequestPayload { Sources = sources, RunSettings = null };
        var exception = new IOException();

        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.TestRunAllSourcesWithDefaultHost, payload, _protocolVersion)).Throws(exception);

        await _requestSender.StartTestRunAsync(sources, null, null, null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null), Times.Once, "Test Run Complete must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        _mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
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
        _requestSender.InitializeCommunication();
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Callback((CancellationToken c) =>
            {
                Task.Run(() => _requestSender.OnProcessExited(), c).Wait(c);

                Assert.IsTrue(c.IsCancellationRequested);
            }).Returns(Task.FromResult((Message)null));

        mockHandler.Setup(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null)).Callback(() => manualEvent.Set());

        _requestSender.StartTestRun(sources, null, new TestPlatformOptions(), null, mockHandler.Object);

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
        await _requestSender.InitializeCommunicationAsync(_waitTimeout);
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Callback((CancellationToken c) =>
            {
                Task.Run(() => _requestSender.OnProcessExited(), c).Wait(c);

                Assert.IsTrue(c.IsCancellationRequested);
            }).Returns(Task.FromResult((Message)null));

        await _requestSender.StartTestRunAsync(sources, null, null, null, mockHandler.Object);

        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Attachments Processing Tests

    [TestMethod]
    public async Task ProcessTestRunAttachmentsAsyncShouldCompleteWithZeroAttachments()
    {
        await InitializeCommunicationAsync();

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
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete));
        _mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
        {
            Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
            Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
        });

        await _requestSender.ProcessTestRunAttachmentsAsync(
            new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
            new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), "sample", typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
            Constants.EmptyRunSettings,
            true,
            mockHandler.Object,
            CancellationToken.None);

        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel), Times.Never);
        mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.Is<TestRunAttachmentsProcessingCompleteEventArgs>(a => !a.IsCanceled && a.Error == null), It.Is<ICollection<AttachmentSet>>(a => a.Count == 0)), Times.Once, "Attachments Processing Complete must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
    }

    [TestMethod]
    public async Task ProcessTestRunAttachmentsAsyncShouldCompleteWithOneAttachment()
    {
        await InitializeCommunicationAsync();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete));
        _mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
        {
            Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
            Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
        });

        await _requestSender.ProcessTestRunAttachmentsAsync(
            new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
            new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), "sample", typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
            Constants.EmptyRunSettings,
            true,
            mockHandler.Object,
            CancellationToken.None);

        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel), Times.Never);
        mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.Is<TestRunAttachmentsProcessingCompleteEventArgs>(a => a.IsCanceled && a.Error != null), It.Is<ICollection<AttachmentSet>>(a => a.Count == 1)), Times.Once, "Attachments Processing Complete must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
    }

    [TestMethod]
    public async Task ProcessTestRunAttachmentsAsyncShouldCompleteWithOneAttachmentAndTestMessage()
    {
        await InitializeCommunicationAsync();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
        _mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
        {
            Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
            Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
        });
        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete)));

        await _requestSender.ProcessTestRunAttachmentsAsync(
            new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
            new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), "sample", typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
            Constants.EmptyRunSettings,
            false,
            mockHandler.Object,
            CancellationToken.None);

        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel), Times.Never);
        mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.IsAny<TestRunAttachmentsProcessingCompleteEventArgs>(), It.Is<ICollection<AttachmentSet>>(a => a.Count == 1)), Times.Once, "Attachments Processing Complete must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Once, "TestMessage event must be called");
    }

    [TestMethod]
    public async Task ProcessTestRunAttachmentsAsyncShouldCompleteWithOneAttachmentAndProgressMessage()
    {
        await InitializeCommunicationAsync();

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
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingProgress));
        _mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
        {
            Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
            Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
        });

        mockHandler.Setup(mh => mh.HandleTestRunAttachmentsProcessingProgress(It.IsAny<TestRunAttachmentsProcessingProgressEventArgs>())).Callback(
            () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete)));

        await _requestSender.ProcessTestRunAttachmentsAsync(
            new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
            new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), "sample", typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
            Constants.EmptyRunSettings,
            false,
            mockHandler.Object,
            CancellationToken.None);

        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel), Times.Never);
        mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.IsAny<TestRunAttachmentsProcessingCompleteEventArgs>(), It.Is<ICollection<AttachmentSet>>(a => a.Count == 1)), Times.Once, "Attachments Processing Complete must be called");
        mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingProgress(It.Is<TestRunAttachmentsProcessingProgressEventArgs>(a => a.CurrentAttachmentProcessorIndex == 1 && a.CurrentAttachmentProcessorUris.First() == new Uri("http://www.bing.com/") && a.CurrentAttachmentProcessorProgress == 50 && a.AttachmentProcessorsCount == 2)), Times.Once, "Attachments processing Progress must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Never);
    }

    [TestMethod]
    public async Task ProcessTestRunAttachmentsAsyncShouldSendCancelMessageIfCancellationTokenCancelled()
    {
        await InitializeCommunicationAsync();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
        _mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
        {
            Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
            Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
        });
        mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(() =>
        {
            cts.Cancel();
            _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete));
        });

        await _requestSender.ProcessTestRunAttachmentsAsync(
            new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
            new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), "sample", typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
            Constants.EmptyRunSettings,
            false,
            mockHandler.Object,
            cts.Token);

        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel));
        mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.IsAny<TestRunAttachmentsProcessingCompleteEventArgs>(), It.Is<ICollection<AttachmentSet>>(a => a.Count == 1)), Times.Once, "Attachments Processing Complete must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Once, "TestMessage event must be called");
    }

    [TestMethod]
    public async Task ProcessTestRunAttachmentsAsyncShouldSendCancelMessageIfCancellationTokenCancelledAtTheBeginning()
    {
        await InitializeCommunicationAsync();

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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(attachmentsProcessingComplete));
        _mockCommunicationManager.Setup(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>())).Callback((string _, object o) =>
        {
            Assert.AreEqual(Constants.EmptyRunSettings, ((TestRunAttachmentsProcessingPayload)o).RunSettings);
            Assert.AreEqual(1, ((TestRunAttachmentsProcessingPayload)o).InvokedDataCollectors.Count());
        });

        await _requestSender.ProcessTestRunAttachmentsAsync(
            new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "a") },
            new List<InvokedDataCollector>() { new InvokedDataCollector(new Uri("datacollector://sample"), "sample", typeof(string).AssemblyQualifiedName, typeof(string).Assembly.Location, false) },
            Constants.EmptyRunSettings,
            true,
            mockHandler.Object,
            cts.Token);

        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>()));
        _mockCommunicationManager.Verify(c => c.SendMessage(MessageType.TestRunAttachmentsProcessingCancel));
        mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.IsAny<TestRunAttachmentsProcessingCompleteEventArgs>(), It.Is<ICollection<AttachmentSet>>(a => a.Count == 1)), Times.Once, "Attachments Processing Complete must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Never, "TestMessage event must be called");
    }

    [TestMethod]
    public async Task ProcessTestRunAttachmentsAsyncShouldAbortOnExceptionInSendMessage()
    {
        var mockHandler = new Mock<ITestRunAttachmentsProcessingEventsHandler>();
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.TestRunAttachmentsProcessingStart, It.IsAny<object>())).Throws(new IOException());

        await _requestSender.ProcessTestRunAttachmentsAsync(new List<AttachmentSet> { new AttachmentSet(new Uri("http://www.bing.com"), "out") }, new List<InvokedDataCollector>(), Constants.EmptyRunSettings, false, mockHandler.Object, CancellationToken.None);

        mockHandler.Verify(mh => mh.HandleTestRunAttachmentsProcessingComplete(It.Is<TestRunAttachmentsProcessingCompleteEventArgs>(a => !a.IsCanceled && a.Error is IOException), null), Times.Once, "Attachments Processing Complete must be called");
        mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        _mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
    }

    #endregion

    #region Sessions API
    private const int MinimumProtocolVersionWithTestSessionSupport = 5;
    private const int TesthostPid = 5000;

    [TestMethod]
    public void StartTestSessionShouldFailIfWrongProtocolVersionIsNegotiated()
    {
        InitializeCommunication(MinimumProtocolVersionWithTestSessionSupport - 1);

        var mockHandler = new Mock<ITestSessionEventsHandler>();
        mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });

        Assert.IsNull(_requestSender.StartTestSession(
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
        await InitializeCommunicationAsync(MinimumProtocolVersionWithTestSessionSupport - 1).ConfigureAwait(false);

        var mockHandler = new Mock<ITestSessionEventsHandler>();
        mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });

        Assert.IsNull(await _requestSender.StartTestSessionAsync(
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
        InitializeCommunication();

        var mockHandler = new Mock<ITestSessionEventsHandler>();
        mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });

        var testSessionInfo = new TestSessionInfo();
        var ackPayload = new Payloads.StartTestSessionAckPayload()
        {
            TestSessionInfo = testSessionInfo
        };
        var message = CreateMessage(
            MessageType.StartTestSessionCallback,
            ackPayload);

        _mockCommunicationManager.Setup(cm => cm.SendMessage(
            MessageType.StartTestSession,
            It.IsAny<Payloads.StartTestSessionPayload>(),
            _protocolVersion)).Callback(() => { });
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(message));

        Assert.AreEqual(
            testSessionInfo,
            _requestSender.StartTestSession(
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
        await InitializeCommunicationAsync().ConfigureAwait(false);

        var mockHandler = new Mock<ITestSessionEventsHandler>();
        mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });

        var testSessionInfo = new TestSessionInfo();
        var ackPayload = new Payloads.StartTestSessionAckPayload()
        {
            TestSessionInfo = testSessionInfo
        };
        var message = CreateMessage(
            MessageType.StartTestSessionCallback,
            ackPayload);

        _mockCommunicationManager.Setup(cm => cm.SendMessage(
            MessageType.StartTestSession,
            It.IsAny<Payloads.StartTestSessionPayload>(),
            _protocolVersion)).Callback(() => { });
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(message));

        Assert.AreEqual(
            testSessionInfo,
            await _requestSender.StartTestSessionAsync(
                new List<string>() { "DummyTestAssembly.dll" },
                string.Empty,
                null,
                mockHandler.Object,
                null).ConfigureAwait(false));
    }

    [TestMethod]
    public void StartTestSessionWithTesthostLauncherShouldSucceed()
    {
        InitializeCommunication();

        // Setup
        var mockHandler = new Mock<ITestSessionEventsHandler>();
        mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });
        var mockTesthostLauncher = new Mock<ITestHostLauncher>();
        mockTesthostLauncher.Setup(tl => tl.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Returns(TesthostPid);

        var launchInfo = new TestProcessStartInfo();
        var launchMessage = CreateMessage(
            MessageType.CustomTestHostLaunch,
            launchInfo);

        var testSessionInfo = new TestSessionInfo();
        var ackPayload = new Payloads.StartTestSessionAckPayload()
        {
            TestSessionInfo = testSessionInfo
        };
        var ackMessage = CreateMessage(
            MessageType.StartTestSessionCallback,
            ackPayload);

        Action reconfigureAction = () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(ackMessage));

        _mockCommunicationManager.Setup(cm => cm.SendMessage(
            MessageType.StartTestSession,
            It.IsAny<Payloads.StartTestSessionPayload>(),
            _protocolVersion)).Callback(() => { });
        _mockCommunicationManager.Setup(cm => cm.SendMessage(
                MessageType.CustomTestHostLaunchCallback,
                It.IsAny<CustomHostLaunchAckPayload>(),
                _protocolVersion))
            .Callback((string messageType, object payload, int version) => Assert.AreEqual(((CustomHostLaunchAckPayload)payload).HostProcessId, TesthostPid));
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(launchMessage))
            .Callback(reconfigureAction);

        // Act
        Assert.AreEqual(
            testSessionInfo,
            _requestSender.StartTestSession(
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
        await InitializeCommunicationAsync().ConfigureAwait(false);

        // Setup
        var mockHandler = new Mock<ITestSessionEventsHandler>();
        mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });
        var mockTesthostLauncher = new Mock<ITestHostLauncher>();
        mockTesthostLauncher.Setup(tl => tl.LaunchTestHost(It.IsAny<TestProcessStartInfo>())).Returns(TesthostPid);

        var launchInfo = new TestProcessStartInfo();
        var launchMessage = CreateMessage(
            MessageType.CustomTestHostLaunch,
            launchInfo);

        var testSessionInfo = new TestSessionInfo();
        var ackPayload = new Payloads.StartTestSessionAckPayload()
        {
            TestSessionInfo = testSessionInfo
        };
        var ackMessage = CreateMessage(
            MessageType.StartTestSessionCallback,
            ackPayload);

        Action reconfigureAction = () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(ackMessage));

        _mockCommunicationManager.Setup(cm => cm.SendMessage(
            MessageType.StartTestSession,
            It.IsAny<Payloads.StartTestSessionPayload>(),
            _protocolVersion)).Callback(() => { });
        _mockCommunicationManager.Setup(cm => cm.SendMessage(
                MessageType.CustomTestHostLaunchCallback,
                It.IsAny<CustomHostLaunchAckPayload>(),
                _protocolVersion))
            .Callback((string messageType, object payload, int version) => Assert.AreEqual(((CustomHostLaunchAckPayload)payload).HostProcessId, TesthostPid));
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(launchMessage))
            .Callback(reconfigureAction);

        // Act
        Assert.AreEqual(
            testSessionInfo,
            await _requestSender.StartTestSessionAsync(
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
        InitializeCommunication();

        // Setup
        var mockHandler = new Mock<ITestSessionEventsHandler>();
        mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });
        var mockTesthostLauncher = new Mock<ITestHostLauncher2>();
        mockTesthostLauncher.Setup(tl => tl.AttachDebuggerToProcess(TesthostPid)).Returns(true);

        var launchMessage = CreateMessage(
            MessageType.EditorAttachDebugger,
            TesthostPid);

        var testSessionInfo = new TestSessionInfo();
        var ackPayload = new Payloads.StartTestSessionAckPayload()
        {
            TestSessionInfo = testSessionInfo
        };
        var ackMessage = CreateMessage(
            MessageType.StartTestSessionCallback,
            ackPayload);

        Action reconfigureAction = () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(ackMessage));

        _mockCommunicationManager.Setup(cm => cm.SendMessage(
            MessageType.StartTestSession,
            It.IsAny<Payloads.StartTestSessionPayload>(),
            _protocolVersion)).Callback(() => { });
        _mockCommunicationManager.Setup(cm => cm.SendMessage(
                MessageType.EditorAttachDebuggerCallback,
                It.IsAny<EditorAttachDebuggerAckPayload>(),
                _protocolVersion))
            .Callback((string messageType, object payload, int version) => Assert.IsTrue(((EditorAttachDebuggerAckPayload)payload).Attached));
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(launchMessage))
            .Callback(reconfigureAction);

        // Act
        Assert.AreEqual(
            testSessionInfo,
            _requestSender.StartTestSession(
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
        await InitializeCommunicationAsync().ConfigureAwait(false);

        // Setup
        var mockHandler = new Mock<ITestSessionEventsHandler>();
        mockHandler.Setup(mh => mh.HandleStartTestSessionComplete(It.IsAny<TestSessionInfo>())).Callback(() => { });
        var mockTesthostLauncher = new Mock<ITestHostLauncher2>();
        mockTesthostLauncher.Setup(tl => tl.AttachDebuggerToProcess(TesthostPid)).Returns(true);

        var launchMessage = CreateMessage(
            MessageType.EditorAttachDebugger,
            TesthostPid);

        var testSessionInfo = new TestSessionInfo();
        var ackPayload = new Payloads.StartTestSessionAckPayload()
        {
            TestSessionInfo = testSessionInfo
        };
        var ackMessage = CreateMessage(
            MessageType.StartTestSessionCallback,
            ackPayload);

        Action reconfigureAction = () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(ackMessage));

        _mockCommunicationManager.Setup(cm => cm.SendMessage(
            MessageType.StartTestSession,
            It.IsAny<Payloads.StartTestSessionPayload>(),
            _protocolVersion)).Callback(() => { });
        _mockCommunicationManager.Setup(cm => cm.SendMessage(
                MessageType.EditorAttachDebuggerCallback,
                It.IsAny<EditorAttachDebuggerAckPayload>(),
                _protocolVersion))
            .Callback((string messageType, object payload, int version) => Assert.IsTrue(((EditorAttachDebuggerAckPayload)payload).Attached));
        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(launchMessage))
            .Callback(reconfigureAction);

        // Act
        Assert.AreEqual(
            testSessionInfo,
            await _requestSender.StartTestSessionAsync(
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
        return _serializer.DeserializeMessage(_serializer.SerializePayload(messageType, payload, _protocolVersion));
    }

    private void InitializeCommunication()
    {
        InitializeCommunication(_protocolVersion);
    }

    private void InitializeCommunication(int protocolVersion)
    {
        var dummyPortInput = 123;
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

        _mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
            .Callback((int timeout) => Task.Delay(200).Wait());

        var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };
        var versionCheck = new Message() { MessageType = MessageType.VersionCheck, Payload = protocolVersion };

        Action changedMessage = () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(versionCheck);

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(sessionConnected);
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, It.IsAny<int>())).Callback(changedMessage);

        var portOutput = _requestSender.InitializeCommunication();
        Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
        var connectionSuccess = _requestSender.WaitForRequestHandlerConnection(_waitTimeout);
        Assert.IsTrue(connectionSuccess, "Connection must succeed.");
    }

    private void SetupMockCommunicationForRunRequest(Mock<ITestRunEventsHandler> mockHandler)
    {
        InitializeCommunication();

        var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
        var testResult = new TestResult(testCase);
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

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
    }

    private async Task InitializeCommunicationAsync()
    {
        await InitializeCommunicationAsync(_protocolVersion);
    }

    private async Task InitializeCommunicationAsync(int protocolVersion)
    {
        var dummyPortInput = 123;
        _mockCommunicationManager.Setup(cm => cm.HostServer(new IPEndPoint(IPAddress.Loopback, 0))).Returns(new IPEndPoint(IPAddress.Loopback, dummyPortInput));
        _mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Returns(Task.FromResult(false)).Callback(() => { });

        var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };
        var versionCheck = new Message() { MessageType = MessageType.VersionCheck, Payload = protocolVersion };

        Action changedMessage = () => _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(versionCheck));

        _mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(sessionConnected));
        _mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, It.IsAny<int>())).Callback(changedMessage);

        var portOutput = await _requestSender.InitializeCommunicationAsync(_waitTimeout);
        Assert.AreEqual(dummyPortInput, portOutput, "Connection must succeed.");
    }

    #endregion
}