// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.TestHostProvider.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.TestHostProvider.Helpers;
    using Microsoft.VisualStudio.TestPlatform.TestHostProvider.Helpers.Interfaces;
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

        private readonly Architecture architecture;
        private readonly Framework framework;
        private ITestHostLauncher customTestHostLauncher;

        private Process testHostProcess;
        private readonly IProcessHelper processHelper;

        private EventHandler registeredExitHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostManager"/> class.
        /// </summary>
        /// <param name="architecture">Platform architecture of the host process.</param>
        /// <param name="framework">Runtime framework for the host process.</param>
        public DefaultTestHostManager(Architecture architecture, Framework framework, bool shared)
            : this(architecture, framework, new ProcessHelper(), shared)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostManager"/> class.
        /// </summary>
        /// <param name="architecture">Platform architecture of the host process.</param>
        /// <param name="framework">Runtime framework for the host process.</param>
        /// <param name="processHelper">Process helper instance.</param>
        /// <param name="shared">Share the manager for multiple sources or not</param>
        internal DefaultTestHostManager(Architecture architecture, Framework framework, IProcessHelper processHelper, bool shared)
        {
            this.architecture = architecture;
            this.framework = framework;
            this.processHelper = processHelper;
            this.testHostProcess = null;

            this.Shared = shared;
        }

        /// <inheritdoc/>
        public bool Shared { get; private set; }

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
        /// <param name="testHostStartInfo">Test host start information.</param>
        /// <returns>ProcessId of launched Process. 0 means not launched.</returns>
        public int LaunchTestHost(TestProcessStartInfo testHostStartInfo)
        {
            this.DeregisterForExitNotification();
            EqtTrace.Verbose("Launching default test Host Process {0} with arguments {1}", testHostStartInfo.FileName, testHostStartInfo.Arguments);

            if (this.customTestHostLauncher == null)
            {
                this.testHostProcess = this.processHelper.LaunchProcess(testHostStartInfo.FileName, testHostStartInfo.Arguments, testHostStartInfo.WorkingDirectory, testHostStartInfo.ExitCallback);
            }
            else
            {
                int processId = this.customTestHostLauncher.LaunchTestHost(testHostStartInfo);
                this.testHostProcess = Process.GetProcessById(processId);
            }

            return this.testHostProcess.Id;
        }

        /// <inheritdoc/>
        public virtual TestProcessStartInfo GetTestHostProcessStartInfo(
            IEnumerable<string> sources,
            IDictionary<string, string> environmentVariables,
            TestRunnerConnectionInfo connectionInfo)
        {
            // Default test host manager supports shared test sources
            var testHostProcessName = (this.architecture == Architecture.X86) ? X86TestHostProcessName : X64TestHostProcessName;
            var currentWorkingDirectory = Path.GetDirectoryName(typeof(DefaultTestHostManager).GetTypeInfo().Assembly.Location);
            var argumentsString = " " + connectionInfo.ToCommandLineOptions();
            var currentProcessPath = this.processHelper.GetCurrentProcessFileName();

            if (currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
                || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            {
                // "TestHost" is the name of the folder which contain Full CLR built testhost package assemblies inside Core CLR package folder.
                testHostProcessName = Path.Combine("TestHost", testHostProcessName);
            }

            if (!this.Shared)
            {
                // Not sharing the host which means we need to pass the test assembly path as argument
                // so that the test host can create an appdomain on startup (Main method) and set appbase
                argumentsString += " " + "--testsourcepath " + "\"" + sources.FirstOrDefault() + "\"";
            }

            var testhostProcessPath = Path.Combine(currentWorkingDirectory, testHostProcessName);
            EqtTrace.Verbose("DefaultTestHostmanager: Full path of {0} is {1}", testHostProcessName, testhostProcessPath);

            // For IDEs and other scenario, current directory should be the
            // working directory (not the vstest.console.exe location).
            // For VS - this becomes the solution directory for example
            // "TestResults" directory will be created at "current directory" of test host
            var processWorkingDirectory = Directory.GetCurrentDirectory();

            return new TestProcessStartInfo
            {
                FileName = testhostProcessPath,
                Arguments = argumentsString,
                EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>(),
                WorkingDirectory = processWorkingDirectory
            };
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources)
        {
            // Default test host manager doesn't provide custom acquisition or discovery of
            // test platform extensions.
            return Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public virtual void RegisterForExitNotification(Action abortCallback)
        {
            if (this.testHostProcess != null && abortCallback != null)
            {
                this.registeredExitHandler = (sender, args) => abortCallback();
                this.testHostProcess.Exited += this.registeredExitHandler;
            }
        }

        /// <inheritdoc/>
        public virtual void DeregisterForExitNotification()
        {
            if (this.testHostProcess != null && this.registeredExitHandler != null)
            {
                this.testHostProcess.Exited -= this.registeredExitHandler;
            }
        }
    }
}
