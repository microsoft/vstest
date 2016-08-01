// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    /// <summary>
    /// The default test host launcher for the engine.
    /// This works for Desktop local scenarios
    /// </summary>
    public class DefaultTestHostManager : ITestHostManager
    {
        private const string X64TestHostProcessName = "testhost.exe";
        private const string X86TestHostProcessName = "testhost.x86.exe";
        private const string DotnetProcessName = "dotnet.exe";
        private const string DotnetProcessNameXPlat = "dotnet";

        private Architecture architecture;
        private ITestHostLauncher customTestHostLauncher;

        private Process testHostProcess;
        private IProcessHelper processHelper;

        private EventHandler registeredExitHandler;

        /// <summary>
        /// The constructor.
        /// </summary>
        ///  <param name="architecture">
        /// The architecture.
        /// </param>
        public DefaultTestHostManager(Architecture architecture)
            : this(architecture, new ProcessHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostProcessManager"/> class.
        /// </summary>
        /// <param name="architecture"> The architecture. </param>
        /// <param name="processHelper"> The process helper. </param>
        internal DefaultTestHostManager(Architecture architecture, IProcessHelper processHelper)
        {
            this.architecture = architecture;
            this.processHelper = processHelper;
            this.testHostProcess = null;
        }

        /// <summary>
        /// Gets the properties of the test executor launcher. These could be the targetID for emulator/phone specific scenarios.
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get
            {
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Sets a custom launcher
        /// </summary>
        /// <param name="customTestHostLauncher">Custom launcher to set</param>
        public void SetCustomLauncher(ITestHostLauncher customTestHostLauncher)
        {
            this.customTestHostLauncher = customTestHostLauncher;
        }

        /// <summary>
        /// Launches the test host for discovery/execution.
        /// </summary>
        /// <param name="environmentVariables">Environment variables for the process.</param>
        /// <param name="commandLineArguments">The command line arguments to pass to the process.</param>
        /// <returns>ProcessId of launched Process. 0 means not launched.</returns>
        public virtual int LaunchTestHost(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            DeregisterForExitNotification();
            var testHostProcessInfo = GetTestHostProcessStartInfo(environmentVariables, commandLineArguments);
            EqtTrace.Verbose("Launching default test Host Process {0} with arguments {1}", testHostProcessInfo.FileName, testHostProcessInfo.Arguments);

            if (this.customTestHostLauncher == null)
            {
                this.testHostProcess = this.processHelper.LaunchProcess(testHostProcessInfo.FileName, testHostProcessInfo.Arguments, testHostProcessInfo.WorkingDirectory);
            }
            else
            {
                int processId = this.customTestHostLauncher.LaunchTestHost(testHostProcessInfo);
                this.testHostProcess = Process.GetProcessById(processId);
            }

            return this.testHostProcess.Id;
        }

        /// <summary>
        /// Gives the ProcessStartInfo for the test host process
        /// </summary>
        /// <param name="environmentVariables"></param>
        /// <param name="commandLineArguments"></param>
        /// <returns>ProcessStartInfo of the test host</returns>
        public virtual TestProcessStartInfo GetTestHostProcessStartInfo(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            var testHostProcessName = (architecture == Architecture.X86) ? X86TestHostProcessName : X64TestHostProcessName;
            var currentWorkingDirectory = Path.GetDirectoryName(typeof(DefaultTestHostManager).GetTypeInfo().Assembly.Location);
            string testhostProcessPath, processWorkingDirectory = null;

            // If we are running in the dotnet.exe context we do not want to launch testhost.exe but dotnet.exe with the testhost assembly. 
            // Since dotnet.exe is already built for multiple platforms this would avoid building testhost.exe also in multiple platforms.
            var currentProcessFileName = this.processHelper.GetCurrentProcessFileName();
            if (currentProcessFileName.EndsWith(DotnetProcessName) || currentProcessFileName.EndsWith(DotnetProcessNameXPlat))
            {
                testhostProcessPath = currentProcessFileName;
                var testhostAssemblyPath = Path.Combine(
                    currentWorkingDirectory,
                    testHostProcessName.Replace("exe", "dll"));
                commandLineArguments.Insert(0, testhostAssemblyPath);
                processWorkingDirectory = Path.GetDirectoryName(currentProcessFileName);
            }
            else
            {
                testhostProcessPath = Path.Combine(currentWorkingDirectory, testHostProcessName);
                // For IDEs and other scenario - Current directory should be the working directory - not the vstest.console.exe location
                // For VS - this becomes the solution directory for example
                // "TestResults" directory will be created at "current directory" of test host
                processWorkingDirectory = Directory.GetCurrentDirectory();
            }
            var argumentsString = string.Join(" ", commandLineArguments);
            return new TestProcessStartInfo { FileName = testhostProcessPath, Arguments = argumentsString, EnvironmentVariables = environmentVariables, WorkingDirectory = processWorkingDirectory };
        }

        /// <summary>
        /// Register for the exit event.
        /// </summary>
        /// <param name="abortCallback"> The callback on exit. </param>
        public virtual void RegisterForExitNotification(Action abortCallback)
        {
            if (this.testHostProcess != null && abortCallback != null)
            {
                this.registeredExitHandler = new EventHandler((sender, args) => abortCallback());
                this.testHostProcess.Exited += this.registeredExitHandler;
            }
        }

        /// <summary>
        /// Deregister for the exit event.
        /// </summary>
        public virtual void DeregisterForExitNotification()
        {
            if(this.testHostProcess != null && this.registeredExitHandler != null)
            {
                this.testHostProcess.Exited -= this.registeredExitHandler;
            }
        }
    }
}
