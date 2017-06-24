using Microsoft.VisualStudio.TestPlatform.DataCollector;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;
using Constants = vstest.console.ConsoleConstants;

namespace vstest.console.Internal
{
    [FriendlyName(BlameLogger.FriendlyName)]
    [ExtensionUri(BlameLogger.ExtensionUri)]
    class BlameLogger : ITestLogger
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

        /// <summary>
        /// Parameter for test run abort
        /// </summary>
        private bool isAborted = false;

        /// <summary>
        /// Parameter for Stack overflow 
        /// </summary>
        private bool isStackoverFlow = false;

        #endregion

        #region Constructor
        public BlameLogger()
        {
        }
        /// <summary>
        /// Constructor added for testing purpose
        /// </summary>
        /// <param name="output"></param>
        internal BlameLogger(IOutput output)
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
        /// <param name="testRunDirectory">Test Run Directory</param>
        public void Initialize(TestLoggerEvents events, string testRunDictionary)
        {
            if (events == null)
            {
                throw new ArgumentNullException("events");
            }

            if (BlameLogger.Output == null)
            {
                BlameLogger.Output = ConsoleOutput.Instance;
            }
            events.TestRunMessage += this.TestMessageHandler;
            events.TestRunComplete += this.TestRunCompleteHandler;
        }

        /// <summary>
        /// Called when a test message is received.
        /// </summary>
        private void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestRunMessageEventArgs>(e, "e");

            if (e.Level == TestMessageLevel.Error)
            {
                this.isAborted = e.Message.Equals(CommandLineResources.TestRunAbort) || e.Message.Contains(CommandLineResources.TestRunAbortStackOverFlow);
                this.isStackoverFlow = e.Message.Contains(CommandLineResources.TestRunAbortStackOverFlow);
            }
            
        }

        /// <summary>
        /// Called when a test run is complete.
        /// </summary>
        private void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            //Gets the faulty test case if test aborted without reason
            if (isAborted)
            {
                Output.WriteLine(string.Empty, OutputLevel.Information);
                string testCaseName = GetFaultyTestCase(e);
                string reason = String.Empty;
                if(isStackoverFlow)
                {
                    reason = CommandLineResources.TestRunAbortStackOverFlow + Environment.NewLine + "Faulty Test is : " + testCaseName;
                }
                else
                {
                    reason = CommandLineResources.TestRunAbort + Environment.NewLine + "Faulty Test is : " + testCaseName;
                }
                Output.Error(reason);
            }
        }

        #endregion

        #region Faulty test case fetch
        /// <summary>
        /// Fetches faulty test case in case of test host crash 
        /// </summary>
        private string GetFaultyTestCase(TestRunCompleteEventArgs e)
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
        /// Reads file for faulty test case
        /// </summary>
        private string GetTestFromFile(string filepath)
        {
            var dataReader = new BlameDataReaderWriter(filepath, new BlameXmlManager());
            var testCase = dataReader.GetLastTestCase();
            return testCase.FullyQualifiedName;
        }
        #endregion
    }
}
