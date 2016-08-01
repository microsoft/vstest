// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;

    using Microsoft.TestPlatform.Extensions.TrxLogger.Utility;
    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Class for unit test result.
    /// </summary>
    internal class UnitTestResult: IXmlTestStore
    {
        #region Fields
        // id of test within run
        private TestResultId id;

        // name of test within run
        private string testName;

        private string computerInfo;

        private TimeSpan duration;

        private DateTime startTime;

        private DateTime endTime;

        // type of test (Guid)
        private TestType testType;

        /// <summary>
        /// The outcome of the test result
        /// </summary>
        private TestOutcome outcome;

        /// <summary>
        /// The test run in which the test was executed
        /// </summary>
        private TestRun testRun;

        private string stdOut;

        private string stdErr;

        private string debugTrace;

        private TestResultErrorInfo errorInfo;

        private TestListCategoryId categoryId;

        private ArrayList textMessages;

        /// <summary>
        /// Directory containing the test result files, relative to the root test results directory
        /// </summary>
        private string relativeTestResultsDirectory;

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
        public UnitTestResult(string computerName, Guid runId, UnitTestElement test, TestOutcome outcome)
        {
            Debug.Assert(computerName != null, "computername is null");
            Debug.Assert(test != null, "test is null");
            Debug.Assert(!Guid.Empty.Equals(test.ExecutionId.Id), "ExecutionId is empty");
            Debug.Assert(!Guid.Empty.Equals(test.Id.Id), "Id is empty");

            this.Initialize();

            this.id = new TestResultId(runId, test.ExecutionId, test.Id);
            this.testName = test.Name;
            this.testType = test.TestType;
            this.computerInfo = computerName;

            this.outcome = outcome;
            this.categoryId = test.CategoryId;
            this.relativeTestResultsDirectory = TestRunDirectories.GetRelativeTestResultsDirectory(test.ExecutionId.Id);
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
            get
            {
                return this.duration;
            }

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
            get
            {
                if (this.errorInfo == null)
                {
                    return string.Empty;
                }

                return this.errorInfo.Message;
            }

            set
            {
                this.errorInfo = new TestResultErrorInfo(value);
            }
        }

        /// <summary>
        /// Gets or sets the error stack trace.
        /// </summary>
        public string ErrorStackTrace
        {
            get
            {
                if (this.errorInfo == null)
                {
                    return string.Empty;
                }

                return this.errorInfo.StackTrace;
            }

            set
            {
                Debug.Assert(this.errorInfo != null, "errorInfo is null");
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
            get
            {
                return (string[])this.textMessages.ToArray(typeof(string));
            }

            internal set
            {
                if (value != null)
                {
                    this.textMessages = new ArrayList(value);
                }
                else
                {
                    this.textMessages.Clear();
                }
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
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, TrxResource.Common_MissingRunInResult));
                }

                return this.testRun.GetResultFilesDirectory(this);
            }
        }

        /// <summary>
        /// Gets the directory containing the test result files, relative to the root results directory
        /// </summary>
        internal string RelativeTestResultsDirectory
        {
            get
            {
                return this.relativeTestResultsDirectory;
            }
        }

        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            UnitTestResult trm = obj as UnitTestResult;
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
                Debug.Assert(Path.IsPathRooted(resultFile), "'resultFile' is a relative path");

                this.resultFiles[FileHelper.MakePathRelative(resultFile, testResultsDirectory)] = null;
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
#if DEBUG
                // Verify that any URI data attachments in the entry have relative paths
                foreach (IDataAttachment attachment in collectorDataEntry.Attachments)
                {
                    UriDataAttachment uriDataAttachment = attachment as UriDataAttachment;
                    if (uriDataAttachment != null)
                    {
                        Debug.Assert(uriDataAttachment.Uri.IsAbsoluteUri, "'collectorDataEntry' contains a URI data attachment with a relative URI");
                    }
                }
#endif

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
        public void Save(System.Xml.XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence helper = new XmlPersistence();

            helper.SaveObject(this.id, element, ".", parameters);
            helper.SaveSimpleField(element, "@testName", this.testName, string.Empty);
            helper.SaveSimpleField(element, "@computerName", this.computerInfo, string.Empty);
            helper.SaveSimpleField(element, "@duration", this.duration, default(TimeSpan));
            helper.SaveSimpleField(element, "@startTime", this.startTime, default(DateTime));
            helper.SaveSimpleField(element, "@endTime", this.endTime, default(DateTime));
            helper.SaveGuid(element, "@testType", this.testType.Id);

            if (this.stdOut != null)
            {
                this.stdOut = this.stdOut.Trim();
            }

            if (this.stdErr != null)
            {
                this.stdErr = this.stdErr.Trim();
            }

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
        }

        #endregion

        private void Initialize()
        {
            this.textMessages = new ArrayList();
        }
    }
}
