// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using Microsoft.Extensions.FileSystemGlobbing;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Internal;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using vstest.console.Internal;
    using CommandLineResources = Resources.Resources;

    [TestClass]
    public class ConsoleLoggerTests
    {
        private Mock<IRequestData> mockRequestData;
        private Mock<IMetricsCollection> mockMetricsCollection;
        private Mock<IOutput> mockOutput;
        private ConsoleLogger consoleLogger;
        private Mock<IProgressIndicator> mockProgressIndicator;

        private const string PassedTestIndicator = "  Passed ";
        private const string FailedTestIndicator = "  Failed ";
        private const string SkippedTestIndicator = "  Skipped ";

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
            var parameters = new Dictionary<string, string>
            {
                { "param1", "value" },
            };

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
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
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "minimal" },
                { DefaultLoggerParameterNames.TargetFramework , "net451"}
            };
            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

            Assert.AreEqual(ConsoleLogger.Verbosity.Minimal, this.consoleLogger.VerbosityLevel);
        }

        [TestMethod]
        public void InitializeWithParametersShouldDefaultToNormalVerbosityLevelForInvalidVerbosity()
        {
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "" },
            };

            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

#if NETFRAMEWORK
            Assert.AreEqual(ConsoleLogger.Verbosity.Normal, this.consoleLogger.VerbosityLevel);
#else
            Assert.AreEqual(ConsoleLogger.Verbosity.Minimal, this.consoleLogger.VerbosityLevel);
