
namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.EventHandlers
{
    
    using System;
#if !NET46
    using System.Runtime.Loader;
#endif
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;

    using TestPlatform.ObjectModel;
    using TestPlatform.ObjectModel.Engine;

    /// <summary>
    /// The test case events handler.
    /// </summary>
    internal class TestCaseEventsHandler : ITestCaseEventsHandler
    {

        private InProcDataCollectionExtensionManager inProcDataCollectionExtensionManager;

        private ITestCaseEventsHandler testCaseEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseEventsHandler"/> class.
        /// </summary>
        /// <param name="inProcDCExtMgr">
        /// The in proc tidc helper.
        /// </param>
        public TestCaseEventsHandler(InProcDataCollectionExtensionManager inProcDCExtMgr, ITestCaseEventsHandler testCaseEvents)
        {
            this.inProcDataCollectionExtensionManager = inProcDCExtMgr;
            this.testCaseEvents = testCaseEvents;
        }

        /// <summary>
        /// The send test case start.
        /// </summary>
        /// <param name="testCase">
        /// The test case.
        /// </param>
        public void SendTestCaseStart(TestCase testCase)
        {
            this.inProcDataCollectionExtensionManager.TriggerTestCaseStart(testCase);
            this.testCaseEvents?.SendTestCaseStart(testCase);
        }

        /// <summary>
        /// The send test case end.
        /// </summary>
        /// <param name="testCase">
        /// The test case.
        /// </param>
        /// <param name="outcome">
        /// The outcome.
        /// </param>
        public void SendTestCaseEnd(TestCase testCase, TestOutcome outcome)
        {
            this.inProcDataCollectionExtensionManager.TriggerTestCaseEnd(testCase, outcome);
            this.testCaseEvents?.SendTestCaseEnd(testCase, outcome);
        }

        /// <summary>
        /// The send test result.
        /// </summary>
        /// <param name="result">
        /// The result.
        /// </param>
        public void SendTestResult(TestResult result)
        {
            this.testCaseEvents?.SendTestResult(result);
        }
    }
}
