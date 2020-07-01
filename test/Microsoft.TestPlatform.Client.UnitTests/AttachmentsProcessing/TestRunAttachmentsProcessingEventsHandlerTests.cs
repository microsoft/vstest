// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.TestPlatform.Client.UnitTests.TestRunAttachmentsProcessing
{
    using Microsoft.VisualStudio.TestPlatform.Client.TestRunAttachmentsProcessing;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestRunAttachmentsProcessingEventsHandlerTests
    {
        private readonly Mock<ICommunicationManager> mockCommunicationManager;
        private readonly ITestRunAttachmentsProcessingEventsHandler handler;

        public TestRunAttachmentsProcessingEventsHandlerTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.handler = new TestRunAttachmentsProcessingEventsHandler(mockCommunicationManager.Object);
        }

        [TestMethod]
        public void EventsHandlerHandleLogMessageShouldSendTestMessage()
        {
            string message = "error message";

            handler.HandleLogMessage(TestMessageLevel.Error, message);

            mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.TestMessage, It.Is<TestMessagePayload>(p => p.MessageLevel == TestMessageLevel.Error && p.Message == message)));
        }

        [TestMethod]
        public void EventsHandlerHandleTestRunAttachmentsProcessingCompleteShouldSendAttachmentsProcessingCompleteMessage()
        {
            var attachments = new[] { new AttachmentSet(new System.Uri("http://www.bing.com/"), "code coverage") };
            var args = new TestRunAttachmentsProcessingCompleteEventArgs(false, null);

            handler.HandleTestRunAttachmentsProcessingComplete(args, attachments);

            mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.TestRunAttachmentsProcessingComplete, It.Is<TestRunAttachmentsProcessingCompletePayload>(p => p.Attachments == attachments && p.AttachmentsProcessingCompleteEventArgs == args)));
        }

        [TestMethod]
        public void EventsHandlerHandleTestRunAttachmentsProcessingProgressShouldSendAttachmentsProcessingProgressMessage()
        {
            var args = new TestRunAttachmentsProcessingProgressEventArgs(1, new[] { new System.Uri("http://www.bing.com/") }, 90, 2);

            handler.HandleTestRunAttachmentsProcessingProgress(args);

            mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.TestRunAttachmentsProcessingProgress, It.Is<TestRunAttachmentsProcessingProgressPayload>(p => p.AttachmentsProcessingProgressEventArgs == args)));
        }

        [TestMethod]
        public void EventsHandlerHandleRawMessageShouldDoNothing()
        {
            handler.HandleRawMessage("any");

            mockCommunicationManager.Verify(cm => cm.SendMessage(It.IsAny<string>()), Times.Never);
            mockCommunicationManager.Verify(cm => cm.SendMessage(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }
    }
}
