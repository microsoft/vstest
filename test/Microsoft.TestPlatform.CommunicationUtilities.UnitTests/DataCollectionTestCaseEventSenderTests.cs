// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Newtonsoft.Json.Linq;

    [TestClass]
    public class DataCollectionTestCaseEventSenderTests
    {
        private DataCollectionTestCaseEventSender dataCollectionTestCaseEventSender;
        private Mock<ICommunicationManager> mockCommunicationManager;

        public DataCollectionTestCaseEventSenderTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.dataCollectionTestCaseEventSender = new TestableDataCollectionTestCaseEventSender(this.mockCommunicationManager.Object);
        }

        [TestMethod]
        public void InitializeShouldInitializeCommunicationManager()
        {
            this.dataCollectionTestCaseEventSender.InitializeCommunication(123);

            this.mockCommunicationManager.Verify(x => x.SetupClientAsync(123), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.SetupClientAsync(It.IsAny<int>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
            {
                this.dataCollectionTestCaseEventSender.InitializeCommunication(123);
            });
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldInvokeWaitForServerConnection()
        {
            this.dataCollectionTestCaseEventSender.WaitForRequestSenderConnection(123);

            this.mockCommunicationManager.Verify(x => x.WaitForServerConnection(It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public void WaitForRequestSenderConnectionShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.WaitForServerConnection(It.IsAny<int>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
            {
                this.dataCollectionTestCaseEventSender.WaitForRequestSenderConnection(123);
            });
        }

        [TestMethod]
        public void CloseShouldDisposeCommunicationManager()
        {
            this.dataCollectionTestCaseEventSender.Close();

            this.mockCommunicationManager.Verify(x => x.StopClient(), Times.Once);
        }

        [TestMethod]
        public void CloseShouldThrowExceptionIfThrownByCommunicationManager()
        {
            this.mockCommunicationManager.Setup(x => x.StopClient()).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
            {
                this.dataCollectionTestCaseEventSender.Close();
            });
        }

        [TestMethod]
        public void SendTestCaseStartShouldSendMessageThroughCommunicationManager()
        {
            var testCase = new TestCase();
            var testcaseStartEventArgs = new TestCaseStartEventArgs(testCase);
            this.dataCollectionTestCaseEventSender.SendTestCaseStart(testcaseStartEventArgs);

            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.DataCollectionTestStart, testcaseStartEventArgs), Times.Once);
        }

        [TestMethod]
        public void SendTestCaseStartShouldThrowExceptionIfThrownByCommunicationManager()
        {
            var testCase = new TestCase();
            var testcaseStartEventArgs = new TestCaseStartEventArgs(testCase);
            this.mockCommunicationManager.Setup(x => x.SendMessage(MessageType.DataCollectionTestStart, testcaseStartEventArgs)).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
            {
                this.dataCollectionTestCaseEventSender.SendTestCaseStart(testcaseStartEventArgs);
            });
        }

        [TestMethod]
        public void SendTestCaseCompletedShouldSendMessageThroughCommunicationManager()
        {
            var testCase = new TestCase();
            var testResultEventArgs = new TestResultEventArgs(new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase));
            var attachmentSet = new AttachmentSet(new Uri("my://attachment"), "displayname");
            this.mockCommunicationManager.Setup(x => x.ReceiveMessage()).Returns(new Message() { MessageType = MessageType.DataCollectionTestEndResult, Payload = JToken.FromObject(new Collection<AttachmentSet>() { attachmentSet }) });
            this.dataCollectionTestCaseEventSender.SendTestCaseComplete(testResultEventArgs);

            Assert.AreEqual(testResultEventArgs.TestResult.Attachments[0].Uri, attachmentSet.Uri);
            Assert.AreEqual(testResultEventArgs.TestResult.Attachments[0].DisplayName, attachmentSet.DisplayName);
        }

        [TestMethod]
        public void SendTestCaseCompletedShouldThrowExceptionIfThrownByCommunicationManager()
        {
            var testCase = new TestCase();
            var testResultEventArgs = new TestResultEventArgs(new VisualStudio.TestPlatform.ObjectModel.TestResult(testCase));
            this.mockCommunicationManager.Setup(x => x.SendMessage(MessageType.DataCollectionTestEnd, It.IsAny<TestResultEventArgs>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
            {
                this.dataCollectionTestCaseEventSender.SendTestCaseComplete(testResultEventArgs);
            });
        }
    }

    internal class TestableDataCollectionTestCaseEventSender : DataCollectionTestCaseEventSender
    {
        public TestableDataCollectionTestCaseEventSender(ICommunicationManager communicationManager)
            : base(communicationManager)
        {
        }
    }
}
