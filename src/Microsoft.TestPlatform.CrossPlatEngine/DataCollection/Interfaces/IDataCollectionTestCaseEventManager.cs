// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    internal interface IDataCollectionTestCaseEventManager
    {
        event EventHandler<SessionStartEventArgs> SessionStart;
        event EventHandler<SessionEndEventArgs> SessionEnd;
        event EventHandler<TestCaseStartEventArgs> TestCaseStart;
        event EventHandler<TestCaseEndEventArgs> TestCaseEnd;
        event EventHandler<TestResultEventArgs> TestResult;

        void RaiseTestCaseStart(TestCaseStartEventArgs e);
        void RaiseTestCaseEnd(TestCaseEndEventArgs e);
        void RaiseSessionStart(SessionStartEventArgs e);
        void RaiseSessionEnd(SessionEndEventArgs e);
        void RaiseTestResult(TestResultEventArgs e);


    }
}
