// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.BlameDataCollector
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using System;
    using System.Collections.Generic;
    using Microsoft.TestPlatform.BlameDataCollector.Resources;

    [FriendlyName(BlameLogger.FriendlyName)]
    [ExtensionUri(BlameLogger.ExtensionUri)]
    public class BlameLogger : ITestLogger
    {
        private readonly IBlameReaderWriter blameReaderWriter;

        #region Constants

        /// <summary>
        /// Uri used to uniquely identify the Blame logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/BlameLogger";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the Blame logger.
        /// </summary>
        public const string FriendlyName = "Blame";

        #endregion      

        #region Constructor

        /// <summary>
        /// Constructor : Default BlameReaderWriter used is XmlReaderWriter
        /// </summary>
        public BlameLogger()
            : this(ConsoleOutput.Instance, new XmlReaderWriter())
        {
        }

        /// <summary>
        /// Constructor added for testing purpose
        /// </summary>
        /// <param name="output"></param>
        /// <param name="blameReaderWriter">BlameReaderWriter Instance</param>
        public BlameLogger(IOutput output, IBlameReaderWriter blameReaderWriter)
        {
            this.Output = output;
            this.blameReaderWriter = blameReaderWriter;
        }

        protected IOutput Output
        {
            get;
            private set;
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
        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestRunCompleteEventArgs>(e, "e");

            if (!e.IsAborted) return;

            Output.WriteLine(string.Empty, OutputLevel.Information);

            // Gets the faulty test case if test aborted 
            var testCaseName = GetFaultyTestCase(e);
            string reason = Resources.Resources.AbortedTestRun + testCaseName;
            Output.Error(reason);
        }

        #endregion

        #region Faulty test case fetch
        
        /// <summary>
        /// Fetches faulty test case
        /// </summary>
        private string GetFaultyTestCase(TestRunCompleteEventArgs e)
        {
            foreach (var attachmentSet in e.AttachmentSets)
            {
                if (attachmentSet.DisplayName.Equals(Constants.BlameDataCollectorName))
                {
                    var filepath = attachmentSet.Attachments[0].Uri.LocalPath;
                    List<TestCase> testCaseList = this.blameReaderWriter.ReadTestSequence(filepath);
                    if (testCaseList.Count > 0)
                    {
                        var testcase = testCaseList[testCaseList.Count - 1];
                        return testcase.FullyQualifiedName;
                    }
                    return String.Empty;
                }
            }
            return String.Empty;
        }
        
        #endregion
    }
}
