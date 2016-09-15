// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Adapter
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The test execution recorder used for recording test results and test messages.
    /// </summary>
    internal class TestExecutionRecorder : TestSessionMessageLogger, ITestExecutionRecorder
    {
        private List<AttachmentSet> attachmentSets;
        private ITestRunCache testRunCache;
        private ITestCaseEventsHandler testCaseEventsHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecutionRecorder"/> class.
        /// </summary>
        /// <param name="testCaseEventsHandler"> The test Case Events Handler. </param>
        /// <param name="testRunCache"> The test run cache.  </param>
        public TestExecutionRecorder(ITestCaseEventsHandler testCaseEventsHandler, ITestRunCache testRunCache)
        {
            this.testRunCache = testRunCache;
            this.testCaseEventsHandler = testCaseEventsHandler;
            this.attachmentSets = new List<AttachmentSet>();
        }

        /// <summary>
        /// Gets the attachments received from adapters.
        /// </summary>
        internal Collection<AttachmentSet> Attachments
        {
            get
            {
                return new Collection<AttachmentSet>(this.attachmentSets);
            }
        }

        /// <summary>
        /// Notify the framework about starting of the test case. 
        /// Framework sends this event to data collectors enabled in the run. If no data collector is enabled, then the event is ignored. 
        /// </summary>
        /// <param name="testCase">test case which will be started.</param>
        public void RecordStart(TestCase testCase)
        {
            this.testRunCache.OnTestStarted(testCase);

            if (this.testCaseEventsHandler != null)
            {
                this.testCaseEventsHandler.SendTestCaseStart(testCase);
            }
        }

        /// <summary>
        /// Notify the framework about the test result.
        /// </summary>
        /// <param name="testResult">Test Result to be sent to the framework.</param>
        /// <exception cref="TestCanceledException">Exception thrown by the framework when an executor attempts to send 
        /// test result to the framework when the test(s) is canceled. </exception>  
        public void RecordResult(TestResult testResult)
        {
            var flushResult = true;
            // For test result, we cannot flush to cache unless the result is updated with datacollection data
            if (this.testCaseEventsHandler != null)
            {
                flushResult = this.testCaseEventsHandler.SendTestResult(testResult);
            }

            if (flushResult)
            {
                this.testRunCache.OnNewTestResult(testResult);
            }
        }

        /// <summary>
        /// Notify the framework about completion of the test case. 
        /// Framework sends this event to data collectors enabled in the run. If no data collector is enabled, then the event is ignored. 
        /// </summary>
        /// <param name="testCase">test case which has completed.</param>
        /// <param name="outcome">outcome of the test case.</param>
        public void RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            this.testRunCache.OnTestCompletion(testCase);

            if (this.testCaseEventsHandler != null)
            {
                this.testCaseEventsHandler.SendTestCaseEnd(testCase, outcome);
            }
        }

        /// <summary>
        /// Notify the framework about run level attachments.
        /// </summary>
        /// <param name="attachments"> The attachment sets. </param>
        public void RecordAttachments(IList<AttachmentSet> attachments)
        {
            this.attachmentSets.AddRange(attachments);
        }
    }
}
