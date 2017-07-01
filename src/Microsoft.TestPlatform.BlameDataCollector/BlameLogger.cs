// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.BlameDataCollector
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using System;

    [FriendlyName(BlameLogger.FriendlyName)]
    [ExtensionUri(BlameLogger.ExtensionUri)]
    public class BlameLogger : ITestLogger
    {
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
        public BlameLogger()
        {
        }
        /// <summary>
        /// Constructor added for testing purpose
        /// </summary>
        /// <param name="output"></param>
        public BlameLogger(IOutput output)
        {
            BlameLogger.Output = output;
        }

        protected static IOutput Output
        {
            get;
            private set;
        }

        #endregion

        #region ITestLoggerWithParameters
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

            if (BlameLogger.Output == null)
            {
                BlameLogger.Output = ConsoleOutput.Instance;
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

            Output.WriteLine(string.Empty, OutputLevel.Information);
            if (!e.IsAborted) return;

            // Gets the faulty test case if test aborted without reason
            var testCaseName = GetFaultyTestCase(e);
            string reason;
            reason = "The active test run was aborted because the host process existed unexpectedly while executing test " + testCaseName;
            Output.Error(reason);
        }

        #endregion

        #region Faulty test case fetch
        /// <summary>
        /// Fetches faulty test case in case of test host crash 
        /// </summary>
        private static string GetFaultyTestCase(TestRunCompleteEventArgs e)
        {
            string testname = null;
            foreach (var attachmentSet in e.AttachmentSets)
            {
                if (attachmentSet.DisplayName.Equals(Constants.BlameDataCollectorName))
                {
                    testname = GetTestFromFile(attachmentSet.Attachments[0].Uri.LocalPath);
                }
            }
            return testname;
        }

        /// <summary>
        /// Reads file for last test case
        /// </summary>
        private static string GetTestFromFile(string filepath)
        {
            var dataReader = new BlameDataReaderWriter(filepath, new XmlFileManager());
            var testCase = dataReader.GetLastTestCase();
            return testCase.FullyQualifiedName;
        }
        #endregion
    }
}
