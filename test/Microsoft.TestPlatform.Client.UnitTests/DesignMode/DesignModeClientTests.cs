// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.UnitTests.DesignMode
{
    using System;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;

    using Microsoft.VisualStudio.TestPlatform.Client.DesignMode;
    using Microsoft.VisualStudio.TestPlatform.Client.RequestHelper;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using ObjectModel.Client;
    using ObjectModel.Engine;
    using ObjectModel.Client.Interfaces;

    [TestClass]
    public class DesignModeClientTests
    {
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
            int portNumber = 123;
            var testRequestManager = new Mock<ITestRequestManager>();
            var communicationManager = new Mock<ICommunicationManager>();
            var testDesignModeClient = new DesignModeClient(communicationManager.Object, JsonDataSerializer.Instance);

            var verCheck = new Message() { MessageType = MessageType.VersionCheck };
            var sessionEnd = new Message() { MessageType = MessageType.SessionEnd };
            communicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            communicationManager.Setup(cm => cm.ReceiveMessage()).Returns(verCheck);

            bool verCheckCalled = false;
            communicationManager.Setup(cm => cm.SendMessage(MessageType.VersionCheck, It.IsAny<int>())).Callback
                (() =>
                {
                    verCheckCalled = true;
                    communicationManager.Setup(cm => cm.ReceiveMessage()).Returns(sessionEnd);
                });

            testDesignModeClient.ConnectToClientAndProcessRequests(portNumber, testRequestManager.Object);

            communicationManager.Verify(cm => cm.SetupClientAsync(portNumber), Times.Once);
            communicationManager.Verify(cm => cm.WaitForServerConnection(It.IsAny<int>()), Times.Once);
            Assert.IsTrue(verCheckCalled, "Version Check must be called");
        }

        [TestMethod]
        public void DesignModeClientWithGetTestRunnerProcessStartInfoShouldDeserializeTestsWithTraitsCorrectly()
        {
            // Arrange.
            var mockTestRequestManager = new Mock<ITestRequestManager>();
            var mockCommunicationManager = new Mock<ICommunicationManager>();

            var testDesignModeClient = new DesignModeClient(mockCommunicationManager.Object, JsonDataSerializer.Instance);

            var testCase = new TestCase("A.C.M", new Uri("d:\\executor"), "A.dll");
            testCase.Traits.Add(new Trait("foo", "bar"));

            var testList = new System.Collections.Generic.List<TestCase> { testCase };
            var testRunPayload = new TestRunRequestPayload() { RunSettings = null, TestCases = testList };

            var getProcessStartInfoMessage = new Message()
                                                 {
                                                     MessageType =
                                                         MessageType
                                                         .GetTestRunnerProcessStartInfoForRunSelected,
                                                     Payload = JToken.FromObject("random")
                                                 };

            var sessionEnd = new Message() { MessageType = MessageType.SessionEnd };
            TestRunRequestPayload receivedTestRunPayload = null;
            var allTasksComplete = new ManualResetEvent(false);

            // Setup mocks.
            mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            mockCommunicationManager.Setup(cm => cm.DeserializePayload<TestRunRequestPayload>(getProcessStartInfoMessage))
                .Returns(testRunPayload);
            
            mockTestRequestManager.Setup(
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
            
            mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage())
                .Returns(getProcessStartInfoMessage)
                .Returns(sessionEnd);
            
            // Act.
            testDesignModeClient.ConnectToClientAndProcessRequests(0, mockTestRequestManager.Object);

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
            var mockTestRequestManager = new Mock<ITestRequestManager>();
            var mockCommunicationManager = new Mock<ICommunicationManager>();

            var testDesignModeClient = new DesignModeClient(mockCommunicationManager.Object, JsonDataSerializer.Instance);

            var testCase = new TestCase("A.C.M", new Uri("d:\\executor"), "A.dll");
            testCase.Traits.Add(new Trait("foo", "bar"));

            var testList = new System.Collections.Generic.List<TestCase> { testCase };
            var testRunPayload = new TestRunRequestPayload() { RunSettings = null, TestCases = testList };

            var getProcessStartInfoMessage = new Message()
            {
                MessageType = MessageType.TestRunSelectedTestCasesDefaultHost,
                Payload = JToken.FromObject("random")
            };

            var sessionEnd = new Message() { MessageType = MessageType.SessionEnd };
            TestRunRequestPayload receivedTestRunPayload = null;
            var allTasksComplete = new ManualResetEvent(false);

            // Setup mocks.
            mockCommunicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(true);
            mockCommunicationManager.Setup(cm => cm.DeserializePayload<TestRunRequestPayload>(getProcessStartInfoMessage))
                .Returns(testRunPayload);

            mockTestRequestManager.Setup(
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

            mockCommunicationManager.SetupSequence(cm => cm.ReceiveMessage())
                .Returns(getProcessStartInfoMessage)
                .Returns(sessionEnd);

            // Act.
            testDesignModeClient.ConnectToClientAndProcessRequests(0, mockTestRequestManager.Object);

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
            int portNumber = 123;
            var designModeHandler = new Mock<ITestRequestManager>();
            var communicationManager = new Mock<ICommunicationManager>();
            var testDesignModeClient = new DesignModeClient(communicationManager.Object, JsonDataSerializer.Instance);

            communicationManager.Setup(cm => cm.WaitForServerConnection(It.IsAny<int>())).Returns(false);

            Assert.ThrowsException<TimeoutException>(() => 
                testDesignModeClient.ConnectToClientAndProcessRequests(portNumber, designModeHandler.Object));

            communicationManager.Verify(cm => cm.SetupClientAsync(portNumber), Times.Once);
            communicationManager.Verify(cm => cm.WaitForServerConnection(It.IsAny<int>()), Times.Once);
            communicationManager.Verify(cm => cm.StopClient(), Times.Once);
        }

        [TestMethod]
        public void DesignModeClientShouldStopCommunicationOnParentProcessExit()
        {
            var designModeHandler = new Mock<ITestRequestManager>();
            var communicationManager = new Mock<ICommunicationManager>();
            var testDesignModeClient = new DesignModeClient(communicationManager.Object, JsonDataSerializer.Instance);

            testDesignModeClient.HandleParentProcessExit();

            communicationManager.Verify(cm => cm.StopClient(), Times.Once);
        }
    }
}
