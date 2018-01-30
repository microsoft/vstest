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
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    [TestClass]
    public class ConsoleLoggerTests
    {
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;
        private Mock<IOutput> mockOutput;
        private ConsoleLogger consoleLogger;

        [TestInitialize]
        public void Initialize()
        {
            RunTestsArgumentProcessorTests.SetupMockExtensions();

            // Setup Mocks and other dependencies
            this.Setup();
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
        public void InitializeWithParametersShouldDefaultToNormalVerbosityLevelForInvalidVerbosity()
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "random");
            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

#if NET451
            Assert.AreEqual(ConsoleLogger.Verbosity.Normal, this.consoleLogger.VerbosityLevel);
#else
            Assert.AreEqual(ConsoleLogger.Verbosity.Minimal, this.consoleLogger.VerbosityLevel);
#endif
        }

        [TestMethod]
        public void InitializeWithParametersShouldSetPrefixValue()
        {
            var parameters = new Dictionary<string, string>();

            Assert.IsFalse(ConsoleLogger.AppendPrefix);

            parameters.Add("prefix", "true");
            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

            Assert.IsTrue(ConsoleLogger.AppendPrefix);

            ConsoleLogger.AppendPrefix = false;
        }

        [TestMethod]
        public void TestMessageHandlerShouldThrowExceptionIfEventArgsIsNull()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();

            Assert.ThrowsException<ArgumentNullException>( () =>
            {
                loggerEvents.RaiseTestRunMessage(default(TestRunMessageEventArgs));
            });
        }

        [TestMethod]
        public void TestMessageHandlerShouldWriteToConsoleIfTestRunEventsAreRaised()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Informational, "Informational123"));
            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Error, "Error123"));
            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Warning, "Warning123"));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 3, 300);

            this.mockOutput.Verify(o => o.WriteLine("Informational123", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("Warning123", OutputLevel.Warning), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("Error123", OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestResultHandlerShouldThowExceptionIfEventArgsIsNull()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                loggerEvents.RaiseTestResult(default(TestResultEventArgs));
            });
        }

        [TestMethod]
        public void TestResultHandlerShouldShowStdOutMessagesBannerIfStdOutIsNotEmpty()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "normal" }
            };
            this.consoleLogger.Initialize(loggerEvents, parameters);

            string message = "Dummy message";
            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.StandardOutCategory, message);
            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Failed
            };
            testresult.Messages.Add(testResultMessage);

            loggerEvents.RaiseTestResult(new TestResultEventArgs(testresult));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdOutMessagesBanner, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void NormalVerbosityShowNotStdOutMessagesForPassedTests()
        {
            // Setup
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "normal" }
            };

            this.consoleLogger.Initialize(this.events.Object, parameters);
            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            string message = "Dummy message";
            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.StandardOutCategory, message);

            var testresult = new ObjectModel.TestResult(testcase);
            testresult.Outcome = TestOutcome.Passed;
            testresult.Messages.Add(testResultMessage);

            var eventArgs = new TestRunChangedEventArgs(null, new List<ObjectModel.TestResult> { testresult }, null);

            // Raise an event on mock object
            this.testRunRequest.Raise(m => m.OnRunStatsChange += null, eventArgs);
            this.FlushLoggerMessages();

            // Verify
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdOutMessagesBanner, OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void DetailedVerbosityShowStdOutMessagesForPassedTests()
        {
            // Setup
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "detailed" }
            };

            this.consoleLogger.Initialize(this.events.Object, parameters);
            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            string message = "Dummy message";
            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.StandardOutCategory, message);

            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Passed
            };

            testresult.Messages.Add(testResultMessage);
            var eventArgs = new TestRunChangedEventArgs(null, new List<ObjectModel.TestResult> { testresult }, null);

            // Act. Raise an event on mock object
            this.testRunRequest.Raise(m => m.OnRunStatsChange += null, eventArgs);
            this.FlushLoggerMessages();

            // Verify
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdOutMessagesBanner, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void TestResultHandlerShouldNotShowStdOutMessagesBannerIfStdOutIsEmpty()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "normal" }
            };
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.StandardOutCategory, null);
            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Failed
            };
            testresult.Messages.Add(testResultMessage);

            loggerEvents.RaiseTestResult(new TestResultEventArgs(testresult));

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdOutMessagesBanner, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void TestResultHandlerShouldShowStdErrMessagesBannerIfStdErrIsNotEmpty()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            string message = "Dummy message";
            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.StandardErrorCategory, message);
            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Failed
            };
            testresult.Messages.Add(testResultMessage);

            loggerEvents.RaiseTestResult(new TestResultEventArgs(testresult));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdErrMessagesBanner, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void TestResultHandlerShouldNotShowStdErrMessagesBannerIfStdErrIsEmpty()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "normal" }
            };
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.StandardErrorCategory, null);
            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Failed
            };
            testresult.Messages.Add(testResultMessage);

            loggerEvents.RaiseTestResult(new TestResultEventArgs(testresult));

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdErrMessagesBanner, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void TestResultHandlerShouldShowAdditionalInfoBannerIfAdditionalInfoIsNotEmpty()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            string message = "Dummy message";
            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.AdditionalInfoCategory, message);
            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Failed
            };
            testresult.Messages.Add(testResultMessage);

            loggerEvents.RaiseTestResult(new TestResultEventArgs(testresult));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.AddnlInfoMessagesBanner, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void TestResultHandlerShouldNotShowAdditionalInfoBannerIfAdditionalInfoIsEmpty()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "normal" }
            };
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");

            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.AdditionalInfoCategory, null);

            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Failed
            };
            testresult.Messages.Add(testResultMessage);

            loggerEvents.RaiseTestResult(new TestResultEventArgs(testresult));

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.AddnlInfoMessagesBanner, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void TestResultHandlerShouldWriteToConsoleShouldShowPassedTestsForNormalVebosity()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultsObject())
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 5, 300);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.PassedTestIndicator, "TestName"), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.FailedTestIndicator, "TestName"), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.SkippedTestIndicator, "TestName"), OutputLevel.Warning), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.NotRunTestIndicator, "TestName"), OutputLevel.Information), Times.Exactly(2));
        }

        [TestMethod]
        public void TestResultHandlerShouldShowNotStdOutMsgOfPassedTestIfVerbosityIsNormal()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            string message = "Dummy message";
            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.StandardOutCategory, message);
            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Passed
            };
            testresult.Messages.Add(testResultMessage);

            loggerEvents.RaiseTestResult(new TestResultEventArgs(testresult));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdOutMessagesBanner, OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void TestResultHandlerShouldShowDbgTrcMsg()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            string message = "Dummy message";
            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.DebugTraceCategory, message);
            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Passed
            };
            testresult.Messages.Add(testResultMessage);

            loggerEvents.RaiseTestResult(new TestResultEventArgs(testresult));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.DbgTrcMessagesBanner, OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Never());
        }


        [TestMethod]
        public void TestResultHandlerShouldWriteToConsoleButSkipPassedTestsForMinimalVerbosity()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "minimal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultsObject())
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 4, 300);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.FailedTestIndicator, "TestName"), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.SkippedTestIndicator, "TestName"), OutputLevel.Warning), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.NotRunTestIndicator, "TestName"), OutputLevel.Information), Times.Exactly(2));
        }

        [TestMethod]
        public void TestResultHandlerShouldWriteToNoTestResultForQuietVerbosity()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "Quiet");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultsObject())
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.PassedTestIndicator, "TestName"), OutputLevel.Information), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.FailedTestIndicator, "TestName"), OutputLevel.Information), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.SkippedTestIndicator, "TestName"), OutputLevel.Warning), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.NotRunTestIndicator, "TestName"), OutputLevel.Information), Times.Never);
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsPass()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultObject(TestOutcome.Passed))
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.CompleteTestRun(null, false, false, null, null, new TimeSpan(1, 0, 0, 0));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, 1, 1, 0, 0), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunSuccessful, OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsFail()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultObject(TestOutcome.Failed))
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.CompleteTestRun(null, false, false, null, null, new TimeSpan(1, 0, 0, 0));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, 1, 0, 1, 0), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunFailed, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsCanceled()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "normal" }
            };
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultObject(TestOutcome.Failed))
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.CompleteTestRun(null, true, false, null, null, new TimeSpan(1, 0, 0, 0));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryForCanceledOrAbortedRun, 0, 1, 0), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunCanceled, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsCanceledWithoutRunningAnyTest()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            loggerEvents.CompleteTestRun(null, true, false, null, null, new TimeSpan(1, 0, 0, 0));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 1, 300);

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunCanceled, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldNotWriteTolatTestToConsoleIfTestsCanceled()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultObject(TestOutcome.Failed))
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.CompleteTestRun(null, true, false, null, null, new TimeSpan(1, 0, 0, 0));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryForCanceledOrAbortedRun, 0, 1, 0), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunCanceled, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldNotWriteTolatTestToConsoleIfTestsAborted()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultObject(TestOutcome.Failed))
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.CompleteTestRun(null, false, true, null, null, new TimeSpan(1, 0, 0, 0));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryForCanceledOrAbortedRun, 0, 1, 0), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunAborted, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsAborted()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultObject(TestOutcome.Failed))
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.CompleteTestRun(null, false, true, null, null, new TimeSpan(1, 0, 0, 0));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryForCanceledOrAbortedRun, 0, 1, 0), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunAborted, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsAbortedWithoutRunningAnyTest()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            loggerEvents.CompleteTestRun(null, false, true, null, null, new TimeSpan(1, 0, 0, 0));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 1, 300);

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunAborted, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void PrintTimeHandlerShouldPrintElapsedTimeOnConsole()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultObject(TestOutcome.Passed))
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.CompleteTestRun(null, false, false, null, null, new TimeSpan(1, 0, 0, 0));
            loggerEvents.CompleteTestRun(null, false, false, null, null, new TimeSpan(0, 1, 0, 0));
            loggerEvents.CompleteTestRun(null, false, false, null, null, new TimeSpan(0, 0, 1, 0));
            loggerEvents.CompleteTestRun(null, false, false, null, null, new TimeSpan(0, 0, 0, 1));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 4, 300);

            // Verify PrintTimeSpan with different formats
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Days), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Hours), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Minutes), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Seconds), OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void DisplayFullInformationShouldWriteErrorMessageAndStackTraceToConsole()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testresults = this.GetTestResultObject(TestOutcome.Failed);
            testresults[0].ErrorMessage = "ErrorMessage";
            testresults[0].ErrorStackTrace = "ErrorStackTrace";
            foreach (var testResult in testresults)
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 4, 300);

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

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testresults = this.GetTestResultObject(TestOutcome.Failed);
            testresults[0].Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, "StandardOutCategory"));
            testresults[0].Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, "StandardErrorCategory"));
            testresults[0].Messages.Add(new TestResultMessage(TestResultMessage.AdditionalInfoCategory, "AdditionalInfoCategory"));
            testresults[0].Messages.Add(new TestResultMessage(TestResultMessage.AdditionalInfoCategory, "AnotherAdditionalInfoCategory"));
            
            foreach (var testResult in testresults)
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 6, 300);

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
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var attachmentSet = new AttachmentSet(new Uri("test://uri"), "myattachmentset");
            var uriDataAttachment = new UriDataAttachment(new Uri("file://server/filename.ext"), "description");
            attachmentSet.Attachments.Add(uriDataAttachment);
            var uriDataAttachment1 = new UriDataAttachment(new Uri("file://server/filename1.ext"), "description");
            attachmentSet.Attachments.Add(uriDataAttachment1);
            var attachmentSetList = new List<AttachmentSet>
            {
                attachmentSet
            };
            loggerEvents.CompleteTestRun(null, false, false, null, new Collection<AttachmentSet>(attachmentSetList), new TimeSpan(1, 0, 0, 0));

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.AttachmentOutputFormat, uriDataAttachment.Uri.LocalPath), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.AttachmentOutputFormat, uriDataAttachment1.Uri.LocalPath), OutputLevel.Information), Times.Once());
        }

        private void Setup()
        {
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            this.mockOutput = new Mock<IOutput>();
            this.consoleLogger = new ConsoleLogger(this.mockOutput.Object);
        }

        private List<ObjectModel.TestResult> GetTestResultsObject()
        {
            var testcase = new TestCase("DymmyNamespace.DummyClass.TestName", new Uri("some://uri"), "TestSource")
            {
                DisplayName = "TestName"
            };

            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Passed
            };

            var testresult1 = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Failed
            };

            var testresult2 = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.None
            };

            var testresult3 = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.NotFound
            };

            var testresult4 = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Skipped
            };

            var testresultList = new List<ObjectModel.TestResult> { testresult, testresult1, testresult2, testresult3, testresult4 };

            return testresultList;
        }

        private List<ObjectModel.TestResult> GetTestResultObject(TestOutcome outcome)
        {
            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = outcome
            };
            var testresultList = new List<ObjectModel.TestResult> { testresult };
            return testresultList;
        }
    }
}
