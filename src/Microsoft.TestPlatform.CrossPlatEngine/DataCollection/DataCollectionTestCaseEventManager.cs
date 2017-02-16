// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    internal class DataCollectionTestCaseEventManager : IDataCollectionTestCaseEventManager
    {
        public event EventHandler<SessionStartEventArgs> SessionStart;
        public event EventHandler<SessionEndEventArgs> SessionEnd;
        public event EventHandler<TestCaseStartEventArgs> TestCaseStart;
        public event EventHandler<TestCaseEndEventArgs> TestCaseEnd;
        public event EventHandler<TestResultEventArgs> TestResult;

        /// <inheritdoc />
        public void RaiseSessionEnd()
        {
           this.SessionEnd.SafeInvoke(this, null, "DataCollectionTestCaseEventManager.RaiseSessionEnd");
        }

        /// <inheritdoc />
        public void RaiseSessionStart()
        {
           this.SessionStart.SafeInvoke(this, null, "DataCollectionTestCaseEventManager.RaiseSessionStart");
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

        public void RaiseTestResult(TestResultEventArgs e)
        {
            this.TestResult.SafeInvoke(this, e, "DataCollectionTestCaseEventManager.TestResult");
        }
    }
}
