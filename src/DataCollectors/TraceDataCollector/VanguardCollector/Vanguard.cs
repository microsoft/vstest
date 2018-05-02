// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using Interfaces;
    using Microsoft.VisualStudio.Collector;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TraceCollector;
    using TestPlatform.ObjectModel;
    using TraceDataCollector.Resources;

    /// <summary>
    /// A managed wrapper for Vanguard
    /// </summary>
    internal class Vanguard : IVangurd
    {
        /// <summary>
        /// Return value of WaitForSingleObject, which means the object is signaled.
        /// </summary>
        private const uint WaitObject0 = 0x00000000;

        /// <summary>
        /// Time limit for vangaurd process exit event
        /// </summary>
        private const int ProcessExitWaitLimit = 60000;

        /// <summary>
        /// Prefix for creating event in global namespace
        /// </summary>
        private const string GlobalEventNamePrefix = "Global\\";

        /// <summary>
        /// Event set when vangaurd process exit
        /// </summary>
        private ManualResetEvent vanguardProcessExitEvent;

        /// <summary>
        /// Vanguard process
        /// </summary>
        private Process vanguardProcess;

        /// <summary>
        /// Session name of the vanguard logger
        /// </summary>
        private string sessionName;

        /// <summary>
        /// Vanguard configuration file name, empty for default config
        /// </summary>
        private string configurationFileName;

        /// <summary>
        /// Configuration XML element
        /// </summary>
        private XmlElement configuration;

        /// <summary>
        /// Output file name of vanguard
        /// </summary>
        private string outputName;

        /// <summary>
        /// Helper object to manage child process lifetimes
        /// </summary>
        private ProcessJobObject jobObject;

        /// <summary>
        /// logger
        /// </summary>
        private IDataCollectionLogger logger;

        /// <summary>
        /// Data collection context
        /// </summary>
        private DataCollectionContext context;

        /// <summary>
        /// Commands definition for vanguard
        /// </summary>
        protected enum Command
        {
            Collect,
            Shutdown,
            Register,
            Unregister,
            UnregisterAll,
            GetCoverageData,
            StartIISCollection
        }

        /// <summary>
        /// Gets output file name of vanguard
        /// </summary>
        public string OutputName
        {
            get { return this.outputName; }
        }

        /// <summary>
        /// Gets a value indicating whether whether vanguard is running
        /// </summary>
        private bool IsRunning
        {
            get { return this.vanguardProcess != null && !this.vanguardProcess.HasExited; }
        }

        /// <inheritdoc />
        public void Initialize(
            string sessionName,
            string configurationFileName,
            XmlElement configuration,
            IDataCollectionLogger logger)
        {
            this.sessionName = sessionName;
            this.configurationFileName = configurationFileName;
            this.configuration = configuration;
            this.logger = logger;
            this.CreateProcessJobObject();
            using (var writer = new StreamWriter(new FileStream(this.configurationFileName, FileMode.Create)))
            {
                writer.WriteLine(this.configuration.OuterXml);
            }
        }

        /// <summary>
        /// Start a vanguard logger
        /// </summary>
        /// <param name="outputName">Output file name</param>
        /// <param name="context">Data collection context. </param>
        public virtual void Start(string outputName, DataCollectionContext context)
        {
            EqtTrace.Info("Vanguard.Start: Starting CodeCoverage.exe for coverage file: {0} datacollection session id: {1}", outputName, context.SessionId);
            this.UnregisterAll();
            this.Stop();
            this.vanguardProcessExitEvent = new ManualResetEvent(false);
            this.outputName = outputName;
            this.context = context;
            this.vanguardProcess = this.StartVanguardProcess(
                GenerateCommandLine(
                    Command.Collect,
                    this.sessionName,
                    this.outputName,
                    this.configurationFileName,
                    null),
                false,
                true);
            this.Wait();
        }

        /// <summary>
        /// Stop vanguard logger
        /// </summary>
        public virtual void Stop()
        {
            EqtTrace.Info("Vanguard.Stop: Stoping Vanguard.");
            if (this.IsRunning)
            {
                Process stopper =
                    this.StartVanguardProcess(
                        GenerateCommandLine(Command.Shutdown, this.sessionName, null, null, null),
                        true);

                // BugFix#1237649 We need to wait for completion of process exited event
                this.vanguardProcessExitEvent.WaitOne(ProcessExitWaitLimit);

                if (this.jobObject != null)
                {
                    this.jobObject.Dispose();
                    this.jobObject = null;
                }
            }
        }

        /// <summary>
        /// IDisposable implementation
        /// </summary>
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
        /// Waitd until the object is in signaled state or the time-out interval elapses.
        /// </summary>
        /// <param name="hHandle">"The handle"</param>
        /// <param name="dwMilliseconds">"The dwMilliseconds"</param>
        /// <returns>Returns int.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        /// <summary>
        /// Close a handle
        /// </summary>
        /// <param name="hObject">Object handle</param>
        /// <returns>True if succeeded</returns>
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Generate a command line string, given some parameters
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="sessionName">Session name</param>
        /// <param name="outputName">Output file name (for collect command)</param>
        /// <param name="configurationFileName">Configuration file name</param>
        /// <param name="entryPoint">Entry point name for register/unregister</param>
        /// <returns>Command line string</returns>
        private static string GenerateCommandLine(
            Command command,
            string sessionName,
            string outputName,
            string configurationFileName,
            string entryPoint)
        {
            StringBuilder builder = new StringBuilder();
            switch (command)
            {
                case Command.Collect:
                    builder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "collect /session:{0}  /output:\"{1}\"",
                        sessionName,
                        outputName);
                    if (!string.IsNullOrEmpty(configurationFileName))
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, " /config:\"{0}\"", configurationFileName);
                    }

                    break;
                case Command.StartIISCollection:
                    builder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "collect /IIS /session:{0} /output:\"{1}\"",
                        sessionName,
                        outputName);
                    if (!string.IsNullOrEmpty(configurationFileName))
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, " /config:\"{0}\"", configurationFileName);
                    }

                    break;
                case Command.Shutdown:
                    builder.AppendFormat(CultureInfo.InvariantCulture, "shutdown /session:{0}", sessionName);
                    break;

                case Command.Register:
                    builder.AppendFormat(CultureInfo.InvariantCulture, "register /force /session:{0} ", sessionName);
                    if (!string.IsNullOrEmpty(configurationFileName))
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, " /config:\"{0}\" ", configurationFileName);
                    }

                    builder.AppendFormat("\"{0}\"", entryPoint);
                    break;
                case Command.Unregister:
                    builder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "unregister /force /session:{0} \"{1}\"",
                        sessionName,
                        entryPoint);
                    break;
                case Command.UnregisterAll:
                    builder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "unregister /wildcard /session:{0}",
                        sessionName);
                    break;
                case Command.GetCoverageData:
                    builder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "getcoverageData /session:{0} /output:\"{1}\"",
                        sessionName,
                        outputName);
                    break;
            }

            EqtTrace.Info("Vanguard.GenerateCommandLine: Created the command: {0}", builder);
            return builder.ToString();
        }

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
        /// Unregister all entry points
        /// </summary>
        private void UnregisterAll()
        {
            EqtTrace.Info("Vanguard.UnregisterAll: Sending unregister all command.");
            Process process = this.StartVanguardProcess(
                GenerateCommandLine(
                    Command.UnregisterAll,
                    DynamicCoverageDataCollectorImpl.MagicMtmSessionPrefix + "*",
                    null,
                    null,
                    null),
                true);
            if (process.ExitCode != 0)
            {
                EqtTrace.Warning("Vanguard.UnregisterAll: Process exited with non zero.");
            }

            process.Dispose();
        }

        /// <summary>
        /// Start a vanguard process
        /// </summary>
        /// <param name="commandLine">Command line options</param>
        /// <param name="wait">Whether to wait until the process exits</param>
        /// <param name="standardErrorAsynchronousCall">The standardErrorAsynchronousCall. </param>
        /// <returns>Process instance of vanguard</returns>
        private Process StartVanguardProcess(
            string commandLine,
            bool wait,
            bool standardErrorAsynchronousCall = false)
        {
            string vanguardPath = CollectorUtility.GetVanguardPath();
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
            else if (this.jobObject != null)
            {
                EqtTrace.Info("Vanguard.StartVanguardProcess: Add Vangaurd process to the project object");
                this.jobObject.AddProcess(process.SafeHandle.DangerousGetHandle());
            }

            EqtTrace.Info(
                "Vanguard.StartVanguardProcess: Started Vangaurd process id :{0}",
                Vanguard.GetProcessId(process));

            return process;
        }

        /// <summary>
        /// Wait until vanguard initialization is finished
        /// </summary>
        private void Wait()
        {
            EqtTrace.Info("Vanguard.Wait: Waiting for CodeCoverage.exe initialization.");
            IntPtr runningEvent = CreateEvent(
                IntPtr.Zero,
                true,
                false,
                GlobalEventNamePrefix + this.sessionName + "_RUNNING");
            if (runningEvent != IntPtr.Zero)
            {
                var timeout = EnvironmentHelper.GetConnectionTimeout();
                uint waitTimeout = (uint)timeout * 1000; // Time limit for waiting the vanguard event

                IntPtr[] handles = new IntPtr[] { runningEvent, this.vanguardProcess.SafeHandle.DangerousGetHandle() };
                uint result = WaitForMultipleObjects((uint)handles.Length, handles, false, waitTimeout);
                CloseHandle(runningEvent);
                switch (result)
                {
                    case WaitObject0:
                        EqtTrace.Info("Vanguard.Wait: Running event received from CodeCoverage.exe.");
                        return;
                    case WaitObject0 + 1:
                        // Process exited, something wrong happened
                        // we have already set to read messages asynchronously, so calling this.vanguardProcess.StandardError.ReadToEnd() which is synchronous is wrong.
                        // throw new VanguardException(string.Format(CultureInfo.CurrentCulture, Resources.ErrorLaunchVanguard, this.vanguardProcess.StandardError.ReadToEnd()));
                        EqtTrace.Error("Vanguard.Wait: CodeCoverage.exe failed to receive running event in {0} seconds", timeout);
                        throw new VanguardException(string.Format(CultureInfo.CurrentUICulture, Resources.NoRunningEventFromVanguard, timeout, EnvironmentHelper.VstestConnectionTimeout), true);
                }
            }

            // Something happens, kill the process
            try
            {
                EqtTrace.Error("Vanguard.Wait: Fail to create running event. Killing CodeCoverage.exe. ");
                this.vanguardProcess.Kill();
            }
            catch (Win32Exception)
            {
            }
            catch (NotSupportedException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            throw new VanguardException(Resources.GeneralErrorLaunchVanguard);
        }

        /// <summary>
        /// Handler for vanguard process exit event
        /// </summary>
        /// <param name="sender">The sender. </param>
        /// <param name="e">Event args. </param>
        private void LoggerProcessExited(object sender, EventArgs e)
        {
            EqtTrace.Info("Vanguard.LoggerProcessExited: Vangaurd process exit callback started.");
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
            EqtTrace.Warning(string.Format(
                CultureInfo.CurrentCulture,
                "ProcessErrorDataReceived : {0}",
                e.Data));

            if (this.logger != null && this.context != null && !string.IsNullOrWhiteSpace(e.Data))
            {
                this.logger.LogWarning(this.context, e.Data);
            }
        }

        /// <summary>
        /// Helper function to create a job object for child process management
        /// </summary>
        private void CreateProcessJobObject()
        {
            try
            {
                this.jobObject = new ProcessJobObject();
            }
            catch (Exception ex)
            {
                EqtTrace.Warning("CreateProcessJobObject Failed : {0}", ex);
            }
        }
    }
}