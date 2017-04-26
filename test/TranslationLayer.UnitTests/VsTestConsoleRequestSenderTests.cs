// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Payloads;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Serialization;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

    [TestClass]
    public class VsTestConsoleRequestSenderTests
    {
        private ITranslationLayerRequestSender requestSender;

        private Mock<ICommunicationManager> mockCommunicationManager;

        private int WaitTimeout = 2000;

        private int protocolVersion = 2;
        private IDataSerializer serializer = JsonDataSerializer.Instance;

        public VsTestConsoleRequestSenderTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.requestSender = new VsTestConsoleRequestSender(this.mockCommunicationManager.Object, JsonDataSerializer.Instance);
        }

        #region Communication Tests

        [TestMethod]
        public void InitializeCommunicationShouldSucceed()
        {
            this.InitializeCommunication();

            this.mockCommunicationManager.Verify(cm => cm.HostServer(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Exactly(2));
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldReturnInvalidPortNumberIfHostServerFails()
        {
            this.mockCommunicationManager.Setup(cm => cm.HostServer()).Throws(new Exception("Fail"));

            var portOutput = this.requestSender.InitializeCommunication();
            Assert.IsTrue(portOutput < 0, "Negative port number must be returned if Hosting Server fails.");

            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsFalse(connectionSuccess, "Connection must fail as server failed to host.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Never);
            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Never);
            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Never);
        }

        [TestMethod]
        public void InitializeCommunicationShouldFailConnectionIfMessageReceiveFailed()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer()).Returns(dummyPortInput);
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
                .Callback((int timeout) => Task.Delay(200).Wait());
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Throws(new Exception("Fail"));

            var portOutput = this.requestSender.InitializeCommunication();

            // Hosting server didn't server, so port number should still be valid
            Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");

            // Connection must not succeed as handshake failed
            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsFalse(connectionSuccess, "Connection must fail if handshake failed.");
            this.mockCommunicationManager.Verify(cm => cm.HostServer(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldFailConnectionIfSessionConnectedDidNotComeFirst()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer()).Returns(dummyPortInput);
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
                .Callback((int timeout) => Task.Delay(200).Wait());

            var discoveryMessage = new Message() { MessageType = MessageType.StartDiscovery };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(discoveryMessage);

            var portOutput = this.requestSender.InitializeCommunication();
            Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsFalse(connectionSuccess, "Connection must fail if version check failed.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Never);
        }

        [TestMethod]
        public void InitializeCommunicationShouldFailConnectionIfSendMessageFailed()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer()).Returns(dummyPortInput);
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Callback(() => { });
            this.mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
                .Callback((int timeout) => Task.Delay(200).Wait());

            var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(sessionConnected);
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion)).Throws(new Exception("Fail"));

            var portOutput = this.requestSender.InitializeCommunication();
            Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsFalse(connectionSuccess, "Connection must fail if version check failed.");

            this.mockCommunicationManager.Verify(cm => cm.HostServer(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once);
        }

        [TestMethod]
        public void InitializeCommunicationShouldFailConnectionIfProtocolIsNotCompatible()
        {
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer()).Returns(dummyPortInput);
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Callback(() => { });

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

            this.mockCommunicationManager.Verify(cm => cm.HostServer(), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.AcceptClientAsync(), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.WaitForClientConnection(Timeout.Infinite), Times.Once);

            this.mockCommunicationManager.Verify(cm => cm.ReceiveMessage(), Times.Exactly(2));
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion), Times.Once);
        }

        #endregion

        #region Discovery Tests

        [TestMethod]
        public void DiscoverTestsShouldCompleteWithZeroTests()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();

            var payload = new DiscoveryCompletePayload() { TotalTests = 0, LastDiscoveredTests = null, IsAborted = false };
            var discoveryComplete = new Message() { MessageType = MessageType.DiscoveryComplete,
                Payload = JToken.FromObject(payload) };
            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete));

            this.requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(0, null, false), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Never, "DiscoveredTests must not be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public void DiscoverTestsShouldCompleteWithSingleTest()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();

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

            this.requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(1, null, false), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Once, "DiscoveredTests must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Never, "TestMessage event must not be called");
        }

        [TestMethod]
        public void DiscoverTestsShouldReportBackTestsWithTraitsInTestsFoundMessage()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();

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

            this.requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, mockHandler.Object);

            Assert.IsNotNull(receivedTestCases);
            Assert.AreEqual(1, receivedTestCases.Count);

            // Verify that the traits are passed through properly.
            var traits = receivedTestCases.ToArray()[0].Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual(traits.ToArray()[0].Name, "a");
            Assert.AreEqual(traits.ToArray()[0].Value, "b");
        }

        [TestMethod]
        public void DiscoverTestsShouldReportBackTestsWithTraitsInDiscoveryCompleteMessage()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            testCase.Traits.Add(new Trait("a", "b"));

            List<TestCase> receivedTestCases = null;
            var testCaseList = new List<TestCase>() { testCase };

            var payload = new DiscoveryCompletePayload() { TotalTests = 1, LastDiscoveredTests = testCaseList, IsAborted = false };
            var discoveryComplete = CreateMessage(MessageType.DiscoveryComplete, payload);

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(discoveryComplete));
            mockHandler.Setup(mh => mh.HandleDiscoveryComplete(It.IsAny<long>(), It.IsAny<IEnumerable<TestCase>>(), It.IsAny<bool>()))
                .Callback(
                    (long totalTests, IEnumerable<TestCase> tests, bool isAborted) =>
                    {
                        receivedTestCases = tests?.ToList();
                    });

            this.requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, mockHandler.Object);

            Assert.IsNotNull(receivedTestCases);
            Assert.AreEqual(1, receivedTestCases.Count);

            // Verify that the traits are passed through properly.
            var traits = receivedTestCases.ToArray()[0].Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual(traits.ToArray()[0].Name, "a");
            Assert.AreEqual(traits.ToArray()[0].Value, "b");
        }

        [TestMethod]
        public void DiscoverTestsShouldCompleteWithTestMessage()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();

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

            this.requestSender.DiscoverTests(new List<string>() { "1.dll" }, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(1, null, false), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleDiscoveredTests(It.IsAny<IEnumerable<TestCase>>()), Times.Once, "DiscoveredTests must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Informational, "Hello"), Times.Once, "TestMessage event must be called");
        }

        [TestMethod]
        public void DiscoverTestsShouldAbortOnExceptionInSendMessage()
        {
            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();
            var sources = new List<string> {"1.dll"};
            var payload = new DiscoveryRequestPayload {Sources = sources, RunSettings = null};
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.StartDiscovery, payload)).Throws(new IOException());

            this.requestSender.DiscoverTests(sources, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleDiscoveryComplete(-1, null, true), Times.Once, "Discovery Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            this.mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
        }

        [TestMethod]
        public void DiscoverTestsShouldAbortWhenProcessExited()
        {
            var mockHandler = new Mock<ITestDiscoveryEventsHandler>();
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

            mockHandler.Setup(mh => mh.HandleDiscoveryComplete(-1, null, true)).Callback(() => manualEvent.Set());

            this.requestSender.DiscoverTests(sources, null, mockHandler.Object);

            manualEvent.WaitOne();
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Once);
        }

        #endregion

        #region RunTests

        [TestMethod]
        public void StartTestRunShouldCompleteWithZeroTests()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();
            
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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

            this.requestSender.StartTestRun(new List<string>() { "1.dll" }, null, mockHandler.Object);

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
            
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Count() > 0, "TestResults must be passed properly");
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
                });

            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () =>
                {
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

            this.requestSender.StartTestRun(new List<string>() { "1.dll" }, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleTestRunStatsChange(It.IsAny<TestRunChangedEventArgs>()), Times.Once, "RunChangedArgs must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()), Times.Once, "TestMessage event must be called");
        }

        [TestMethod]
        public void StartTestRunWithCustomHostShouldComplete()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Count() > 0, "TestResults must be passed properly");
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

            this.requestSender.StartTestRunWithCustomHost(new List<string>() { "1.dll" }, null, mockHandler.Object, mockLauncher.Object);

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

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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

            this.requestSender.StartTestRunWithCustomHost(new List<string>() { "1.dll" }, null, mockHandler.Object, mockLauncher.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(),
                It.IsAny<TestRunChangedEventArgs>(), null, null), Times.Once, "Run Complete must be called");
        }

        [TestMethod]
        public void StartTestRunWithSelectedTestsShouldCompleteWithZeroTests()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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

            this.requestSender.StartTestRun(new List<TestCase>(), null, mockHandler.Object);

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

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Count() > 0, "TestResults must be passed properly");
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message));
                });

            mockHandler.Setup(mh => mh.HandleLogMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>())).Callback(
                () =>
                {
                    this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                });

            this.requestSender.StartTestRun(testCaseList, null, mockHandler.Object);

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
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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

            this.requestSender.StartTestRun(testCaseList, null, mockHandler.Object);

            Assert.IsNotNull(receivedChangeEventArgs);
            Assert.IsTrue(receivedChangeEventArgs.NewTestResults.Count() > 0);
            
            // Verify that the traits are passed through properly.
            var traits = receivedChangeEventArgs.NewTestResults.ToArray()[0].TestCase.Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual(traits.ToArray()[0].Name, "a");
            Assert.AreEqual(traits.ToArray()[0].Value, "b");
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
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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

            this.requestSender.StartTestRun(testCaseList, null, mockHandler.Object);

            Assert.IsNotNull(receivedChangeEventArgs);
            Assert.IsTrue(receivedChangeEventArgs.NewTestResults.Any());

            // Verify that the traits are passed through properly.
            var traits = receivedChangeEventArgs.NewTestResults.ToArray()[0].TestCase.Traits;
            Assert.IsNotNull(traits);
            Assert.AreEqual(traits.ToArray()[0].Name, "a");
            Assert.AreEqual(traits.ToArray()[0].Value, "b");
        }

        [TestMethod]
        public void StartTestRunWithSelectedTestsAndCustomHostShouldComplete()
        {
            this.InitializeCommunication();

            var mockHandler = new Mock<ITestRunEventsHandler>();

            var testCase = new TestCase("hello", new Uri("world://how"), "1.dll");
            var testResult = new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase);
            testResult.Outcome = TestOutcome.Passed;

            var testCaseList = new List<TestCase>() { testCase };

            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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
                    Assert.IsTrue(testRunChangedArgs.NewTestResults != null && testsChangedArgs.NewTestResults.Count() > 0, "TestResults must be passed properly");
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

            this.requestSender.StartTestRunWithCustomHost(testCaseList, null, mockHandler.Object, mockLauncher.Object);

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
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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
                   if(startInfo.FileName.Equals(p1.FileName))
                   {
                       this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(message2));
                   }
                   else if (startInfo.FileName.Equals(p2.FileName))
                   {
                       this.mockCommunicationManager.Setup(cm => cm.ReceiveMessageAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(runComplete));
                   }
               });
            this.requestSender.InitializeCommunication();
            this.requestSender.StartTestRunWithCustomHost(sources, null, mockHandler.Object, mockLauncher.Object);

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

            this.requestSender.StartTestRun(sources, null, mockHandler.Object);

            mockHandler.Verify(mh => mh.HandleTestRunComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null), Times.Once, "Test Run Complete must be called");
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once, "TestMessage event must be called");
            this.mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Never);
        }

        [TestMethod]
        public void StartRunTestsShouldAbortOnProcessExited()
        {
            var mockHandler = new Mock<ITestRunEventsHandler>();
            var manualEvent = new ManualResetEvent(false);
            var sources = new List<string> { "1.dll" };
            var dummyCompleteArgs = new TestRunCompleteEventArgs(null, false, false, null, null, TimeSpan.FromMilliseconds(1));
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

            this.requestSender.StartTestRun(sources, null, mockHandler.Object);

            manualEvent.WaitOne();
            mockHandler.Verify(mh => mh.HandleLogMessage(TestMessageLevel.Error, It.IsAny<string>()), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.StopServer(), Times.Once);
        }

        #endregion

        #region private methods

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
            var dummyPortInput = 123;
            this.mockCommunicationManager.Setup(cm => cm.HostServer()).Returns(dummyPortInput);
            this.mockCommunicationManager.Setup(cm => cm.AcceptClientAsync()).Callback(() => { });

            this.mockCommunicationManager.Setup(cm => cm.WaitForClientConnection(Timeout.Infinite))
                .Callback((int timeout) => Task.Delay(200).Wait());

            var sessionConnected = new Message() { MessageType = MessageType.SessionConnected };
            var versionCheck = new Message() { MessageType = MessageType.VersionCheck, Payload = this.protocolVersion };

            Action changedMessage = () =>
            {
                this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(versionCheck);
            };

            this.mockCommunicationManager.Setup(cm => cm.ReceiveMessage()).Returns(sessionConnected);
            this.mockCommunicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, this.protocolVersion)).Callback(changedMessage);

            var portOutput = this.requestSender.InitializeCommunication();
            Assert.AreEqual(dummyPortInput, portOutput, "Port number must return without changes.");
            var connectionSuccess = this.requestSender.WaitForRequestHandlerConnection(this.WaitTimeout);
            Assert.IsTrue(connectionSuccess, "Connection must succeed.");
        }

        #endregion
    }
}
