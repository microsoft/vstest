// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the interface that can manage a process 
    /// </summary>
    internal interface IProcessManager
    {
        /// <summary>
        /// Starts the Process 
        /// </summary>
        void StartProcess(ConsoleParameters consoleParameters);

        /// <summary>
        /// Is Process Initialized
        /// </summary>
        /// <returns>True, if process initialized</returns>
        bool IsProcessInitialized();

        /// <summary>
        /// Shutdown Process
        /// </summary>
        void ShutdownProcess();

        /// <summary>
        /// Raise event on process exit
        /// </summary>
        event EventHandler ProcessExited;
    }
}
