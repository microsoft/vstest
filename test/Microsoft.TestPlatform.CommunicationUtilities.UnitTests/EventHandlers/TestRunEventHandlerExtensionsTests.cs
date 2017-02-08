// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommunicationUtilities.UnitTests.EventHandlers
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class TestRunEventHandlerExtensionsTests
    {
        private Exception exception;
        private Mock<IDataSerializer> dataSerializerMock;
        private ITestRunEventsHandler testRunEventsHandler;
        private Mock<ITestRequestHandler> testRequestHandler;

        [TestInitialize]
        public void Init()
        {
            this.exception = new Exception("Error Message");
            this.dataSerializerMock = new Mock<IDataSerializer>();
            this.testRequestHandler = new Mock<ITestRequestHandler>();
            this.testRunEventsHandler = new TestRunEventsHandler(testRequestHandler.Object);
        }

        [TestMethod]
        public void OnAbortShouldRaiseExceptionWhenDataSerializerIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => this.testRunEventsHandler.OnAbort(null, exception));
        }

        [TestMethod]
        public void OnAbortShouldCallSendLogAndSendExecutionCompleteIfExceptionIsNull()
        {
            this.testRunEventsHandler.OnAbort(dataSerializerMock.Object, null);
            this.testRequestHandler.Verify((rh) => rh.SendLog(TestMessageLevel.Error, It.IsAny<string>()));
            this.testRequestHandler.Verify((rh) => rh.SendExecutionComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null));
        }

        [TestMethod]
        public void OnAbortShouldCallSendLog()
        {
            var message = string.Empty;
            this.testRequestHandler.Setup((rh) => rh.SendLog(TestMessageLevel.Error, It.IsAny<string>()))
                .Callback<TestMessageLevel, string>((l, m) => message = m);

            this.testRunEventsHandler.OnAbort(dataSerializerMock.Object, exception);

            this.testRequestHandler.Verify((rh) => rh.SendLog(TestMessageLevel.Error, message));
            StringAssert.Contains(message, exception.Message);
        }

        [TestMethod]
        public void OnAbortShouldCallSendExecutionComplete()
        {
            TestRunCompleteEventArgs testRunCompleteArgs = null;
            this.testRequestHandler.Setup(rh => rh.SendExecutionComplete(
                    It.IsAny<TestRunCompleteEventArgs>(),
                    It.IsAny<TestRunChangedEventArgs>(),
                    It.IsAny<ICollection<AttachmentSet>>(),
                    It.IsAny<ICollection<string>>()))
                .Callback
                <TestRunCompleteEventArgs, TestRunChangedEventArgs, ICollection<AttachmentSet>, ICollection<string>>
                ((completeArgs, changeArgs, attachments, utis) => { testRunCompleteArgs = completeArgs; });
            this.testRunEventsHandler.OnAbort(dataSerializerMock.Object, exception);

            this.testRequestHandler.Verify( rh => rh.SendExecutionComplete(It.IsAny<TestRunCompleteEventArgs>(), null, null, null));
            Assert.IsNotNull(testRunCompleteArgs);
            Assert.IsTrue(testRunCompleteArgs.IsAborted);
        }
    }
}
