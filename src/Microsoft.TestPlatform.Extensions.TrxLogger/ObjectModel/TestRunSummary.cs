// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.Extensions.TrxLogger.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Xml;

    using Microsoft.TestPlatform.Extensions.TrxLogger.XML;

    /// <summary>
    /// XML object for saving test summary - Outcome and counts (passed, failed etc)
    /// </summary>
    internal class TestRunSummary : IXmlTestStore
    {
        #region Fields

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@total")]
        private int totalTests;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@executed")]
        private int executedTests;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@passed")]
        private int passedTests;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@failed")]
        private int failedTests;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@error")]
        private int errorTests;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@timeout")]
        private int timeoutTests;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@aborted")]
        private int abortedTests;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@inconclusive")]
        private int inconclusiveTests;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@passedButRunAborted")]
        private int passedButRunAborted;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@notRunnable")]
        private int notRunnable;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@notExecuted")]
        private int notExecuted;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@disconnected")]
        private int disconnected;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@warning")]
        private int warning;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@completed")]
        private int completed;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@inProgress")]
        private int inProgress;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Counters/@pending")]
        private int pending;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField]
        private TestOutcome outcome = TestOutcome.Pending;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "Reviewed. Suppression is OK here.")]
        [StoreXmlSimpleField("Output/StdOut", "")]
        private string stdOut = string.Empty;

        private List<RunInfo> runLevelErrorsAndWarnings;

        private List<CollectorDataEntry> collectorDataEntries;

        private IList<String> resultFiles;

        #endregion

        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunSummary"/> class.
        /// </summary>
        /// <param name="total">
        /// The total number of tests discover in this run.
        /// </param>
        /// <param name="executed">
        /// The executed tests.
        /// </param>
        /// <param name="pass">
        /// The pass tests.
        /// </param>
        /// <param name="fail">
        /// The fail tests.
        /// </param>
        /// <param name="outcome">
        /// The outcome.
        /// </param>
        /// <param name="runMessages">
        /// The run messages.
        /// </param>
        /// <param name="stdOut">
        /// The standard out.
        /// </param>
        /// <param name="resultFiles">
        /// The result files.
        /// </param>
        /// <param name="dataCollectors">
        /// The data collectors.
        /// </param>
        public TestRunSummary(
            int total,
            int executed,
            int pass,
            int fail,
            TestOutcome outcome,
            List<RunInfo> runMessages,
            string stdOut,
            IList<String> resultFiles,
            List<CollectorDataEntry> dataCollectors)
        {
            this.totalTests = total;
            this.executedTests = executed;
            this.passedTests = pass;
            this.failedTests = fail;
            int countForNonExistingResults = 0; // if below values are assigned constants 0, compiler gives warning CS0414
            this.abortedTests = countForNonExistingResults;
            this.errorTests = countForNonExistingResults;
            this.timeoutTests = countForNonExistingResults;
            this.inconclusiveTests = countForNonExistingResults;
            this.passedButRunAborted = countForNonExistingResults;
            this.notRunnable = countForNonExistingResults;
            this.notExecuted = countForNonExistingResults;
            this.disconnected = countForNonExistingResults;
            this.warning = countForNonExistingResults;
            this.completed = countForNonExistingResults;
            this.inProgress = countForNonExistingResults;
            this.pending = countForNonExistingResults;

            this.outcome = outcome;
            this.stdOut = stdOut;

            this.runLevelErrorsAndWarnings = runMessages;
            this.resultFiles = resultFiles;
            this.collectorDataEntries = dataCollectors;
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
        public void Save(XmlElement element, XmlTestStoreParameters parameters)
        {
            XmlPersistence helper = new XmlPersistence();
            helper.SaveSingleFields(element, this, parameters);
            helper.SaveIEnumerable(this.runLevelErrorsAndWarnings, element, "RunInfos", ".", "RunInfo", parameters);
            helper.SaveIEnumerable(this.resultFiles, element, "ResultFiles", "@path", "ResultFile", parameters);
            helper.SaveIEnumerable(this.collectorDataEntries, element, "CollectorDataEntries", ".", "Collector", parameters);
        }

        #endregion
    }
}