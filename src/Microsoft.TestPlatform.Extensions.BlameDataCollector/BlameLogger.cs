// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The blame logger.
    /// </summary>
    [FriendlyName(BlameLogger.FriendlyName)]
    [ExtensionUri(BlameLogger.ExtensionUri)]
    public class BlameLogger : ITestLoggerWithParameters
    {
        /// <summary>
        /// Uri used to uniquely identify the Blame logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/Extensions/Blame/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the Blame logger.
        /// </summary>
        public const string FriendlyName = "Blame";

        /// <summary>
        /// List of dump files
        /// </summary>
        private static List<string> dumpList;

        /// <summary>
        /// The blame reader writer.
        /// </summary>
        private readonly IBlameReaderWriter blameReaderWriter;

        /// <summary>
        /// The output.
        /// </summary>
        private readonly IOutput output;

        /// <summary>
        /// The Environment
        /// </summary>
        private IEnvironment environment;

        /// <summary>
        /// Is Dump Enabled
        /// </summary>
        private bool isDumpEnabled;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameLogger"/> class.
        /// </summary>
        public BlameLogger()
            : this(ConsoleOutput.Instance, new XmlReaderWriter(), new PlatformEnvironment())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameLogger"/> class.
        /// Constructor added for testing purpose
        /// </summary>
        /// <param name="output">Output Instance</param>
        /// <param name="blameReaderWriter">BlameReaderWriter Instance</param>
        /// <param name="environment">Environment</param>
        protected BlameLogger(IOutput output, IBlameReaderWriter blameReaderWriter, IEnvironment environment)
        {
            this.output = output;
            this.blameReaderWriter = blameReaderWriter;
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            dumpList = new List<string>();
        }

        #endregion

        #region ITestLogger

        /// <summary>
        /// Initializes the Logger.
        /// </summary>
        /// <param name="events">Events that can be registered for.</param>
        /// <param name="testRunDictionary">Test Run Directory</param>
        public void Initialize(TestLoggerEvents events, string testRunDictionary)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            events.TestRunComplete += this.TestRunCompleteHandler;
        }

        /// <summary>
        /// Initializes the Logger.
        /// </summary>
        /// <param name="events">Events that can be registered for.</param>
        /// <param name="testRunDictionary">Test Run Directory</param>
        public void Initialize(TestLoggerEvents events, Dictionary<string, string> testRunDictionary)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            events.TestRunComplete += this.TestRunCompleteHandler;

            if (testRunDictionary.ContainsKey(Constants.DumpKey))
            {
                this.isDumpEnabled = true;
            }
        }

        internal static void AddFileToDumpList(string filename)
        {
            if (dumpList == null)
            {
                dumpList = new List<string>();
            }

            dumpList.Add(filename);
        }

        internal static int GetDumpListCount()
        {
            if (dumpList == null)
            {
                return 0;
            }

            return dumpList.Count;
        }

        internal static void ClearDumpList()
        {
            if (dumpList != null)
            {
                dumpList.Clear();
            }
        }

        /// <summary>
        /// Called when a test run is complete.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">TestRunCompleteEventArgs</param>
        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestRunCompleteEventArgs>(e, "e");

            if (!e.IsAborted)
            {
                return;
            }

            // Gets the faulty test case if test aborted
            var testCaseName = this.GetFaultyTestCase(e);
            if (testCaseName == string.Empty)
            {
                return;
            }

            this.output.WriteLine(string.Empty, OutputLevel.Information);
            var reason = Resources.Resources.AbortedTestRun + testCaseName;
            this.output.Error(reason);

            // Checks for operating system
            // If windows, prints the dump folder name if obtained
            if (this.environment.OperatingSystem.Equals(PlatformOperatingSystem.Windows))
            {
                if (this.isDumpEnabled)
                {
                    if (dumpList.Count > 0)
                    {
                        this.output.WriteLine(string.Empty, OutputLevel.Information);
                        this.output.WriteLine(Resources.Resources.LocalCrashDumpLink, OutputLevel.Information);
                        this.output.WriteLine("  " + dumpList.FirstOrDefault(), OutputLevel.Information);
                    }
                    else
                    {
                        this.output.WriteLine(string.Empty, OutputLevel.Information);
                        this.output.WriteLine(Resources.Resources.EnableLocalCrashDumpGuidance + LocalCrashDumpUtilities.EnableLocalCrashDumpForwardLink, OutputLevel.Information);
                    }
                }
                else
                {
                    this.output.WriteLine(string.Empty, OutputLevel.Information);
                    this.output.WriteLine(Resources.Resources.EnableLocalCrashDumpGuidance + LocalCrashDumpUtilities.EnableLocalCrashDumpForwardLink, OutputLevel.Information);
                }
            }
        }

        #endregion

        #region Faulty test case fetch

        /// <summary>
        /// Fetches faulty test case
        /// </summary>
        /// <param name="e">
        /// The TestRunCompleteEventArgs.
        /// </param>
        /// <returns>
        /// Faulty test case name
        /// </returns>
        private string GetFaultyTestCase(TestRunCompleteEventArgs e)
        {
            foreach (var attachmentSet in e.AttachmentSets)
            {
                if (attachmentSet.DisplayName.Equals(Constants.BlameDataCollectorName))
                {
                    var uriDataAttachment = attachmentSet.Attachments.LastOrDefault();
                    if (uriDataAttachment != null)
                    {
                        var filepath = uriDataAttachment.Uri.LocalPath;
                        var testCaseList = this.blameReaderWriter.ReadTestSequence(filepath);
                        if (testCaseList.Count > 0)
                        {
                            var testcase = testCaseList.Last();
                            return testcase.FullyQualifiedName;
                        }
                    }

                    return string.Empty;
                }
            }

            return string.Empty;
        }

        #endregion
    }
}
