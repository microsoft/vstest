// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces
{
    using System;
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
        /// <param name="exitCallback">Call back for on process exit</param>
        /// <returns>The process created.</returns>
        Process LaunchProcess(string processPath, string arguments, string workingDirectory, Action<Process, string> errorCallback);

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
        /// Gets the pid of test engine.
        /// </summary>
        /// <returns>pid of test engine.</returns>
        int GetCurrentProcessId();
    }
}
