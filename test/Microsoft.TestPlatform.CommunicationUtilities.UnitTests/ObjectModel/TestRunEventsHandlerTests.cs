// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.CommunicationUtilities.UnitTests.ObjectModel
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestRunEventsHandlerTests
    {
        private Mock<ITestRequestHandler> mockClient;
        private TestRunEventsHandler testRunEventHandler;

        [TestInitialize]
        public void InitializeTests()
        {
            this.mockClient = new Mock<ITestRequestHandler>();
            this.testRunEventHandler = new TestRunEventsHandler(this.mockClient.Object);
        }

        [TestMethod]
        public void HandleTestRunStatsChangeShouldSendTestRunStatisticsToClient()
        {
            this.testRunEventHandler.HandleTestRunStatsChange(null);
            this.mockClient.Verify(th => th.SendTestRunStatistics(null), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldInformClient()
        {
            this.testRunEventHandler.HandleTestRunComplete(null, null, null, null);
            this.mockClient.Verify(th => th.SendExecutionComplete(null, null, null, null), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunMessageShouldSendMessageToClient()
        {
            this.testRunEventHandler.HandleLogMessage(TestMessageLevel.Informational, null);
            this.mockClient.Verify(th => th.SendLog(0, null), Times.AtLeast(1));
        }
    }
}
