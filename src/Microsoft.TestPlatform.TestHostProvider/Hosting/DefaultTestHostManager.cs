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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    /// <summary>
    /// The default test host launcher for the engine.
    /// This works for Desktop local scenarios
    /// </summary>
    [ExtensionUri(DefaultTestHostUri)]
    [FriendlyName(DefaultTestHostFriendltName)]
    public class DefaultTestHostManager : ITestRuntimeProvider
    {
        private const string X64TestHostProcessName = "testhost.exe";
        private const string X86TestHostProcessName = "testhost.x86.exe";

        private const string DefaultTestHostUri = "HostProvider://DefaultTestHost";
        private const string DefaultTestHostFriendltName = "DefaultTestHost";

        private Architecture architecture;

        private IProcessHelper processHelper;

        private ITestHostLauncher customTestHostLauncher;
        private Process testHostProcess;
        private CancellationTokenSource hostLaunchCts;
        private StringBuilder testHostProcessStdError;
        private IMessageLogger messageLogger;
        private bool hostExitedEventRaised;

        public DefaultTestHostManager()
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
            this.processHelper = processHelper;
            this.testHostProcess = null;

            this.Shared = shared;
            this.hostExitedEventRaised = false;
        }

        public event EventHandler<HostProviderEventArgs> HostLaunched;

        public event EventHandler<HostProviderEventArgs> HostExited;

        /// <inheritdoc/>
        public bool Shared { get; private set; }

        /// <summary>
        /// Gets the properties of the test executor launcher. These could be the targetID for emulator/phone specific scenarios.
        /// </summary>
        public IDictionary<string, string> Properties => new Dictionary<string, string>();

        protected int ErrorLength { get; set; } = 1000;

        protected int TimeOut { get; set; } = 10000;

        /// <summary>
        /// Callback on process exit
        /// </summary>
        private Action<object> ExitCallBack => ((process) =>
        {
            var exitCode = 0;
            this.processHelper.TryGetExitCode(process, out exitCode);

            this.OnHostExited(new HostProviderEventArgs(this.testHostProcessStdError.ToString(), exitCode, (process as Process).Id));
        });

        /// <summary>
        /// Callback to read from process error stream
        /// </summary>
        private Action<object, string> ErrorReceivedCallback => ((process, data) =>
        {
            var exitCode = 0;
            if (!string.IsNullOrEmpty(data))
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
            }

            if (this.processHelper.TryGetExitCode(process, out exitCode))
            {
                EqtTrace.Error("Test host exited with error: {0}", this.testHostProcessStdError);
                this.OnHostExited(new HostProviderEventArgs(this.testHostProcessStdError.ToString(), exitCode, (process as Process).Id));
            }
        });


        /// <inheritdoc/>
        public void SetCustomLauncher(ITestHostLauncher customLauncher)
        {
            this.customTestHostLauncher = customLauncher;
        }

        /// <inheritdoc/>
        public async Task<int> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo)
        {
            return await Task.Run(() => this.LaunchHost(testHostStartInfo), this.GetCancellationTokenSource().Token);
        }

        /// <inheritdoc/>
        public virtual TestProcessStartInfo GetTestHostProcessStartInfo(
            IEnumerable<string> sources,
            IDictionary<string, string> environmentVariables,
            TestRunnerConnectionInfo connectionInfo)
        {
            // Default test host manager supports shared test sources
            var testHostProcessName = (this.architecture == Architecture.X86) ? X86TestHostProcessName : X64TestHostProcessName;
            var currentWorkingDirectory = Path.Combine(Path.GetDirectoryName(typeof(DefaultTestHostManager).GetTypeInfo().Assembly.Location), "..//");
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
        public bool CanExecuteCurrentRunConfiguration(string runsettingsXml)
        {
            var config = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
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
            if (!this.hostExitedEventRaised)
            {
                this.hostExitedEventRaised = true;
                this.HostExited.SafeInvoke(this, e, "HostProviderEvents.OnHostError");
            }   
        }

        /// <inheritdoc/>
        public void Initialize(IMessageLogger logger, string runsettingsXml)
        {
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);

            this.messageLogger = logger;
            this.architecture = runConfiguration.TargetPlatform;
            this.processHelper = new ProcessHelper();
            this.testHostProcess = null;

            this.Shared = !runConfiguration.DisableAppDomain;
            this.hostExitedEventRaised = false;
        }

        /// <inheritdoc/>
        private CancellationTokenSource GetCancellationTokenSource()
        {
            this.hostLaunchCts = new CancellationTokenSource(this.TimeOut);
            return this.hostLaunchCts;
        }

        private int LaunchHost(TestProcessStartInfo testHostStartInfo)
        {
            try
            {
                this.testHostProcessStdError = new StringBuilder(this.ErrorLength, this.ErrorLength);
                EqtTrace.Verbose("Launching default test Host Process {0} with arguments {1}", testHostStartInfo.FileName, testHostStartInfo.Arguments);

                if (this.customTestHostLauncher == null)
                {
                    EqtTrace.Verbose("DefaultTestHostManager: Starting process '{0}' with command line '{1}'", testHostStartInfo.FileName, testHostStartInfo.Arguments);
                    this.testHostProcess = this.processHelper.LaunchProcess(testHostStartInfo.FileName, testHostStartInfo.Arguments, testHostStartInfo.WorkingDirectory, testHostStartInfo.EnvironmentVariables, this.ErrorReceivedCallback, this.ExitCallBack) as Process;
                }
                else
                {
                    int processId = this.customTestHostLauncher.LaunchTestHost(testHostStartInfo);
                    this.testHostProcess = Process.GetProcessById(processId);
                }
            }
            catch (OperationCanceledException ex)
            {
                this.OnHostExited(new HostProviderEventArgs(ex.Message, -1, 0));
                return -1;
            }

            var pId = this.testHostProcess != null ? this.testHostProcess.Id : 0;
            this.OnHostLaunched(new HostProviderEventArgs("Test Runtime launched with Pid: " + pId));
            return pId;
        }
    }
}
