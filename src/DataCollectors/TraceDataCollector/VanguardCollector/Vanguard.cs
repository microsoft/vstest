// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.Coverage
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
/*#if !NETSTANDARD
    using System.Security.Principal;
#endif*/
    using System.Text;
    using System.Threading;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.Diagnostics;
    using Microsoft.VisualStudio.TraceCollector;
    using Microsoft.VisualStudio.Collector;
    using TraceDataCollector;
    using TraceDataCollector.Resources;

    /// <summary>
    /// A managed wrapper for Vanguard
    /// </summary>
    internal class Vanguard : IDisposable
    {
        /// <summary>
        /// Commands definition for vanguard
        /// </summary>
        protected enum Command
        {
            Collect, Shutdown, Register, Unregister, UnregisterAll, GetCoverageData, StartIISCollection
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
        private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        /// <summary>
        /// Waits until one or all of the specified objects are in the signaled state or the time-out interval elapses.
        /// </summary>
        /// <param name="nCount">Number of handles</param>
        /// <param name="lpHandles">Object handles</param>
        /// <param name="bWaitAll">Whether to wait all handles</param>
        /// <param name="dwMilliseconds">Time-out interval</param>
        /// <returns>Wait result</returns>
        [DllImport("kernel32.dll")]
        private static extern uint WaitForMultipleObjects(uint nCount, IntPtr[] lpHandles, bool bWaitAll, uint dwMilliseconds);

        /// <summary>
        /// Waitd until the object is in signaled state or the time-out interval elapses.
        /// </summary>
        /// <param name="hHandle"></param>
        /// <param name="dwMilliseconds"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);


        /// <summary>
        /// Close a handle
        /// </summary>
        /// <param name="hObject">Object handle</param>
        /// <returns>True if succeeded</returns>
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Time limit for waiting the vanguard event
        /// </summary>
        const uint WAIT_LIMIT = 10000;

        /// <summary>
        /// Return value of WaitForSingleObject, which means the object is signaled.
        /// </summary>
        const uint WAIT_OBJECT_0 = 0x00000000;

        /// <summary>
        /// Time limit for vangaurd process exit event
        /// </summary>
        const int PROCESS_EXIT_WAIT_LIMIT = 60000;

        /// <summary>
        /// Key on the team build machine
        /// </summary>
        public const string SideBySideKeyOnTeamBuildMachine = @"SOFTWARE\Microsoft\VisualStudio\SxS\VS7";

        /// <summary>
        /// The element name for Allowed users configuration
        /// </summary>
        private const string AllowedUsersKey = "AllowedUsers";

        /// <summary>
        /// The element name for each item under Allowed users
        /// </summary>
        private const string AllowedUsersItemKey = "User";

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
        protected string sessionName;

        /// <summary>
        /// Vanguard configuration file name, empty for default config
        /// </summary>
        protected string configurationFileName;

        /// <summary>
        /// Configuration XML element
        /// </summary>
        protected XmlElement configuration;

        /// <summary>
        /// Output file name of vanguard
        /// </summary>
        protected string outputName;

        /// <summary>
        /// Entry points
        /// </summary>
        protected List<string> entryPoints;

        /// <summary>
        /// Helper object to manage child process lifetimes
        /// </summary>
        protected ProcessJobObject jobObject;

        /// <summary>
        /// logger
        /// </summary>
        protected IDataCollectionLogger logger;

        /// <summary>
        /// Data collection context
        /// </summary>
        protected DataCollectionContext context;

        /// <summary>
        /// Prefix for creating event in global namespace
        /// </summary>
        protected const string globalEventNamePrefix = "Global\\";

        /// <summary>
        /// Output file name of vanguard
        /// </summary>
        public string OutputName
        {
            get
            {
                return this.outputName;
            }
        }

        /// <summary>
        /// Entry points
        /// </summary>
        public List<string> EntryPoints
        {
            get
            {
                return this.entryPoints;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sessionName">Session name</param>
        /// <param name="configurationFileName">Configuration file name</param>
        /// <param name="configuration">Configuration XML element</param>
        /// <param name="entryPoints">Entry points</param>
        public Vanguard(string sessionName, string configurationFileName, XmlElement configuration, IEnumerable<string> entryPoints, IDataCollectionLogger logger)
        {
            this.sessionName = sessionName;
            this.configurationFileName = configurationFileName;
            this.configuration = configuration;
            this.logger = logger;
            this.entryPoints = new List<string>();
            this.entryPoints.AddRange(entryPoints);
            this.CreateProcessJobObject();
        }

        public Vanguard(string sessionName, string configurationFileName, XmlElement configuration, IEnumerable<string> entryPoints) :
            this(sessionName, configurationFileName, configuration, entryPoints, null)
        {
        }

        /// <summary>
        /// Whether vanguard is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.vanguardProcess != null && !this.vanguardProcess.HasExited;
            }
        }

        /// <summary>
        /// Update the configuration and save it to a temp folder
        /// </summary>
        /// <param name="allowedUsers">Allowed users</param>
/*#if !NETSTANDARD
        public void InitializeConfiguration(IEnumerable<SecurityIdentifier> allowedUsers)
#else*/
        public void InitializeConfiguration()
// #endif
        {
/*#if !NETSTANDARD
            List<SecurityIdentifier> allowedUserList = new List<SecurityIdentifier>();

            if (allowedUsers != null)
            {
                allowedUserList.AddRange(allowedUsers);
            }

            //Add current user to the list of allowed users
            allowedUserList.Add(WindowsIdentity.GetCurrent().User);

            AddAllowedUser(this.configuration, allowedUserList);
#endif*/
            using (var writer = new StreamWriter(new FileStream(this.configurationFileName, FileMode.Create)))
            {
                writer.WriteLine(this.configuration.OuterXml);
            }
        }

        /// <summary>
        /// Start a vanguard logger
        /// </summary>
        /// <param name="outputName">Output file name</param>
        public virtual void Start(string outputName, DataCollectionContext context)
        {
            this.UnregisterAll();
            foreach (var entryPoint in this.entryPoints)
            {
                this.Register(entryPoint, this.configurationFileName);
            }

            this.Stop();
            vanguardProcessExitEvent = new ManualResetEvent(false);
            this.outputName = outputName;
            this.context = context;
            this.vanguardProcess = StartVanguardProcess(GenerateCommandLine(Command.Collect, this.sessionName, this.outputName, this.configurationFileName, null), false, true);
            this.Wait();
        }

        /// <summary>
        /// Wait until vanguard initialization is finished
        /// </summary>
        private void Wait()
        {
            IntPtr runningEvent = CreateEvent(IntPtr.Zero, true, false, globalEventNamePrefix + this.sessionName + "_RUNNING");
            if (runningEvent != IntPtr.Zero)
            {
                IntPtr[] handles = new IntPtr[] { runningEvent, this.vanguardProcess.SafeHandle.DangerousGetHandle() };
                uint result = WaitForMultipleObjects((uint)handles.Length, handles, false, WAIT_LIMIT);
                CloseHandle(runningEvent);
                switch (result)
                {
                    case WAIT_OBJECT_0:
                        // Running event received
                        return;
                    case WAIT_OBJECT_0 + 1:
                        // Process exited, something wrong happened
                        //we have already set to read messages asynchronously, so calling this.vanguardProcess.StandardError.ReadToEnd() which is synchronous is wrong 
                        //throw new VanguardException(string.Format(CultureInfo.CurrentCulture, Resources.ErrorLaunchVanguard, this.vanguardProcess.StandardError.ReadToEnd()));
                        throw new VanguardException(Resources.GeneralErrorLaunchVanguard, true);
                }
            }

            // Something happens, kill the process
            try
            {
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
        /// Helper for waiting for a given event
        /// </summary>
        /// <param name="eventName"></param>
        protected void WaitForEvent(string eventName)
        {
            IntPtr waitEvent = CreateEvent(IntPtr.Zero, true, false, eventName);
            if (waitEvent != IntPtr.Zero)
            {
                uint result = WaitForSingleObject(waitEvent, WAIT_LIMIT);
                CloseHandle(waitEvent);
                if (result != WAIT_OBJECT_0)
                {
                    throw new VanguardException(Resources.GeneralErrorLaunchVanguard);
                }
            }
        }

        /// <summary>
        /// Stop vanguard logger
        /// </summary>
        public virtual void Stop()
        {
            if (this.IsRunning)
            {
                foreach (var entryPoint in this.entryPoints)
                {
                    Unregister(entryPoint);
                }

                Process stopper = StartVanguardProcess(GenerateCommandLine(Command.Shutdown, this.sessionName, null, null, null), true);
                //BugFix#1237649 We need to wait for completion of process exited event
                this.vanguardProcessExitEvent.WaitOne(PROCESS_EXIT_WAIT_LIMIT);

                if (this.jobObject != null)
                {
                    this.jobObject.Dispose();
                    this.jobObject = null;
                }
            }
        }

        /// <summary>
        /// Get current coverage data from the logger
        /// </summary>
        /// <param name="outputName">Output file to write coverage data to</param>
        public void GetCoverageData(string outputName)
        {
            using (Process process = StartVanguardProcess(GenerateCommandLine(Command.GetCoverageData, this.sessionName, outputName, null, null), true, false))
            {
                if (process.ExitCode != 0)
                {
                    throw new VanguardException(process.StandardError.ReadToEnd());
                }
            }
        }

        /// <summary>
        /// Start a vanguard process
        /// </summary>
        /// <param name="commandLine">Command line options</param>
        /// <param name="wait">Whether to wait until the process exits</param>
        /// <returns>Process instance of vanguard</returns>
        protected Process StartVanguardProcess(string commandLine, bool wait, bool standardErrorAsynchronousCall = false)
        {
            string vanguardPath = CollectorUtility.GetVanguardPath();
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
                process.ErrorDataReceived += new DataReceivedEventHandler(LoggerProcessErrorDataReceived);
                process.Exited += new System.EventHandler(LoggerProcessExited);
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
            else if (jobObject != null)
            {
                EqtTrace.Info("Add Vangaurd process to the project object");
                jobObject.AddProcess(process.SafeHandle.DangerousGetHandle());
            }

            EqtTrace.Info("Started Vangaurd process with command line {0}", commandLine);
            return process;
        }

        /// <summary>
        /// Handler for vanguard process exit event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoggerProcessExited(object sender, EventArgs e)
        {
            if (this.vanguardProcess != null)
            {
                if (this.vanguardProcess.HasExited == true && this.vanguardProcess.ExitCode != 0)
                {
                    EqtTrace.Warning(String.Format(CultureInfo.CurrentCulture,
                                "An error occurred in Code Coverage process. Error code = {0}", this.vanguardProcess.ExitCode));
                }

                this.vanguardProcess.Exited -= LoggerProcessExited;
                this.vanguardProcess.ErrorDataReceived -= LoggerProcessErrorDataReceived;

                this.vanguardProcessExitEvent.Set();
            }
        }

        /// <summary>
        ///  Handler for vanguard process error stream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoggerProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            EqtTrace.Warning(String.Format(CultureInfo.CurrentCulture,
                "ProcessErrorDataReceived : {0}", e.Data));

            if (this.logger != null && this.context != null && !string.IsNullOrWhiteSpace(e.Data))
            {
                logger.LogWarning(this.context, e.Data);
            }
        }

        /// <summary>
        /// Helper function to create a job object for child process management
        /// </summary>
        private void CreateProcessJobObject()
        {
            try
            {
                jobObject = new ProcessJobObject();
            }
            catch (Exception ex)
            {
                EqtTrace.Warning("CreateProcessJobObject Failed : {0}", ex);
            }
        }

        /// <summary>
        /// Generate a command line string, given some parameters
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="sessionName">Session name</param>
        /// <param name="outputName">Output file name (for collect command)</param>
        /// <param name="configurationFileName">Configuration file name</param>
        /// <param name="entryPoint">Entry point name for register/unregister</param>
        /// <returns>Command line string</returns>
        protected static string GenerateCommandLine(Command command, string sessionName, string outputName, string configurationFileName, string entryPoint)
        {
            StringBuilder builder = new StringBuilder();
            switch (command)
            {
                case Command.Collect:
                    builder.AppendFormat(CultureInfo.InvariantCulture, "collect /session:{0}  /output:\"{1}\"", sessionName, outputName);
                    if (!string.IsNullOrEmpty(configurationFileName))
                    {
                        builder.AppendFormat(CultureInfo.InvariantCulture, " /config:\"{0}\"", configurationFileName);
                    }

                    break;
                case Command.StartIISCollection:
                    builder.AppendFormat(CultureInfo.InvariantCulture, "collect /IIS /session:{0} /output:\"{1}\"", sessionName, outputName);
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
                    builder.AppendFormat(CultureInfo.InvariantCulture, "unregister /force /session:{0} \"{1}\"", sessionName, entryPoint);
                    break;
                case Command.UnregisterAll:
                    builder.AppendFormat(CultureInfo.InvariantCulture, "unregister /wildcard /session:{0}", sessionName);
                    break;
                case Command.GetCoverageData:
                    builder.AppendFormat(CultureInfo.InvariantCulture, "getcoverageData /session:{0} /output:\"{1}\"", sessionName, outputName);
                    break;
            }

            return builder.ToString();
        }

/*#if !NETSTANDARD
        /// <summary>
        /// Add users to allowed list in vanguard config
        /// </summary>
        /// <param name="config">Config XML</param>
        /// <param name="users">User list</param>
        public static void AddAllowedUser(XmlElement config, IEnumerable<SecurityIdentifier> users)
        {
            if (users == null)
            {
                return;
            }

            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            XmlElement allowedUser = config[AllowedUsersKey];
            if (allowedUser == null)
            {
                allowedUser = config.OwnerDocument.CreateElement(AllowedUsersKey);
                config.AppendChild(allowedUser);
            }

            foreach (var user in users)
            {
                var userElement = config.OwnerDocument.CreateElement(AllowedUsersItemKey);
                NTAccount account = user.Translate(typeof(NTAccount)) as NTAccount;
                userElement.InnerText = account.Value;
                allowedUser.AppendChild(userElement);
            }
        }
#endif*/

        /// <summary>
        /// Register an entry point
        /// </summary>
        /// <param name="entryPoint">Entry point</param>
        public virtual void Register(string entryPoint, string configFileName)
        {
            Process process = StartVanguardProcess(GenerateCommandLine(Command.Register, this.sessionName, null, configFileName, entryPoint), true);
            if (process.ExitCode != 0)
            {
                throw new VanguardException(process.StandardError.ReadToEnd(), true);
            }

            process.Dispose();
        }

        /// <summary>
        /// Unregister an entry point
        /// </summary>
        /// <param name="entryPoint">Entry point</param>
        public virtual void Unregister(string entryPoint)
        {
            Process process = StartVanguardProcess(GenerateCommandLine(Command.Unregister, this.sessionName, null, null, entryPoint), true);
            if (process.ExitCode != 0)
            {
                throw new VanguardException(process.StandardError.ReadToEnd(), true);
            }

            process.Dispose();
        }

        /// <summary>
        /// Unregister all entry points
        /// </summary>
        public virtual void UnregisterAll()
        {
            Process process = StartVanguardProcess(GenerateCommandLine(Command.UnregisterAll, DynamicCoverageDataCollectorImpl.MagicMtmSessionPrefix + "*", null, null, null), true);
            if (process.ExitCode != 0)
            {
                // nothrow
            }

            process.Dispose();
        }

        /// <summary>
        /// IDisposable implementation
        /// </summary>
        public virtual void Dispose()
        {
            if (this.vanguardProcess != null)
            {
                this.vanguardProcess.Dispose();
                this.vanguardProcess = null;
            }
        }
    }
}
