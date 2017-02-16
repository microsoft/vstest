// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionTestCaseEventSenderTests
    {
        private DataCollectionTestCaseEventSender dataCollectionTestCaseEventSender;
        private Mock<ICommunicationManager> mockCommunicationManager;

        public DataCollectionTestCaseEventSenderTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.dataCollectionTestCaseEventSender = new DataCollectionTestCaseEventSender(this.mockCommunicationManager.Object);
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
            this.dataCollectionTestCaseEventSender.SendTestCaseStart(testCase);

            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.BeforeTestCaseStart, testCase), Times.Once);
        }

        [TestMethod]
        public void SendTestCaseStartShouldThrowExceptionIfThrownByCommunicationManager()
        {
            var testCase = new TestCase();
            this.mockCommunicationManager.Setup(x => x.SendMessage(MessageType.BeforeTestCaseStart, testCase)).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
            {
                this.dataCollectionTestCaseEventSender.SendTestCaseStart(testCase);
            });
        }

        [TestMethod]
        public void SendTestCaseCompletedShouldSendMessageThroughCommunicationManager()
        {
            var testCase = new TestCase();

            this.dataCollectionTestCaseEventSender.SendTestCaseCompleted(testCase, TestOutcome.Passed);

            this.mockCommunicationManager.Verify(x => x.SendMessage(MessageType.AfterTestCaseCompleted, JsonDataSerializer.Instance.Serialize<object[]>(new object[] { testCase, TestOutcome.Passed })), Times.Once);
        }

        [TestMethod]
        public void SendTestCaseCompletedShouldThrowExceptionIfThrownByCommunicationManager()
        {
            var testCase = new TestCase();
            this.mockCommunicationManager.Setup(x => x.SendMessage(MessageType.AfterTestCaseCompleted, It.IsAny<string>())).Throws<Exception>();

            Assert.ThrowsException<Exception>(() =>
            {
                this.dataCollectionTestCaseEventSender.SendTestCaseCompleted(testCase, TestOutcome.Passed);
            });
        }
    }
}
