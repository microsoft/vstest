// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.ObjectModel
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class TestDiscoveryEventHandlerTests
    {
        private Mock<ITestRequestHandler> mockClient;
        private TestDiscoveryEventHandler testDiscoveryEventHandler;

        [TestInitialize]
        public void InitializeTests()
        {
            mockClient = new Mock<ITestRequestHandler>();
            testDiscoveryEventHandler = new TestDiscoveryEventHandler(mockClient.Object);
        }

        [TestMethod]
        public void HandleDiscoveredTestShouldSendTestCasesToClient()
        {
            testDiscoveryEventHandler.HandleDiscoveredTests(null);
            mockClient.Verify(th => th.SendTestCases(null), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldInformClient()
        {
            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(0, false);

            testDiscoveryEventHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, null);
            mockClient.Verify(th => th.DiscoveryComplete(discoveryCompleteEventArgs, null), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldNotSendASeparateTestFoundMessageToClient()
        {
            testDiscoveryEventHandler.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(0, false), null);

            mockClient.Verify(th => th.SendTestCases(null), Times.Never);
        }

        [TestMethod]
        public void HandleDiscoveryMessageShouldSendMessageToClient()
        {
            testDiscoveryEventHandler.HandleLogMessage(TestMessageLevel.Informational, string.Empty);

            mockClient.Verify(th => th.SendLog(TestMessageLevel.Informational, string.Empty), Times.AtLeast(1));
        }
    }
}
