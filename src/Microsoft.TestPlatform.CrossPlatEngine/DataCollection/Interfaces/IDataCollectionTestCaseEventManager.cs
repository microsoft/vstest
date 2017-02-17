// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Manager for sending test case events to data collectors.
    /// </summary>
    internal interface IDataCollectionTestCaseEventManager
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
        /// The raise test case start.
        /// </summary>
        /// <param name="e">
        /// Test case start event arguments.
        /// </param>
        void RaiseTestCaseStart(TestCaseStartEventArgs e);

        /// <summary>
        /// The raise test case end.
        /// </summary>
        /// <param name="e">
        /// Test case end event arguments.
        /// </param>
        void RaiseTestCaseEnd(TestCaseEndEventArgs e);

        /// <summary>
        /// The raise session start.
        /// </summary>
        /// <param name="e">
        /// Session start event arguments.
        /// </param>
        void RaiseSessionStart(SessionStartEventArgs e);

        /// <summary>
        /// The raise session end.
        /// </summary>
        /// <param name="e">
        /// Session end event arguments.
        /// </param>
        void RaiseSessionEnd(SessionEndEventArgs e);

        /// <summary>
        /// The raise test result.
        /// </summary>
        /// <param name="e">
        /// Test results event arguments.
        /// </param>
        void RaiseTestResult(TestResultEventArgs e);


    }
}
