// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface defining contract for custom test host implementations
    /// </summary>
    public interface ITestHostLauncher
    {
        /// <summary>
        /// Is Debug Launcher
        /// </summary>
        bool IsDebug { get; }

        /// <summary>
        /// Launches custom test host using the default test process start info
        /// </summary>
        /// <param name="architecture">Architecture for the test host</param>
        /// <param name="defaultTestHostStartInfo">Default TestHost Process Info</param>
        /// <returns>Process id of the launched test host</returns>
        int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo);
    }
}
