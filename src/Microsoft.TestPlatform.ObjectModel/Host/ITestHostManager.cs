// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Host
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    /// <summary>
    /// Interface for HostManager which manages test host processes for test engine.
    /// </summary>
    public interface ITestHostProvider
    {
        /// <summary>
        /// Gets a value indicating whether the test host is specific to a test source. If yes, each test source
        /// is launched in a separate host process.
        /// </summary>
        bool Shared { get; }

        /// <summary>
        /// Gets a value indicating whether the test host is specific to a test source. If yes, each test source
        /// is launched in a separate host process.
        /// </summary>
        bool CanExecuteCurrentRunConfiguration(RunConfiguration runConfiguration);

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
        /// Gets paths of any additional extensions.
        /// A test host manager may choose to provide a custom extensions acquisition and discovery
        /// mechanism. E.g. for .net core, extensions are discovered from the <c>testproject.deps.json</c> file.
        /// </summary>
        /// <param name="sources">List of test sources.</param>
        /// <returns>List of paths to extension assemblies.</returns>
        IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources);

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

        /// <summary>
        /// Gets or sets the diagnostics log file.
        /// </summary>
        public string LogFile
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the runner process id.
        /// </summary>
        public int RunnerProcessId
        {
            get;
            set;
        }
    }
}
