// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The blame logger.
    /// </summary>
    [FriendlyName(BlameLogger.FriendlyName)]
    [ExtensionUri(BlameLogger.ExtensionUri)]
    public class BlameLogger : ITestLogger
    {
        #region Constants

        /// <summary>
        /// Uri used to uniquely identify the Blame logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/Extensions/Blame/v1";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the Blame logger.
        /// </summary>
        public const string FriendlyName = "Blame";

        /// <summary>
        /// The blame reader writer.
        /// </summary>
        private readonly IBlameReaderWriter blameReaderWriter;

        /// <summary>
        /// The output.
        /// </summary>
        private readonly IOutput output;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameLogger"/> class.
        /// </summary>
        public BlameLogger()
            : this(ConsoleOutput.Instance, new XmlReaderWriter())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameLogger"/> class.
        /// Constructor added for testing purpose
        /// </summary>
        /// <param name="output">Output Instance</param>
        /// <param name="blameReaderWriter">BlameReaderWriter Instance</param>
        protected BlameLogger(IOutput output, IBlameReaderWriter blameReaderWriter)
        {
            this.output = output;
            this.blameReaderWriter = blameReaderWriter;
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

            this.output.WriteLine(string.Empty, OutputLevel.Information);

            // Gets the faulty test cases if test aborted
            var testCaseNames = this.GetFaultyTestCaseNames(e);
            if (testCaseNames.Count() == 0)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (var tcn in testCaseNames)
            {
                sb.Append(tcn).Append(Environment.NewLine);
            }

            this.output.Error(false, Resources.Resources.AbortedTestRun, sb.ToString());
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
        /// Faulty test cases name
        /// </returns>
        private IEnumerable<string> GetFaultyTestCaseNames(TestRunCompleteEventArgs e)
        {
            var faultyTestCaseNames = new List<string>();
            foreach (var attachmentSet in e.AttachmentSets)
            {
                if (attachmentSet.DisplayName.Equals(Constants.BlameDataCollectorName))
                {
                    // Process only Sequence_<GUID>.xml attachments
                    var uriDataAttachment = attachmentSet.Attachments.LastOrDefault((attachment) => attachment.Uri.ToString().EndsWith(".xml"));

                    if (uriDataAttachment != null)
                    {
                        var filepath = uriDataAttachment.Uri.LocalPath;
                        var testCaseList = this.blameReaderWriter.ReadTestSequence(filepath);
                        if (testCaseList.Count > 0)
                        {
                            var testcases = testCaseList.Where(t => !t.IsCompleted).Select(t => t.FullyQualifiedName).ToList();
                            faultyTestCaseNames.AddRange(testcases);
                        }
                    }
                }
            }

            return faultyTestCaseNames;
        }

        #endregion
    }
}