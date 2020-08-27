// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger
{
    using Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using ObjectModel.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using TrxLoggerConstants = Microsoft.TestPlatform.Extensions.TrxLogger.Utility.Constants;
    using TrxLoggerObjectModel = Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel;
    using TrxLoggerResources = Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    /// <summary>
    /// Logger for Generating TRX
    /// </summary>
    [FriendlyName(TrxLoggerConstants.FriendlyName)]
    [ExtensionUri(TrxLoggerConstants.ExtensionUri)]
    public class TrxLogger : ITestLoggerWithParameters
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TrxLogger"/> class.
        /// </summary>
        public TrxLogger() : this(new Utilities.Helpers.FileHelper()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrxLogger"/> class.
        /// Constructor with Dependency injection. Used for unit testing.
        /// </summary>
        /// <param name="fileHelper">The file helper interface.</param>
        protected TrxLogger(IFileHelper fileHelper) : this(new Utilities.Helpers.FileHelper(), new TrxFileHelper()) { }

        internal TrxLogger(IFileHelper fileHelper, TrxFileHelper trxFileHelper)
        {
            this.converter = new Converter(fileHelper, trxFileHelper);
            this.trxFileHelper = trxFileHelper;
        }

        #endregion

        #region Fields

        /// <summary>
        /// Cache the TRX file path
        /// </summary>
        private string trxFilePath;

        // The converter class
        private Converter converter;

        private TrxLoggerObjectModel.TestRun testRun;
        private ConcurrentDictionary<Guid, TrxLoggerObjectModel.ITestResult> results;
        private ConcurrentDictionary<Guid, TrxLoggerObjectModel.ITestElement> testElements;
        private ConcurrentDictionary<Guid, TestEntry> entries;

        // Caching results and inner test entries for constant time lookup for inner parents.
        private ConcurrentDictionary<Guid, TrxLoggerObjectModel.ITestResult> innerResults;
        private ConcurrentDictionary<Guid, TestEntry> innerTestEntries;

        private readonly TrxFileHelper trxFileHelper;

        /// <summary>
        /// Specifies the run level "out" messages
        /// </summary>
        private StringBuilder runLevelStdOut;

        // List of run level errors and warnings generated. These are logged in the Trx in the Results Summary.
        private List<TrxLoggerObjectModel.RunInfo> runLevelErrorsAndWarnings;

        private TrxLoggerObjectModel.TestOutcome testRunOutcome = TrxLoggerObjectModel.TestOutcome.Passed;

        private int totalTests, passTests, failTests;

        private DateTime testRunStartTime;

        private string trxFileExtension = ".trx";

        /// <summary>
        /// Parameters dictionary for logger. Ex: {"LogFileName":"TestResults.trx"}.
        /// </summary>
        private Dictionary<string, string> parametersDictionary;

        /// <summary>
        /// Gets the directory under which default trx file and test results attachments should be saved.
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

            var isLogFilePrefixParameterExists = parameters.TryGetValue(TrxLoggerConstants.LogFilePrefixKey, out string logFilePrefixValue);
            var isLogFileNameParameterExists = parameters.TryGetValue(TrxLoggerConstants.LogFileNameKey, out string logFileNameValue);

            if (isLogFilePrefixParameterExists && isLogFileNameParameterExists)
            {
                var trxParameterErrorMsg = string.Format(CultureInfo.CurrentCulture,
                        TrxLoggerResources.PrefixAndNameProvidedError);

                EqtTrace.Error(trxParameterErrorMsg);
                throw new ArgumentException(trxParameterErrorMsg);
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
            // Create test run
            if (this.testRun == null)
                CreateTestRun();

            // Convert skipped test to a log entry as that is the behavior of mstest.
            if (e.Result.Outcome == ObjectModel.TestOutcome.Skipped)
                this.HandleSkippedTest(e.Result);

            var testType = this.converter.GetTestType(e.Result);
            var executionId = this.converter.GetExecutionId(e.Result);

            // Setting parent properties like parent result, parent test element, parent execution id.
            var parentExecutionId = this.converter.GetParentExecutionId(e.Result);
            var parentTestResult = GetTestResult(parentExecutionId);
            var parentTestElement = (parentTestResult != null) ? GetTestElement(parentTestResult.Id.TestId) : null;

            // Switch to flat test results in case any parent related information is missing.
            if (parentTestResult == null || parentTestElement == null || parentExecutionId == Guid.Empty)
            {
                parentTestResult = null;
                parentTestElement = null;
                parentExecutionId = Guid.Empty;
            }

            // Create trx test element from rocksteady test case
            var testElement = GetOrCreateTestElement(executionId, parentExecutionId, testType, parentTestElement, e.Result);

            // Update test links. Test Links are updated in case of Ordered test.
            UpdateTestLinks(testElement, parentTestElement);

            // Convert the rocksteady result to trx test result
            var testResult = CreateTestResult(executionId, parentExecutionId, testType, testElement, parentTestElement, parentTestResult, e.Result);

            // Update test entries
            UpdateTestEntries(executionId, parentExecutionId, testElement, parentTestElement);

            // Set various counts (passed tests, failed tests, total tests)
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
            // Create test run
            // If abort occurs there is no call to TestResultHandler which results in testRun not created.
            // This happens when some test aborts in the first batch of execution.
            if (this.testRun == null)
                CreateTestRun();

            XmlPersistence helper = new XmlPersistence();
            XmlTestStoreParameters parameters = XmlTestStoreParameters.GetParameters();
            XmlElement rootElement = helper.CreateRootElement("TestRun");

            // Save runId/username/creation time etc.
            this.testRun.Finished = DateTime.UtcNow;
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
            List<CollectorDataEntry> collectorEntries = this.converter.ToCollectionEntries(e.AttachmentSets, this.testRun, this.testResultsDirPath);
            IList<String> resultFiles = this.converter.ToResultFiles(e.AttachmentSets, this.testRun, this.testResultsDirPath, errorMessages);

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


            this.ReserveTrxFilePath();
            this.PopulateTrxFile(this.trxFilePath, rootElement);
        }

        /// <summary>
        /// populate trx file from the xml element
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
                using (var fs = File.Open(trxFileName, FileMode.Truncate))
                {
                    using (XmlWriter writer = XmlWriter.Create(fs, new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize, Indent = true }))
                    {
                        rootElement.OwnerDocument.Save(writer);
                        writer.Flush();
                    }
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
            this.results = new ConcurrentDictionary<Guid, TrxLoggerObjectModel.ITestResult>();
            this.innerResults = new ConcurrentDictionary<Guid, TrxLoggerObjectModel.ITestResult>();
            this.testElements = new ConcurrentDictionary<Guid, ITestElement>();
            this.entries = new ConcurrentDictionary<Guid, TestEntry>();
            this.innerTestEntries = new ConcurrentDictionary<Guid, TestEntry>();
            this.runLevelErrorsAndWarnings = new List<RunInfo>();
            this.testRun = null;
            this.totalTests = 0;
            this.passTests = 0;
            this.failTests = 0;
            this.runLevelStdOut = new StringBuilder();
            this.testRunStartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Add run level informational message
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        private void AddRunLevelInformationalMessage(string message)
        {
            this.runLevelStdOut.AppendLine(message);
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

        private void ReserveTrxFilePath()
        {
            for (short retries = 0; retries != short.MaxValue; retries++)
            {
                var filePath = AcquireTrxFileNamePath(out var shouldOverwrite);

                if (shouldOverwrite && File.Exists(filePath))
                {
                    var overwriteWarningMsg = string.Format(CultureInfo.CurrentCulture, TrxLoggerResources.TrxLoggerResultsFileOverwriteWarning, filePath);
                    ConsoleOutput.Instance.Warning(false, overwriteWarningMsg);
                    EqtTrace.Warning(overwriteWarningMsg);
                }
                else
                {
                    try
                    {
                        using (var fs = File.Open(filePath, FileMode.CreateNew)) { }
                    }
                    catch (IOException)
                    {
                        // File already exists, try again!
                        continue;
                    }
                }

                trxFilePath = filePath;
                return;
            }
        }

        private string AcquireTrxFileNamePath(out bool shouldOverwrite)
        {
            shouldOverwrite = false;
            var isLogFileNameParameterExists = parametersDictionary.TryGetValue(TrxLoggerConstants.LogFileNameKey, out string logFileNameValue) && !string.IsNullOrWhiteSpace(logFileNameValue);
            var isLogFilePrefixParameterExists = parametersDictionary.TryGetValue(TrxLoggerConstants.LogFilePrefixKey, out string logFilePrefixValue) && !string.IsNullOrWhiteSpace(logFilePrefixValue);

            string filePath = null;
            if (isLogFilePrefixParameterExists)
            {
                if (parametersDictionary.TryGetValue(DefaultLoggerParameterNames.TargetFramework, out var framework) && framework != null)
                {
                    framework = Framework.GetShortFolderName(framework);
                    logFilePrefixValue = logFilePrefixValue + "_" + framework;
                }

                filePath = trxFileHelper.GetNextTimestampFileName(this.testResultsDirPath, logFilePrefixValue + this.trxFileExtension, "_yyyyMMddHHmmss");
            }

            else if (isLogFileNameParameterExists)
            {
                filePath = Path.Combine(this.testResultsDirPath, logFileNameValue);
                shouldOverwrite = true;
            }

            filePath = filePath ?? this.SetDefaultTrxFilePath();

            var trxFileDirPath = Path.GetDirectoryName(filePath);
            if (Directory.Exists(trxFileDirPath) == false)
            {
                Directory.CreateDirectory(trxFileDirPath);
            }

            return filePath;
        }

        /// <summary>
        /// Returns an auto generated Trx file name under test results directory.
        /// </summary>
        private string SetDefaultTrxFilePath()
        {
            var defaultTrxFileName = this.testRun.RunConfiguration.RunDeploymentRootDirectory + ".trx";
            
            return trxFileHelper.GetNextIterationFileName(this.testResultsDirPath, defaultTrxFileName, false);
        }

        /// <summary>
        /// Creates test run.
        /// </summary>
        private void CreateTestRun()
        {
            // Skip run creation if already exists.
            if (testRun != null)
                return;

            Guid runId = Guid.NewGuid();
            this.testRun = new TestRun(runId);

            // We cannot rely on the StartTime for the first test result
            // In case of parallel, first test result is the fastest test and not the one which started first.
            // Setting Started to DateTime.Now in Initialize will make sure we include the startup cost, which was being ignored earlier.
            // This is in parity with the way we set this.testRun.Finished
            this.testRun.Started = this.testRunStartTime;

            // Save default test settings
            string runDeploymentRoot = trxFileHelper.ReplaceInvalidFileNameChars(this.testRun.Name);
            TestRunConfiguration testrunConfig = new TestRunConfiguration("default", trxFileHelper);
            testrunConfig.RunDeploymentRootDirectory = runDeploymentRoot;
            this.testRun.RunConfiguration = testrunConfig;
        }

        /// <summary>
        /// Gets test result from stored test results.
        /// </summary>
        /// <param name="executionId"></param>
        /// <returns>Test result</returns>
        private ITestResult GetTestResult(Guid executionId)
        {
            ITestResult testResult = null;

            if (executionId != Guid.Empty)
            {
                this.results.TryGetValue(executionId, out testResult);

                if (testResult == null)
                    this.innerResults.TryGetValue(executionId, out testResult);
            }

            return testResult;
        }

        /// <summary>
        /// Gets test element from stored test elements.
        /// </summary>
        /// <param name="testId"></param>
        /// <returns></returns>
        private ITestElement GetTestElement(Guid testId)
        {
            this.testElements.TryGetValue(testId, out var testElement);
            return testElement;
        }

        /// <summary>
        /// Gets or creates test element.
        /// </summary>
        /// <param name="executionId"></param>
        /// <param name="parentExecutionId"></param>
        /// <param name="testType"></param>
        /// <param name="parentTestElement"></param>
        /// <param name="rockSteadyTestCase"></param>
        /// <returns>Trx test element</returns>
        private ITestElement GetOrCreateTestElement(Guid executionId, Guid parentExecutionId, TestType testType, ITestElement parentTestElement, ObjectModel.TestResult rockSteadyTestResult)
        {
            ITestElement testElement = parentTestElement;

            // For scenarios like data driven tests, test element is same as parent test element.
            if (parentTestElement != null && !parentTestElement.TestType.Equals(TrxLoggerConstants.OrderedTestType))
            {
                return testElement;
            }

            TestCase testCase = rockSteadyTestResult.TestCase;
            Guid testId = this.converter.GetTestId(testCase);

            // Scenario for inner test case when parent test element is not present.
            var testName = testCase.DisplayName;
            var adapter = testCase.ExecutorUri.ToString();
            if (adapter.Contains(TrxLoggerConstants.MstestAdapterString) &&
                parentTestElement == null &&
                !string.IsNullOrEmpty(rockSteadyTestResult.DisplayName))
            {
                // Note: For old mstest adapters hierarchical support was not present. Thus inner result of data driven was identified using test result display name.
                // Non null test result display name means its a inner result of data driven/ordered test.
                // Changing GUID to keep supporting old mstest adapters.
                testId = Guid.NewGuid();
                testName = rockSteadyTestResult.DisplayName;
            }

            // Get test element
            testElement = GetTestElement(testId);

            // Create test element
            if (testElement == null)
            {
                testElement = this.converter.ToTestElement(testId, executionId, parentExecutionId, testName, testType, testCase);
                testElements.TryAdd(testId, testElement);
            }

            return testElement;
        }

        /// <summary>
        /// Update test links
        /// </summary>
        /// <param name="testElement"></param>
        /// <param name="parentTestElement"></param>
        private void UpdateTestLinks(ITestElement testElement, ITestElement parentTestElement)
        {
            if (parentTestElement != null &&
                parentTestElement.TestType.Equals(TrxLoggerConstants.OrderedTestType) &&
                !(parentTestElement as OrderedTestElement).TestLinks.ContainsKey(testElement.Id.Id))
            {
                (parentTestElement as OrderedTestElement).TestLinks.Add(testElement.Id.Id, new TestLink(testElement.Id.Id, testElement.Name, testElement.Storage));
            }
        }

        /// <summary>
        /// Creates test result
        /// </summary>
        /// <param name="executionId"></param>
        /// <param name="parentExecutionId"></param>
        /// <param name="testType"></param>
        /// <param name="testElement"></param>
        /// <param name="parentTestElement"></param>
        /// <param name="parentTestResult"></param>
        /// <param name="rocksteadyTestResult"></param>
        /// <returns>Trx test result</returns>
        private ITestResult CreateTestResult(Guid executionId, Guid parentExecutionId, TestType testType,
            ITestElement testElement, ITestElement parentTestElement, ITestResult parentTestResult, ObjectModel.TestResult rocksteadyTestResult)
        {
            // Create test result
            TrxLoggerObjectModel.TestOutcome testOutcome = this.converter.ToOutcome(rocksteadyTestResult.Outcome);
            var testResult = this.converter.ToTestResult(testElement.Id.Id, executionId, parentExecutionId, testElement.Name,
                this.testResultsDirPath, testType, testElement.CategoryId, testOutcome, this.testRun, rocksteadyTestResult);

            // Normal result scenario
            if (parentTestResult == null)
            {
                this.results.TryAdd(executionId, testResult);
                return testResult;
            }

            // Ordered test inner result scenario
            if (parentTestElement != null && parentTestElement.TestType.Equals(TrxLoggerConstants.OrderedTestType))
            {
                (parentTestResult as TestResultAggregation).InnerResults.Add(testResult);
                this.innerResults.TryAdd(executionId, testResult);
                return testResult;
            }

            // Data driven inner result scenario
            if (parentTestElement != null && parentTestElement.TestType.Equals(TrxLoggerConstants.UnitTestType))
            {
                (parentTestResult as TestResultAggregation).InnerResults.Add(testResult);
                testResult.DataRowInfo = (parentTestResult as TestResultAggregation).InnerResults.Count;
                testResult.ResultType = TrxLoggerConstants.InnerDataDrivenResultType;
                parentTestResult.ResultType = TrxLoggerConstants.ParentDataDrivenResultType;
                return testResult;
            }

            return testResult;
        }

        /// <summary>
        /// Update test entries
        /// </summary>
        /// <param name="executionId"></param>
        /// <param name="parentExecutionId"></param>
        /// <param name="testElement"></param>
        /// <param name="parentTestElement"></param>
        private void UpdateTestEntries(Guid executionId, Guid parentExecutionId, ITestElement testElement, ITestElement parentTestElement)
        {
            TestEntry te = new TestEntry(testElement.Id, TestListCategory.UncategorizedResults.Id);
            te.ExecutionId = executionId;

            if (parentTestElement == null)
            {
                this.entries.TryAdd(executionId, te);
            }
            else if (parentTestElement.TestType.Equals(TrxLoggerConstants.OrderedTestType))
            {
                te.ParentExecutionId = parentExecutionId;

                var parentTestEntry = GetTestEntry(parentExecutionId);
                if (parentTestEntry != null)
                    parentTestEntry.TestEntries.Add(te);

                this.innerTestEntries.TryAdd(executionId, te);
            }
        }

        /// <summary>
        /// Gets test entry from stored test entries.
        /// </summary>
        /// <param name="executionId"></param>
        /// <returns>Test entry</returns>
        private TestEntry GetTestEntry(Guid executionId)
        {
            TestEntry testEntry = null;

            if (executionId != Guid.Empty)
            {
                this.entries.TryGetValue(executionId, out testEntry);

                if (testEntry == null)
                    this.innerTestEntries.TryGetValue(executionId, out testEntry);
            }

            return testEntry;
        }

        #endregion
    }
}
