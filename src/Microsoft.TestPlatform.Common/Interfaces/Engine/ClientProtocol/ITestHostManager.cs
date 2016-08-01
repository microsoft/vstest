// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for HostManager which manages test host processes for test engine.
    /// </summary>
    public interface ITestHostManager
    {
        /// <summary>
        /// Sets a custom launcher
        /// </summary>
        /// <param name="customTestHostLauncher">Custom launcher to set</param>
        void SetCustomLauncher(ITestHostLauncher customTestHostLauncher);

        /// <summary>
        /// Launches the test host for discovery/execution.
        /// </summary>
        /// <param name="environmentVariables">Environment variables for the process.</param>
        /// <param name="commandLineArguments">The command line arguments to pass to the process.</param>
        /// <returns>ProcessId of launched Process. 0 means not launched.</returns>
        int LaunchTestHost(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments);

        /// <summary>
        /// Gives the ProcessStartInfo for the test host process
        /// </summary>
        /// <param name="environmentVariables"></param>
        /// <param name="commandLineArguments"></param>
        /// <returns>ProcessStartInfo of the test host</returns>
        TestProcessStartInfo GetTestHostProcessStartInfo(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments);
        
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
}
