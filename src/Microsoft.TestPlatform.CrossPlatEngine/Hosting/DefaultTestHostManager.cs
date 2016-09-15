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
        private const string NetCoreDirectoryName = "NetCore";
        
        private Architecture architecture;
        private Framework framework;
        private ITestHostLauncher customTestHostLauncher;

        private Process testHostProcess;
        private IProcessHelper processHelper;

        private EventHandler registeredExitHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostManager"/> class.
        /// </summary>
        /// <param name="architecture">Platform architecture of the host process.</param>
        /// <param name="framework">Runtime framework for the host process.</param>
        public DefaultTestHostManager(Architecture architecture, Framework framework)
            : this(architecture, framework, new ProcessHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostManager"/> class.
        /// </summary>
        /// <param name="architecture">Platform architecture of the host process.</param>
        /// <param name="framework">Runtime framework for the host process.</param>
        /// <param name="processHelper">Process helper instance.</param>
        internal DefaultTestHostManager(Architecture architecture, Framework framework, IProcessHelper processHelper)
        {
            this.architecture = architecture;
            this.framework = framework;
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
        /// Sets a custom launcher.
        /// </summary>
        /// <param name="customLauncher">Custom launcher to set</param>
        public void SetCustomLauncher(ITestHostLauncher customLauncher)
        {
            this.customTestHostLauncher = customLauncher;
        }

        /// <summary>
        /// Launches the test host for discovery/execution.
        /// </summary>
        /// <param name="environmentVariables">Environment variables for the process.</param>
        /// <param name="commandLineArguments">The command line arguments to pass to the process.</param>
        /// <returns>ProcessId of launched Process. 0 means not launched.</returns>
        public virtual int LaunchTestHost(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            this.DeregisterForExitNotification();
            var testHostProcessInfo = this.GetTestHostProcessStartInfo(environmentVariables, commandLineArguments);
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
        /// <param name="environmentVariables">Set of environment variables.</param>
        /// <param name="commandLineArguments">Arguments for the test host process.</param>
        /// <returns>ProcessStartInfo of the test host</returns>
        public virtual TestProcessStartInfo GetTestHostProcessStartInfo(IDictionary<string, string> environmentVariables, IList<string> commandLineArguments)
        {
            var testHostProcessName = X64TestHostProcessName;
            string testHostDirectory = Path.GetDirectoryName(typeof(DefaultTestHostManager).GetTypeInfo().Assembly.Location);
            string testhostProcessPath;

            // If we are running in the dotnet.exe context we do not want to launch testhost.exe but dotnet.exe with the testhost assembly. 
            // Since dotnet.exe is already built for multiple platforms this would avoid building testhost.exe also in multiple platforms.
            var currentProcessFileName = this.processHelper.GetCurrentProcessFileName();
            if (currentProcessFileName.EndsWith(DotnetProcessName) || currentProcessFileName.EndsWith(DotnetProcessNameXPlat))
            {
                testhostProcessPath = currentProcessFileName;
                var testhostAssemblyPath = Path.Combine(
                    testHostDirectory,
                    testHostProcessName.Replace("exe", "dll"));
                commandLineArguments.Insert(0, "\"" + testhostAssemblyPath + "\"");
            }
            else
            {
                // Running on Windows with vstest.console.exe for desktop (or VS IDE). Spawn the dotnet.exe
                // on path with testhost bundled with vstest.console.
                if (this.framework.Name.ToLower().Contains("netstandard") || this.framework.Name.ToLower().Contains("netcoreapp"))
                {
                    testhostProcessPath = DotnetProcessName;
                    var testhostAssemblyPath = Path.Combine(
                        Path.GetDirectoryName(currentProcessFileName),
                        NetCoreDirectoryName,
                        testHostProcessName.Replace("exe", "dll"));
                    commandLineArguments.Insert(0, "\"" + testhostAssemblyPath + "\"");
                }
                else
                {
                    testHostProcessName = (this.architecture == Architecture.X86) ? X86TestHostProcessName : X64TestHostProcessName;
                    testhostProcessPath = Path.Combine(testHostDirectory, testHostProcessName);
                }
            }

            // For IDEs and other scenario - Current directory should be the working directory - not the vstest.console.exe location
            // For VS - this becomes the solution directory for example
            // "TestResults" directory will be created at "current directory" of test host
            string processWorkingDirectory = Directory.GetCurrentDirectory();

            string argumentsString = string.Join(" ", commandLineArguments);
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
                this.registeredExitHandler = (sender, args) => abortCallback();
                this.testHostProcess.Exited += this.registeredExitHandler;
            }
        }

        /// <summary>
        /// Deregister for the exit event.
        /// </summary>
        public virtual void DeregisterForExitNotification()
        {
            if (this.testHostProcess != null && this.registeredExitHandler != null)
            {
                this.testHostProcess.Exited -= this.registeredExitHandler;
            }
        }
    }
}
