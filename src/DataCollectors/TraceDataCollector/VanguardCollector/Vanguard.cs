// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TraceCollector;
    using TestPlatform.ObjectModel;
    using TraceCollector.Interfaces;
    using TraceDataCollector.Resources;

    /// <summary>
    /// A managed wrapper for Vanguard
    /// </summary>
    internal class Vanguard : IVanguard
    {
        /// <summary>
        /// Return value of WaitForSingleObject, which means the object is signaled.
        /// </summary>
        private const uint WaitObject0 = 0x00000000;

        /// <summary>
        /// Time limit for vanguard process exit event
        /// </summary>
        private const int ProcessExitWaitLimit = 60000;

        /// <summary>
        /// Prefix for creating event in global namespace
        /// </summary>
        private const string GlobalEventNamePrefix = "Global\\";

        /// <summary>
        /// Stop() will use this event to check whether the stop vanguard command
        /// successful stop the collect command codecoverage.exe.
        /// </summary>
        private ManualResetEvent vanguardProcessExitEvent;

        /// <summary>
        /// To monitor vanguard collect command CodeCoverage.exe process.
        /// </summary>
        private Process vanguardProcess;

        /// <summary>
        /// Session name of the vanguard logger.
        /// </summary>
        private string sessionName;

        /// <summary>
        /// Vanguard configuration file name.
        /// </summary>
        private string configurationFileName;

        /// <summary>
        /// Vanguard output file name.
        /// </summary>
        private string outputName;

        /// <summary>
        /// Helper object to manage child process lifetimes
        /// </summary>
        private IProcessJobObject processJobObject;

        /// <summary>
        /// Data collection logger
        /// </summary>
        private IDataCollectionLogger logger;

        /// <summary>
        /// Data collection context
        /// </summary>
        private DataCollectionContext context;

        private IVanguardLocationProvider vanguardLocationProvider;

        private IVanguardCommandBuilder vanguardCommandBuilder;

        public Vanguard()
            : this(new VanguardLocationProvider(), new VanguardCommandBuilder(), new ProcessJobObject())
        {
        }

        internal Vanguard(
            IVanguardLocationProvider vanguardLocationProvider,
            IVanguardCommandBuilder commandBuilder,
            IProcessJobObject processJobObject)
        {
            this.vanguardLocationProvider = vanguardLocationProvider;
            this.vanguardCommandBuilder = commandBuilder;
            this.processJobObject = processJobObject;
        }

        /// <summary>
        /// Gets a value indicating whether vanguard is running
        /// </summary>
        private bool IsRunning
        {
            get { return this.vanguardProcess != null && !this.vanguardProcess.HasExited; }
        }

        /// <inheritdoc />
        public void Initialize(
            string sessionName,
            string configurationFileName,
            IDataCollectionLogger logger)
        {
            EqtTrace.Info("Vanguard.Initialize: Session name: {0}, config filename: {1}", sessionName, configurationFileName);

            this.sessionName = sessionName;
            this.configurationFileName = configurationFileName;
            this.logger = logger;
        }

        /// <inheritdoc />
        public virtual void Start(string outputName, DataCollectionContext context)
        {
            EqtTrace.Info("Vanguard.Start: Starting CodeCoverage.exe for output file: {0} datacollection session id: {1}", outputName, context.SessionId);

            this.vanguardProcessExitEvent = new ManualResetEvent(false);
            this.outputName = outputName;
            this.context = context;
            var collectCommand = this.vanguardCommandBuilder.GenerateCommandLine(
                VanguardCommand.Collect,
                this.sessionName,
                this.outputName,
                this.configurationFileName);

            this.vanguardProcess = this.StartVanguardProcess(collectCommand, false, true);
            this.WaitForRunningEvent();
        }

        /// <inheritdoc />
        public virtual void Stop()
        {
            EqtTrace.Info("Vanguard.Stop: Stopping Vanguard.");
            if (this.IsRunning)
            {
                var shutdownCommand = this.vanguardCommandBuilder.GenerateCommandLine(
                    VanguardCommand.Shutdown,
                    this.sessionName,
                    null,
                    null);
                this.StartVanguardProcess(shutdownCommand, true);

                if (this.vanguardProcessExitEvent.WaitOne(ProcessExitWaitLimit) == false)
                {
                    EqtTrace.Warning("Vanguard.Stop: Vanguard process not exited in {0} ms", ProcessExitWaitLimit);
                }

                if (this.processJobObject != null)
                {
                    this.processJobObject.Dispose();
                    this.processJobObject = null;
                }
            }
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            EqtTrace.Info("Vanguard.Dispose: Disposing vanguard process.");
            if (this.vanguardProcess != null)
            {
                this.vanguardProcess.Dispose();
                this.vanguardProcess = null;
            }
        }

        /// <summary>
        /// CreateEvent API
        /// </summary>
        /// <param name="lpEventAttributes">A point to SECURITY_ATTRIBUTES structure</param>
        /// <param name="bManualReset">Whether the event needs manual reset</param>
        /// <param name="bInitialState">Initial state of the event</param>
        /// <param name="lpName">Name of the event</param>
        /// <returns>Event handle</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateEvent(
            IntPtr lpEventAttributes,
            bool bManualReset,
            bool bInitialState,
            string lpName);

        /// <summary>
        /// Waits until one or all of the specified objects are in the signaled state or the time-out interval elapses.
        /// </summary>
        /// <param name="nCount">Number of handles</param>
        /// <param name="lpHandles">Object handles</param>
        /// <param name="bWaitAll">Whether to wait all handles</param>
        /// <param name="dwMilliseconds">Time-out interval</param>
        /// <returns>Wait result</returns>
        [DllImport("kernel32.dll")]
        private static extern uint WaitForMultipleObjects(
            uint nCount,
            IntPtr[] lpHandles,
            bool bWaitAll,
            uint dwMilliseconds);

        /// <summary>
        /// Close a handle
        /// </summary>
        /// <param name="hObject">Object handle</param>
        /// <returns>True if succeeded</returns>
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static int GetProcessId(Process process)
        {
            var id = 0;
            try
            {
                id = process.Id;
            }
            catch (Exception ex)
            {
                EqtTrace.Info("Vanguard.GetProcessId: Fail to get process id with exception: {0}", ex);
            }

            return id;
        }

        /// <summary>
        /// Start a vanguard process
        /// </summary>
        /// <param name="commandLine">VanguardCommand line options</param>
        /// <param name="wait">Whether to wait until the process exits</param>
        /// <param name="standardErrorAsynchronousCall">The standardErrorAsynchronousCall. </param>
        /// <returns>Process instance of vanguard</returns>
        private Process StartVanguardProcess(
            string commandLine,
            bool wait,
            bool standardErrorAsynchronousCall = false)
        {
            string vanguardPath = this.vanguardLocationProvider.GetVanguardPath();
            EqtTrace.Info(
                "Vanguard.StartVanguardProcess: Starting {0} with command line: {1}, wait for exit:{2}, Read stderr: {3}",
                vanguardPath,
                commandLine,
                wait,
                standardErrorAsynchronousCall);
            ProcessStartInfo info = new ProcessStartInfo(vanguardPath, commandLine);
            info.WorkingDirectory = Directory.GetCurrentDirectory();
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardError = true;

            Process process = new Process();
            process.StartInfo = info;
            process.EnableRaisingEvents = true;

            if (standardErrorAsynchronousCall)
            {
                process.ErrorDataReceived += this.LoggerProcessErrorDataReceived;
                process.Exited += this.LoggerProcessExited;
            }

            process.Start();

            if (standardErrorAsynchronousCall)
            {
                process.BeginErrorReadLine();
            }

            if (wait)
            {
                process.WaitForExit();
            }
            else if (this.processJobObject != null)
            {
                EqtTrace.Info("Vanguard.StartVanguardProcess: Add Vanguard process to the project object");
                this.processJobObject.AddProcess(process.SafeHandle.DangerousGetHandle());
            }

            EqtTrace.Info(
                "Vanguard.StartVanguardProcess: Started Vanguard process id :{0}",
                Vanguard.GetProcessId(process));

            return process;
        }

        /// <summary>
        /// WaitForRunningEvent until vanguard initialization is finished
        /// </summary>
        private void WaitForRunningEvent()
        {
            EqtTrace.Info("Vanguard.WaitForRunningEvent: Waiting for CodeCoverage.exe initialization.");
            IntPtr runningEvent = CreateEvent(
                IntPtr.Zero,
                true,
                false,
                GlobalEventNamePrefix + this.sessionName + "_RUNNING");
            var timeout = EnvironmentHelper.GetConnectionTimeout();
            if (runningEvent != IntPtr.Zero)
            {
                uint waitTimeout = (uint)timeout * 1000; // Time limit for waiting for vanguard running event.

                IntPtr[] handles = new IntPtr[] { runningEvent, this.vanguardProcess.SafeHandle.DangerousGetHandle() };
                uint result = WaitForMultipleObjects((uint)handles.Length, handles, false, waitTimeout);
                CloseHandle(runningEvent);
                switch (result)
                {
                    case WaitObject0:
                        EqtTrace.Info("Vanguard.WaitForRunningEvent: Running event received from CodeCoverage.exe.");
                        return;
                    case WaitObject0 + 1:
                        // Process exited, something wrong happened
                        // we have already set to read messages asynchronously, so calling this.vanguardProcess.StandardError.ReadToEnd() which is synchronous is wrong.
                        // throw new VanguardException(string.Format(CultureInfo.CurrentCulture, Resources.ErrorLaunchVanguard, this.vanguardProcess.StandardError.ReadToEnd()));
                        EqtTrace.Error("Vanguard.WaitForRunningEvent: From CodeCoverage.exe failed to receive running event in {0} seconds", timeout);
                        throw new VanguardException(string.Format(CultureInfo.CurrentUICulture, Resources.NoRunningEventFromVanguard));
                }
            }

            // No running event received from code coverage.exe and not exited. Kill the CodeCoverage.exe.
            try
            {
                EqtTrace.Error("Vanguard.WaitForRunningEvent: Fail to create running event. Killing CodeCoverage.exe. ");
                this.vanguardProcess.Kill();
            }
            catch (Exception ex)
            {
                EqtTrace.Warning("Vanguard.WaitForRunningEvent: Fail to kill CodeCoverage.exe. process with exception: {0}", ex);
            }

            throw new VanguardException(string.Format(CultureInfo.CurrentUICulture, Resources.VanguardConnectionTimeout, timeout, EnvironmentHelper.VstestConnectionTimeout));
        }

        /// <summary>
        /// Handler for vanguard process exit event
        /// </summary>
        /// <param name="sender">The sender. </param>
        /// <param name="e">Event args. </param>
        private void LoggerProcessExited(object sender, EventArgs e)
        {
            EqtTrace.Info("Vanguard.LoggerProcessExited: Vanguard process exit callback started.");
            if (this.vanguardProcess != null)
            {
                if (this.vanguardProcess.HasExited == true && this.vanguardProcess.ExitCode != 0)
                {
                    EqtTrace.Warning("Vanguard.LoggerProcessExited: An error occurred in Code Coverage process. Error code = {0}", this.vanguardProcess.ExitCode);
                }

                this.vanguardProcess.Exited -= this.LoggerProcessExited;
                this.vanguardProcess.ErrorDataReceived -= this.LoggerProcessErrorDataReceived;

                this.vanguardProcessExitEvent.Set();
            }
        }

        /// <summary>
        ///  Handler for vanguard process error stream
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">Event args</param>
        private void LoggerProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            EqtTrace.Warning("Vanguard.LoggerProcessErrorDataReceived: received error data: {0}", e.Data);

            if (this.logger != null && this.context != null && !string.IsNullOrWhiteSpace(e.Data))
            {
                this.logger.LogWarning(this.context, e.Data);
            }
        }
    }
}