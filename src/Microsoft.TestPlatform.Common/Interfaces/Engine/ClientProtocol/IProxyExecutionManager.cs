// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

    /// <summary>
    /// Orchestrates test execution related functionality for the engine communicating with the client.
    /// </summary>
    public interface IProxyExecutionManager
    {
        /// <summary>
        /// Initializes test execution. Create the test host, setup channel and initialize extensions.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Starts the test run.
        /// </summary>
        /// <param name="testRunCriteria">The settings/options for the test run.</param>
        /// <param name="eventHandler">EventHandler for handling execution events from Engine.</param>
        /// <returns>The process id of the runner executing tests.</returns>
        int StartTestRun(TestRunCriteria testRunCriteria, ITestRunEventsHandler eventHandler);

        /// <summary>
        /// Cancels the test run
        /// TODO: what's the difference between abort and cancel
        /// </summary>
        void Cancel();

        /// <summary>
        /// Aborts the test operation.
        /// </summary>
        void Abort();

        /// <summary>
        /// Dispose the test operation.
        /// </summary>
        void Dispose();
    }
}