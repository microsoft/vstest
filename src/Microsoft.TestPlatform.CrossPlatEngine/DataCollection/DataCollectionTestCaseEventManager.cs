// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The data collection test case event manager for sending events to in-proc and out-of-proc data collectors.
    /// </summary>
    internal class DataCollectionTestCaseEventManager : IDataCollectionTestCaseEventManager
    {
        /// <inheritdoc />
        public event EventHandler<SessionStartEventArgs> SessionStart;

        /// <inheritdoc />
        public event EventHandler<SessionEndEventArgs> SessionEnd;

        /// <inheritdoc />
        public event EventHandler<TestCaseStartEventArgs> TestCaseStart;

        /// <inheritdoc />
        public event EventHandler<TestCaseEndEventArgs> TestCaseEnd;

        /// <inheritdoc />
        public event EventHandler<TestResultEventArgs> TestResult;

        /// <inheritdoc />
        public void RaiseSessionEnd(SessionEndEventArgs e)
        {
            this.SessionEnd.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.RaiseSessionEnd");
        }

        /// <inheritdoc />
        public void RaiseSessionStart(SessionStartEventArgs e)
        {
            this.SessionStart.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.RaiseSessionStart");
        }

        /// <inheritdoc />
        public void RaiseTestCaseEnd(TestCaseEndEventArgs e)
        {
            this.TestCaseEnd.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.RaiseTestCaseEnd");
        }

        /// <inheritdoc />
        public void RaiseTestCaseStart(TestCaseStartEventArgs e)
        {
            this.TestCaseStart.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.RaiseTestCaseStart");
        }

        /// <inheritdoc />
        public void RaiseTestResult(TestResultEventArgs e)
        {
            this.TestResult.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.TestResult");
        }
    }
}
