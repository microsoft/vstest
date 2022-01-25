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
        private readonly IProcessHelper processHelper;
        private readonly IFileHelper fileHelper;
        private readonly IEnvironment environment;
        private readonly IDotnetHostHelper dotnetHostHelper;

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
        private Action<object> ExitCallBack => (process) => TestHostManagerCallbacks.ExitCallBack(processHelper, process, testHostProcessStdError, OnHostExited);

        /// <summary>
        /// Gets callback to read from process error stream
        /// </summary>
        private Action<object, string> ErrorReceivedCallback => (process, data) => TestHostManagerCallbacks.ErrorReceivedCallback(testHostProcessStdError, data);

        /// <inheritdoc/>
        public void SetCustomLauncher(ITestHostLauncher customLauncher)
        {
            customTestHostLauncher = customLauncher;
        }

        /// <inheritdoc/>
        public TestHostConnectionInfo GetTestHostConnectionInfo()
        {
            return new TestHostConnectionInfo { Endpoint = "127.0.0.1:0", Role = ConnectionRole.Client, Transport = Transport.Sockets };
        }

        /// <inheritdoc/>
        public async Task<bool> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
        {
            return await Task.Run(() => LaunchHost(testHostStartInfo, cancellationToken), cancellationToken);
        }

        /// <inheritdoc/>
        public virtual TestProcessStartInfo GetTestHostProcessStartInfo(
            IEnumerable<string> sources,
            IDictionary<string, string> environmentVariables,
            TestRunnerConnectionInfo connectionInfo)
        {
            string testHostProcessName;
            if (targetFramework.Name.StartsWith(".NETFramework,Version=v"))
            {
                var targetFrameworkMoniker = "net" + targetFramework.Name.Replace(".NETFramework,Version=v", string.Empty).Replace(".", string.Empty);

                // Net451 or older will use the default testhost.exe that is compiled against net451.
                var isSupportedNetTarget = new[] { "net452", "net46", "net461", "net462", "net47", "net471", "net472", "net48" }.Contains(targetFrameworkMoniker);
                var targetFrameworkSuffix = isSupportedNetTarget ? $".{targetFrameworkMoniker}" : string.Empty;

                // Default test host manager supports shared test sources
                testHostProcessName = string.Format(architecture == Architecture.X86 ? X86TestHostProcessName : X64TestHostProcessName, targetFrameworkSuffix);
            }
            else
            {
                // This path is probably happening only in our tests, because otherwise we are first running CanExecuteCurrentRunConfiguration
                // which would disqualify anything that is not netframework.
                testHostProcessName = string.Format(architecture == Architecture.X86 ? X86TestHostProcessName : X64TestHostProcessName, string.Empty);
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

            if (!Shared)
            {
                // Not sharing the host which means we need to pass the test assembly path as argument
                // so that the test host can create an appdomain on startup (Main method) and set appbase
                argumentsString += " --testsourcepath " + sources.FirstOrDefault().AddDoubleQuote();
            }

            EqtTrace.Verbose("DefaultTestHostmanager: Full path of {0} is {1}", testHostProcessName, testhostProcessPath);

            var launcherPath = testhostProcessPath;
            if (!environment.OperatingSystem.Equals(PlatformOperatingSystem.Windows) &&
                !processHelper.GetCurrentProcessFileName().EndsWith(DotnetHostHelper.MONOEXENAME, StringComparison.OrdinalIgnoreCase))
            {
                launcherPath = dotnetHostHelper.GetMonoPath();
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
                extensions = extensions.Concat(sources.SelectMany(s => fileHelper.EnumerateFiles(Path.GetDirectoryName(s), SearchOption.TopDirectoryOnly, TestAdapterEndsWithPattern)));
            }

            extensions = FilterExtensionsBasedOnVersion(extensions);

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
                List<string> actualSources = new();
                foreach (var uwpSource in uwpSources)
                {
                    actualSources.Add(Path.Combine(Path.GetDirectoryName(uwpSource), GetUwpSources(uwpSource)));
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
            return framework.Name.IndexOf("NETFramework", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <inheritdoc/>
        public void Initialize(IMessageLogger logger, string runsettingsXml)
        {
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);

            messageLogger = logger;
            architecture = runConfiguration.TargetPlatform;
            targetFramework = runConfiguration.TargetFramework;
            testHostProcess = null;

            Shared = !runConfiguration.DisableAppDomain;
            hostExitedEventRaised = false;
        }

        /// <inheritdoc/>
        public Task CleanTestHostAsync(CancellationToken cancellationToken)
        {
            try
            {
                processHelper.TerminateProcess(testHostProcess);
            }
            catch (Exception ex)
            {
                EqtTrace.Warning("DefaultTestHostManager: Unable to terminate test host process: " + ex);
            }

            testHostProcess?.Dispose();

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public bool AttachDebuggerToTestHost()
        {
            return customTestHostLauncher is ITestHostLauncher2 launcher
&& launcher.AttachDebuggerToProcess(testHostProcess.Id);
        }

        /// <summary>
        /// Filter duplicate extensions, include only the highest versioned extension
        /// </summary>
        /// <param name="extensions">Entire list of extensions</param>
        /// <returns>Filtered list of extensions</returns>
        private IEnumerable<string> FilterExtensionsBasedOnVersion(IEnumerable<string> extensions)
        {
            Dictionary<string, string> selectedExtensions = new();
            Dictionary<string, Version> highestFileVersions = new();
            Dictionary<string, Version> conflictingExtensions = new();

            foreach (var extensionFullPath in extensions)
            {
                // assemblyName is the key
                var extensionAssemblyName = Path.GetFileNameWithoutExtension(extensionFullPath);

                if (selectedExtensions.TryGetValue(extensionAssemblyName, out var oldExtensionPath))
                {
                    // This extension is duplicate
                    var currentVersion = GetAndLogFileVersion(extensionFullPath);

                    var oldVersionFound = highestFileVersions.TryGetValue(extensionAssemblyName, out var oldVersion);
                    if (!oldVersionFound)
                    {
                        oldVersion = GetAndLogFileVersion(oldExtensionPath);
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
                messageLogger.SendMessage(TestMessageLevel.Warning, message);
            }

            return selectedExtensions.Values;
        }

        private Version GetAndLogFileVersion(string path)
        {
            var fileVersion = fileHelper.GetFileVersion(path);
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
            HostLaunched.SafeInvoke(this, e, "HostProviderEvents.OnHostLaunched");
        }

        /// <summary>
        /// Raises HostExited event
        /// </summary>
        /// <param name="e">host provider event args</param>
        private void OnHostExited(HostProviderEventArgs e)
        {
            if (!hostExitedEventRaised)
            {
                hostExitedEventRaised = true;
                HostExited.SafeInvoke(this, e, "HostProviderEvents.OnHostExited");
            }
        }

        private bool LaunchHost(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
        {
            testHostProcessStdError = new StringBuilder(0, CoreUtilities.Constants.StandardErrorMaxLength);
            EqtTrace.Verbose("Launching default test Host Process {0} with arguments {1}", testHostStartInfo.FileName, testHostStartInfo.Arguments);

            // We launch the test host process here if we're on the normal test running workflow.
            // If we're debugging and we have access to the newest version of the testhost launcher
            // interface we launch it here as well, but we expect to attach later to the test host
            // process by using its PID.
            // For every other workflow (e.g.: profiling) we ask the IDE to launch the custom test
            // host for us. In the profiling case this is needed because then the IDE sets some
            // additional environmental variables for us to help with probing.
            if ((customTestHostLauncher == null)
                || (customTestHostLauncher.IsDebug
                    && customTestHostLauncher is ITestHostLauncher2))
            {
                EqtTrace.Verbose("DefaultTestHostManager: Starting process '{0}' with command line '{1}'", testHostStartInfo.FileName, testHostStartInfo.Arguments);
                cancellationToken.ThrowIfCancellationRequested();
                testHostProcess = processHelper.LaunchProcess(
                    testHostStartInfo.FileName,
                    testHostStartInfo.Arguments,
                    testHostStartInfo.WorkingDirectory,
                    testHostStartInfo.EnvironmentVariables,
                    ErrorReceivedCallback,
                    ExitCallBack,
                    null) as Process;
            }
            else
            {
                int processId = customTestHostLauncher.LaunchTestHost(testHostStartInfo, cancellationToken);
                testHostProcess = Process.GetProcessById(processId);
                processHelper.SetExitCallback(processId, ExitCallBack);
            }

            OnHostLaunched(new HostProviderEventArgs("Test Runtime launched", 0, testHostProcess.Id));
            return testHostProcess != null;
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