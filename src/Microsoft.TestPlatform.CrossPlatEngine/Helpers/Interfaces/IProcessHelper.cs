// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Interface for any process related functionality. This is needed for clean unit-testing.
    /// </summary>
    internal interface IProcessHelper
    {
        /// <summary>
        /// Launches the process with the given arguments.
        /// </summary>
        /// <param name="processPath">The full file name of the process.</param>
        /// <param name="arguments">The command-line arguments.</param>
        /// <param name="workingDirectory">The working directory for this process.</param>
        /// <param name="environmentVariables">Environment variables to set while bootstrapping the process.</param>
        /// <param name="errorCallback">Call back for to read error stream data</param>
        /// <param name="exitCallBack">Call back for on process exit</param>
        /// <returns>The process created.</returns>
        Process LaunchProcess(string processPath, string arguments, string workingDirectory, IDictionary<string, string> environmentVariables, Action<Process, string> errorCallback, Action<Process> exitCallBack);

        /// <summary>
        /// Gets the current process file path.
        /// </summary>
        /// <returns>The current process file path.</returns>
        string GetCurrentProcessFileName();

        /// <summary>
        /// Gets the location of test engine.
        /// </summary>
        /// <returns>Location of test engine.</returns>
        string GetTestEngineDirectory();

        /// <summary>
        /// Gets the process id of test engine.
        /// </summary>
        /// <returns>process id of test engine.</returns>
        int GetCurrentProcessId();

        /// <summary>
        /// Gets the process name for given process id.
        /// </summary>
        /// <param name="processId">process id</param>
        /// <returns>Name of process</returns>
        string GetProcessName(int processId);

        /// <summary>
        /// False if process has not exited, True otherwise. Set exitCode only if process has exited.
        /// </summary>
        /// <param name="process">process parameter</param>
        /// <param name="exitCode">return value of exitCode</param>
        /// <returns>False if process has not exited, True otherwise</returns>
        bool TryGetExitCode(Process process, out int exitCode);
    }
}
