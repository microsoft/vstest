// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Host
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Interface for TestRuntimeProvider which manages test host processes for test engine.
    /// </summary>
    public interface ITestRuntimeProvider
    {
        #region events
        /// <summary>
        /// Raised when host is launched successfully
        /// </summary>
        event EventHandler<HostProviderEventArgs> HostLaunched;

        /// <summary>
        /// Raised when host is reports Error
        /// </summary>
        event EventHandler<HostProviderEventArgs> HostExited;

        #endregion

        /// <summary>
        /// Gets a value indicating whether the test host is specific to a test source. If yes, each test source
        /// is launched in a separate host process.
        /// </summary>
        bool Shared { get; }

        /// <summary>
        /// Sets a Message Logger
        /// </summary>
        /// <param name="logger">provide logger to runtimes</param>
        void Initialize(IMessageLogger logger);

        /// <summary>
        /// Gets a value indicating whether the test host is specific to a test source. If yes, each test source
        /// is launched in a separate host process.
        /// </summary>
        /// <param name="runConfiguration">
        /// The run Configuration.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        bool CanExecuteCurrentRunConfiguration(string runConfiguration);

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
        Task<int> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo);

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

    public class HostProviderEventArgs : EventArgs
    {
        public HostProviderEventArgs(string message)
        {
            this.Data = message;
            this.ErrroCode = 0;
        }

        public HostProviderEventArgs(string message, int errorCode)
        {
            this.Data = message;
            this.ErrroCode = errorCode;
        }

        public string Data { get; set; }

        public int ErrroCode { get; set; }
    }
}
