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
            this.mockClient = new Mock<ITestRequestHandler>();
            this.testDiscoveryEventHandler = new TestDiscoveryEventHandler(this.mockClient.Object);
        }

        [TestMethod]
        public void HandleDiscoveredTestShouldSendTestCasesToClient()
        {
            this.testDiscoveryEventHandler.HandleDiscoveredTests(null);
            this.mockClient.Verify(th => th.SendTestCases(null), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldInformClient()
        {
            var discoveryCompleteEventArgs = new DiscoveryCompleteEventArgs(0, false);

            this.testDiscoveryEventHandler.HandleDiscoveryComplete(discoveryCompleteEventArgs, null);
            this.mockClient.Verify(th => th.DiscoveryComplete(discoveryCompleteEventArgs, null), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldNotSendASeparateTestFoundMessageToClient()
        {
            this.testDiscoveryEventHandler.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(0, false), null);

            this.mockClient.Verify(th => th.SendTestCases(null), Times.Never);
        }

        [TestMethod]
        public void HandleDiscoveryMessageShouldSendMessageToClient()
        {
            this.testDiscoveryEventHandler.HandleLogMessage(TestMessageLevel.Informational, string.Empty);

            this.mockClient.Verify(th => th.SendLog(TestMessageLevel.Informational, string.Empty), Times.AtLeast(1));
        }
    }
}
