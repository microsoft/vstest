// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

    /// <summary>
    /// Interface for HostManager which manages test host processes for test engine.
    /// </summary>
    public interface ITestHostManager
    {
        /// <summary>
        /// Gets a value indicating whether the test host is specific to a test source. If yes, each test source
        /// is launched in a separate host process.
        /// </summary>
        bool Shared { get; }

        /// <summary>
        /// Sets a custom launcher
        /// </summary>
        /// <param name="customLauncher">Custom launcher to set</param>
        void SetCustomLauncher(ITestHostLauncher customLauncher);

        /// <summary>
        /// Launches the test host for discovery/execution.
        /// </summary>
        /// <param name="testHostStartInfo">Start parameters for the test host.</param>
        /// <returns>ProcessId of launched Process. 0 means not launched.</returns>
        int LaunchTestHost(TestProcessStartInfo testHostStartInfo);

        /// <summary>
        /// Gets the start parameters for the test host.
        /// </summary>
        /// <param name="sources">Test source paths.</param>
        /// <param name="environmentVariables">Set of environment variables for the test host process.</param>
        /// <param name="connectionInfo">Set of connection parameters for the test host process to communicate with test runner.</param>
        /// <returns>ProcessStartInfo of the test host.</returns>
        TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo);
        
        /// <summary>
        /// Register for the exit event.
        /// </summary>
        /// <param name="abortCallback"> The callback on exit. </param>
        void RegisterForExitNotification(Action abortCallback);

        /// <summary>
        /// Deregister for the exit event.
        /// </summary>
        void DeregisterForExitNotification();
    }

    /// <summary>
    /// Connection information for a test host to communicate with test runner.
    /// </summary>
    public struct TestRunnerConnectionInfo
    {
        /// <summary>
        /// Gets or sets the port opened by test runner for host communication.
        /// </summary>
        public int Port
        {
            get;
            set;
        }
    }
}
