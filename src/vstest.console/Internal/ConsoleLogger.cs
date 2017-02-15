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

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    /// <summary>
    /// Logger for sending output to the console.
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
                Output.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, timeSpan.TotalDays, CommandLineResources.Days), OutputLevel.Information);
            }
            else if (timeSpan.TotalHours >= 1)
            {
                Output.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, timeSpan.TotalHours, CommandLineResources.Hours), OutputLevel.Information);
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                Output.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, timeSpan.TotalMinutes, CommandLineResources.Minutes), OutputLevel.Information);
            }
            else
            {
                Output.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.ExecutionTimeFormatString, timeSpan.TotalSeconds, CommandLineResources.Seconds), OutputLevel.Information);
            }
        }

        /// <summary>
        ///       Constructs a well formatted string using the given prefix before every message content on each line.
        /// </summary>
        private static string GetFormattedOutput(Collection<TestResultMessage> testMessageCollection)
        {
            if (testMessageCollection != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var message in testMessageCollection)
                {
                    string prefix = String.Format(CultureInfo.CurrentCulture, "{0}{1}", Environment.NewLine, TestMessageFormattingPrefix);
                    string messageText = message.Text.Replace(Environment.NewLine, prefix).TrimEnd(TestMessageFormattingPrefix.ToCharArray());
                    sb.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}", TestMessageFormattingPrefix, messageText);
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
            Collection<TestResultMessage> requiredMessageCollection = new Collection<TestResultMessage>(selectedMessages.ToList());
            return requiredMessageCollection;
        }

        /// <summary>
        /// outputs the Error messages, Stack Trace, and other messages for the parameter test.
        /// </summary>
        private static void DisplayFullInformation(TestResult result)
        {
            bool hasData = false;

            Debug.Assert(result != null, "a null result can not be displayed");
            if (!String.IsNullOrEmpty(result.ErrorMessage))
            {
                hasData = true;
                Output.WriteLine(CommandLineResources.ErrorMessageBanner, OutputLevel.Information);
                string errorMessage = String.Format(CultureInfo.CurrentCulture, "{0}{1}", TestMessageFormattingPrefix, result.ErrorMessage);
                Output.WriteLine(errorMessage, OutputLevel.Information);
            }

            if (!String.IsNullOrEmpty(result.ErrorStackTrace))
            {
                hasData = true;
                Output.WriteLine(CommandLineResources.StacktraceBanner, OutputLevel.Information);
                string stackTrace = String.Format(CultureInfo.CurrentCulture, "{0}", result.ErrorStackTrace);
                Output.Write(stackTrace, OutputLevel.Information);
            }

            Collection<TestResultMessage> stdOutMessagesCollection = GetTestMessages(result.Messages, TestResultMessage.StandardOutCategory);
            if (stdOutMessagesCollection.Count > 0)
            {
                hasData = true;
                string stdOutMessages = GetFormattedOutput(stdOutMessagesCollection);
                Output.WriteLine(CommandLineResources.StdOutMessagesBanner, OutputLevel.Information);
                Output.Write(stdOutMessages, OutputLevel.Information);
            }

            Collection<TestResultMessage> stdErrMessagesCollection = GetTestMessages(result.Messages, TestResultMessage.StandardErrorCategory);
            if (stdErrMessagesCollection.Count > 0)
            {
                hasData = true;
                string stdErrMessages = GetFormattedOutput(stdErrMessagesCollection);
                Output.WriteLine(CommandLineResources.StdErrMessagesBanner, OutputLevel.Error);
                Output.Write(stdErrMessages, OutputLevel.Error);
            }

            Collection<TestResultMessage> addnlInfoMessagesCollection = GetTestMessages(result.Messages, TestResultMessage.AdditionalInfoCategory);
            if (addnlInfoMessagesCollection.Count > 0)
            {
                hasData = true;
                Output.WriteLine(CommandLineResources.AddnlInfoMessagesBanner, OutputLevel.Information);
                string addnlInfoMessages = GetFormattedOutput(addnlInfoMessagesCollection);
                Output.Write(addnlInfoMessages, OutputLevel.Information);
            }
            if (hasData)
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

            Output.WriteLine(string.Empty, (OutputLevel)e.Level);
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
                string output = string.Format(CultureInfo.CurrentCulture, CommandLineResources.SkippedTestIndicator, name);
                Output.WriteLine(output, OutputLevel.Information);
                DisplayFullInformation(e.Result);
            }
            else if (e.Result.Outcome == TestOutcome.Failed)
            {
                this.testOutcome = TestOutcome.Failed;
                this.testsFailed++;
                string output = string.Format(CultureInfo.CurrentCulture, CommandLineResources.FailedTestIndicator, name);
                Output.WriteLine(output, OutputLevel.Information);
                DisplayFullInformation(e.Result);
            }
            else if (e.Result.Outcome == TestOutcome.Passed)
            {
                string output = string.Format(CultureInfo.CurrentCulture, CommandLineResources.PassedTestIndicator, name);
                if (!this.verbosityLevel.Equals(Verbosity.Minimal))
                {
                    Output.WriteLine(output, OutputLevel.Information);
                }
                this.testsPassed++;
            }
        }

        /// <summary>
        /// Called when a test run is completed.
        /// </summary>
        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            Output.WriteLine(string.Empty, OutputLevel.Information);

            // Printing Run-level Attachments
            int runLevelAttachementCount = (e.AttachmentSets == null) ? 0 : e.AttachmentSets.Sum(attachmentSet => attachmentSet.Attachments.Count);
            if (runLevelAttachementCount > 0)
            {
                Output.WriteLine(CommandLineResources.AttachmentsBanner, OutputLevel.Information);
                foreach (AttachmentSet attachmentSet in e.AttachmentSets)
                {
                    foreach (UriDataAttachment uriDataAttachment in attachmentSet.Attachments)
                    {
                        string attachmentOutput = string.Format(CultureInfo.CurrentCulture, CommandLineResources.AttachmentOutputFormat, uriDataAttachment.Uri.LocalPath);
                        Output.WriteLine(attachmentOutput, OutputLevel.Information);
                    }
                }
                Output.WriteLine(String.Empty, OutputLevel.Information);
            }

            // Output a summary.                            
            if (this.testsTotal > 0)
            {
                if (this.testOutcome == TestOutcome.Failed)
                {
                    Output.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, testsTotal, testsPassed, testsFailed, testsSkipped), OutputLevel.Information);
                    using (new ConsoleColorHelper(ConsoleColor.Red))
                    {
                        Output.WriteLine(CommandLineResources.TestRunFailed, OutputLevel.Error);
                    }
                }
                else
                {
                    Output.WriteLine(string.Format(CultureInfo.CurrentCulture, CommandLineResources.TestRunSummary, testsTotal, testsPassed, testsFailed, testsSkipped), OutputLevel.Information);
                    using (new ConsoleColorHelper(ConsoleColor.Green))
                    {
                        Output.WriteLine(CommandLineResources.TestRunSuccessful, OutputLevel.Information);
                    }
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
