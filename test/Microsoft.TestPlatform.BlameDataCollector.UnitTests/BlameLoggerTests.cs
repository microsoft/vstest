// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.BlameDataCollector.UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.TestPlatform.BlameDataCollector;
    using System;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Moq;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.TestPlatform.BlameDataCollector.Properties;

    [TestClass]
    public class BlameLoggerTests
    {
        private Mock<ITestRunRequest> testRunRequest;
        private Mock<TestLoggerEvents> events;
        private Mock<IOutput> mockOutput;
        private Mock<IBlameReaderWriter> mockBlameReaderWriter;
        private TestLoggerManager testLoggerManager;
        private BlameLogger blameLogger;


        public BlameLoggerTests()
        {
            // Mock for ITestRunRequest
            this.testRunRequest = new Mock<ITestRunRequest>();
            this.events = new Mock<TestLoggerEvents>();
            this.mockOutput = new Mock<IOutput>();
            this.mockBlameReaderWriter = new Mock<IBlameReaderWriter>();
            this.blameLogger = new BlameLogger(this.mockOutput.Object, mockBlameReaderWriter.Object);

            // Create Instance of TestLoggerManager
            this.testLoggerManager = new DummyTestLoggerManager();
            this.testLoggerManager.AddLogger(this.blameLogger, BlameLogger.ExtensionUri, null);
            this.testLoggerManager.EnableLogging();

            // Register TestRunRequest object
            this.testLoggerManager.RegisterTestRunEvents(this.testRunRequest.Object);
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
        public void TestResulCompleteHandlerShouldThowExceptionIfEventArgsIsNull()
        {
            // Raise an event on mock object
            Assert.ThrowsException<NullReferenceException>(() =>
            {
                this.testRunRequest.Raise(m => m.OnRunCompletion += null, default(TestRunCompleteEventArgs));
            });
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldGetFaultyTestCaseIfTestRunAborted()
        {
            // Initialize
            var attachmentSet = new AttachmentSet(new Uri("test://uri"), "Blame");
            var uriDataAttachment = new UriDataAttachment(new Uri("C:/folder1/sequence.xml"), "description");
            attachmentSet.Attachments.Add(uriDataAttachment);
            var attachmentSetList = new List<AttachmentSet>();
            attachmentSetList.Add(attachmentSet);

            // Initialize Blame Logger
            this.blameLogger.Initialize(this.events.Object, null);

            List<TestCase> testCaseList = new List<TestCase>();
            testCaseList.Add(new TestCase("ABC.UnitTestMethod1", new Uri("test://uri"), "C://test/filepath"));
            testCaseList.Add(new TestCase("ABC.UnitTestMethod2", new Uri("test://uri"), "C://test/filepath"));

            // Setup and Raise event
            this.mockBlameReaderWriter.Setup(x => x.ReadTestSequence(It.IsAny<string>())).Returns(testCaseList);
            this.testRunRequest.Raise(
               m => m.OnRunCompletion += null,
               new TestRunCompleteEventArgs(stats: null, isCanceled: false, isAborted: true, error: null, attachmentSets: new Collection<AttachmentSet>(attachmentSetList), elapsedTime: new TimeSpan(1, 0, 0, 0)));

            // Verify Call
            this.mockBlameReaderWriter.Verify(x => x.ReadTestSequence(It.IsAny<string>()), Times.Once);
        }

        internal class DummyTestLoggerManager : TestLoggerManager
        {
            public DummyTestLoggerManager() : base(TestSessionMessageLogger.Instance, new InternalTestLoggerEvents(TestSessionMessageLogger.Instance))
            {
            }
        }
    }
}
