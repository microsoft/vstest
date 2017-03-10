// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Text;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// The default test host launcher for the engine.
    /// This works for Desktop local scenarios
    /// </summary>
    public class DefaultTestHostManager : ITestRuntimeProvider
    {
        private const string X64TestHostProcessName = "testhost.exe";
        private const string X86TestHostProcessName = "testhost.x86.exe";

        private readonly Architecture architecture;
        private readonly Framework framework;
        private ITestHostLauncher customTestHostLauncher;

        private Process testHostProcess;
        private readonly IProcessHelper processHelper;

        private CancellationTokenSource hostLaunchCTS;
        private StringBuilder testHostProcessStdError;

        private IMessageLogger messageLogger;
        protected int ErrorLength { get; set; } = 1000;
        protected int TimeOut { get; set; } = 10000;

        public event EventHandler<HostProviderEventArgs> HostLaunched;
        public event EventHandler<HostProviderEventArgs> HostExited;

        /// <summary>
        /// Callback on process exit
        /// </summary>
        private Action<Process, string> ErrorReceivedCallback => ((process, data) => 
        {
            if (data != null)
            {
                // if incoming data stream is huge empty entire testError stream, & limit data stream to MaxCapacity
                if (data.Length > this.testHostProcessStdError.MaxCapacity)
                {
                    this.testHostProcessStdError.Clear();
                    data = data.Substring(data.Length - this.testHostProcessStdError.MaxCapacity);
                }

                // remove only what is required, from beginning of error stream
                else
                {
                    int required = data.Length + this.testHostProcessStdError.Length - this.testHostProcessStdError.MaxCapacity;
                    if (required > 0)
                    {
                        this.testHostProcessStdError.Remove(0, required);
                    }
                }

                this.testHostProcessStdError.Append(data);
                this.messageLogger.SendMessage(TestMessageLevel.Warning, this.testHostProcessStdError.ToString());
            }

            if (process.HasExited && process.ExitCode != 0)
            {
                EqtTrace.Error("Test host exited with error: {0}", this.testHostProcessStdError);
                this.OnHostExited(new HostProviderEventArgs(this.testHostProcessStdError.ToString(), process.ExitCode));
            }
        });
        
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

        /// <inheritdoc/>
        public void SetCustomLauncher(ITestHostLauncher customLauncher)
        {
            this.customTestHostLauncher = customLauncher;
        }

        /// <inheritdoc/>
        public async Task<int> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo)
        {
            return await Task.Run(() => LaunchHost(testHostStartInfo), this.GetCancellationTokenSource().Token);
        }

        /// <inheritdoc/>
        private CancellationTokenSource GetCancellationTokenSource()
        {
            this.hostLaunchCTS = new CancellationTokenSource(TimeOut);
            return this.hostLaunchCTS;
        }

        private int LaunchHost(TestProcessStartInfo testHostStartInfo)
        {
            try
            {
                this.testHostProcessStdError = new StringBuilder(this.ErrorLength, this.ErrorLength);
                EqtTrace.Verbose("Launching default test Host Process {0} with arguments {1}", testHostStartInfo.FileName, testHostStartInfo.Arguments);

                if (this.customTestHostLauncher == null)
                {
                    this.testHostProcess = this.processHelper.LaunchProcess(testHostStartInfo.FileName, testHostStartInfo.Arguments, testHostStartInfo.WorkingDirectory, this.ErrorReceivedCallback);
                }
                else
                {
                    int processId = this.customTestHostLauncher.LaunchTestHost(testHostStartInfo);
                    this.testHostProcess = Process.GetProcessById(processId);
                }

            }
            catch (OperationCanceledException ex)
            {
                this.OnHostExited(new HostProviderEventArgs(ex.Message, -1));
                return -1;
            }
            this.OnHostLaunched(new HostProviderEventArgs("Test Runtime launched with Pid: " + this.testHostProcess.Id));
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

        public bool CanExecuteCurrentRunConfiguration(string runConfiguration)
        {
            RunConfiguration config = XmlRunSettingsUtilities.GetRunConfigurationNode(runConfiguration);
            var framework = config.TargetFrameworkVersion;

            // This is expected to be called once every run so returning a new instance every time.
            if (framework.Name.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                || framework.Name.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return true;
        }

        public void OnHostLaunched(HostProviderEventArgs e)
        {
            this.HostLaunched.SafeInvoke(this, e, "HostProviderEvents.OnHostLaunched");
        }

        public void OnHostExited(HostProviderEventArgs e)
        {
            this.HostExited.SafeInvoke(this, e, "HostProviderEvents.OnHostError");
        }

        public void Initialize(IMessageLogger logger)
        {
            this.messageLogger = logger;
        }
    }
}
