// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using TrxLoggerResources = Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.Resources.TrxResource;

    /// <summary>
    /// Class to uniquely identify test results
    /// </summary>
    internal sealed class TestResultId : IXmlTestStore
    {
        #region Fields

        private Guid runId;
        private Guid executionId;
        private Guid parentExecutionId;
        private Guid testId;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestResultId"/> class.
        /// </summary>
        /// <param name="runId">
        /// The run id.
        /// </param>
        /// <param name="executionId">
        /// The execution id.
        /// </param>
        /// <param name="parentExecutionId">
        /// The parent execution id.
        /// </param>
        /// <param name="testId">
        /// The test id.
        /// </param>
        public TestResultId(Guid runId, Guid executionId, Guid parentExecutionId, Guid testId)
        {
            this.runId = runId;
            this.executionId = executionId;
            this.parentExecutionId = parentExecutionId;
            this.testId = testId;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the execution id.
        /// </summary>
        public Guid ExecutionId
        {
            get { return this.executionId; }
        }

        /// <summary>
        /// Gets the parent execution id.
        /// </summary>
        public Guid ParentExecutionId
        {
            get { return this.parentExecutionId; }
        }

        /// <summary>
        /// Gets the test id.
        /// </summary>
        public Guid TestId
        {
            get { return this.testId; }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Override function for Equals
        /// </summary>
        /// <param name="obj">
        /// The object to compare
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            TestResultId tmpId = obj as TestResultId;
            if (tmpId == null)
            {
                return false;
            }

            return this.runId.Equals(tmpId.runId) && this.executionId.Equals((object)tmpId.executionId);
        }

        /// <summary>
        /// Override function for GetHashCode.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return this.runId.GetHashCode() ^ this.executionId.GetHashCode();
        }

        /// <summary>
        /// Override function for ToString.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            return this.executionId.ToString("B");
        }
        #endregion

        #region IXmlTestStore Members

        /// <summary>
        /// Saves the class under the XmlElement..
        /// </summary>
        /// <param name="element">
        /// The parent xml.
        /// </param>
        /// <param name="parameters">
        /// The parameter
        /// </param>
        public void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence helper = new XmlPersistence();

            if (this.executionId != null)
                helper.SaveGuid(element, "@executionId", this.executionId);
            if (this.parentExecutionId != null)
                helper.SaveGuid(element, "@parentExecutionId", this.parentExecutionId);

            helper.SaveGuid(element, "@testId", this.testId);
        }

        #endregion
    }

    /// <summary>
    /// The test result error info class.
    /// </summary>
    internal sealed class TestResultErrorInfo : IXmlTestStore
    {
        [StoreXmlSimpleField("Message", "")]
        private string message;

        [StoreXmlSimpleField("StackTrace", "")]
        private string stackTrace;


        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message
        {
            get { return this.message; }
            set { this.message = value; }
        }

        /// <summary>
        /// Gets or sets the stack trace.
        /// </summary>
        public string StackTrace
        {
            get { return this.stackTrace; }
            set { this.stackTrace = value; }
        }

        #region IXmlTestStore Members

        /// <summary>
        /// Saves the class under the XmlElement..
        /// </summary>
        /// <param name="element">
        /// The parent xml.
        /// </param>
        /// <param name="parameters">
        /// The parameter
        /// </param>
        public void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence.SaveUsingReflection(element, this, typeof(TestResultErrorInfo), parameters);
        }

        #endregion
    }

    /// <summary>
    /// Class for test result.
    /// </summary>
    internal class TestResult : ITestResult, IXmlTestStore
    {
        #region Fields

        private TestResultId id;
        private string resultName;
        private string computerInfo;
        private string stdOut;
        private string stdErr;
        private string debugTrace;
        private string resultType;
        private int dataRowInfo;
        private TimeSpan duration;
        private DateTime startTime;
        private DateTime endTime;
        private TestType testType;
        private TestOutcome outcome;
        private TestRun testRun;
        private TestResultErrorInfo errorInfo;
        private TestListCategoryId categoryId;
        private ArrayList textMessages;

        /// <summary>
        /// Directory containing the test result files, relative to the root test results directory
        /// </summary>
        private string relativeTestResultsDirectory;
        private readonly TrxFileHelper trxFileHelper;

        /// <summary>
        /// Paths to test result files, relative to the test results folder, sorted in increasing order
        /// </summary>
        private SortedList<string, object> resultFiles = new SortedList<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Information provided by data collectors for the test case
        /// </summary>
        private List<CollectorDataEntry> collectorDataEntries = new List<CollectorDataEntry>();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestResult"/> class.
        /// </summary>
        /// <param name="computerName">
        /// The computer name.
        /// </param>
        /// <param name="runId">
        /// The run id.
        /// </param>
        /// <param name="test">
        /// The test.
        /// </param>
        /// <param name="outcome">
        /// The outcome.
        /// </param>
        public TestResult(
            Guid runId,
            Guid testId,
            Guid executionId,
            Guid parentExecutionId,
            string resultName,
            string computerName,
            TestOutcome outcome,
            TestType testType,
            TestListCategoryId testCategoryId,
            TrxFileHelper trxFileHelper) 
        {
            Debug.Assert(computerName != null, "computername is null");
            Debug.Assert(!Guid.Empty.Equals(executionId), "ExecutionId is empty");
            Debug.Assert(!Guid.Empty.Equals(testId), "TestId is empty");

            this.Initialize();

            this.id = new TestResultId(runId, executionId, parentExecutionId, testId);
            this.resultName = resultName;
            this.testType = testType;
            this.computerInfo = computerName;
            this.outcome = outcome;
            this.categoryId = testCategoryId;
            this.relativeTestResultsDirectory = TestRunDirectories.GetRelativeTestResultsDirectory(executionId);
            this.trxFileHelper = trxFileHelper;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        public DateTime EndTime
        {
            get { return this.endTime; }
            set { this.endTime = value; }
        }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public DateTime StartTime
        {
            get { return this.startTime; }
            set { this.startTime = value; }
        }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        public TimeSpan Duration
        {
            get { return this.duration; }

            set
            {
                // On some hardware the Stopwatch.Elapsed can return a negative number.  This tends
                // to happen when the duration of the test is very short and it is hardware dependent
                // (seems to happen most on virtual machines or machines with AMD processors).  To prevent
                // reporting a negative duration, use TimeSpan.Zero when the elapsed time is less than zero.
                EqtTrace.WarningIf(value < TimeSpan.Zero, "TestResult.Duration: The duration is being set to {0}.  Since the duration is negative the duration will be updated to zero.", value);
                this.duration = value > TimeSpan.Zero ? value : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Gets the computer name.
        /// </summary>
        public string ComputerName
        {
            get { return this.computerInfo; }
        }

        /// <summary>
        /// Gets or sets the outcome.
        /// </summary>
        public TestOutcome Outcome
        {
            get { return this.outcome; }
            set { this.outcome = value; }
        }


        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public TestResultId Id
        {
            get { return this.id; }
            internal set { this.id = value; }
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage
        {
            get { return this.errorInfo?.Message ?? string.Empty; }
            set
            {
                if (this.errorInfo == null)
                    this.errorInfo = new TestResultErrorInfo();

                this.errorInfo.Message = value;
            }
        }

        /// <summary>
        /// Gets or sets the error stack trace.
        /// </summary>
        public string ErrorStackTrace
        {
            get { return this.errorInfo?.StackTrace ?? string.Empty; }

            set
            {
                if (this.errorInfo == null)
                    this.errorInfo = new TestResultErrorInfo();

                this.errorInfo.StackTrace = value;
            }
        }

        /// <summary>
        /// Gets the text messages.
        /// </summary>
        /// <remarks>
        /// Additional information messages from TestTextResultMessage, e.g. generated by TestOutcome.WriteLine.
        /// Avoid using this property in the following way: for (int i=0; i&lt;prop.Length; i++) { ... prop[i] ...}
        /// </remarks>
        public string[] TextMessages
        {
            get { return (string[])this.textMessages.ToArray(typeof(string)); }

            set
            {
                if (value != null)
                    this.textMessages = new ArrayList(value);
                else
                    this.textMessages.Clear();
            }
        }

        /// <summary>
        /// Gets or sets the standard out.
        /// </summary>
        public string StdOut
        {
            get { return this.stdOut ?? string.Empty; }
            set { this.stdOut = value; }
        }

        /// <summary>
        /// Gets or sets the standard err.
        /// </summary>
        public string StdErr
        {
            get { return this.stdErr ?? string.Empty; }
            set { this.stdErr = value; }
        }

        /// <summary>
        /// Gets or sets the debug trace.
        /// </summary>
        public string DebugTrace
        {
            get { return this.debugTrace ?? string.Empty; }
            set { this.debugTrace = value; }
        }

        /// <summary>
        /// Gets the path to the test results directory
        /// </summary>
        public string TestResultsDirectory
        {
            get
            {
                if (this.testRun == null)
                {
                    Debug.Fail("'m_testRun' is null");
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, TrxLoggerResources.Common_MissingRunInResult));
                }

                return this.testRun.GetResultFilesDirectory(this);
            }
        }

        /// <summary>
        /// Gets the directory containing the test result files, relative to the root results directory
        /// </summary>
        public string RelativeTestResultsDirectory
        {
            get { return this.relativeTestResultsDirectory; }
        }

        /// <summary>
        /// Gets or sets the data row info.
        /// </summary>
        public int DataRowInfo
        {
            get { return this.dataRowInfo; }
            set { this.dataRowInfo = value; }
        }

        /// <summary>
        /// Gets or sets the result type.
        /// </summary>
        public string ResultType
        {
            get { return this.resultType; }
            set { this.resultType = value; }
        }

        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            TestResult trm = obj as TestResult;
            if (trm == null)
            {
                return false;
            }
            Debug.Assert(this.id != null, "id is null");
            Debug.Assert(trm.id != null, "test result message id is null");
            return this.id.Equals(trm.id);
        }

        public override int GetHashCode()
        {
            Debug.Assert(this.id != null, "id is null");
            return this.id.GetHashCode();
        }

        #endregion

        /// <summary>
        /// Helper function to add a text message info to the test result
        /// </summary>
        /// <param name="text">Message to be added</param>
        public void AddTextMessage(string text)
        {
            EqtAssert.ParameterNotNull(text, "text");
            this.textMessages.Add(text);
        }

        /// <summary>
        /// Sets the test run the test was executed in
        /// </summary>
        /// <param name="testRun">The test run the test was executed in</param>
        internal virtual void SetTestRun(TestRun testRun)
        {
            Debug.Assert(testRun != null, "'testRun' is null");
            this.testRun = testRun;
        }

        /// <summary>
        /// Adds result files to the <see cref="resultFiles"/> collection
        /// </summary>
        /// <param name="resultFileList">Paths to the result files</param>
        internal void AddResultFiles(IEnumerable<string> resultFileList)
        {
            Debug.Assert(resultFileList != null, "'resultFileList' is null");

            string testResultsDirectory = this.TestResultsDirectory;
            foreach (string resultFile in resultFileList)
            {
                Debug.Assert(!string.IsNullOrEmpty(resultFile), "'resultFile' is null or empty");
                Debug.Assert(resultFile.Trim() == resultFile, "'resultFile' has whitespace at the ends");

                this.resultFiles[trxFileHelper.MakePathRelative(resultFile, testResultsDirectory)] = null;
            }
        }

        /// <summary>
        /// Adds collector data entries to the <see cref="collectorDataEntries"/> collection
        /// </summary>
        /// <param name="collectorDataEntryList">The collector data entry to add</param>
        internal void AddCollectorDataEntries(IEnumerable<CollectorDataEntry> collectorDataEntryList)
        {
            Debug.Assert(collectorDataEntryList != null, "'collectorDataEntryList' is null");

            string testResultsDirectory = this.TestResultsDirectory;
            foreach (CollectorDataEntry collectorDataEntry in collectorDataEntryList)
            {
                Debug.Assert(collectorDataEntry != null, "'collectorDataEntry' is null");
                Debug.Assert(!this.collectorDataEntries.Contains(collectorDataEntry), "The collector data entry already exists in the collection");

                this.collectorDataEntries.Add(collectorDataEntry.Clone(testResultsDirectory, false));
            }
        }


        #region IXmlTestStore Members

        /// <summary>
        /// Saves the class under the XmlElement..
        /// </summary>
        /// <param name="element">
        /// The parent xml.
        /// </param>
        /// <param name="parameters">
        /// The parameter
        /// </param>
        public virtual void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence helper = new XmlPersistence();

            helper.SaveObject(this.id, element, ".", parameters);
            helper.SaveSimpleField(element, "@testName", this.resultName, string.Empty);
            helper.SaveSimpleField(element, "@computerName", this.computerInfo, string.Empty);
            helper.SaveSimpleField(element, "@duration", this.duration, default(TimeSpan));
            helper.SaveSimpleField(element, "@startTime", this.startTime, default(DateTime));
            helper.SaveSimpleField(element, "@endTime", this.endTime, default(DateTime));
            helper.SaveGuid(element, "@testType", this.testType.Id);

            if (this.stdOut != null)
                this.stdOut = this.stdOut.Trim();

            if (this.stdErr != null)
                this.stdErr = this.stdErr.Trim();

            helper.SaveSimpleField(element, "@outcome", this.outcome, default(TestOutcome));
            helper.SaveSimpleField(element, "Output/StdOut", this.stdOut, string.Empty);
            helper.SaveSimpleField(element, "Output/StdErr", this.stdErr, string.Empty);
            helper.SaveSimpleField(element, "Output/DebugTrace", this.debugTrace, string.Empty);
            helper.SaveObject(this.errorInfo, element, "Output/ErrorInfo", parameters);
            helper.SaveGuid(element, "@testListId", this.categoryId.Id);
            helper.SaveIEnumerable(this.textMessages, element, "Output/TextMessages", ".", "Message", parameters);
            helper.SaveSimpleField(element, "@relativeResultsDirectory", this.relativeTestResultsDirectory, null);
            helper.SaveIEnumerable(this.resultFiles.Keys, element, "ResultFiles", "@path", "ResultFile", parameters);
            helper.SaveIEnumerable(this.collectorDataEntries, element, "CollectorDataEntries", ".", "Collector", parameters);

            if (this.dataRowInfo >= 0)
                helper.SaveSimpleField(element, "@dataRowInfo", this.dataRowInfo, -1);

            if (!string.IsNullOrEmpty(this.resultType))
                helper.SaveSimpleField(element, "@resultType", this.resultType, string.Empty);
        }

        #endregion

        private void Initialize()
        {
            this.textMessages = new ArrayList();
            this.dataRowInfo = -1;
        }
    }
}
