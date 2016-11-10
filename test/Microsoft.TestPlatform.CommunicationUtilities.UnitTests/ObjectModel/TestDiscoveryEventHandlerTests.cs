// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CommunicationUtilities.UnitTests.ObjectModel
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
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
            this.testDiscoveryEventHandler.HandleDiscoveryComplete(0, null, false);
            this.mockClient.Verify(th => th.DiscoveryComplete(0, null, false), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldNotSendASeparateTestFoundMessageToClient()
        {
            this.testDiscoveryEventHandler.HandleDiscoveryComplete(0, null, false);
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
