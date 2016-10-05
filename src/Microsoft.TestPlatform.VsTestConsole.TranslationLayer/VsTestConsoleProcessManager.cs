// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer
{
    using Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Vstest.console.exe process manager
    /// </summary>
    internal class VsTestConsoleProcessManager : IProcessManager
    {
        private string vstestConsolePath;

        private object syncObject = new object();

        private bool vstestConsoleStarted = false;

        private bool vstestConsoleExited = false;

        private Process process;

        public event EventHandler ProcessExited;

        #region Constructor

        public VsTestConsoleProcessManager(string vstestConsolePath)
        {
            this.vstestConsolePath = vstestConsolePath;
        }

        #endregion Constructor

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
        public void StartProcess(string[] args)
        {
            this.process = new Process();
            process.StartInfo.FileName = vstestConsolePath;
            if (args != null)
            {
                process.StartInfo.Arguments = args.Length < 2 ? args[0] : string.Join(" ", args);
            }
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
    }
}