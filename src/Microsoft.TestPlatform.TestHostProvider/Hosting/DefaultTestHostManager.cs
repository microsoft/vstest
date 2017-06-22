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
    using Microsoft.TestPlatform.TestHostProvider.Hosting;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostManager"/> class.
        /// </summary>
        public DefaultTestHostManager()
            : this(new ProcessHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostManager"/> class.
        /// </summary>
        /// <param name="processHelper">Process helper instance.</param>
        internal DefaultTestHostManager(IProcessHelper processHelper)
        {
            this.processHelper = processHelper;
        }

        /// <inheritdoc/>
        public event EventHandler<HostProviderEventArgs> HostLaunched;

        /// <inheritdoc/>
        public event EventHandler<HostProviderEventArgs> HostExited;

        /// <inheritdoc/>
        public bool Shared { get; private set; }

        /// <summary>
        /// Gets the properties of the test executor launcher. These could be the targetID for emulator/phone specific scenarios.
        /// </summary>
        public IDictionary<string, string> Properties => new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the error length for runtime error stream.
        /// </summary>
        protected int ErrorLength { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the Timeout for runtime to initialize.
        /// </summary>
        protected int TimeOut { get; set; } = 10000;

        /// <summary>
        /// Gets callback on process exit
        /// </summary>
        private Action<object> ExitCallBack => (process) =>
        {
            TestHostManagerCallbacks.ExitCallBack(this.processHelper, process, this.testHostProcessStdError, this.OnHostExited);
        };

        /// <summary>
        /// Gets callback to read from process error stream
        /// </summary>
        private Action<object, string> ErrorReceivedCallback => (process, data) =>
        {
            TestHostManagerCallbacks.ErrorReceivedCallback(this.testHostProcessStdError, data);
        };

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

            // "TestHost" is the name of the folder which contain Full CLR built testhost package assemblies.
            testHostProcessName = Path.Combine("TestHost", testHostProcessName);

            if (!this.Shared)
            {
                // Not sharing the host which means we need to pass the test assembly path as argument
                // so that the test host can create an appdomain on startup (Main method) and set appbase
                argumentsString += " " + "--testsourcepath " + sources.FirstOrDefault().AddDoubleQuote();
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
        public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources, IEnumerable<string> extensions)
        {
            return extensions;
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

        /// <inheritdoc/>
        public void Initialize(IMessageLogger logger, string runsettingsXml)
        {
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);

            this.messageLogger = logger;
            this.architecture = runConfiguration.TargetPlatform;
            this.testHostProcess = null;

            this.Shared = !runConfiguration.DisableAppDomain;
            this.hostExitedEventRaised = false;
        }

        /// <inheritdoc/>
        public Task TerminateAsync(int processId, CancellationToken cancellationToken)
        {
            try
            {
                this.processHelper.TerminateProcess(processId);
            }
            catch (Exception ex)
            {
                EqtTrace.Warning("DefaultTestHostManager: Unable to terminate test host process: " + ex);
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Raises HostLaunched event
        /// </summary>
        /// <param name="e">hostprovider event args</param>
        private void OnHostLaunched(HostProviderEventArgs e)
        {
            this.HostLaunched.SafeInvoke(this, e, "HostProviderEvents.OnHostLaunched");
        }

        /// <summary>
        /// Raises HostExited event
        /// </summary>
        /// <param name="e">hostprovider event args</param>
        private void OnHostExited(HostProviderEventArgs e)
        {
            if (!this.hostExitedEventRaised)
            {
                this.hostExitedEventRaised = true;
                this.HostExited.SafeInvoke(this, e, "HostProviderEvents.OnHostError");
            }
        }

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