#endif
        }

        [TestMethod]
        public void InitializeWithParametersShouldSetPrefixValue()
        {
            var parameters = new Dictionary<string, string>
            {
                { "prefix", "true" },
            };
            Assert.IsFalse(ConsoleLogger.AppendPrefix);

            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

            Assert.IsTrue(ConsoleLogger.AppendPrefix);
            ConsoleLogger.AppendPrefix = false;
        }

        [TestMethod]
        public void InitializeWithParametersShouldSetNoProgress()
        {
            var parameters = new Dictionary<string, string>();

            Assert.IsFalse(ConsoleLogger.EnableProgress);

            parameters.Add("progress", "true");
            this.consoleLogger.Initialize(new Mock<TestLoggerEvents>().Object, parameters);

            Assert.IsTrue(ConsoleLogger.EnableProgress);

            ConsoleLogger.EnableProgress = false;
        }

        [TestMethod]
        public void TestMessageHandlerShouldThrowExceptionIfEventArgsIsNull()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();

            Assert.ThrowsException<ArgumentNullException>(() =>
           {
               loggerEvents.RaiseTestRunMessage(default(TestRunMessageEventArgs));
           });
        }

        [TestMethod]
        public void TestMessageHandlerShouldWriteToConsoleWhenTestRunMessageIsRaised()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            this.SetupForTestMessageHandler(out var loggerEvents);

            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Informational, "Informational123"));
            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Error, "Error123"));
            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Warning, "Warning123"));
            loggerEvents.WaitForEventCompletion();

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 3, 300);

            this.AssertsForTestMessageHandler();
            this.mockProgressIndicator.Verify(pi => pi.Pause(), Times.Exactly(3));
            this.mockProgressIndicator.Verify(pi => pi.Start(), Times.Exactly(3));
        }

        [TestMethod]
        public void TestMessageHandlerShouldWriteToConsoleWhenTestDiscoveryMessageIsRaised()
        {
            var count = 0;
            this.mockOutput.Setup(o => o.WriteLine(It.IsAny<string>(), It.IsAny<OutputLevel>())).Callback<string, OutputLevel>(
                (s, o) => { count++; });

            this.SetupForTestMessageHandler(out var loggerEvents);

            loggerEvents.RaiseDiscoveryMessage(new TestRunMessageEventArgs(TestMessageLevel.Informational, "Informational123"));
            loggerEvents.RaiseDiscoveryMessage(new TestRunMessageEventArgs(TestMessageLevel.Error, "Error123"));
            loggerEvents.RaiseDiscoveryMessage(new TestRunMessageEventArgs(TestMessageLevel.Warning, "Warning123"));
            loggerEvents.WaitForEventCompletion();

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 3, 300);

            this.AssertsForTestMessageHandler();
            this.mockProgressIndicator.Verify(pi => pi.Pause(), Times.Exactly(3));
            this.mockProgressIndicator.Verify(pi => pi.Start(), Times.Exactly(3));
        }

        private void AssertsForTestMessageHandler()
        {
            this.mockOutput.Verify(o => o.WriteLine("Informational123", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("Warning123", OutputLevel.Warning), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("Error123", OutputLevel.Error), Times.Once());
        }

        private void SetupForTestMessageHandler(out InternalTestLoggerEvents loggerEvents)
        {
            loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);
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
            loggerEvents.WaitForEventCompletion();

            // Added this for synchronization
            SpinWait.SpinUntil(() => count == 2, 300);

            this.mockOutput.Verify(o => o.WriteLine("  Standard Output Messages:", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void NormalVerbosityShowNotStdOutMessagesForPassedTests()
        {
            // Setup
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "normal" }
            };

            this.consoleLogger.Initialize(loggerEvents, parameters);
            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            string message = "Dummy message";
            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.StandardOutCategory, message);

            var testresult = new ObjectModel.TestResult(testcase);
            testresult.Outcome = TestOutcome.Passed;
            testresult.Messages.Add(testResultMessage);

            // Raise an event on mock object
            loggerEvents.RaiseTestResult(new TestResultEventArgs(testresult));
            loggerEvents.WaitForEventCompletion();

            // Verify
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdOutMessagesBanner, OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void DetailedVerbosityShowStdOutMessagesForPassedTests()
        {
            // Setup
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "detailed" }
            };

            this.consoleLogger.Initialize(loggerEvents, parameters);
            var testcase = new TestCase("TestName", new Uri("some://uri"), "TestSource");
            string message = "Dummy message";

            TestResultMessage testResultMessage = new TestResultMessage(TestResultMessage.StandardOutCategory, message);
            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Passed
            };

            testresult.Messages.Add(testResultMessage);

            // Act. Raise an event on mock object
            loggerEvents.RaiseTestResult(new TestResultEventArgs(testresult));
            loggerEvents.WaitForEventCompletion();

            // Verify
            this.mockOutput.Verify(o => o.WriteLine("  Standard Output Messages:", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void TestRunErrorMessageShowShouldTestRunFailed()
        {
            // Setup
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "detailed" }
            };

            this.consoleLogger.Initialize(loggerEvents, parameters);
            string message = "Adapter Error";

            // Act. Raise an event on mock object
            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Error, message));
            loggerEvents.RaiseTestRunComplete(new TestRunCompleteEventArgs(new Mock<ITestRunStatistics>().Object, false, false, null, new Collection<AttachmentSet>(), TimeSpan.FromSeconds(1)));
            loggerEvents.WaitForEventCompletion();

            // Verify
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunFailed, OutputLevel.Error), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(message, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void InQuietModeTestErrorMessageShouldShowTestRunFailed()
        {
            // Setup
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "quiet" },
                { DefaultLoggerParameterNames.TargetFramework , "abc" }
            };

            this.consoleLogger.Initialize(loggerEvents, parameters);
            string message = "Adapter Error";

            // Act. Raise an event on mock object
            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Error, message));
            loggerEvents.RaiseTestRunComplete(new TestRunCompleteEventArgs(new Mock<ITestRunStatistics>().Object, false, false, null, new Collection<AttachmentSet>(), TimeSpan.FromSeconds(1)));
            loggerEvents.WaitForEventCompletion();

            // Verify
            this.mockOutput.Verify(o => o.WriteLine(message, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void InQuietModeTestWarningMessageShouldNotShow()
        {
            // Setup
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "quiet" },
                { DefaultLoggerParameterNames.TargetFramework , "abc" }
            };

            this.consoleLogger.Initialize(loggerEvents, parameters);
            string message = "Adapter Warning";

            // Act. Raise an event on mock object
            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Warning, message));
            loggerEvents.RaiseTestRunComplete(new TestRunCompleteEventArgs(new Mock<ITestRunStatistics>().Object, false, false, null, new Collection<AttachmentSet>(), TimeSpan.FromSeconds(1)));
            loggerEvents.WaitForEventCompletion();

            // Verify
            this.mockOutput.Verify(o => o.WriteLine(message, OutputLevel.Warning), Times.Never());
        }

        [TestMethod]
        public void InNormalModeTestWarningAndErrorMessagesShouldShow()
        {
            // Setup
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "normal" }
            };

            this.consoleLogger.Initialize(loggerEvents, parameters);
            string message = "Adapter Warning";
            string errorMessage = "Adapter Error";

            // Act. Raise an event on mock object
            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Warning, message));
            loggerEvents.RaiseTestRunMessage(new TestRunMessageEventArgs(TestMessageLevel.Error, errorMessage));
            loggerEvents.RaiseTestRunComplete(new TestRunCompleteEventArgs(new Mock<ITestRunStatistics>().Object, false, false, null, new Collection<AttachmentSet>(), TimeSpan.FromSeconds(1)));
            loggerEvents.WaitForEventCompletion();

            // Verify
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunFailed, OutputLevel.Error), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(message, OutputLevel.Warning), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(errorMessage, OutputLevel.Error), Times.Once());
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
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdOutMessagesBanner, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void TestResultHandlerShouldShowStdErrMessagesBannerIfStdErrIsNotEmpty()
        {
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
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine("  Standard Error Messages:", OutputLevel.Information), Times.Once());
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
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.StdErrMessagesBanner, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void TestResultHandlerShouldShowAdditionalInfoBannerIfAdditionalInfoIsNotEmpty()
        {
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
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine("  Additional Information Messages:", OutputLevel.Information), Times.Once());
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
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.AddnlInfoMessagesBanner, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void TestResultHandlerShouldShowPassedTestsForNormalVebosity()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultsObject())
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.Write(PassedTestIndicator, OutputLevel.Information), Times.Once);
            this.mockOutput.Verify(o => o.WriteLine("TestName [1 h 2 m]", OutputLevel.Information), Times.Once);
            this.mockOutput.Verify(o => o.Write(FailedTestIndicator, OutputLevel.Information), Times.Once);
            this.mockOutput.Verify(o => o.WriteLine("TestName [4 m 5 s]", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.Write(SkippedTestIndicator, OutputLevel.Information), Times.Exactly(3));
            this.mockOutput.Verify(o => o.WriteLine("TestName", OutputLevel.Information), Times.Exactly(3));
            this.mockProgressIndicator.Verify(pi => pi.Pause(), Times.Exactly(5));
            this.mockProgressIndicator.Verify(pi => pi.Start(), Times.Exactly(5));
        }

        [DataRow(".NETFramework,version=v4.5.1", "(net451)", "quiet")]
        [DataRow(".NETFramework,version=v4.5.1", "(net451)", "minimal")]
        [DataRow(null, null, "quiet")]
        [DataRow(null, null, "minimal")]
        [TestMethod]
        public void TestResultHandlerShouldShowFailedTestsAndPassedTestsForQuietVebosity(string framework, string expectedFramework, string verbosityLevel)
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", verbosityLevel },
                { DefaultLoggerParameterNames.TargetFramework , framework}
            };
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultsObject())
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }

            foreach (var testResult in this.GetPassedTestResultsObject())
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }

            loggerEvents.RaiseTestRunComplete(new TestRunCompleteEventArgs(new Mock<ITestRunStatistics>().Object, false, false, null, new Collection<AttachmentSet>(), TimeSpan.FromSeconds(1)));
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.Write(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, 
                (CommandLineResources.PassedTestIndicator + "!").PadRight(8),
                0.ToString().PadLeft(5), 
                1.ToString().PadLeft(5), 
                1.ToString().PadLeft(5), 2
                .ToString().PadLeft(5), 
                "1 m 2 s"), OutputLevel.Information), Times.Once);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryAssemblyAndFramework, 
                "TestSourcePassed", 
                expectedFramework), OutputLevel.Information), Times.Once);    
            
            this.mockOutput.Verify(o => o.Write(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, 
                (CommandLineResources.FailedTestIndicator + "!").PadRight(8),
                1.ToString().PadLeft(5),
                1.ToString().PadLeft(5),
                1.ToString().PadLeft(5),
                3.ToString().PadLeft(5), 
                "1 h 6 m"), OutputLevel.Information), Times.Once);

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryAssemblyAndFramework, 
                "TestSource", 
                expectedFramework), OutputLevel.Information), Times.Once);
        }

        [TestMethod]
        [DataRow("normal")]
        [DataRow("detailed")]
        public void TestResultHandlerShouldNotShowformattedFailedTestsAndPassedTestsForOtherThanQuietVebosity(string verbosityLevel)
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", verbosityLevel },
                { DefaultLoggerParameterNames.TargetFramework , "net451"}
            };
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultsObject())
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }

            foreach (var testResult in this.GetPassedTestResultsObject())
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }

            loggerEvents.RaiseTestRunComplete(new TestRunCompleteEventArgs(new Mock<ITestRunStatistics>().Object, false, false, null, new Collection<AttachmentSet>(), TimeSpan.FromSeconds(1)));
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, CommandLineResources.PassedTestIndicator, 2, 1, 0, 1, "1 m 2 s", "TestSourcePassed", "(net451)"), OutputLevel.Information), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, CommandLineResources.FailedTestIndicator, 5, 1, 1, 1, "1 h 6 m", "TestSource", "(net451)"), OutputLevel.Information), Times.Never);
        }

        [TestMethod]
        public void TestResultHandlerShouldNotShowNotStdOutMsgOfPassedTestIfVerbosityIsNormal()
        {
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
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine("", OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void TestResultHandlerShouldNotShowDbgTrcMsg()
        {
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
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.DbgTrcMessagesBanner, OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(" " + message, OutputLevel.Information), Times.Never());
        }

        [TestMethod]
        public void TestResultHandlerShouldWriteToConsoleButSkipPassedTestsForMinimalVerbosity()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "minimal" },
                { DefaultLoggerParameterNames.TargetFramework , "net451"}
            };
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultsObject())
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.Write(PassedTestIndicator, OutputLevel.Information), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine("TestName [1 h 2 m]", OutputLevel.Information), Times.Never);
            this.mockOutput.Verify(o => o.Write(FailedTestIndicator, OutputLevel.Information), Times.Once);
            this.mockOutput.Verify(o => o.WriteLine("TestName [4 m 5 s]", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.Write(SkippedTestIndicator, OutputLevel.Information), Times.Exactly(3));
            this.mockOutput.Verify(o => o.WriteLine("TestName", OutputLevel.Information), Times.Exactly(3));
        }

        [TestMethod]
        public void TestResultHandlerShouldWriteToNoTestResultForQuietVerbosity()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>
            {
                { "verbosity", "quiet" },
                { DefaultLoggerParameterNames.TargetFramework , "net451"}
            };
            this.consoleLogger.Initialize(loggerEvents, parameters);

            foreach (var testResult in this.GetTestResultsObject())
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.PassedTestIndicator, "TestName [1 h 2 m]"), OutputLevel.Information), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.FailedTestIndicator, "TestName [4 m 5 s]"), OutputLevel.Information), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.SkippedTestIndicator, "TestName"), OutputLevel.Warning), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.NotRunTestIndicator, "TestName"), OutputLevel.Information), Times.Never);
        }

        [DataRow("[1 h 2 m]", new int[5] { 0, 1, 2, 3, 78 })]
        [DataRow("[4 m 3 s]", new int[5] { 0, 0, 4, 3, 78 })]
        [DataRow("[3 s]", new int[5] { 0, 0, 0, 3, 78 })]
        [DataRow("[78 ms]", new int[5] { 0, 0, 0, 0, 78 })]
        [DataRow("[1 h]", new int[5] { 0, 1, 0, 5, 78 })]
        [DataRow("[5 m]", new int[5] { 0, 0, 5, 0, 78 })]
        [DataRow("[4 s]", new int[5] { 0, 0, 0, 4, 0 })]
        [DataTestMethod]
        public void TestResultHandlerForTestResultWithDurationShouldPrintDurationInfo(string expectedDuration, int[] timeSpanArgs)
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);
            var TestResultWithHrMinSecMs = new ObjectModel.TestResult(new TestCase("DymmyNamespace.DummyClass.TestName", new Uri("some://uri"), "TestSource") { DisplayName = "TestName" })
            {
                Outcome = TestOutcome.Passed,
                Duration = new TimeSpan(timeSpanArgs[0], timeSpanArgs[1], timeSpanArgs[2], timeSpanArgs[3], timeSpanArgs[4])
            };

            loggerEvents.RaiseTestResult(new TestResultEventArgs(TestResultWithHrMinSecMs));
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.Write(PassedTestIndicator, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("TestName " + expectedDuration, OutputLevel.Information), Times.Once());
        }

        [DataTestMethod]
        public void TestResultHandlerForTestResultWithDurationLessThanOneMsShouldPrintDurationInfo()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);
            var TestResultWithHrMinSecMs = new ObjectModel.TestResult(new TestCase("DymmyNamespace.DummyClass.TestName", new Uri("some://uri"), "TestSource") { DisplayName = "TestName" })
            {
                Outcome = TestOutcome.Passed,
                Duration = TimeSpan.FromTicks(50)
            };

            loggerEvents.RaiseTestResult(new TestResultEventArgs(TestResultWithHrMinSecMs));
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.Write(PassedTestIndicator, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("TestName [< 1 ms]", OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsPass()
        {
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

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryTotalTests, 1), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryPassedTests, 1), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryFailedTests, 0), OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummarySkippedTests, 0), OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunSuccessful, OutputLevel.Information), Times.Once());
            this.mockProgressIndicator.Verify(pi => pi.Stop(), Times.Once);
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsFail()
        {
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

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryTotalTests, 1), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryFailedTests, 1), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryPassedTests, 0), OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummarySkippedTests, 0), OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunFailed, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsCanceled()
        {
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

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryForCanceledOrAbortedRun), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryFailedTests, 1), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryPassedTests, 0), OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummarySkippedTests, 0), OutputLevel.Information), Times.Never());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunCanceled, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsCanceledWithoutRunningAnyTest()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            loggerEvents.CompleteTestRun(null, true, false, null, null, new TimeSpan(1, 0, 0, 0));

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunCanceled, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsAborted()
        {
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

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryForCanceledOrAbortedRun), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunAborted, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunCompleteHandlerShouldWriteToConsoleIfTestsAbortedWithoutRunningAnyTest()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            loggerEvents.CompleteTestRun(null, false, true, null, null, new TimeSpan(1, 0, 0, 0));

            this.mockOutput.Verify(o => o.WriteLine(CommandLineResources.TestRunAborted, OutputLevel.Error), Times.Once());
        }

        [TestMethod]
        public void TestRunStartHandlerShouldWriteNumberOfTestSourcesDiscoveredOnConsole()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();

            var fileHelper = new Mock<IFileHelper>();
            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.FileHelper = fileHelper.Object;
            CommandLineOptions.Instance.FilePatternParser = new FilePatternParser(new Mock<Matcher>().Object, fileHelper.Object);
            string testFilePath = "C:\\DummyTestFile.dll";
            fileHelper.Setup(fh => fh.Exists(testFilePath)).Returns(true);

            CommandLineOptions.Instance.AddSource(testFilePath);

            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testRunStartEventArgs = new TestRunStartEventArgs(new TestRunCriteria(new List<string> { "C:\\DummyTestFile.dll" }, 1));
            loggerEvents.RaiseTestRunStart(testRunStartEventArgs);
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestSourcesDiscovered, CommandLineOptions.Instance.Sources.Count()), OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void TestRunStartHandlerShouldWriteTestSourcesDiscoveredOnConsoleIfVerbosityDetailed()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();

            var fileHelper = new Mock<IFileHelper>();
            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.FileHelper = fileHelper.Object;
            CommandLineOptions.Instance.FilePatternParser = new FilePatternParser(new Mock<Matcher>().Object, fileHelper.Object);
            string testFilePath = "C:\\DummyTestFile.dll";
            fileHelper.Setup(fh => fh.Exists(testFilePath)).Returns(true);
            string testFilePath2 = "C:\\DummyTestFile2.dll";
            fileHelper.Setup(fh => fh.Exists(testFilePath2)).Returns(true);

            CommandLineOptions.Instance.AddSource(testFilePath);
            CommandLineOptions.Instance.AddSource(testFilePath2);

            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "detailed");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testRunStartEventArgs = new TestRunStartEventArgs(new TestRunCriteria(new List<string> { "C:\\DummyTestFile.dll" }, 1));
            loggerEvents.RaiseTestRunStart(testRunStartEventArgs);
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestSourcesDiscovered, CommandLineOptions.Instance.Sources.Count()), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("C:\\DummyTestFile.dll", OutputLevel.Information), Times.Once);
            this.mockOutput.Verify(o => o.WriteLine("C:\\DummyTestFile2.dll", OutputLevel.Information), Times.Once);
        }

        [TestMethod]
        public void TestRunStartHandlerShouldNotWriteTestSourcesDiscoveredOnConsoleIfVerbosityNotDetailed()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();

            var fileHelper = new Mock<IFileHelper>();
            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.FileHelper = fileHelper.Object;
            CommandLineOptions.Instance.FilePatternParser = new FilePatternParser(new Mock<Matcher>().Object, fileHelper.Object);
            string testFilePath = "C:\\DummyTestFile.dll";
            fileHelper.Setup(fh => fh.Exists(testFilePath)).Returns(true);
            string testFilePath2 = "C:\\DummyTestFile2.dll";
            fileHelper.Setup(fh => fh.Exists(testFilePath2)).Returns(true);

            CommandLineOptions.Instance.AddSource(testFilePath);
            CommandLineOptions.Instance.AddSource(testFilePath2);

            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testRunStartEventArgs = new TestRunStartEventArgs(new TestRunCriteria(new List<string> { "C:\\DummyTestFile.dll" }, 1));
            loggerEvents.RaiseTestRunStart(testRunStartEventArgs);
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestSourcesDiscovered, CommandLineOptions.Instance.Sources.Count()), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("C:\\DummyTestFile.dll", OutputLevel.Information), Times.Never);
            this.mockOutput.Verify(o => o.WriteLine("C:\\DummyTestFile2.dll", OutputLevel.Information), Times.Never);
        }

        [TestMethod]
        public void PrintTimeHandlerShouldPrintElapsedTimeOnConsole()
        {
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

            // Verify PrintTimeSpan with different formats
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Days), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Hours), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Minutes), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, 1, CommandLineResources.Seconds), OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void DisplayFullInformationShouldWriteErrorMessageAndStackTraceToConsole()
        {
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
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0}", "   ErrorMessage"), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0}", "  ErrorStackTrace"), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("  Error Message:", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("  Stack Trace:", OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void DisplayFullInformationShouldWriteStdMessageWithNewLine()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "detailed");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            var testresults = this.GetTestResultObject(TestOutcome.Passed);
            testresults[0].Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, "Hello"));

            foreach (var testResult in testresults)
            {
                loggerEvents.RaiseTestResult(new TestResultEventArgs(testResult));
            }
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.Write(PassedTestIndicator, OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine("TestName", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" Hello", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(String.Empty, OutputLevel.Information), Times.AtLeastOnce);
        }

        [TestMethod]
        public void GetTestMessagesShouldWriteMessageAndStackTraceToConsole()
        {
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
            loggerEvents.WaitForEventCompletion();

            this.mockOutput.Verify(o => o.WriteLine("  Standard Output Messages:", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" StandardOutCategory", OutputLevel.Information), Times.Once());

            this.mockOutput.Verify(o => o.WriteLine("  Standard Error Messages:", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" StandardErrorCategory", OutputLevel.Information), Times.Once());

            this.mockOutput.Verify(o => o.WriteLine("  Additional Information Messages:", OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(" AdditionalInfoCategory AnotherAdditionalInfoCategory", OutputLevel.Information), Times.Once());
        }

        [DataRow("quiet")]
        [DataRow("Normal")]
        [DataRow("minimal")]
        [DataRow("detailed")]
        [TestMethod]
        public void AttachmentInformationShouldBeWrittenToConsoleIfAttachmentsArePresent(string verbosityLevel)
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", verbosityLevel);
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

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.AttachmentOutputFormat, uriDataAttachment.Uri.LocalPath), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.AttachmentOutputFormat, uriDataAttachment1.Uri.LocalPath), OutputLevel.Information), Times.Once());
        }

        [TestMethod]
        public void ResultsInHeirarchichalOrderShouldReportCorrectCount()
        {
            var loggerEvents = new InternalTestLoggerEvents(TestSessionMessageLogger.Instance);
            loggerEvents.EnableEvents();
            var parameters = new Dictionary<string, string>();
            parameters.Add("verbosity", "normal");
            this.consoleLogger.Initialize(loggerEvents, parameters);

            TestCase testCase1 = CreateTestCase("TestCase1");
            TestCase testCase2 = CreateTestCase("TestCase2");
            TestCase testCase3 = CreateTestCase("TestCase3");

            Guid parentExecutionId = Guid.NewGuid();
            TestProperty ParentExecIdProperty = TestProperty.Register("ParentExecId", "ParentExecId", typeof(Guid), TestPropertyAttributes.Hidden, typeof(ObjectModel.TestResult));
            TestProperty ExecutionIdProperty = TestProperty.Register("ExecutionId", "ExecutionId", typeof(Guid), TestPropertyAttributes.Hidden, typeof(ObjectModel.TestResult));
            TestProperty TestTypeProperty = TestProperty.Register("TestType", "TestType" , typeof(Guid), TestPropertyAttributes.Hidden, typeof(ObjectModel.TestResult));

            var result1 = new ObjectModel.TestResult(testCase1) { Outcome = TestOutcome.Failed };
            result1.SetPropertyValue(ExecutionIdProperty, parentExecutionId);

            var result2 = new ObjectModel.TestResult(testCase2) { Outcome = TestOutcome.Passed};
            result2.SetPropertyValue(ExecutionIdProperty, Guid.NewGuid());
            result2.SetPropertyValue(ParentExecIdProperty, parentExecutionId);

            var result3 = new ObjectModel.TestResult(testCase3) { Outcome = TestOutcome.Failed };
            result3.SetPropertyValue(ExecutionIdProperty, Guid.NewGuid());
            result3.SetPropertyValue(ParentExecIdProperty, parentExecutionId);

            loggerEvents.RaiseTestResult(new TestResultEventArgs(result1));
            loggerEvents.RaiseTestResult(new TestResultEventArgs(result2));
            loggerEvents.RaiseTestResult(new TestResultEventArgs(result3));

            loggerEvents.CompleteTestRun(null, false, false, null, null, new TimeSpan(1, 0, 0, 0));

            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryFailedTests, 1), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryPassedTests, 1), OutputLevel.Information), Times.Once());
            this.mockOutput.Verify(o => o.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummaryTotalTests, 2), OutputLevel.Information), Times.Once());
        }

        private TestCase CreateTestCase(string testCaseName)
        {
            return new TestCase(testCaseName, new Uri("some://uri"), "DummySourceFileName");
        }

        private void Setup()
        {
            this.mockRequestData = new Mock<IRequestData>();
            this.mockMetricsCollection = new Mock<IMetricsCollection>();
            mockRequestData.Setup(rd => rd.MetricsCollection).Returns(mockMetricsCollection.Object);

            this.mockOutput = new Mock<IOutput>();
            this.mockProgressIndicator = new Mock<IProgressIndicator>();
            this.consoleLogger = new ConsoleLogger(this.mockOutput.Object, this.mockProgressIndicator.Object);
        }

        private List<ObjectModel.TestResult> GetTestResultsObject()
        {
            var testcase = new TestCase("DymmyNamespace.DummyClass.TestName", new Uri("some://uri"), "TestSource")
            {
                DisplayName = "TestName"
            };

            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Passed,
                Duration = new TimeSpan(1, 2, 3)
            };

            var testresult1 = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Failed,
                Duration = new TimeSpan(0, 0, 4, 5, 60)
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

        private List<ObjectModel.TestResult> GetPassedTestResultsObject()
        {
            var testcase = new TestCase("DymmyNamespace.DummyClass.TestName", new Uri("some://uri"), "TestSourcePassed")
            {
                DisplayName = "TestName"
            };

            var testresult = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Passed,
                Duration = new TimeSpan(0, 0, 1, 2, 3)
            };

            var testresult1 = new ObjectModel.TestResult(testcase)
            {
                Outcome = TestOutcome.Skipped
            };

            var testresultList = new List<ObjectModel.TestResult> { testresult, testresult1 };

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
