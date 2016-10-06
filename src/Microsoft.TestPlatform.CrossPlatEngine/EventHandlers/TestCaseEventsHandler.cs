
namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.EventHandlers
{

    using System;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
#if !NET46
    using System.Runtime.Loader;
#endif
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    /// <summary>
    /// The test case events handler.
    /// </summary>
    internal class TestCaseEventsHandler : ITestCaseEventsHandler
    {

     //   private InProcDataCollectionExtensionManager inProcDataCollectionExtensionManager;
        private ITestCaseEventsHandler testCaseEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseEventsHandler"/> class.
        /// </summary>
        /// <param name="inProcDCExtMgr">
        /// The in proc tidc helper.
        /// </param>
        public TestCaseEventsHandler(ITestCaseEventsHandler testCaseEvents)
        {
           // this.inProcDataCollectionExtensionManager = inProcDataCollectionExtensionManager;
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
           // this.inProcDataCollectionExtensionManager.TriggerTestCaseStart(testCase);
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
           // this.inProcDataCollectionExtensionManager.TriggerTestCaseEnd(testCase, outcome);
            this.testCaseEvents?.SendTestCaseEnd(testCase, outcome);
        }

        /// <summary>
        /// The send test result.
        /// </summary>
        /// <param name="result">
        /// The result.
        /// </param>
        public bool SendTestResult(TestResult result)
        {
          //  var flushResult = this.inProcDataCollectionExtensionManager.TriggerUpdateTestResult(result);
            this.testCaseEvents?.SendTestResult(result);
            return true;
        }
    }
}
