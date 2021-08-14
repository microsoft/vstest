// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.TestPlatform.TestHostProvider.Hosting;
    using Microsoft.TestPlatform.TestHostProvider.Resources;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.DesktopTestHostRuntimeProvider;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

    /// <summary>
    /// The default test host launcher for the engine.
    /// This works for Desktop local scenarios
    /// </summary>
    [ExtensionUri(DefaultTestHostUri)]
    [FriendlyName(DefaultTestHostFriendlyName)]
    public class DefaultTestHostManager : ITestRuntimeProvider2
    {
        private const string X64TestHostProcessName = "testhost{0}.exe";
        private const string X86TestHostProcessName = "testhost{0}.x86.exe";

        private const string DefaultTestHostUri = "HostProvider://DefaultTestHost";
        private const string DefaultTestHostFriendlyName = "DefaultTestHost";
        private const string TestAdapterEndsWithPattern = @"TestAdapter.dll";

        private Architecture architecture;
        private Framework targetFramework;
        private IProcessHelper processHelper;
        private IFileHelper fileHelper;
        private IEnvironment environment;
        private IDotnetHostHelper dotnetHostHelper;

        private ITestHostLauncher customTestHostLauncher;
        private Process testHostProcess;
        private StringBuilder testHostProcessStdError;
        private IMessageLogger messageLogger;
        private bool hostExitedEventRaised;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostManager"/> class.
        /// </summary>
        public DefaultTestHostManager()
            : this(new ProcessHelper(), new FileHelper(), new PlatformEnvironment(), new DotnetHostHelper())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTestHostManager"/> class.
        /// </summary>
        /// <param name="processHelper">Process helper instance.</param>
        /// <param name="fileHelper">File helper instance.</param>
        /// <param name="environment">Instance of platform environment.</param>
        /// <param name="dotnetHostHelper">Instance of dotnet host helper.</param>
        internal DefaultTestHostManager(IProcessHelper processHelper, IFileHelper fileHelper, IEnvironment environment, IDotnetHostHelper dotnetHostHelper)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.environment = environment;
            this.dotnetHostHelper = dotnetHostHelper;
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
        public TestHostConnectionInfo GetTestHostConnectionInfo()
        {
            return new TestHostConnectionInfo { Endpoint = "127.0.0.1:0", Role = ConnectionRole.Client, Transport = Transport.Sockets };
        }

        /// <inheritdoc/>
        public async Task<bool> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
        {
            return await Task.Run(() => this.LaunchHost(testHostStartInfo, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public virtual TestProcessStartInfo GetTestHostProcessStartInfo(
            IEnumerable<string> sources,
            IDictionary<string, string> environmentVariables,
            TestRunnerConnectionInfo connectionInfo)
        {
            string testHostProcessName;
            if (this.targetFramework.Name.StartsWith(".NETFramework,Version=v"))
            {
                var targetFrameworkMoniker = "net" + this.targetFramework.Name.Replace(".NETFramework,Version=v", string.Empty).Replace(".", string.Empty);

                // Net451 or older will use the default testhost.exe that is compiled against net451.
                var isSupportedNetTarget = new[] { "net452", "net46", "net461", "net462", "net47", "net471", "net472", "net48" }.Contains(targetFrameworkMoniker);
                var targetFrameworkSuffix = isSupportedNetTarget ? $".{targetFrameworkMoniker}" : string.Empty;

                // Default test host manager supports shared test sources
                testHostProcessName = string.Format(this.architecture == Architecture.X86 ? X86TestHostProcessName : X64TestHostProcessName, targetFrameworkSuffix);
            }
            else
            {
                // This path is probably happening only in our tests, because otherwise we are first running CanExecuteCurrentRunConfiguration
                // which would disqualify anything that is not netframework.
                testHostProcessName = string.Format(this.architecture == Architecture.X86 ? X86TestHostProcessName : X64TestHostProcessName, string.Empty);
            }

            var currentWorkingDirectory = Path.Combine(Path.GetDirectoryName(typeof(DefaultTestHostManager).GetTypeInfo().Assembly.Location), "..//");
            var argumentsString = " " + connectionInfo.ToCommandLineOptions();

            // check in current location for testhost exe
            var testhostProcessPath = Path.Combine(currentWorkingDirectory, testHostProcessName);

            if (!File.Exists(testhostProcessPath))
            {
                // "TestHost" is the name of the folder which contain Full CLR built testhost package assemblies.
                testHostProcessName = Path.Combine("TestHost", testHostProcessName);
                testhostProcessPath = Path.Combine(currentWorkingDirectory, testHostProcessName);
            }

            if (!this.Shared)
            {
                // Not sharing the host which means we need to pass the test assembly path as argument
                // so that the test host can create an appdomain on startup (Main method) and set appbase
                argumentsString += " --testsourcepath " + sources.FirstOrDefault().AddDoubleQuote();
            }

            EqtTrace.Verbose("DefaultTestHostmanager: Full path of {0} is {1}", testHostProcessName, testhostProcessPath);

            var launcherPath = testhostProcessPath;
            if (!this.environment.OperatingSystem.Equals(PlatformOperatingSystem.Windows) &&
                !this.processHelper.GetCurrentProcessFileName().EndsWith(DotnetHostHelper.MONOEXENAME, StringComparison.OrdinalIgnoreCase))
            {
                launcherPath = this.dotnetHostHelper.GetMonoPath();
                argumentsString = testhostProcessPath.AddDoubleQuote() + " " + argumentsString;
            }

            // For IDEs and other scenario, current directory should be the
            // working directory (not the vstest.console.exe location).
            // For VS - this becomes the solution directory for example
            // "TestResults" directory will be created at "current directory" of test host
            var processWorkingDirectory = Directory.GetCurrentDirectory();

            return new TestProcessStartInfo
            {
                FileName = launcherPath,
                Arguments = argumentsString,
                EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>(),
                WorkingDirectory = processWorkingDirectory
            };
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources, IEnumerable<string> extensions)
        {
            if (sources != null && sources.Any())
            {
                extensions = extensions.Concat(sources.SelectMany(s => this.fileHelper.EnumerateFiles(Path.GetDirectoryName(s), SearchOption.TopDirectoryOnly, TestAdapterEndsWithPattern)));
            }

            extensions = this.FilterExtensionsBasedOnVersion(extensions);

            return extensions;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetTestSources(IEnumerable<string> sources)
        {
            // We are doing this specifically for UWP, should we extract it out to some other utility?
            // Why? Lets say if we have to do same for some other source extension, would we just add another if check?
            var uwpSources = sources.Where(source => source.EndsWith(".appxrecipe", StringComparison.OrdinalIgnoreCase));

            if (uwpSources.Any())
            {
                List<string> actualSources = new List<string>();
                foreach (var uwpSource in uwpSources)
                {
                    actualSources.Add(Path.Combine(Path.GetDirectoryName(uwpSource), this.GetUwpSources(uwpSource)));
                }

                return actualSources;
            }

            return sources;
        }

        /// <inheritdoc/>
        public bool CanExecuteCurrentRunConfiguration(string runsettingsXml)
        {
            var config = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
            var framework = config.TargetFramework;

            // This is expected to be called once every run so returning a new instance every time.
            if (framework.Name.IndexOf("NETFramework", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public void Initialize(IMessageLogger logger, string runsettingsXml)
        {
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);

            this.messageLogger = logger;
            this.architecture = runConfiguration.TargetPlatform;
            this.targetFramework = runConfiguration.TargetFramework;
            this.testHostProcess = null;

            this.Shared = !runConfiguration.DisableAppDomain;
            this.hostExitedEventRaised = false;
        }

        /// <inheritdoc/>
        public Task CleanTestHostAsync(CancellationToken cancellationToken)
        {
            try
            {
                this.processHelper.TerminateProcess(this.testHostProcess);
            }
            catch (Exception ex)
            {
                EqtTrace.Warning("DefaultTestHostManager: Unable to terminate test host process: " + ex);
            }

            this.testHostProcess?.Dispose();

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public bool AttachDebuggerToTestHost()
        {
            return this.customTestHostLauncher is ITestHostLauncher2 launcher
                ? launcher.AttachDebuggerToProcess(this.testHostProcess.Id)
                : false;
        }

        /// <summary>
        /// Filter duplicate extensions, include only the highest versioned extension
        /// </summary>
        /// <param name="extensions">Entire list of extensions</param>
        /// <returns>Filtered list of extensions</returns>
        private IEnumerable<string> FilterExtensionsBasedOnVersion(IEnumerable<string> extensions)
        {
            Dictionary<string, string> selectedExtensions = new Dictionary<string, string>();
            Dictionary<string, Version> highestFileVersions = new Dictionary<string, Version>();
            Dictionary<string, Version> conflictingExtensions = new Dictionary<string, Version>();

            foreach (var extensionFullPath in extensions)
            {
                // assemblyName is the key
                var extensionAssemblyName = Path.GetFileNameWithoutExtension(extensionFullPath);

                if (selectedExtensions.TryGetValue(extensionAssemblyName, out var oldExtensionPath))
                {
                    // This extension is duplicate
                    var currentVersion = this.GetAndLogFileVersion(extensionFullPath);

                    var oldVersionFound = highestFileVersions.TryGetValue(extensionAssemblyName, out var oldVersion);
                    if (!oldVersionFound)
                    {
                        oldVersion = this.GetAndLogFileVersion(oldExtensionPath);
                    }

                    // If the version of current file is higher than the one in the map
                    // replace the older with the current file
                    if (currentVersion > oldVersion)
                    {
                        highestFileVersions[extensionAssemblyName] = currentVersion;
                        conflictingExtensions[extensionAssemblyName] = currentVersion;
                        selectedExtensions[extensionAssemblyName] = extensionFullPath;
                    }
                    else
                    {
                        if (currentVersion < oldVersion)
                        {
                            conflictingExtensions[extensionAssemblyName] = oldVersion;
                        }

                        if (!oldVersionFound)
                        {
                            highestFileVersions.Add(extensionAssemblyName, oldVersion);
                        }
                    }
                }
                else
                {
                    selectedExtensions.Add(extensionAssemblyName, extensionFullPath);
                }
            }

            // Log warning if conflicting version extensions are found
            if (conflictingExtensions.Any())
            {
                var extensionsString = string.Join("\n", conflictingExtensions.Select(kv => string.Format("  {0} : {1}", kv.Key, kv.Value)));
                string message = string.Format(CultureInfo.CurrentCulture, Resources.MultipleFileVersions, extensionsString);
                this.messageLogger.SendMessage(TestMessageLevel.Warning, message);
            }

            return selectedExtensions.Values;
        }

        private Version GetAndLogFileVersion(string path)
        {
            var fileVersion = this.fileHelper.GetFileVersion(path);
            if (EqtTrace.IsVerboseEnabled)
            {
                EqtTrace.Verbose("FileVersion for {0} : {1}", path, fileVersion);
            }

            return fileVersion;
        }

        /// <summary>
        /// Raises HostLaunched event
        /// </summary>
        /// <param name="e">host provider event args</param>
        private void OnHostLaunched(HostProviderEventArgs e)
        {
            this.HostLaunched.SafeInvoke(this, e, "HostProviderEvents.OnHostLaunched");
        }

        /// <summary>
        /// Raises HostExited event
        /// </summary>
        /// <param name="e">host provider event args</param>
        private void OnHostExited(HostProviderEventArgs e)
        {
            if (!this.hostExitedEventRaised)
            {
                this.hostExitedEventRaised = true;
                this.HostExited.SafeInvoke(this, e, "HostProviderEvents.OnHostExited");
            }
        }

        private void WarnIfDebugEnvironment()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSTEST_HOST_NATIVE_DEBUG")))
            {
                EqtTrace.Warning("VSTEST_HOST_NATIVE_DEBUG is set and will cause testhost to hang.");
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSTEST_HOST_DEBUG")))
            {
                EqtTrace.Warning("VSTEST_HOST_DEBUG is set and will cause testhost to hang.");
            }
        }

        private bool LaunchHost(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
        {
            this.testHostProcessStdError = new StringBuilder(0, CoreUtilities.Constants.StandardErrorMaxLength);
            EqtTrace.Verbose("Launching default test Host Process {0} with arguments {1}", testHostStartInfo.FileName, testHostStartInfo.Arguments);

            // within Visual Studio there is no information provided that
            // the debug environment vars are set. This will make sure the
            // log which is written before the call to the waiting testhost
            // will signal a hang is possible so at least the logs for
            // test platform can notify that this is happening
            this.WarnIfDebugEnvironment();

            // We launch the test host process here if we're on the normal test running workflow.
            // If we're debugging and we have access to the newest version of the testhost launcher
            // interface we launch it here as well, but we expect to attach later to the test host
            // process by using its PID.
            // For every other workflow (e.g.: profiling) we ask the IDE to launch the custom test
            // host for us. In the profiling case this is needed because then the IDE sets some
            // additional environmental variables for us to help with probing.
            if ((this.customTestHostLauncher == null)
                || (this.customTestHostLauncher.IsDebug
                    && this.customTestHostLauncher is ITestHostLauncher2))
            {
                EqtTrace.Verbose("DefaultTestHostManager: Starting process '{0}' with command line '{1}'", testHostStartInfo.FileName, testHostStartInfo.Arguments);
                cancellationToken.ThrowIfCancellationRequested();
                this.testHostProcess = this.processHelper.LaunchProcess(
                    testHostStartInfo.FileName,
                    testHostStartInfo.Arguments,
                    testHostStartInfo.WorkingDirectory,
                    testHostStartInfo.EnvironmentVariables,
                    this.ErrorReceivedCallback,
                    this.ExitCallBack,
                    null) as Process;
            }
            else
            {
                int processId = this.customTestHostLauncher.LaunchTestHost(testHostStartInfo, cancellationToken);
                this.testHostProcess = Process.GetProcessById(processId);
                this.processHelper.SetExitCallback(processId, this.ExitCallBack);
            }

            this.OnHostLaunched(new HostProviderEventArgs("Test Runtime launched", 0, this.testHostProcess.Id));
            return this.testHostProcess != null;
        }

        private string GetUwpSources(string uwpSource)
        {
            var doc = XDocument.Load(uwpSource);
            var ns = doc.Root.Name.Namespace;

            string appxManifestPath = doc.Element(ns + "Project").
                Element(ns + "ItemGroup").
                Element(ns + "AppXManifest").
                Attribute("Include").Value;

            if (!Path.IsPathRooted(appxManifestPath))
            {
                appxManifestPath = Path.Combine(Path.GetDirectoryName(uwpSource), appxManifestPath);
            }

            return AppxManifestFile.GetApplicationExecutableName(appxManifestPath);
        }
    }
}