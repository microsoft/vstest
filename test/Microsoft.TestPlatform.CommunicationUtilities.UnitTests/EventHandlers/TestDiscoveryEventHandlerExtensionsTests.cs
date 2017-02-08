// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.EventHandlers
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class TestDiscoveryEventHandlerExtensionsTests
    {
        private Exception exception;
        private Mock<IDataSerializer> dataSerializerMock;
        private ITestDiscoveryEventsHandler tesDiscoveryEventsHandler;
        private Mock<ITestRequestHandler> testRequestHandler;

        [TestInitialize]
        public void Init()
        {
            this.exception = new Exception("Error Message");
            this.dataSerializerMock = new Mock<IDataSerializer>();
            this.testRequestHandler = new Mock<ITestRequestHandler>();
            this.tesDiscoveryEventsHandler = new TestDiscoveryEventHandler(testRequestHandler.Object);
        }

        [TestMethod]
        public void OnAbortShouldRaiseExceptionWhenDataSerializerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => this.tesDiscoveryEventsHandler.OnAbort(null, exception));
        }

        [TestMethod]
        public void OnAbortShouldCallSendLogAndSendExecutionCompleteIfExceptionIsNull()
        {
            this.tesDiscoveryEventsHandler.OnAbort(dataSerializerMock.Object, null);

            this.testRequestHandler.Verify((rh) => rh.SendLog(TestMessageLevel.Error, It.IsAny<string>()));
            this.testRequestHandler.Verify((rh) => rh.DiscoveryComplete(-1, null, true));
        }

        [TestMethod]
        public void OnAbortShouldCallSendLog()
        {
            var message = string.Empty;
            this.testRequestHandler.Setup((rh) => rh.SendLog(TestMessageLevel.Error, It.IsAny<string>()))
                .Callback<TestMessageLevel, string>((l, m) => message = m);

            this.tesDiscoveryEventsHandler.OnAbort(dataSerializerMock.Object, exception);

            this.testRequestHandler.Verify((rh) => rh.SendLog(TestMessageLevel.Error, message));
            StringAssert.Contains(message, exception.Message);
        }

        [TestMethod]
        public void OnAbortShouldCallSendExecutionComplete()
        {
            this.tesDiscoveryEventsHandler.OnAbort(dataSerializerMock.Object, exception);

            this.testRequestHandler.Verify((rh) => rh.DiscoveryComplete(-1, null, true));
        }
    }
}
