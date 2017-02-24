// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.EventHandlers
{
#if !NET46
    using System.Runtime.Loader;
#endif
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    /// <summary>
    /// The test case events handler.
    /// </summary>
    internal class TestCaseEventsHandler : ITestCaseEventsHandler
    {
        private IDataCollectionTestCaseEventManager dataCollectionTestCaseEventManager;
        private ITestCaseEventsHandler testCaseEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseEventsHandler"/> class.
        /// </summary>
        /// <param name="dataCollectionTestCaseEventManager">
        /// The data Collection Test Case Event Manager.
        /// </param>
        /// <param name="testCaseEvents">
        /// The test Case Events.
        /// </param>
        public TestCaseEventsHandler(IDataCollectionTestCaseEventManager dataCollectionTestCaseEventManager, ITestCaseEventsHandler testCaseEvents)
        {
            this.dataCollectionTestCaseEventManager = dataCollectionTestCaseEventManager;
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
            this.dataCollectionTestCaseEventManager.RaiseTestCaseStart(new TestCaseStartEventArgs(testCase));
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
            this.dataCollectionTestCaseEventManager.RaiseTestCaseEnd(new TestCaseEndEventArgs(testCase, outcome));
            this.testCaseEvents?.SendTestCaseEnd(testCase, outcome);
        }

        /// <summary>
        /// The send test result.
        /// </summary>
        /// <param name="result">
        /// The result.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool SendTestResult(TestResult result)
        {
            this.dataCollectionTestCaseEventManager.RaiseTestResult(new TestResultEventArgs(result));

            var flushResult = result.GetPropertyValue<bool?>(InProcDataCollectionExtensionManager.FlushResultTestResultPoperty, null);

            this.testCaseEvents?.SendTestResult(result);

            return flushResult == null ? true : flushResult.Value;
        }
    }
}
