// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common.Execution;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// The test level events handler.
    /// </summary>
    public interface ITestEventsHandler
    {
        /// <summary>
        /// The session start event.
        /// </summary>
        event EventHandler<SessionStartEventArgs> SessionStart;

        /// <summary>
        /// The session end event.
        /// </summary>
        event EventHandler<SessionEndEventArgs> SessionEnd;

        /// <summary>
        /// The test case start event.
        /// </summary>
        event EventHandler<TestCaseStartEventArgs> TestCaseStart;

        /// <summary>
        /// The test case end event.
        /// </summary>
        event EventHandler<TestCaseEndEventArgs> TestCaseEnd;

        /// <summary>
        /// The test result event.
        /// </summary>
        event EventHandler<TestResultEventArgs> TestResult;

        /// <summary>
        /// Initializes TestCaseEvents Handler.
        /// </summary>
        /// <param name="testRunCache">
        /// The test run cache.
        /// </param>
        void Initialize(ITestRunCache testRunCache);

        /// <summary>
        /// Sends session start.
        /// </summary>
        void SendTestSessionStart();

        /// <summary>
        /// Sends test session end.
        /// </summary>
        void SendTestSessionEnd();

        /// <summary>
        ///  Report start of executing a test case.
        /// </summary>
        /// <param name="testCase">Details of the test case whose execution is just started.</param>
        void SendTestCaseStart(TestCase testCase);

        /// <summary>
        /// Report end of executing a test case.
        /// </summary>
        /// <param name="testCase">Details of the test case.</param>
        /// <param name="outcome">Result of the test case executed.</param>
        void SendTestCaseEnd(TestCase testCase, TestOutcome outcome);

        /// <summary>
        /// Sends the test result
        /// </summary>
        /// <param name="result"> The result. </param>
        /// <returns>True, if result can be flushed</returns>
        bool SendTestResult(TestResult result);

        /// <summary>
        /// Flush any test results that are cached in dictionary
        /// </summary>
        void FlushLastChunkResults();
    }
}
