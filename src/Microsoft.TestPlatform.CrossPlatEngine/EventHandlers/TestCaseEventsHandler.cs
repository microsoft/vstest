// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.EventHandlers
{
    using System;
#if !NET451
    using System.Runtime.Loader;
#endif
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The test case events handler.
    /// </summary>
    internal class TestCaseEventsHandler : ITestCaseEventsHandler, ITestEventsPublisher
    {
        public event EventHandler<SessionStartEventArgs> SessionStart;
        public event EventHandler<SessionEndEventArgs> SessionEnd;
        public event EventHandler<TestCaseStartEventArgs> TestCaseStart;
        public event EventHandler<TestCaseEndEventArgs> TestCaseEnd;
        public event EventHandler<TestResultEventArgs> TestResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseEventsHandler"/> class.
        /// </summary>
        public TestCaseEventsHandler()
        {
        }

        /// <inheritdoc />
        public void SendTestCaseStart(TestCase testCase)
        {
            this.TestCaseStart.SafeInvoke(this, new TestCaseStartEventArgs(testCase), "TestCaseEventsHandler.RaiseTestCaseStart");
        }

        /// <inheritdoc />
        public void SendTestCaseEnd(TestCase testCase, TestOutcome outcome)
        {
            this.TestCaseEnd.SafeInvoke(this, new TestCaseEndEventArgs(testCase, outcome), "TestCaseEventsHandler.RaiseTestCaseEnd");
        }

        /// <inheritdoc />
        public void SendTestResult(TestResult result)
        {
            this.TestResult.SafeInvoke(this, new TestResultEventArgs(result), "TestCaseEventsHandler.RaiseTestCaseEnd");
        }

        /// <inheritdoc />
        public void SendSessionStart()
        {
            this.SessionStart.SafeInvoke(this, new SessionStartEventArgs(), "TestCaseEventsHandler.RaiseSessionStart");
        }

        /// <inheritdoc />
        public void SendSessionEnd()
        {
            this.SessionEnd.SafeInvoke(this, new SessionEndEventArgs(), "TestCaseEventsHandler.RaiseSessionEnd");
        }
    }
}
