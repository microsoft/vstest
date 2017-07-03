// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Constants = vstest.console.ConsoleConstants;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
    
    /// <summary>
    /// Logger for sending output to the console.
    /// All the console logger messages prints to Standard Output with respective color, except OutputLevel.Error messages
    /// from adapters and test run result on failed.
    /// </summary>
    [FriendlyName(ConsoleLogger.FriendlyName)]
    [ExtensionUri(ConsoleLogger.ExtensionUri)]
    internal class ConsoleLogger : ITestLoggerWithParameters
    {
        #region Constants
        private const string TestMessageFormattingPrefix = " ";

        /// <summary>
        /// Uri used to uniquely identify the console logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/ConsoleLogger/v2";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the console logger.
        /// </summary>
        public const string FriendlyName = "Console";

        /// <summary>
        /// Parameter for Verbosity
        /// </summary>
        public const string VerbosityParam = "verbosity";

        #endregion

        internal enum Verbosity
        {
            Quiet,
            Minimal,
            Normal
        }

        #region Fields

        /// <summary>
        /// Level of verbosity
        /// </summary>
        private Verbosity verbosityLevel = Verbosity.Minimal;

        private TestOutcome testOutcome = TestOutcome.None;
        private int testsTotal = 0;
        private int testsPassed = 0;
        private int testsFailed = 0;
        private int testsSkipped = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ConsoleLogger()
        {
        }

        /// <summary>
        /// Constructor added for testing purpose
        /// </summary>
        /// <param name="output"></param>
        internal ConsoleLogger(IOutput output)
        {
            ConsoleLogger.Output = output;
        }

        #endregion

        #region Properties
        /// <summary>
        /// Gets instance of IOutput used for sending output.
        /// </summary>
        /// <remarks>Protected so this can be detoured for testing purposes.</remarks>
        protected static IOutput Output
        {
            get;
            private set;
        }

        /// <summary>
        /// Get the verbosity level for the console logger
        /// </summary>
        public Verbosity VerbosityLevel => verbosityLevel;

        #endregion

        #region ITestLoggerWithParameters

        /// <summary>
        /// Initializes the Test Logger.
        /// </summary>
        /// <param name="events">Events that can be registered for.</param>
        /// <param name="testRunDirectory">Test Run Directory</param>
        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            if (events == null)
            {
                throw new ArgumentNullException("events");
            }

            if (ConsoleLogger.Output == null)
            {
                ConsoleLogger.Output = ConsoleOutput.Instance;
            }

            // Register for the events.
            events.TestRunMessage += this.TestMessageHandler;
            events.TestResult += this.TestResultHandler;
            events.TestRunComplete += this.TestRunCompleteHandler;
        }

        public void Initialize(TestLoggerEvents events, Dictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (parameters.Count == 0)
            {
                throw new ArgumentException("No default parameters added", nameof(parameters));
            }

            var verbosityExists = parameters.TryGetValue(ConsoleLogger.VerbosityParam, out string verbosity);
            if (verbosityExists && Enum.TryParse(verbosity, true, out Verbosity verbosityLevel))
            {
                this.verbosityLevel = verbosityLevel;
            }

            this.Initialize(events, String.Empty);
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Prints the timespan onto console. 
        /// </summary>
        private static void PrintTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                Output.Information(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, timeSpan.TotalDays, CommandLineResources.Days));
            }
            else if (timeSpan.TotalHours >= 1)
            {
                Output.Information(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, timeSpan.TotalHours, CommandLineResources.Hours));
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                Output.Information(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, timeSpan.TotalMinutes, CommandLineResources.Minutes));
            }
            else
            {
                Output.Information(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, timeSpan.TotalSeconds, CommandLineResources.Seconds));
            }
        }

        /// <summary>
        /// Constructs a well formatted string using the given prefix before every message content on each line.
        /// </summary>
        private static string GetFormattedOutput(Collection<TestResultMessage> testMessageCollection)
        {
            if (testMessageCollection != null)
            {
                var sb = new StringBuilder();
                foreach (var message in testMessageCollection)
                {
                    var prefix = String.Format(CultureInfo.CurrentCulture, "{0}{1}", Environment.NewLine, TestMessageFormattingPrefix);
                    var messageText = message.Text?.Replace(Environment.NewLine, prefix).TrimEnd(TestMessageFormattingPrefix.ToCharArray());

                    if (!string.IsNullOrWhiteSpace(messageText))
                    {
                        sb.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}", TestMessageFormattingPrefix, messageText);
                    }
                }
                return sb.ToString();
            }
            return String.Empty;
        }

        /// <summary>
        /// Collects all the messages of a particular category(Standard Output/Standard Error/Debug Traces) and returns a collection.
        /// </summary>
        private static Collection<TestResultMessage> GetTestMessages(Collection<TestResultMessage> Messages, string requiredCategory)
        {
            var selectedMessages = Messages.Where(msg => msg.Category.Equals(requiredCategory, StringComparison.OrdinalIgnoreCase));
            var requiredMessageCollection = new Collection<TestResultMessage>(selectedMessages.ToList());
            return requiredMessageCollection;
        }

        /// <summary>
        /// outputs the Error messages, Stack Trace, and other messages for the parameter test.
        /// </summary>
        private static void DisplayFullInformation(TestResult result)
        {
            // Add newline if it is not in given output data.
            var addAdditionalNewLine = false;

            Debug.Assert(result != null, "a null result can not be displayed");
            if (!String.IsNullOrEmpty(result.ErrorMessage))
            {
                addAdditionalNewLine = true;
                Output.Information(ConsoleColor.Red, CommandLineResources.ErrorMessageBanner);
                var errorMessage = String.Format(CultureInfo.CurrentCulture, "{0}{1}", TestMessageFormattingPrefix, result.ErrorMessage);
                Output.Information(ConsoleColor.Red, errorMessage);
            }

            if (!String.IsNullOrEmpty(result.ErrorStackTrace))
            {
                addAdditionalNewLine = false;
                Output.Information(ConsoleColor.Red, CommandLineResources.StacktraceBanner);
                var stackTrace = String.Format(CultureInfo.CurrentCulture, "{0}", result.ErrorStackTrace);
                Output.Information(ConsoleColor.Red, stackTrace);
            }

            var stdOutMessagesCollection = GetTestMessages(result.Messages, TestResultMessage.StandardOutCategory);
            if (stdOutMessagesCollection.Count > 0)
            {
                addAdditionalNewLine = false;
                var stdOutMessages = GetFormattedOutput(stdOutMessagesCollection);

                if (!string.IsNullOrEmpty(stdOutMessages))
                {
                    Output.Information(CommandLineResources.StdOutMessagesBanner);
                    Output.Information(stdOutMessages);
                }
            }

            var stdErrMessagesCollection = GetTestMessages(result.Messages, TestResultMessage.StandardErrorCategory);
            if (stdErrMessagesCollection.Count > 0)
            {
                addAdditionalNewLine = false;
                var stdErrMessages = GetFormattedOutput(stdErrMessagesCollection);

                if (!string.IsNullOrEmpty(stdErrMessages))
                {
                    Output.Information(ConsoleColor.Red, CommandLineResources.StdErrMessagesBanner);
                    Output.Information(ConsoleColor.Red, stdErrMessages);
                }
            }

            var DbgTrcMessagesCollection = GetTestMessages(result.Messages, TestResultMessage.DebugTraceCategory);
            if (DbgTrcMessagesCollection.Count > 0)
            {
                addAdditionalNewLine = false;
                var dbgTrcMessages = GetFormattedOutput(DbgTrcMessagesCollection);

                if (!string.IsNullOrEmpty(dbgTrcMessages))
                {
                    Output.Information(CommandLineResources.DbgTrcMessagesBanner);
                    Output.Information(dbgTrcMessages);
                }
            }

            var addnlInfoMessagesCollection = GetTestMessages(result.Messages, TestResultMessage.AdditionalInfoCategory);
            if (addnlInfoMessagesCollection.Count > 0)
            {
                addAdditionalNewLine = false;
                var addnlInfoMessages = GetFormattedOutput(addnlInfoMessagesCollection);

                if (!string.IsNullOrEmpty(addnlInfoMessages))
                {
                    Output.Information(CommandLineResources.AddnlInfoMessagesBanner);
                    Output.Information(addnlInfoMessages);
                }
            }

            if (addAdditionalNewLine)
            {
                Output.WriteLine(String.Empty, OutputLevel.Information);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when a test message is received.
        /// </summary>
        private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestRunMessageEventArgs>(e, "e");

            switch (e.Level)
            {
                case TestMessageLevel.Informational:
                    Output.Information(e.Message);
                    break;
                case TestMessageLevel.Warning:
                    Output.Warning(e.Message);
                    break;
                case TestMessageLevel.Error:
                    this.testOutcome = TestOutcome.Failed;
                    Output.Error(e.Message);
                    break;
                default:
                    Debug.Fail("ConsoleLogger.TestMessageHandler: The test message level is unrecognized: {0}", e.Level.ToString());
                    break;
            }
        }

        /// <summary>
        /// Called when a test result is received.
        /// </summary>
        private void TestResultHandler(object sender, TestResultEventArgs e)
        {
            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestResultEventArgs>(e, "e");

            // Update the test count statistics based on the result of the test. 
            this.testsTotal++;

            string name = null;
            name = !string.IsNullOrEmpty(e.Result.DisplayName) ? e.Result.DisplayName : e.Result.TestCase.FullyQualifiedName;

            if (e.Result.Outcome == TestOutcome.Skipped)
            {
                this.testsSkipped++;
                if (!this.verbosityLevel.Equals(Verbosity.Quiet))
                {
                    var output = string.Format(CultureInfo.CurrentCulture, CommandLineResources.SkippedTestIndicator,
                        name);
                    Output.Warning(output);
                    DisplayFullInformation(e.Result);
                }
            }
            else if (e.Result.Outcome == TestOutcome.Failed)
            {
                this.testOutcome = TestOutcome.Failed;
                this.testsFailed++;
                if (!this.verbosityLevel.Equals(Verbosity.Quiet))
                {
                    var output = string.Format(CultureInfo.CurrentCulture, CommandLineResources.FailedTestIndicator,
                        name);
                    Output.Information(ConsoleColor.Red, output);
                    DisplayFullInformation(e.Result);
                }
            }
            else if (e.Result.Outcome == TestOutcome.Passed)
            {
                if (this.verbosityLevel.Equals(Verbosity.Normal))
                {
                    var output = string.Format(CultureInfo.CurrentCulture, CommandLineResources.PassedTestIndicator, name);
                    Output.Information(output);
                    DisplayFullInformation(e.Result);
                }
                this.testsPassed++;
            }
            else
            {
                if (!this.verbosityLevel.Equals(Verbosity.Quiet))
                {
                    var output = string.Format(CultureInfo.CurrentCulture, CommandLineResources.NotRunTestIndicator,
                        name);
                    Output.Information(output);
                    DisplayFullInformation(e.Result);
                }
            }
        }

        /// <summary>
        /// Called when a test run is completed.
        /// </summary>
        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            Output.WriteLine(string.Empty, OutputLevel.Information);
            var runLevelAttachementCount = (e.AttachmentSets == null) ? 0 : e.AttachmentSets.Sum(attachmentSet => attachmentSet.Attachments.Count);
            
            // Printing Run-level Attachments
            if (runLevelAttachementCount > 0)
            {
                foreach (var attachmentSet in e.AttachmentSets)
                {
                    if (!attachmentSet.DisplayName.Equals(Constants.BlameDataCollectorName))
                    {
                        Output.Information(CommandLineResources.AttachmentsBanner);
                        break;
                    }
                }
            }
            if (runLevelAttachementCount > 0)
            {
                foreach (var attachmentSet in e.AttachmentSets)
                {
                    if (!attachmentSet.DisplayName.Equals(Constants.BlameDataCollectorName))
                    {
                        foreach (var uriDataAttachment in attachmentSet.Attachments)
                        {
                            var attachmentOutput = string.Format(CultureInfo.CurrentCulture, CommandLineResources.AttachmentOutputFormat, uriDataAttachment.Uri.LocalPath);
                            Output.Information(attachmentOutput);
                        }
                    }
                }
                Output.WriteLine(String.Empty, OutputLevel.Information);
            }

            // Output a summary.
            if (this.testsTotal > 0)
            {
                Output.Information(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, testsTotal, testsPassed, testsFailed, testsSkipped));

                if (this.testOutcome == TestOutcome.Failed)
                {
                    Output.Error(CommandLineResources.TestRunFailed);
                }
                else
                {
                    Output.Information(ConsoleColor.Green, CommandLineResources.TestRunSuccessful);
                }

                if (!e.ElapsedTimeInRunningTests.Equals(TimeSpan.Zero))
                {
                    PrintTimeSpan(e.ElapsedTimeInRunningTests);
                }
                else
                {
                    EqtTrace.Info("Skipped printing test execution time on console because it looks like the test run had faced some errors");
                }
            }
        }
        #endregion
    }
}
