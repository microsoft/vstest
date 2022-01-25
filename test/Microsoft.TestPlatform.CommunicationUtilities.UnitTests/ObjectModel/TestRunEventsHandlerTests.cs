// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.ObjectModel
{
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
            mockClient = new Mock<ITestRequestHandler>();
            testRunEventHandler = new TestRunEventsHandler(mockClient.Object);
        }

        [TestMethod]
        public void HandleTestRunStatsChangeShouldSendTestRunStatisticsToClient()
        {
            testRunEventHandler.HandleTestRunStatsChange(null);
            mockClient.Verify(th => th.SendTestRunStatistics(null), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunCompleteShouldInformClient()
        {
            testRunEventHandler.HandleTestRunComplete(null, null, null, null);
            mockClient.Verify(th => th.SendExecutionComplete(null, null, null, null), Times.Once);
        }

        [TestMethod]
        public void HandleTestRunMessageShouldSendMessageToClient()
        {
            testRunEventHandler.HandleLogMessage(TestMessageLevel.Informational, string.Empty);

            mockClient.Verify(th => th.SendLog(TestMessageLevel.Informational, string.Empty), Times.AtLeast(1));
        }
    }
}
