// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.DesignMode
{
    using System;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class DesignModeClientTests
    {
        private const int PortNumber = 123;

        private readonly Mock<ITestRequestManager> mockTestRequestManager;

        private readonly Mock<ICommunicationManager> mockCommunicationManager;

        private readonly DesignModeClient designModeClient;

        public DesignModeClientTests()
        {
            this.mockTestRequestManager = new Mock<ITestRequestManager>();
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.designModeClient = new DesignModeClient(this.mockCommunicationManager.Object, JsonDataSerializer.Instance);
        }

        [TestMethod]
        public void DesignModeClientBeforeConnectInstanceShouldReturnNull()
        {
            Assert.IsNull(DesignModeClient.Instance);
        }

        [TestMethod]
        public void DesignModeClientInitializeShouldInstantiateClassAndCreateClient()
        {
            DesignModeClient.Initialize();
            Assert.IsNotNull(DesignModeClient.Instance);
        }

        [TestMethod]
        public void DesignModeClientConnectShouldSetupChannel()
        {
            var verCheck = new Message { MessageType = MessageType.VersionCheck };
            var sessionEnd = new Message { MessageType = MessageType.SessionEnd };
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage()).Returns(verCheck).Returns(sessionEnd);

            this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object);

            this.mockCommunicationManager.Verify(cm => cm.SetupClientAsync(PortNumber), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.WaitForServerConnection(It.IsAny<int>()), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.SessionConnected), Times.Once());
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, It.IsAny<int>()), Times.Once());
        }

        [TestMethod]
        public void DesignModeClientConnectShouldNotSendConnectedIfServerConnectionTimesOut()
        {
            var verCheck = new Message { MessageType = MessageType.VersionCheck };
            var sessionEnd = new Message { MessageType = MessageType.SessionEnd };
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(false);
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage()).Returns(verCheck).Returns(sessionEnd);

            Assert.ThrowsException<TimeoutException>(() => this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object));

            this.mockCommunicationManager.Verify(cm => cm.SetupClientAsync(PortNumber), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.WaitForServerConnection(It.IsAny<int>()), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.SessionConnected), Times.Never);
            this.mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.VersionCheck, It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void DesignModeClientWithGetTestRunnerProcessStartInfoShouldDeserializeTestsWithTraitsCorrectly()
        {
            // Arrange.
            var testCase = new TestCase("A.C.M", new Uri("d:\\executor"), "A.dll");
            testCase.Traits.Add(new Trait("foo", "bar"));

            var testList = new System.Collections.Generic.List<TestCase> { testCase };
            var testRunPayload = new TestRunRequestPayload { RunSettings = null, TestCases = testList };

            var getProcessStartInfoMessage = new Message
                                                 {
                                                     MessageType = MessageType.GetTestRunnerProcessStartInfoForRunSelected,
                                                     Payload = JToken.FromObject("random")
                                                 };

            var sessionEnd = new Message { MessageType = MessageType.SessionEnd };
            TestRunRequestPayload receivedTestRunPayload = null;
            var allTasksComplete = new ManualResetEvent(false);

            // Setup mocks.
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.Setup(cm => cm.DeserializePayload<TestRunRequestPayload>(getProcessStartInfoMessage))
                .Returns(testRunPayload);

            this.mockTestRequestManager.Setup(
                    trm =>
                        trm.RunTests(
                            It.IsAny<TestRunRequestPayload>(),
                            It.IsAny<ITestHostLauncher>(),
                            It.IsAny<ITestRunEventsRegistrar>()))
                .Callback(
                    (TestRunRequestPayload trp,
                     ITestHostLauncher testHostManager,
                     ITestRunEventsRegistrar testRunEventsRegistrar) =>
                        {
                            allTasksComplete.Set();
                            receivedTestRunPayload = trp;
                        });

            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage())
                .Returns(getProcessStartInfoMessage)
                .Returns(sessionEnd);
            
            // Act.
            this.designModeClient.ConnectToClientAndProcessRequests(0, this.mockTestRequestManager.Object);

            // wait for the internal spawned of tasks to complete.
            allTasksComplete.WaitOne(1000);

            // Assert.
            Assert.IsNotNull(receivedTestRunPayload);
            Assert.IsNotNull(receivedTestRunPayload.TestCases);
            Assert.AreEqual(1, receivedTestRunPayload.TestCases.Count);

            // Validate traits
            var traits = receivedTestRunPayload.TestCases.ToArray()[0].Traits;
            Assert.AreEqual("foo", traits.ToArray()[0].Name);
            Assert.AreEqual("bar", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public void DesignModeClientWithRunSelectedTestCasesShouldDeserializeTestsWithTraitsCorrectly()
        {
            // Arrange.
            var testCase = new TestCase("A.C.M", new Uri("d:\\executor"), "A.dll");
            testCase.Traits.Add(new Trait("foo", "bar"));

            var testList = new System.Collections.Generic.List<TestCase> { testCase };
            var testRunPayload = new TestRunRequestPayload { RunSettings = null, TestCases = testList };

            var getProcessStartInfoMessage = new Message
                                                 {
                                                     MessageType = MessageType.TestRunSelectedTestCasesDefaultHost,
                                                     Payload = JToken.FromObject("random")
                                                 };

            var sessionEnd = new Message { MessageType = MessageType.SessionEnd };
            TestRunRequestPayload receivedTestRunPayload = null;
            var allTasksComplete = new ManualResetEvent(false);

            // Setup mocks.
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            this.mockCommunicationManager.Setup(cm => cm.DeserializePayload<TestRunRequestPayload>(getProcessStartInfoMessage))
                .Returns(testRunPayload);
            this.mockTestRequestManager.Setup(
                trm =>
                trm.RunTests(
                    It.IsAny<TestRunRequestPayload>(),
                    It.IsAny<ITestHostLauncher>(),
                    It.IsAny<ITestRunEventsRegistrar>()))
                .Callback(
                    (TestRunRequestPayload trp,
                     ITestHostLauncher testHostManager,
                     ITestRunEventsRegistrar testRunEventsRegistrar) =>
                    {
                        allTasksComplete.Set();
                        receivedTestRunPayload = trp;
                    });
            this.mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage())
                .Returns(getProcessStartInfoMessage)
                .Returns(sessionEnd);

            // Act.
            this.designModeClient.ConnectToClientAndProcessRequests(0, this.mockTestRequestManager.Object);

            // wait for the internal spawned of tasks to complete.
            allTasksComplete.WaitOne(1000);

            // Assert.
            Assert.IsNotNull(receivedTestRunPayload);
            Assert.IsNotNull(receivedTestRunPayload.TestCases);
            Assert.AreEqual(1, receivedTestRunPayload.TestCases.Count);

            // Validate traits
            var traits = receivedTestRunPayload.TestCases.ToArray()[0].Traits;
            Assert.AreEqual("foo", traits.ToArray()[0].Name);
            Assert.AreEqual("bar", traits.ToArray()[0].Value);
        }

        [TestMethod]
        public void DesignModeClientOnBadConnectionShouldStopServerAndThrowTimeoutException()
        {
            this.mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(false);

            Assert.ThrowsException<TimeoutException>(() => this.designModeClient.ConnectToClientAndProcessRequests(PortNumber, this.mockTestRequestManager.Object));

            this.mockCommunicationManager.Verify(cm => cm.SetupClientAsync(PortNumber), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.WaitForServerConnection(It.IsAny<int>()), Times.Once);
            this.mockCommunicationManager.Verify(cm => cm.StopClient(), Times.Once);
        }

        [TestMethod]
        public void DesignModeClientShouldStopCommunicationOnParentProcessExit()
        {
            this.designModeClient.HandleParentProcessExit();

            this.mockCommunicationManager.Verify(cm => cm.StopClient(), Times.Once);
        }
    }
}
