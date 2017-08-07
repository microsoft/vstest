// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.TestPlatform.TestHostProvider.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// The BlameModeTestHostLauncher
    /// </summary>
    public class BlameModeTestHostLauncher : ITestHostLauncher
    {
        private int processId;
        private IProcessHelper processHelper;
        private string processName;
        private bool hostExitedEventRaised;
        private StringBuilder testHostProcessStdError;
        private LocalCrashDumpUtilities crashDumpUtilities;
        private IEnvironment environment;
        private IFileHelper fileHelper;
        private IBlameDumpFolder blameDumpFolderGetter;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameModeTestHostLauncher"/> class.
        /// </summary>
        public BlameModeTestHostLauncher()
            : this(new ProcessHelper(), new LocalCrashDumpUtilities(), new PlatformEnvironment(), new FileHelper(), new BlameDumpFolder())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlameModeTestHostLauncher"/> class.
        /// </summary>
        /// <param name="processHelper">Process Helper</param>
        /// <param name="crashDumpUtilities">Crash dump utilities class</param>
        /// <param name="fileHelper">File Helper</param>
        /// <param name="platformEnvironment">Platform environment</param>
        /// <param name="blameDumpFolder">Blame Dump foldere getter</param>
        protected BlameModeTestHostLauncher(IProcessHelper processHelper, LocalCrashDumpUtilities crashDumpUtilities, IEnvironment platformEnvironment, IFileHelper fileHelper, IBlameDumpFolder blameDumpFolder)
        {
            this.processHelper = processHelper;
            this.crashDumpUtilities = crashDumpUtilities;
            this.environment = platformEnvironment;
            this.fileHelper = fileHelper;
            this.blameDumpFolderGetter = blameDumpFolder;
        }

        /// <summary>
        /// HostExited event handler
        /// </summary>
        public event EventHandler<HostProviderEventArgs> HostExited;

        /// <summary>
        /// Gets a value indicating whether isDebug
        /// </summary>
        public bool IsDebug { get => false; }

        /// <summary>
        /// Gets or sets hostExited event handler
        /// </summary>
        protected int ErrorLength { get; set; } = 4096;

        /// <summary>
        /// Gets hostExited event handler
        /// </summary>
        private Action<object> ExitCallBack => (process) =>
        {
            TestHostManagerCallbacks.ExitCallBack(this.processHelper, process, this.testHostProcessStdError, this.OnHostExited);
        };

        /// <summary>
        /// Gets hostExited event handler
        /// </summary>
        private Action<object, string> ErrorReceivedCallback => (process, data) =>
        {
            TestHostManagerCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, data);
        };

        /// <summary>
        /// Launches Test Host
        /// </summary>
        /// <param name="testHostStartInfo">Test Host Start Info</param>
        /// <returns>Test Host Process Id</returns>
        public int LaunchTestHost(TestProcessStartInfo testHostStartInfo)
        {
            if (testHostStartInfo == null)
            {
                throw new ArgumentNullException(nameof(testHostStartInfo));
            }

            this.testHostProcessStdError = new StringBuilder(this.ErrorLength, this.ErrorLength);
            var testHostProcess = this.processHelper.LaunchProcess(
                testHostStartInfo.FileName,
                testHostStartInfo.Arguments,
                testHostStartInfo.WorkingDirectory,
                testHostStartInfo.EnvironmentVariables,
                this.ErrorReceivedCallback,
                this.ExitCallBack) as Process;

            char[] delimiterChar = { '\\' };
            string[] splits = testHostStartInfo.FileName.Split(delimiterChar);

            // Get the name of the exe launched
            if (splits.Length > 0)
            {
                this.processName = splits[splits.Length - 1];
            }

            this.processId = testHostProcess.Id;
            return this.processId;
        }

        /// <summary>
        /// OnHostExited
        /// </summary>
        /// <param name="e">HostProviderEventArgs</param>
        private void OnHostExited(HostProviderEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            if (e.ErrroCode != 0)
            {
                // Tries to get dump folder for windows os
                if (this.environment.OperatingSystem.Equals(PlatformOperatingSystem.Windows))
                {
                    string crashDumpPath = null;
                    if (this.blameDumpFolderGetter.GetCrashDumpFolderPath(this.processName, out crashDumpPath))
                    {
                        string crashDumpFile = this.crashDumpUtilities.GetCrashDumpFile(crashDumpPath, this.processName, this.processId);
                        if (!string.IsNullOrEmpty(crashDumpFile) && this.fileHelper.Exists(crashDumpFile))
                        {
                            BlameLogger.AddFileToDumpList(crashDumpFile);
                        }
                    }
                }
            }

            if (!this.hostExitedEventRaised)
            {
                this.hostExitedEventRaised = true;
                this.HostExited.SafeInvoke(this, e, "HostProviderEvents.OnHostExited");
            }
        }
    }
}
