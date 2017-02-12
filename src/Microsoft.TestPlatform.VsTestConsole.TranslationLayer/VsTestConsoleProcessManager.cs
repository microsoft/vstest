// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Vstest.console.exe process manager
    /// </summary>
    internal class VsTestConsoleProcessManager : IProcessManager
    {
        #region Private Members

        /// <summary>
        /// Port number for communicating with Vstest CLI
        /// </summary>
        private const string PORT_ARGUMENT = "/port:{0}";

        /// <summary>
        /// Process Id of the Current Process which is launching Vstest CLI
        /// Helps Vstest CLI in auto-exit if current process dies without notifying it
        /// </summary>
        private const string PARENT_PROCESSID_ARGUMENT = "/parentprocessid:{0}";

        /// <summary>
        /// Diagnostics argument for Vstest CLI
        /// Enables Diagnostic logging for Vstest CLI and TestHost - Optional
        /// </summary>
        private const string DIAG_ARGUMENT = "/diag:{0}";

        private string vstestConsolePath;
        private object syncObject = new object();
        private bool vstestConsoleStarted = false;
        private bool vstestConsoleExited = false;
        private Process process;

        #endregion

        /// <inheritdoc/>
        public event EventHandler ProcessExited;

        #region Constructor

        /// <summary>
        /// Creates an instance of VsTestConsoleProcessManager class.
        /// </summary>
        /// <param name="vstestConsolePath">The fullpath to vstest.console.exe</param>
        public VsTestConsoleProcessManager(string vstestConsolePath)
        {
            this.vstestConsolePath = vstestConsolePath;
        }

        #endregion Constructor

        /// <summary>
        /// Checks if the process has been initialized.
        /// </summary>
        /// <returns>True if process is successfully initialized</returns>
        public bool IsProcessInitialized()
        {
            lock (syncObject)
            {
                return this.vstestConsoleStarted && !vstestConsoleExited &&
                       this.process != null;
            }
        }

        /// <summary>
        /// Call xUnit.console.exe with the parameters previously specified
        /// </summary>
        public void StartProcess(ConsoleParameters consoleParameters)
        {
            this.process = new Process();
            process.StartInfo.FileName = vstestConsolePath;
            process.StartInfo.Arguments = string.Join(" ", BuildArguments(consoleParameters));

            //process.StartInfo.WorkingDirectory = WorkingDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            //process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.RedirectStandardError = true;

            EqtTrace.Verbose("VsTestCommandLineWrapper: {0} {1}", process.StartInfo.FileName,
                process.StartInfo.Arguments);

            process.Start();
            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;

            lock (syncObject)
            {
                vstestConsoleExited = false;
                vstestConsoleStarted = true;
            }
        }

        /// <summary>
        /// Shutdown the vstest.console process
        /// </summary>
        public void ShutdownProcess()
        {
            // Ideally process should die by itself
            if (IsProcessInitialized())
            {
                this.process.Kill();
                this.process.Dispose();
                this.process = null;
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            lock (syncObject)
            {
                vstestConsoleExited = true;
                this.ProcessExited?.Invoke(sender, e);
            }
        }

        private string[] BuildArguments(ConsoleParameters parameters)
        {
            var args = new List<string>();

            // Start Vstest.console.exe with args: --parentProcessId|/parentprocessid:<ppid> --port|/port:<port>
            args.Add(string.Format(CultureInfo.InvariantCulture, PARENT_PROCESSID_ARGUMENT, parameters.ParentProcessId));
            args.Add(string.Format(CultureInfo.InvariantCulture, PORT_ARGUMENT, parameters.PortNumber));

            if(!string.IsNullOrEmpty(parameters.LogFilePath))
            {
                // Extra args: --diag|/diag:<PathToLogFile>
                args.Add(string.Format(CultureInfo.InvariantCulture, DIAG_ARGUMENT, parameters.LogFilePath));
            }

            return args.ToArray();
        }
    }
}