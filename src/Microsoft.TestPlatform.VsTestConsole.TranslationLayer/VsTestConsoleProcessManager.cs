// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using Resources = Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Resources.Resources;

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

        /// <summary>
        /// EndSession timeout
        /// </summary>
        private const int ENDSESSIONTIMEOUT = 1000;

        private string vstestConsolePath;
        private object syncObject = new object();
        private bool vstestConsoleStarted = false;
        private bool vstestConsoleExited = false;
        private readonly bool isNetCoreRunner;
        private string dotnetExePath;
        private Process process;
        private ManualResetEvent processExitedEvent = new ManualResetEvent(false);

        internal IFileHelper FileHelper { get; set; }

        #endregion

        /// <inheritdoc/>
        public event EventHandler ProcessExited;

        #region Constructor

        /// <summary>
        /// Creates an instance of VsTestConsoleProcessManager class.
        /// </summary>
        /// <param name="vstestConsolePath">The full path to vstest.console</param>
        public VsTestConsoleProcessManager(string vstestConsolePath)
        {
            this.FileHelper = new FileHelper();
            if (!this.FileHelper.Exists(vstestConsolePath))
            {
                EqtTrace.Error("Invalid File Path: {0}", vstestConsolePath);
                throw new Exception(string.Format(CultureInfo.CurrentCulture, Resources.InvalidFilePath, vstestConsolePath));
            }
            this.vstestConsolePath = vstestConsolePath;
            isNetCoreRunner = vstestConsolePath.EndsWith(".dll");
        }

        public VsTestConsoleProcessManager(string vstestConsolePath, string dotnetExePath) : this(vstestConsolePath)
        {
            this.dotnetExePath = dotnetExePath;
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

            EqtTrace.Verbose("VsTestCommandLineWrapper: Process Start Info {0} {1}", info.FileName, info.Arguments);

#if NETFRAMEWORK
            if (consoleParameters.EnvironmentVariables != null)
            {
                info.EnvironmentVariables.Clear();
                foreach (var envVariable in consoleParameters.EnvironmentVariables)
                {
                    if (envVariable.Key != null)
                    {
                        info.EnvironmentVariables.Add(envVariable.Key.ToString(), envVariable.Value?.ToString());
                    }
                }
            }
#endif
            this.process = Process.Start(info);

            lock (syncObject)
            {
                vstestConsoleExited = false;
                vstestConsoleStarted = true;
            }

            this.process.EnableRaisingEvents = true;
            this.process.Exited += Process_Exited;

            this.process.OutputDataReceived += Process_OutputDataReceived;
            this.process.ErrorDataReceived += Process_ErrorDataReceived;
            this.process.BeginOutputReadLine();
            this.process.BeginErrorReadLine();
            processExitedEvent.Reset();
        }

        /// <summary>
        /// Shutdown the vstest.console process
        /// </summary>
        public void ShutdownProcess()
        {
            // Ideally process should die by itself
            if(!processExitedEvent.WaitOne(ENDSESSIONTIMEOUT) && IsProcessInitialized())
            {
                EqtTrace.Info($"VsTestConsoleProcessManager.ShutDownProcess : Terminating vstest.console process after waiting for {ENDSESSIONTIMEOUT} milliseconds.");
                vstestConsoleExited = true;
                this.process.OutputDataReceived -= Process_OutputDataReceived;
                this.process.ErrorDataReceived -= Process_ErrorDataReceived;
                SafelyTerminateProcess();
                this.process.Dispose();
                this.process = null;
            }
        }

        private void SafelyTerminateProcess()
        {
            try
            {
                if (this.process != null && !this.process.HasExited)
                {
                    this.process.Kill();
                }
            }
            catch (InvalidOperationException ex)
            {
                EqtTrace.Info("VsTestCommandLineWrapper: Error While Terminating Process {0} ", ex.Message);
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            lock (syncObject)
            {
                processExitedEvent.Set();
                vstestConsoleExited = true;
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
            return isNetCoreRunner ? ( string.IsNullOrEmpty(this.dotnetExePath) ? new DotnetHostHelper().GetDotnetPath() : this.dotnetExePath) : vstestConsolePath;
        }
    }
}