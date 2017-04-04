// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using vstest.console.UnitTests.TestDoubles;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    [TestClass]
    public class ConsoleLoggerTests
    {
        private Mock<ITestRunRequest> testRunRequest;
        private Mock<TestLoggerEvents> events;
        private Mock<IOutput> mockOutput;
        private TestLoggerManager testLoggerManager;
        private ConsoleLogger consoleLogger;

        [TestInitialize]
        public void Initialize()
        {
            RunTestsArgumentProcessorTests.SetupMockExtensions();

            // Setup Mocks and other dependencies
            this.Setup();
        }

        [TestCleanup]
        public void Cleanup()
        {
            DummyTestLoggerManager.Cleanup();
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.consoleLogger.Initialize(null, string.Empty);
            });
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfEventsIsNotNull()
        {
            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, string.Empty);
        }

        [TestMethod]
        public void InitializeWithParametersShouldThrowExceptionIfEventsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                var parameters = new Dictionary<string, string>();
                parameters.Add("parma1", "value");
                this.consoleLogger.Initialize(null, parameters);
            });
        }

        [TestMethod]
        public void InitializeWithParametersShouldThrowExceptionIfParametersIsEmpty()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, new Dictionary<string, string>());
            });
        }

        [TestMethod]
        public void InitializeWithParametersShouldThrowExceptionIfParametersIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, (Dictionary<string, string>)null);
            });
        }

        [TestMethod]
        public void InitializeWithParametersShouldSetVerbosityLevel()
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "minimal");
            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

            Assert.AreEqual(ConsoleLogger.Verbosity.Minimal, this.consoleLogger.VerbosityLevel);
        }

        [TestMethod]
        public void InitializeWithParametersShouldDefaultToMinimalVerbosityLevelForInvalidVerbosity()
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "random");
            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

            Assert.AreEqual(ConsoleLogger.Verbosity.Minimal, this.consoleLogger.VerbosityLevel);
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
        public void TestMessageHandlerShouldWriteToConsoleIfTestRunEventsAreRaised()
        {
            // Raise events on mock object
            this.testRunRequest.Raise(m => m.TestRunMessage += null, new TestRunMessageEventArgs(TestMessageLevel.Informational, "Informational123"));
            this.testRunRequest.Raise(m => m.TestRunMessage += null, new TestRunMessageEventArgs(TestMessageLevel.Error, "Error123"));
            this.testRunRequest.Raise(m => m.TestRunMessage += null, new TestRunMessageEventArgs(TestMessageLevel.Warning, "Warning123"));
            this.FlushLoggerMessages();

            this.mockOutput.Verify(o => o.WriteLine("Informational123", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("Warning123", OutputLevel.Warning), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("Error123", OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestResultHandlerShouldThowExceptionIfEventArgsIsNull()
        {
            var eventarg = default(TestRunChangedEventArgs);

            // Raise an event on mock object
            Assert.ThrowsException<NullReferenceException>(() =>
            {
                testRunRequest.Raise(m => m.OnRunStatsChange += null, eventarg);
            });
        }

        [TestMethod]
        public void TestResultHandlerShouldWriteToConsoleShouldShowPassedTestsForNormalVebosity()
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(this.events.Object, parameters);

            var eventArgs = new TestRunChangedEventArgs(null, this.GetTestResultsObject(), null);

            // Raise an event on mock object
            this.testRunRequest.Raise(m => m.OnRunStatsChange += null, eventArgs);
            this.FlushLoggerMessages();

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.PassedTestIndicator, "TestName"), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.FailedTestIndicator, "TestName"), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.SkippedTestIndicator, "TestName"), OutputLevel.Warning), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.NotRunTestIndicator, "TestName"), OutputLevel.Information), Times.Exactly(2));
        }

        [TestMethod]
        public void TestResultHandlerShouldWriteToConsoleButSkipPassedTestsForMinimalVerbosity()
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "minimal");
            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

            var eventArgs = new TestRunChangedEventArgs(null, this.GetTestResultsObject(), null);

            // Raise an event on mock object
            this.testRunRequest.Raise(m => m.OnRunStatsChange += null, eventArgs);
            this.FlushLoggerMessages();

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.FailedTestIndicator, "TestName"), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.SkippedTestIndicator, "TestName"), OutputLevel.Warning), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.NotRunTestIndicator, "TestName"), OutputLevel.Information), Times.Exactly(2));
        }

        [TestMethod]
        public void TestResultHandlerShouldWriteToNoTestResultForQuietVerbosity()
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "quiet");
            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

            var eventArgs = new TestRunChangedEventArgs(null, this.GetTestResultsObject(), null);

            // Raise an event on mock object
            this.testRunRequest.Raise(m => m.OnRunStatsChange += null, eventArgs);
            this.FlushLoggerMessages();

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.PassedTestIndicator, "TestName"), OutputLevel.Information), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.FailedTestIndicator, "TestName"), OutputLevel.Information), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.SkippedTestIndicator, "TestName"), OutputLevel.Warning), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.NotRunTestIndicator, "TestName"), OutputLevel.Information), Times.Never);
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
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsPass()
        {
            // Raise an event on mock object raised to register test case count
            var eventArgs = new TestRunChangedEventArgs(null, this.GetTestResultObject(TestOutcome.Passed), null);
            this.testRunRequest.Raise(m => m.OnRunStatsChange += null, eventArgs);

            // Raise an event on mock object
            this.testRunRequest.Raise(m => m.OnRunCompletion += null, new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan(1, 0, 0, 0)));

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, 1, 1, 0, 0), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunSuccessful, OutputLevel.Information), Times.Once());
        }
        
        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsFail()
        {
            // Raise an event on mock object raised to register test case count and mark Outcome as Outcome.Failed
            var eventArgs = new TestRunChangedEventArgs(null, this.GetTestResultObject(TestOutcome.Failed), null);
            this.testRunRequest.Raise(m => m.OnRunStatsChange += null, eventArgs);

            // Raise an event on mock object
            this.testRunRequest.Raise(m => m.OnRunCompletion += null, new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan(1, 0, 0, 0)));

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, 1, 0, 1, 0), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunFailed, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void PrintTimeHandlerShouldPrintElapsedTimeOnConsole()
        {
            // Raise an event on mock object raised to register test case count
            var eventArgs = new TestRunChangedEventArgs(null, this.GetTestResultObject(TestOutcome.Passed), null);
            this.testRunRequest.Raise(m => m.OnRunStatsChange += null, eventArgs);

            // Raise events on mock object
            this.testRunRequest.Raise(m => m.OnRunCompletion += null, new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan(1, 0, 0, 0)));
            this.testRunRequest.Raise(m => m.OnRunCompletion += null, new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan(0, 1, 0, 0)));
            this.testRunRequest.Raise(m => m.OnRunCompletion += null, new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan(0, 0, 1, 0)));
            this.testRunRequest.Raise(m => m.OnRunCompletion += null, new TestRunCompleteEventArgs(null, false, false, null, null, new TimeSpan(0, 0, 0, 1)));

            // Verify PrintTimeSpan with different formats
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Days), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Hours), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Minutes), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Seconds), OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void DisplayFullInformationShouldWriteErrorMessageAndStackTraceToConsole()
        {
            var testresults = this.GetTestResultObject(TestOutcome.Failed);
            testresults[0].ErrorMessage = "ErrorMessage";
            testresults[0].ErrorStackTrace = "ErrorStackTrace";

            var eventArgs = new TestRunChangedEventArgs(null, testresults, null);

            // Raise an event on mock object
            this.testRunRequest.Raise(m => m.OnRunStatsChange += null, eventArgs);
            this.FlushLoggerMessages();

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0}", " ErrorMessage"), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0}", "ErrorStackTrace"), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.ErrorMessageBanner, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StacktraceBanner, OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void GetTestMessagesShouldWriteMessageAndStackTraceToConsole()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var testresults = this.GetTestResultObject(TestOutcome.Failed);
            testresults[0].Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, "StandardOutCategory"));
            testresults[0].Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, "StandardErrorCategory"));
            testresults[0].Messages.Add(new TestResultMessage(TestResultMessage.AdditionalInfoCategory, "AdditionalInfoCategory"));
            testresults[0].Messages.Add(new TestResultMessage(TestResultMessage.AdditionalInfoCategory, "AnotherAdditionalInfoCategory"));
            var eventArgs = new TestRunChangedEventArgs(null, testresults, null);

            // Raise an event on mock object
            this.testRunRequest.Raise(m => m.OnRunStatsChange += null, eventArgs);
            this.FlushLoggerMessages();

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 3, 300);

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdOutMessagesBanner, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" StandardOutCategory", OutputLevel.Information), Times.Once());

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdErrMessagesBanner, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" StandardErrorCategory", OutputLevel.Information), Times.Once());

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.AddnlInfoMessagesBanner, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" AdditionalInfoCategory AnotherAdditionalInfoCategory", OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void AttachmentInformationShouldBeWrittenToConsoleIfAttachmentsArePresent()
        {
            var attachmentSet = new AttachmentSet(new Uri("test://uri"), "myattachmentset");
            var uriDataAttachment = new UriDataAttachment(new Uri("file://server/filename.ext"), "description");
            attachmentSet.Attachments.Add(uriDataAttachment);
            var uriDataAttachment1 = new UriDataAttachment(new Uri("file://server/filename1.ext"), "description");
            attachmentSet.Attachments.Add(uriDataAttachment1);
            var attachmentSetList = new List<AttachmentSet>();
            attachmentSetList.Add(attachmentSet);
            var testRunCompleteEventArgs = new TestRunCompleteEventArgs(null, false, false, null, new Collection<AttachmentSet>(attachmentSetList), new TimeSpan(1, 0, 0, 0));

            // Raise an event on mock object raised to register test case count and mark Outcome as Outcome.Failed
            this.testRunRequest.Raise(m => m.OnRunCompletion += null, testRunCompleteEventArgs);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.AttachmentOutputFormat, uriDataAttachment.Uri.LocalPath), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.AttachmentOutputFormat, uriDataAttachment1.Uri.LocalPath), OutputLevel.Information), Times.Once());
        }

        /// <summary>
        /// Setup Mocks and other dependencies
        /// </summary>
        private void Setup()
        {
            // mock for ITestRunRequest
            this.testRunRequest = new Mock<ITestRunRequest>();
            this.events = new Mock<TestLoggerEvents>();
            this.mockOutput = new Mock<IOutput>();

            this.consoleLogger = new ConsoleLogger(this.mockOutput.Object);

            DummyTestLoggerManager.Cleanup();

            // Create Instance of TestLoggerManager
            this.testLoggerManager = TestLoggerManager.Instance;
            this.testLoggerManager.AddLogger(this.consoleLogger, ConsoleLogger.ExtensionUri, new Dictionary<string, string>());
            this.testLoggerManager.EnableLogging();

            // Register TestRunRequest object
            this.testLoggerManager.RegisterTestRunEvents(this.testRunRequest.Object);
        }

        private void FlushLoggerMessages()
        {
            // Raise a test run complete message to flush out any pending messages in queue
            this.testRunRequest.Raise(
                m => m.OnRunCompletion += null,
                new TestRunCompleteEventArgs(stats: null, isCanceled: false, isAborted: false, error: null, attachmentSets: null, elapsedTime: new TimeSpan(1, 0, 0, 0)));
        }

        private List<ObjectModel.TestResult> GetTestResultsObject()
        {
            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            var testresult = new ObjectModel.TestResult(testcase);
            testresult.Outcome = TestOutcome.Passed;

            var testresult1 = new ObjectModel.TestResult(testcase);
            testresult1.Outcome = TestOutcome.Failed;

            var testresult2 = new ObjectModel.TestResult(testcase);
            testresult2.Outcome = TestOutcome.None;

            var testresult3 = new ObjectModel.TestResult(testcase);
            testresult3.Outcome = TestOutcome.NotFound;

            var testresult4 = new ObjectModel.TestResult(testcase);
            testresult4.Outcome = TestOutcome.Skipped;

            var testresultList = new List<ObjectModel.TestResult> { testresult, testresult1, testresult2, testresult3, testresult4 };

            return testresultList;
        }

        private List<ObjectModel.TestResult> GetTestResultObject(TestOutcome outcome)
        {
            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            var testresult = new ObjectModel.TestResult(testcase);
            testresult.Outcome = outcome;
            var testresultList = new List<ObjectModel.TestResult> { testresult };
            return testresultList;
        }
    }
}
