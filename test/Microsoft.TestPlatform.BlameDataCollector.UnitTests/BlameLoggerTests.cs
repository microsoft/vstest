// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.BlameDataCollector.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.TestPlatform.BlameDataCollector;
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Moq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;

    [TestClass]
    public class BlameLoggerTests
    {
        private Mock<ITestRunRequest> testRunRequest;
        private Mock<TestLoggerEvents> events;
        private Mock<IOutput> mockOutput;
        private TestLoggerManager testLoggerManager;
        private BlameLogger blameLogger;

        public BlameLoggerTests()
        {
            Setup();
        }
  
        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.blameLogger.Initialize(null, string.Empty);
            });
        }

        [TestMethod]
        public void TestMessageHandlerShouldThrowExceptionIfEventArgsIsNull()
        {
            // Raise an event on mock object
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.testRunRequest.Raise(m => m.TestRunMessage += null, default(TestRunMessageEventArgs));
            });
        }

        [TestMethod]
        public void TestResulCompleteHandlerShouldThowExceptionIfEventArgsIsNull()
        {
            // Raise an event on mock object
            Assert.ThrowsException<NullReferenceException>(() =>
            {
                this.testRunRequest.Raise(m => m.OnRunCompletion += null, default(TestRunCompleteEventArgs));
            });
        }
        private void Setup()
        {
            // mock for ITestRunRequest
            this.testRunRequest = new Mock<ITestRunRequest>();
            this.events = new Mock<TestLoggerEvents>();
            this.mockOutput = new Mock<IOutput>();

            this.blameLogger = new BlameLogger(this.mockOutput.Object);

            // Create Instance of TestLoggerManager
            this.testLoggerManager = TestLoggerManager.Instance;
            this.testLoggerManager.AddLogger(this.blameLogger, BlameLogger.ExtensionUri, null);
            this.testLoggerManager.EnableLogging();

            // Register TestRunRequest object
            this.testLoggerManager.RegisterTestRunEvents(this.testRunRequest.Object);
        }

    }
}
