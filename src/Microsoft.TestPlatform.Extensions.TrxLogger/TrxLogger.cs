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
    using System.Linq;
    using System.Text;
    using System.Xml;
    using Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using ObjectModel.Logging;
    using TrxLoggerConstants = Microsoft.TestPlatform.Extensions.TrxLogger.Utility.Constants;
    using TrxLoggerObjectModel = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    using TrxLoggerResources = Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    /// <summary>
    /// Logger for Generating TRX
    /// </summary>
    [FriendlyName(TrxLoggerConstants.FriendlyName)]
    [ExtensionUri(TrxLoggerConstants.ExtensionUri)]
    internal class TrxLogger : ITestLoggerWithParameters
    {
        #region Fields

        /// <summary>
        /// Cache the TRX file path
        /// </summary>
        private string trxFilePath;

        private TrxLoggerObjectModel.TestRun testRun;
        private Dictionary<Guid, TrxLoggerObjectModel.ITestResult> results;
        private Dictionary<Guid, TrxLoggerObjectModel.ITestResult> additionalResults;
        private Dictionary<Guid, TrxLoggerObjectModel.ITestElement> testElements;
        private Dictionary<Guid, TestEntry> entries;

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
        /// Parameters dictionary for logger. Ex: {"LogFileName":"TestResults.trx"}.
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
            System.Diagnostics.Debugger.Launch();
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
        /// Creates test run.
        /// </summary>
        private void CreateTestRun()
        {
            // Don't create run if already created.
            if (testRun != null)
                return;

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

        private int GetInnerResultsCount(ObjectModel.TestResult testResult)
        {
            ObjectModel.TestProperty innerResultsCountProperty = testResult.Properties.FirstOrDefault(property => property.Id.Equals(TrxLoggerConstants.InnerResultsCountPropertyIdentifier));
            return innerResultsCountProperty == null ? 0 : testResult.GetPropertyValue(innerResultsCountProperty, default(int));
        }

        private Guid GetParentExecutionId(ObjectModel.TestResult testResult)
        {
            ObjectModel.TestProperty parentExecutionIdProperty = testResult.Properties.FirstOrDefault(property => property.Id.Equals(TrxLoggerConstants.ParentExecutionIdPropertyIdentifier));
            return parentExecutionIdProperty == null ? Guid.Empty : testResult.GetPropertyValue(parentExecutionIdProperty, default(Guid));
        }

        private Guid GetExecutionId(ObjectModel.TestResult testResult)
        {
            ObjectModel.TestProperty executionIdProperty = testResult.Properties.FirstOrDefault(property => property.Id.Equals(TrxLoggerConstants.ExecutionIdPropertyIdentifier));// TODO: should we pass executionid and parentExecutionId from trxlogger itself?
            var executionId = Guid.Empty;
            if (executionIdProperty != null)
            {
                executionId = testResult.GetPropertyValue(executionIdProperty, Guid.NewGuid());
            }
            return executionId.Equals(Guid.Empty) ? Guid.NewGuid() : executionId;
        }

        private Guid GetTestType(ObjectModel.TestResult testResult)
        {
            var testType = TrxLoggerConstants.UnitTestType;

            // Get test type from property. default to unit test type.
            ObjectModel.TestProperty testTypeProperty = testResult.Properties.FirstOrDefault(property => property.Id.Equals(TrxLoggerConstants.TestTypePropertyIdentifier));
            testType = (testTypeProperty == null) ? testType : testResult.GetPropertyValue(testTypeProperty, testType);

            // Except OrderedTest, all test types are updated to unit test type.
            testType = (testType == TrxLoggerConstants.OrderedTestType) ? testType : TrxLoggerConstants.UnitTestType;

            return testType;
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
            System.Diagnostics.Debugger.Launch();
            if (this.testRun == null)
                CreateTestRun();

            // Convert skipped test to a log entry as that is the behaviour of mstest.
            if (e.Result.Outcome == ObjectModel.TestOutcome.Skipped)
                this.HandleSkippedTest(e.Result);

            var innerResultsCount = GetInnerResultsCount(e.Result);
            var executionId = GetExecutionId(e.Result); // TODO: instead of creating these in converter also, create at only one place.
            var parentExecutionId = GetParentExecutionId(e.Result);

            ITestResult parentTestResult = null;
            ITestElement parentTestElement = null;
            if (parentExecutionId != Guid.Empty)
            {
                this.results.TryGetValue(parentExecutionId, out parentTestResult); // todo: handle guid empty case
                if (parentTestResult == null)
                {
                    this.additionalResults.TryGetValue(parentExecutionId, out parentTestResult);
                }
                // we should always get parentTestResult. If not that a problem.
                this.testElements.TryGetValue(parentTestResult.Id.TestId.Id, out parentTestElement); // todo: handle parent test result null case.
            }

            var testType = GetTestType(e.Result);
                //e.Result.TestCase.ExecutorUri.ToString().ToLower().Contains("orderedtestadapter") ? "orderedtest" : "unittest"; //TODO: all hard coded vars in consts

            // Create MSTest test element from rocksteady test case
            ITestElement testElement = null;
            if (parentExecutionId == Guid.Empty)
            {
                Guid testId = Converter.GetTestId(e.Result.TestCase);
                if (!this.testElements.ContainsKey(testId))
                {
                    var name = e.Result.TestCase.DisplayName;
                    testElement = Converter.ToTestElement(testType, name, executionId, parentExecutionId, e.Result.TestCase); // TODO: if resulttype is not used anywhere else, then move creation of result type also in converter.
                    testElements.Add(testId, testElement);
                }
                else
                {
                    this.testElements.TryGetValue(testId, out testElement);
                }
            }
            else if(parentTestElement != null && parentTestElement is OrderedTestElement)
            {
                Guid testId = Converter.GetTestId(e.Result.TestCase); // TODO: if we are adding test id here, then dont create test id in totestelement. its a duplicacy.
                if (!this.testElements.ContainsKey(testId))
                {
                    var name = e.Result.TestCase.DisplayName;
                    testElement = Converter.ToTestElement(testType, name, executionId, parentExecutionId, e.Result.TestCase); // TODO: if resulttype is not used anywhere else, then move creation of result type also in converter.
                    testElements.Add(testId, testElement);
                    (parentTestElement as OrderedTestElement).TestLinks.Add(new TestLink(testElement.Id.Id, testElement.Name, testElement.Storage));
                }
                else
                {
                    this.testElements.TryGetValue(testId, out testElement);
                }
                //create/get test element if not exists. and link this test element to parent test element
            }
            else if (parentTestElement != null)
            {
                testElement = parentTestElement;
                //getParent test element
            }
            else
            {
                testElement = null;
                // this case should never come. If it comes, throw error.
            }

            // Convert the rocksteady result to MSTest result
            ITestResult testResult;
            TrxLoggerObjectModel.TestOutcome testOutcome = Converter.ToOutcome(e.Result.Outcome);
            if (testElement is OrderedTestElement)
            {
                // it should be orderedtestelement
                testResult = Converter.ToTestResult(e.Result, executionId, parentExecutionId, testElement, testOutcome, this.testRun, this.testResultsDirPath); // TODO: if resulttype is not used anywhere else, then move creation of result type also in converter.
            }
            else
            {
                // it should be unitTestElement
                testResult = Converter.ToTestResult(e.Result, executionId, parentExecutionId, testElement, testOutcome, this.testRun, this.testResultsDirPath); // TODO: if resulttype is not used anywhere else, then move creation of result type also in converter.
            }

            if (parentExecutionId == Guid.Empty)
            {
                this.results.Add(executionId, testResult);

                TestEntry te = new TestEntry(testElement.Id, TestListCategory.UncategorizedResults.Id);
                te.ExecId = testElement.ExecutionId;
                this.entries.Add(executionId, te);
            }
            else if (parentTestElement is OrderedTestElement)
            {
                (parentTestResult as TestResultAggregation).InnerResults.Add(testResult);
                this.additionalResults.Add(executionId, testResult);

                TestEntry te = new TestEntry(testElement.Id, TestListCategory.UncategorizedResults.Id);
                te.ExecId = testElement.ExecutionId;
                te.ParentExecId = testElement.ParentExecutionId;

                this.entries.TryGetValue(parentExecutionId, out var parentTestEntry); // todo: handle null case.
                parentTestEntry.TestEntries.Add(te);
            }
            else
            {
                testResult.DataRowInfo = (parentTestResult as TestResultAggregation).InnerResults.Count;
                testResult.ResultType = "DataDrivenDataRow"; // todo: hard coding
                parentTestResult.ResultType = "DataDrivenTest"; // todo: hard coding
                (parentTestResult as TestResultAggregation).InnerResults.Add(testResult);
            }
            // todo: is there any use of inner results? If not remove it.
            // todo: in case parent test result or parent test element is not found, what should we do? this case should never happen. So we should error out.


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
            System.Diagnostics.Debugger.Launch();
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
                helper.SaveIEnumerable(this.results.Values, rootElement, "Results", ".", null, parameters);

                // Save test definitions
                helper.SaveIEnumerable(this.testElements.Values, rootElement, "TestDefinitions", ".", null, parameters);

                // Save test entries
                helper.SaveIEnumerable(this.entries.Values, rootElement, "TestEntries", ".", "TestEntry", parameters);

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
                    var overwriteWarningMsg = string.Format(CultureInfo.CurrentCulture,
                        TrxLoggerResources.TrxLoggerResultsFileOverwriteWarning, trxFileName);
                    ConsoleOutput.Instance.Warning(false, overwriteWarningMsg);
                    EqtTrace.Warning(overwriteWarningMsg);
                }

                using (var fs = File.Open(trxFileName, FileMode.Create))
                {
                    rootElement.OwnerDocument.Save(fs);
                }
                String resultsFileMessage = String.Format(CultureInfo.CurrentCulture, TrxLoggerResources.TrxLoggerResultsFile, trxFileName);
                ConsoleOutput.Instance.Information(false, resultsFileMessage);
                EqtTrace.Info(resultsFileMessage);
            }
            catch (System.UnauthorizedAccessException fileWriteException)
            {
                ConsoleOutput.Instance.Error(false, fileWriteException.Message);
            }
        }

        // Initializes trx logger cache.
        private void InitializeInternal()
        {
            this.results = new Dictionary<Guid, TrxLoggerObjectModel.ITestResult>();
            this.additionalResults = new Dictionary<Guid, TrxLoggerObjectModel.ITestResult>();
            this.testElements = new Dictionary<Guid, ITestElement>();
            this.entries = new Dictionary<Guid,TestEntry>();
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
                var isLogFileNameParameterExists = this.parametersDictionary.TryGetValue(TrxLoggerConstants.LogFileNameKey, out string logFileNameValue);
                if (isLogFileNameParameterExists && !string.IsNullOrWhiteSpace(logFileNameValue))
                {
                    this.trxFilePath = Path.Combine(this.testResultsDirPath, logFileNameValue);
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

        /// <summary>
        /// Sets auto generated Trx file name under test results directory.
        /// </summary>
        private void SetDefaultTrxFilePath()
        {
            var defaultTrxFileName = this.testRun.RunConfiguration.RunDeploymentRootDirectory + ".trx";
            this.trxFilePath = FileHelper.GetNextIterationFileName(this.testResultsDirPath, defaultTrxFileName, false);
        }

        #endregion
    }
}