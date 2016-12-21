// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    using ObjectModel.Logging;

    using TrxLoggerObjectModel = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    using TrxLoggerResources = Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    /// <summary>
    /// Logger for Generating TRX
    /// </summary>
    [FriendlyName(TrxLogger.FriendlyName)]
    [ExtensionUri(TrxLogger.ExtensionUri)]
    internal class TrxLogger : ITestLoggerWithParameters
    {
        #region Constants

        /// <summary>
        /// Uri used to uniquely identify the TRX logger.
        /// </summary>
        public const string ExtensionUri = "logger://Microsoft/TestPlatform/TrxLogger/v2";

        /// <summary>
        /// Alternate user friendly string to uniquely identify the console logger.
        /// </summary>
        public const string FriendlyName = "Trx";

        /// <summary>
        /// Prefix of the data collector
        /// </summary>
        public const string DataCollectorUriPrefix = "dataCollector://";

        /// <summary>
        /// Log file parameter key
        /// </summary>
        public const string LogFileKey = "LogFile";

        /// <summary>
        /// Overwrite parameter key, default value is true
        /// </summary>
        public const string OverwriteKey = "Overwrite";

        #endregion

        #region Fields

        /// <summary>
        /// Cache the TRX file path
        /// </summary>
        private string trxFilePath;

        private TrxLoggerObjectModel.TestRun testRun;
        private List<TrxLoggerObjectModel.UnitTestResult> results;
        private List<TrxLoggerObjectModel.UnitTestElement> testElements;
        private List<TestEntry> entries;

        /// <summary>
        /// Specifies the run level "out" messages
        /// </summary>
        private StringBuilder runLevelStdOut;

        // List of run level errors and warnings generated. These are logged in the Trx in the Results Summary.
        private List<TrxLoggerObjectModel.RunInfo> runLevelErrorsAndWarnings;

        private TrxLoggerObjectModel.TestOutcome testRunOutcome = TrxLoggerObjectModel.TestOutcome.Passed;

        private int totalTests, passTests, failTests;

        private DateTime testRunStartTime;

        /// <summary>
        /// Parameters dictionary for logger. Ex: {"logfile":"c:pathto/logfile.trx"}.
        /// </summary>
        private Dictionary<string, string> parametersDictionary;

        /// <summary>
        /// Gets the directory under which default trx file and test results attachements should be saved.
        /// </summary>
        private string testResultsDirPath;

        #endregion

        #region ITestLogger

        /// <inheritdoc/>
        public void Initialize(TestLoggerEvents events, string testResultsDirPath)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (string.IsNullOrEmpty(testResultsDirPath))
            {
                throw new ArgumentNullException(nameof(testResultsDirPath));
            }

            // Register for the events.
            events.TestRunMessage += this.TestMessageHandler;
            events.TestResult += this.TestResultHandler;
            events.TestRunComplete += this.TestRunCompleteHandler;

            this.testResultsDirPath = testResultsDirPath;

            this.InitializeInternal();
        }

        /// <inheritdoc/>
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

            this.parametersDictionary = parameters;
            this.Initialize(events, this.parametersDictionary[DefaultLoggerParameterNames.TestRunDirectory]);
        }
        #endregion

        #region ForTesting

        internal string GetRunLevelInformationalMessage()
        {
            return this.runLevelStdOut.ToString();
        }

        internal List<TrxLoggerObjectModel.RunInfo> GetRunLevelErrorsAndWarnings()
        {
            return this.runLevelErrorsAndWarnings;
        }

        internal DateTime TestRunStartTime
        {
            get { return this.testRunStartTime; }
        }

        internal TestRun LoggerTestRun
        {
            get { return this.testRun; }
        }

        internal int TotalTestCount
        {
            get { return totalTests; }
        }

        internal int PassedTestCount
        {
            get { return passTests; }
        }

        internal int FailedTestCount
        {
            get { return failTests; }
        }

        internal int TestResultCount
        {
            get { return this.results.Count; }
        }

        internal int UnitTestElementCount
        {
            get { return this.testElements.Count; }
        }

        internal int TestEntryCount
        {
            get { return this.entries.Count; }
        }

        internal TrxLoggerObjectModel.TestOutcome TestResultOutcome
        {
            get { return this.testRunOutcome; }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when a test message is received.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// Event args
        /// </param>
        internal void TestMessageHandler(object sender, TestRunMessageEventArgs e)
        {
            ValidateArg.NotNull<object>(sender, "sender");
            ValidateArg.NotNull<TestRunMessageEventArgs>(e, "e");

            TrxLoggerObjectModel.RunInfo runMessage;
            switch (e.Level)
            {
                case TestMessageLevel.Informational:
                    this.AddRunLevelInformationalMessage(e.Message);
                    break;
                case TestMessageLevel.Warning:
                    runMessage = new TrxLoggerObjectModel.RunInfo(e.Message, null, Environment.MachineName, TrxLoggerObjectModel.TestOutcome.Warning);
                    this.runLevelErrorsAndWarnings.Add(runMessage);
                    break;
                case TestMessageLevel.Error:
                    this.testRunOutcome = TrxLoggerObjectModel.TestOutcome.Failed;
                    runMessage = new TrxLoggerObjectModel.RunInfo(e.Message, null, Environment.MachineName, TrxLoggerObjectModel.TestOutcome.Error);
                    this.runLevelErrorsAndWarnings.Add(runMessage);
                    break;
                default:
                    Debug.Fail("TrxLogger.TestMessageHandler: The test message level is unrecognized: {0}", e.Level.ToString());
                    break;
            }
        }

        /// <summary>
        /// Called when a test result is received.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The eventArgs.
        /// </param>
        internal void TestResultHandler(object sender, ObjectModel.Logging.TestResultEventArgs e)
        {
            if (this.testRun == null)
            {
                Guid runId = Guid.NewGuid();

                this.testRun = new TestRun(runId);

                // We cannot rely on the StartTime for the first test result
                // In case of parallel, first test result is the fastest test and not the one which started first.
                // Setting Started to DateTime.Now in Intialize will make sure we include the startup cost, which was being ignored earlier.
                // This is in parity with the way we set this.testRun.Finished
                this.testRun.Started = this.testRunStartTime;

                // Save default test settings 
                string runDeploymentRoot = FileHelper.ReplaceInvalidFileNameChars(this.testRun.Name);
                TestRunConfiguration testrunConfig = new TestRunConfiguration("default");

                testrunConfig.RunDeploymentRootDirectory = runDeploymentRoot;

                this.testRun.RunConfiguration = testrunConfig;
            }

            // Convert skipped test to a log entry as that is the behaviour of mstest.
            if (e.Result.Outcome == ObjectModel.TestOutcome.Skipped)
            {
                this.HandleSkippedTest(e.Result);
            }

            // Create MSTest test element from rocksteady test case
            UnitTestElement testElement = Converter.ToUnitTestElement(e.Result);

            // Conver the rocksteady result to MSTest result
            TrxLoggerObjectModel.TestOutcome testOutcome = Converter.ToOutcome(e.Result.Outcome);
            TrxLoggerObjectModel.UnitTestResult testResult = Converter.ToUnitTestResult(e.Result, testElement, testOutcome, this.testRun, this.testResultsDirPath);

            // Set various counts (passtests, failed tests, total tests)
            this.totalTests++;
            if (testResult.Outcome == TrxLoggerObjectModel.TestOutcome.Failed)
            {
                this.testRunOutcome = TrxLoggerObjectModel.TestOutcome.Failed;
                this.failTests++;
            }
            else if (testResult.Outcome == TrxLoggerObjectModel.TestOutcome.Passed)
            {
                this.passTests++;
            }

            // Add results to in-memory lists that are saved to the xml at completion.
            this.results.Add(testResult);

            if (!this.testElements.Contains(testElement))
            {
                this.testElements.Add(testElement);
            }

            // create a test entry            
            TestEntry te = new TestEntry(testElement.Id, TestListCategory.UncategorizedResults.Id);
            te.ExecId = testElement.ExecutionId;
            this.entries.Add(te);
        }

        /// <summary>
        /// Called when a test run is completed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// Test run complete events arguments.
        /// </param>
        internal void TestRunCompleteHandler(object sender, TestRunCompleteEventArgs e)
        {
            if (this.testRun != null)
            {
                XmlPersistence helper = new XmlPersistence();
                XmlTestStoreParameters parameters = XmlTestStoreParameters.GetParameters();
                XmlElement rootElement = helper.CreateRootElement("TestRun");

                // Save runId/username/creation time etc.
                this.testRun.Finished = DateTime.Now;
                helper.SaveSingleFields(rootElement, this.testRun, parameters);

                // Save test settings
                helper.SaveObject(this.testRun.RunConfiguration, rootElement, "TestSettings", parameters);

                // Save test results
                helper.SaveIEnumerable(this.results, rootElement, "Results", ".", null, parameters);

                // Save test definitions
                helper.SaveIEnumerable(this.testElements, rootElement, "TestDefinitions", ".", null, parameters);

                // Save test entries
                helper.SaveIEnumerable(this.entries, rootElement, "TestEntries", ".", "TestEntry", parameters);

                // Save default categories
                List<TestListCategory> categories = new List<TestListCategory>();
                categories.Add(TestListCategory.UncategorizedResults);
                categories.Add(TestListCategory.AllResults);
                helper.SaveList<TestListCategory>(categories, rootElement, "TestLists", ".", "TestList", parameters);

                // Save summary
                if (this.testRunOutcome == TrxLoggerObjectModel.TestOutcome.Passed)
                {
                    this.testRunOutcome = TrxLoggerObjectModel.TestOutcome.Completed;
                }

                List<string> errorMessages = new List<string>();
                List<CollectorDataEntry> collectorEntries = Converter.ToCollectionEntries(e.AttachmentSets, this.testRun, this.testResultsDirPath);
                IList<String> resultFiles = Converter.ToResultFiles(e.AttachmentSets, this.testRun, this.testResultsDirPath, errorMessages);

                if (errorMessages.Count > 0)
                {
                    // Got some errors while attaching files, report them and set the outcome of testrun to be Error...
                    this.testRunOutcome = TrxLoggerObjectModel.TestOutcome.Error;
                    foreach (string msg in errorMessages)
                    {
                        RunInfo runMessage = new RunInfo(msg, null, Environment.MachineName, TrxLoggerObjectModel.TestOutcome.Error);
                        this.runLevelErrorsAndWarnings.Add(runMessage);
                    }
                }

                TestRunSummary runSummary = new TestRunSummary(
                    this.totalTests,
                    this.passTests + this.failTests,
                    this.passTests,
                    this.failTests,
                    this.testRunOutcome,
                    this.runLevelErrorsAndWarnings,
                    this.runLevelStdOut.ToString(),
                    resultFiles,
                    collectorEntries);

                helper.SaveObject(runSummary, rootElement, "ResultSummary", parameters);

                //Save results to Trx file
                this.DeriveTrxFilePath();
                this.PopulateTrxFile(this.trxFilePath, rootElement);
            }
        }

        /// <summary>
        /// populate trx file from the xmlelement
        /// </summary>
        /// <param name="trxFileName">
        /// Trx full path
        /// </param>
        /// <param name="rootElement">
        /// XmlElement.
        /// </param>
        internal virtual void PopulateTrxFile(string trxFileName, XmlElement rootElement)
        {
            try
            {
                var trxFileDirPath = Path.GetDirectoryName(trxFilePath);
                if (Directory.Exists(trxFileDirPath) == false)
                {
                    Directory.CreateDirectory(trxFileDirPath);
                }

                if (File.Exists(trxFilePath))
                {
                    Console.WriteLine(String.Format(CultureInfo.CurrentCulture, TrxLoggerResources.TrxLoggerResultsFileOverwriteWarning, trxFileName));
                }

                FileStream fs = File.OpenWrite(trxFileName);
                rootElement.OwnerDocument.Save(fs);
                String resultsFileMessage = String.Format(CultureInfo.CurrentCulture, TrxLoggerResources.TrxLoggerResultsFile, trxFileName);
                Console.WriteLine(resultsFileMessage);
            }
            catch (System.UnauthorizedAccessException fileWriteException)
            {
                Console.WriteLine(fileWriteException.Message);
            }
        }

        // Initializes trx logger cache.
        private void InitializeInternal()
        {
            this.results = new List<TrxLoggerObjectModel.UnitTestResult>();
            this.testElements = new List<UnitTestElement>();
            this.entries = new List<TestEntry>();
            this.runLevelErrorsAndWarnings = new List<RunInfo>();
            this.testRun = null;
            this.totalTests = 0;
            this.passTests = 0;
            this.failTests = 0;
            this.runLevelStdOut = new StringBuilder();
            this.testRunStartTime = DateTime.Now;
        }

        /// <summary>
        /// Add run level informational message
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        private void AddRunLevelInformationalMessage(string message)
        {
            this.runLevelStdOut.Append(message);
        }

        // Handle the skipped test result
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed. Suppression is OK here.")]
        private void HandleSkippedTest(ObjectModel.TestResult rsTestResult)
        {
            Debug.Assert(rsTestResult.Outcome == ObjectModel.TestOutcome.Skipped, "Test Result should be skipped but it is " + rsTestResult.Outcome);

            ObjectModel.TestCase testCase = rsTestResult.TestCase;
            string testCaseName = !string.IsNullOrEmpty(testCase.DisplayName) ? testCase.DisplayName : testCase.FullyQualifiedName;
            string message = String.Format(CultureInfo.CurrentCulture, TrxLoggerResources.MessageForSkippedTests, testCaseName);
            this.AddRunLevelInformationalMessage(message);
        }

        private void DeriveTrxFilePath()
        {
            if (this.parametersDictionary != null)
            {
                var isLogFileParameterExists = this.parametersDictionary.TryGetValue(TrxLogger.LogFileKey, out string logFileValue);
                if (isLogFileParameterExists && !string.IsNullOrWhiteSpace(logFileValue))
                {
                    this.SetCustomTrxFilePath(logFileValue);
                }
                else
                {
                    this.SetDefaultTrxFilePath();
                }
            }
            else
            {
                this.SetDefaultTrxFilePath();
            }
        }

        private void SetCustomTrxFilePath(string logFileValue)
        {
            this.trxFilePath = Path.GetFullPath(logFileValue);
            if (File.Exists(this.trxFilePath))
            {
                // Get overwrite parameter.
                var isOverwriteParameterExists =
                    this.parametersDictionary.TryGetValue(TrxLogger.OverwriteKey,
                        out string overwriteParameterValue);

                if (isOverwriteParameterExists &&
                    string.Equals(overwriteParameterValue, "false", StringComparison.OrdinalIgnoreCase))
                {
                    // Generate file name if overwrite is false.
                    this.trxFilePath =
                        FileHelper.GetNextIterationFileName(Path.GetDirectoryName(this.trxFilePath),
                            Path.GetFileName(this.trxFilePath), false);
                }
            }
        }

        /// <summary>
        /// Sets auto generated Trx file name under test results directory.
        /// </summary>
        private void SetDefaultTrxFilePath()
        {
            // [RunDeploymentRootDirectory] Replace white space with underscore from trx file name to make it command line friendly
            var defaultTrxFileName = this.testRun.RunConfiguration.RunDeploymentRootDirectory.Replace(' ', '_') + ".trx";
            this.trxFilePath = FileHelper.GetNextIterationFileName(this.testResultsDirPath, defaultTrxFileName, false);
        }

        #endregion
    }
}