// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;

    /// <summary>
    /// Orchestrates test execution related functionality for the engine communicating with the test host process.
    /// </summary>
    public interface IExecutionManager
    {
        /// <summary>
        /// Initializes the execution manager.
        /// </summary>
        /// <param name="pathToAdditionalExtensions"> The path to additional extensions. </param>
        void Initialize(IEnumerable<string> pathToAdditionalExtensions);

        /// <summary>
        /// Starts the test run with sources.
        /// </summary>
        /// <param name="adapterSourceMap"> The adapter Source Map.  </param>
        /// <param name="runSettings"> The run Settings.  </param>
        /// <param name="testExecutionContext"> The test Execution Context. </param>
        /// <param name="testCaseEvents"> EventHandler for handling test cases level events from Engine. </param>
        /// <param name="eventHandler"> EventHandler for handling execution events from Engine. </param>
        void StartTestRun(Dictionary<string, IEnumerable<string>> adapterSourceMap, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEvents, ITestRunEventsHandler eventHandler);

        /// <summary>
        /// Starts the test run with tests.
        /// </summary>
        /// <param name="tests"> The test list. </param>
        /// <param name="runSettings"> The run Settings.  </param>
        /// <param name="testExecutionContext"> The test Execution Context. </param>
        /// /// <param name="testCaseEvents"> EventHandler for handling test cases level events from Engine. </param>
        /// <param name="eventHandler"> EventHandler for handling execution events from Engine. </param>
        void StartTestRun(IEnumerable<TestCase> tests, string runSettings, TestExecutionContext testExecutionContext, ITestCaseEventsHandler testCaseEvents, ITestRunEventsHandler eventHandler);

        /// <summary>
        /// Cancel the test execution.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Aborts the test execution.
        /// </summary>
        void Abort();
    }
}
