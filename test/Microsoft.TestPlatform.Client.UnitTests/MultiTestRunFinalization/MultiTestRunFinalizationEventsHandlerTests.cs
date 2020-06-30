// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.TestPlatform.Client.UnitTests.MultiTestRunFinalization
{
    using Microsoft.VisualStudio.TestPlatform.Client.MultiTestRunFinalization;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class MultiTestRunFinalizationEventsHandlerTests
    {
        private readonly Mock<ICommunicationManager> mockCommunicationManager;
        private readonly IMultiTestRunFinalizationEventsHandler handler;

        public MultiTestRunFinalizationEventsHandlerTests()
        {
            this.mockCommunicationManager = new Mock<ICommunicationManager>();
            this.handler = new MultiTestRunFinalizationEventsHandler(mockCommunicationManager.Object);
        }

        [TestMethod]
        public void EventsHandlerHandleLogMessageShouldSendTestMessage()
        {
            string message = "error message";

            handler.HandleLogMessage(TestMessageLevel.Error, message);

            mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.TestMessage, It.Is<TestMessagePayload>(p => p.MessageLevel == TestMessageLevel.Error && p.Message == message)));
        }

        [TestMethod]
        public void EventsHandlerHandleMultiTestRunFinalizationCompleteShouldSendFinalizationCompleteMessage()
        {
            var attachments = new[] { new AttachmentSet(new System.Uri("http://www.bing.com/"), "code coverage") };
            var args = new MultiTestRunFinalizationCompleteEventArgs(false, null);

            handler.HandleMultiTestRunFinalizationComplete(args, attachments);

            mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.MultiTestRunFinalizationComplete, It.Is<MultiTestRunFinalizationCompletePayload>(p => p.Attachments == attachments && p.FinalizationCompleteEventArgs == args)));
        }

        [TestMethod]
        public void EventsHandlerHandleMultiTestRunFinalizationProgressShouldSendFinalizationProgressMessage()
        {
            var args = new MultiTestRunFinalizationProgressEventArgs(1, new[] { new System.Uri("http://www.bing.com/") }, 90, 2);

            handler.HandleMultiTestRunFinalizationProgress(args);

            mockCommunicationManager.Verify(cm => cm.SendMessage(MessageType.MultiTestRunFinalizationProgress, It.Is<MultiTestRunFinalizationProgressPayload>(p => p.FinalizationProgressEventArgs == args)));
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
