// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace TranslationLayer.UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Moq;

    [TestClass]
    public class DiscoveryCompleteEventHandlerTests
    {
        private Mock<ITestDiscoveryEventsHandler> mockTestDiscoveryCompleteEventHandler;
        public DiscoveryCompleteEventHandlerTests()
        {
            this.mockTestDiscoveryCompleteEventHandler = new Mock<ITestDiscoveryEventsHandler>();
        }

        [TestMethod]
        public void ConstructorShouldThrowArgumentExceptionIfTestDiscoveryEventHandlerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>( () => new DiscoveryEventsHandler(null));
        }

        [TestMethod]
        public void HandleDiscoveryCompleteShouldCallTestDiscoveryHandler1Method()
        {
            var discoveryCompleteEventHandler = new DiscoveryEventsHandler(this.mockTestDiscoveryCompleteEventHandler.Object);

            discoveryCompleteEventHandler.HandleDiscoveryComplete(new DiscoveryCompleteEventArgs(-1, false), null);
            this.mockTestDiscoveryCompleteEventHandler.Verify(o => o.HandleDiscoveryComplete(-1, null, false), Times.Once);
        }

        [TestMethod]
        public void HandleDiscoveryTestShouldCallTestDiscoveryHandler1Method()
        {
            var discoveryCompleteEventHandler = new DiscoveryEventsHandler(this.mockTestDiscoveryCompleteEventHandler.Object);

            discoveryCompleteEventHandler.HandleDiscoveredTests(null);

            this.mockTestDiscoveryCompleteEventHandler.Verify(o => o.HandleDiscoveredTests(null), Times.Once);
        }

        [TestMethod]
        public void HandleRawMessageShouldCallTestDiscoveryHandler1Method()
        {
            var discoveryCompleteEventHandler = new DiscoveryEventsHandler(this.mockTestDiscoveryCompleteEventHandler.Object);

            discoveryCompleteEventHandler.HandleRawMessage("DummyMessage");

            this.mockTestDiscoveryCompleteEventHandler.Verify(o => o.HandleRawMessage("DummyMessage"), Times.Once);
        }

        [TestMethod]
        public void HandleLogMessageShouldCallTestDiscoveryHandler1Method()
        {
            var discoveryCompleteEventHandler = new DiscoveryEventsHandler(this.mockTestDiscoveryCompleteEventHandler.Object);

            discoveryCompleteEventHandler.HandleLogMessage(TestMessageLevel.Warning, "DummyMessage");

            this.mockTestDiscoveryCompleteEventHandler.Verify(o => o.HandleLogMessage(TestMessageLevel.Warning, "DummyMessage"), Times.Once);
        }
    }
}
