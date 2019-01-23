// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Vstest.console process manager
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
        private const string DIAG_ARGUMENT = "/diag:{0};tracelevel={1}";

        private string vstestConsolePath;
        private object syncObject = new object();
        private bool vstestConsoleStarted = false;
        private bool vstestConsoleExited = false;
        private readonly bool isNetCoreRunner;
        private Process process;

        #endregion

        /// <inheritdoc/>
        public event EventHandler ProcessExited;

        #region Constructor

        /// <summary>
        /// Creates an instance of VsTestConsoleProcessManager class.
        /// </summary>
        /// <param name="vstestConsolePath">The fullpath to vstest.console</param>
        public VsTestConsoleProcessManager(string vstestConsolePath)
        {
            this.vstestConsolePath = vstestConsolePath;
            isNetCoreRunner = vstestConsolePath.EndsWith(".dll");
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
        /// Call vstest.console with the parameters previously specified
        /// </summary>
        public void StartProcess(ConsoleParameters consoleParameters)
        {
            var info = new ProcessStartInfo(GetConsoleRunner(), string.Join(" ", BuildArguments(consoleParameters)))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            EqtTrace.Verbose("VsTestCommandLineWrapper: {0} {1}", info.FileName, info.Arguments);

            process = Process.Start(info);

            lock (syncObject)
            {
                vstestConsoleExited = false;
                vstestConsoleStarted = true;
            }

            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;

            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        /// <summary>
        /// Shutdown the vstest.console process
        /// </summary>
        public void ShutdownProcess()
        {
            // Ideally process should die by itself
            if (IsProcessInitialized())
            {
                vstestConsoleExited = true;
                process.OutputDataReceived -= Process_OutputDataReceived;
                process.ErrorDataReceived -= Process_ErrorDataReceived;
                process.Dispose();
                this.process = null;
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            lock (syncObject)
            {
                ShutdownProcess();

                this.ProcessExited?.Invoke(sender, e);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                EqtTrace.Error(e.Data);
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                EqtTrace.Verbose(e.Data);
            }
        }

        private string[] BuildArguments(ConsoleParameters parameters)
        {
            var args = new List<string>
            {
                // Start Vstest.console with args: --parentProcessId|/parentprocessid:<ppid> --port|/port:<port>
                string.Format(CultureInfo.InvariantCulture, PARENT_PROCESSID_ARGUMENT, parameters.ParentProcessId),
                string.Format(CultureInfo.InvariantCulture, PORT_ARGUMENT, parameters.PortNumber)
            };

            if (!string.IsNullOrEmpty(parameters.LogFilePath))
            {
                // Extra args: --diag|/diag:<PathToLogFile>;tracelevel=<tracelevel>
                args.Add(string.Format(CultureInfo.InvariantCulture, DIAG_ARGUMENT, parameters.LogFilePath, parameters.TraceLevel));
            }

            if (isNetCoreRunner)
            {
                args.Insert(0, vstestConsolePath);
            }

            return args.ToArray();
        }

        private string GetConsoleRunner()
        {
            return isNetCoreRunner ? new DotnetHostHelper().GetDotnetPath() : vstestConsolePath;
        }
    }
}