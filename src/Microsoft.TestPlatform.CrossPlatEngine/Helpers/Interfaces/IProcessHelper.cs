// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces
{
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
        /// <returns>The process created.</returns>
        Process LaunchProcess(string processPath, string arguments, string workingDirectory);

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
