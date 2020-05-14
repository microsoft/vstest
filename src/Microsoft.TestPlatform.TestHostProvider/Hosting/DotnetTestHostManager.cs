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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyModel;
    using Microsoft.TestPlatform.TestHostProvider.Hosting;
    using Microsoft.TestPlatform.TestHostProvider.Resources;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Helpers.Interfaces;
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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A host manager for <c>dotnet</c> core runtime.
    /// </summary>
    /// <remarks>
    /// Note that some functionality of this entity overlaps with that of <see cref="DefaultTestHostManager"/>. That is
    /// intentional since we want to move this to a separate assembly (with some runtime extensibility discovery).
    /// </remarks>
    [ExtensionUri(DotnetTestHostUri)]
    [FriendlyName(DotnetTestHostFriendlyName)]
    public class DotnetTestHostManager : ITestRuntimeProvider2
    {
        private const string DotnetTestHostUri = "HostProvider://DotnetTestHost";
        private const string DotnetTestHostFriendlyName = "DotnetTestHost";
        private const string TestAdapterRegexPattern = @"TestAdapter.dll";

        private IDotnetHostHelper dotnetHostHelper;
        private IEnvironment platformEnvironment;
        private IProcessHelper processHelper;

        private IFileHelper fileHelper;

        private ITestHostLauncher customTestHostLauncher;

        private Process testHostProcess;

        private StringBuilder testHostProcessStdError;

        private IMessageLogger messageLogger;

        private bool hostExitedEventRaised;

        private string hostPackageVersion = "15.0.0";

        private Architecture architecture;

        private bool isVersionCheckRequired = true;

        private string dotnetHostPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetTestHostManager"/> class.
        /// </summary>
        public DotnetTestHostManager()
            : this(new ProcessHelper(), new FileHelper(), new DotnetHostHelper(), new PlatformEnvironment())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotnetTestHostManager"/> class.
        /// </summary>
        /// <param name="processHelper">Process helper instance.</param>
        /// <param name="fileHelper">File helper instance.</param>
        /// <param name="dotnetHostHelper">DotnetHostHelper helper instance.</param>
        /// <param name="platformEnvironment">Platform Environment</param>
        internal DotnetTestHostManager(
            IProcessHelper processHelper,
            IFileHelper fileHelper,
            IDotnetHostHelper dotnetHostHelper,
            IEnvironment platformEnvironment)
        {
            this.processHelper = processHelper;
            this.fileHelper = fileHelper;
            this.dotnetHostHelper = dotnetHostHelper;
            this.platformEnvironment = platformEnvironment;
        }

        /// <inheritdoc />
        public event EventHandler<HostProviderEventArgs> HostLaunched;

        /// <inheritdoc />
        public event EventHandler<HostProviderEventArgs> HostExited;

        /// <summary>
        /// Gets a value indicating whether gets a value indicating if the test host can be shared for multiple sources.
        /// </summary>
        /// <remarks>
        /// Dependency resolution for .net core projects are pivoted by the test project. Hence each test
        /// project must be launched in a separate test host process.
        /// </remarks>
        public bool Shared => false;

        /// <summary>
        /// Gets a value indicating whether the test host supports protocol version check
        /// By default this is set to true. For host package version 15.0.0, this will be set to false;
        /// </summary>
        internal virtual bool IsVersionCheckRequired
        {
            get
            {
                return this.isVersionCheckRequired;
            }

            private set
            {
                this.isVersionCheckRequired = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the test host supports protocol version check
        /// </summary>
        internal bool MakeRunsettingsCompatible => this.hostPackageVersion.StartsWith("15.0.0-preview");

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
        public void Initialize(IMessageLogger logger, string runsettingsXml)
        {
            this.messageLogger = logger;
            this.hostExitedEventRaised = false;

            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
            this.architecture = runConfiguration.TargetPlatform;
            this.dotnetHostPath = runConfiguration.DotnetHostPath;
        }

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
            var startInfo = new TestProcessStartInfo();

            // .NET core host manager is not a shared host. It will expect a single test source to be provided.
            var args = string.Empty;
            var sourcePath = sources.Single();
            var sourceFile = Path.GetFileNameWithoutExtension(sourcePath);
            var sourceDirectory = Path.GetDirectoryName(sourcePath);

            // Probe for runtime config and deps file for the test source
            var runtimeConfigPath = Path.Combine(sourceDirectory, string.Concat(sourceFile, ".runtimeconfig.json"));
            if (this.fileHelper.Exists(runtimeConfigPath))
            {
                string argsToAdd = " --runtimeconfig " + runtimeConfigPath.AddDoubleQuote();
                args += argsToAdd;
                EqtTrace.Verbose("DotnetTestHostmanager: Adding {0} in args", argsToAdd);
            }
            else
            {
                EqtTrace.Verbose("DotnetTestHostmanager: File {0}, does not exist", runtimeConfigPath);
            }

            // Use the deps.json for test source
            var depsFilePath = Path.Combine(sourceDirectory, string.Concat(sourceFile, ".deps.json"));
            if (this.fileHelper.Exists(depsFilePath))
            {
                string argsToAdd = " --depsfile " + depsFilePath.AddDoubleQuote();
                args += argsToAdd;
                EqtTrace.Verbose("DotnetTestHostmanager: Adding {0} in args", argsToAdd);
            }
            else
            {
                EqtTrace.Verbose("DotnetTestHostmanager: File {0}, does not exist", depsFilePath);
            }

            var runtimeConfigDevPath = Path.Combine(sourceDirectory, string.Concat(sourceFile, ".runtimeconfig.dev.json"));
            string testHostPath = string.Empty;
            bool useCustomDotnetHostpath = !string.IsNullOrEmpty(this.dotnetHostPath);

            if (useCustomDotnetHostpath)
            {
                EqtTrace.Verbose("DotnetTestHostmanager: User specified custom path to dotnet host: '{0}'.", this.dotnetHostPath);
            }

            // If testhost.exe is available use it, unless user specified path to dotnet.exe, then we will use the testhost.dll
            bool testHostExeFound = false;
            if (!useCustomDotnetHostpath && this.platformEnvironment.OperatingSystem.Equals(PlatformOperatingSystem.Windows))
            {
                var exeName = this.architecture == Architecture.X86 ? "testhost.x86.exe" : "testhost.exe";
                var fullExePath = Path.Combine(sourceDirectory, exeName);

                // check for testhost.exe in sourceDirectory. If not found, check in nuget folder.
                if (this.fileHelper.Exists(fullExePath))
                {
                    EqtTrace.Verbose("DotnetTestHostManager: Testhost.exe/testhost.x86.exe found at path: " + fullExePath);
                    startInfo.FileName = fullExePath;
                    testHostExeFound = true;
                }
                else
                {
                    // Check if testhost.dll is found in nuget folder.
                    testHostPath = this.GetTestHostPath(runtimeConfigDevPath, depsFilePath, sourceDirectory);
                    if (testHostPath.IndexOf("microsoft.testplatform.testhost", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // testhost.dll is present in path {testHostNugetRoot}\lib\netcoreapp2.1\testhost.dll
                        // testhost.(x86).exe is present in location {testHostNugetRoot}\build\netcoreapp2.1\{x86/x64}\{testhost.x86.exe/testhost.exe}
                        var folderName = this.architecture == Architecture.X86 ? "x86" : "x64";
                        var testHostNugetRoot = new DirectoryInfo(testHostPath).Parent.Parent.Parent;
                        var testHostExeNugetPath = Path.Combine(testHostNugetRoot.FullName, "build", "netcoreapp2.1", folderName, exeName);

                        if (this.fileHelper.Exists(testHostExeNugetPath))
                        {
                            EqtTrace.Verbose("DotnetTestHostManager: Testhost.exe/testhost.x86.exe found at path: " + testHostExeNugetPath);
                            startInfo.FileName = testHostExeNugetPath;
                            testHostExeFound = true;
                        }
                    }
                }
            }

            if (!testHostExeFound)
            {
                if (string.IsNullOrEmpty(testHostPath))
                {
                    testHostPath = this.GetTestHostPath(runtimeConfigDevPath, depsFilePath, sourceDirectory);
                }

                var currentProcessPath = this.processHelper.GetCurrentProcessFileName();
                if (useCustomDotnetHostpath)
                {
                    startInfo.FileName = this.dotnetHostPath;
                }

                // This host manager can create process start info for dotnet core targets only.
                // If already running with the dotnet executable, use it; otherwise pick up the dotnet available on path.
                // Wrap the paths with quotes in case dotnet executable is installed on a path with whitespace.
                else if (currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
                   || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.FileName = currentProcessPath;
                }
                else
                {
                    startInfo.FileName = this.dotnetHostHelper.GetDotnetPath();
                }

                EqtTrace.Verbose("DotnetTestHostmanager: Full path of testhost.dll is {0}", testHostPath);
                args = "exec" + args;
                args += " " + testHostPath.AddDoubleQuote();
            }

            EqtTrace.Verbose("DotnetTestHostmanager: Full path of host exe is {0}", startInfo.FileName);

            args += " " + connectionInfo.ToCommandLineOptions();

            // Create a additional probing path args with Nuget.Client
            // args += "--additionalprobingpath xxx"
            // TODO this may be required in ASP.net, requires validation

            // Sample command line for the spawned test host
            // "D:\dd\gh\Microsoft\vstest\tools\dotnet\dotnet.exe" exec
            // --runtimeconfig G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.runtimeconfig.json
            // --depsfile G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.deps.json
            // --additionalprobingpath C:\Users\username\.nuget\packages\
            // G:\nuget-package-path\microsoft.testplatform.testhost\version\**\testhost.dll
            // G:\tmp\netcore-test\bin\Debug\netcoreapp1.0\netcore-test.dll
            startInfo.Arguments = args;
            startInfo.EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
            startInfo.WorkingDirectory = sourceDirectory;

            return startInfo;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources, IEnumerable<string> extensions)
        {
            List<string> extensionPaths = new List<string>();
            var sourceDirectory = Path.GetDirectoryName(sources.Single());

            if (!string.IsNullOrEmpty(sourceDirectory) && this.fileHelper.DirectoryExists(sourceDirectory))
            {
                extensionPaths.AddRange(this.fileHelper.EnumerateFiles(sourceDirectory, SearchOption.TopDirectoryOnly, TestAdapterRegexPattern));
            }

            return extensionPaths;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetTestSources(IEnumerable<string> sources)
        {
            // We do not have scenario where netcore tests are deployed to remote machine, so no need to update sources
            return sources;
        }

        /// <inheritdoc/>
        public bool CanExecuteCurrentRunConfiguration(string runsettingsXml)
        {
            var config = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
            var framework = config.TargetFramework;

            // This is expected to be called once every run so returning a new instance every time.
            if (framework.Name.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                || framework.Name.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
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
                EqtTrace.Warning("DotnetTestHostManager: Unable to terminate test host process: " + ex);
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
                EqtTrace.Verbose("DotnetTestHostManager.OnHostExited: invoking OnHostExited callback");
                this.HostExited.SafeInvoke(this, e, "HostProviderEvents.OnHostExited");
            }
            else
            {
                EqtTrace.Verbose("DotnetTestHostManager.OnHostExited: exit event was already raised, skipping");
            }
        }

        private bool LaunchHost(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
        {
            this.testHostProcessStdError = new StringBuilder(0, CoreUtilities.Constants.StandardErrorMaxLength);

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
                EqtTrace.Verbose("DotnetTestHostManager: Starting process '{0}' with command line '{1}'", testHostStartInfo.FileName, testHostStartInfo.Arguments);

                cancellationToken.ThrowIfCancellationRequested();
                this.testHostProcess = this.processHelper.LaunchProcess(testHostStartInfo.FileName, testHostStartInfo.Arguments, testHostStartInfo.WorkingDirectory, testHostStartInfo.EnvironmentVariables, this.ErrorReceivedCallback, this.ExitCallBack, null) as Process;
            }
            else
            {
                var processId = this.customTestHostLauncher.LaunchTestHost(testHostStartInfo, cancellationToken);
                this.testHostProcess = Process.GetProcessById(processId);
                this.processHelper.SetExitCallback(processId, this.ExitCallBack);
            }

            this.OnHostLaunched(new HostProviderEventArgs("Test Runtime launched", 0, this.testHostProcess.Id));

            return this.testHostProcess != null;
        }

        private string GetTestHostPath(string runtimeConfigDevPath, string depsFilePath, string sourceDirectory)
        {
            string testHostPackageName = "microsoft.testplatform.testhost";
            string testHostPath = string.Empty;
            string errorMessage = null;

            if (this.fileHelper.Exists(depsFilePath))
            {
                if (this.fileHelper.Exists(runtimeConfigDevPath))
                {
                    EqtTrace.Verbose("DotnetTestHostmanager: Reading file {0} to get path of testhost.dll", depsFilePath);

                    // Get testhost relative path
                    using (var stream = this.fileHelper.GetStream(depsFilePath, FileMode.Open, FileAccess.Read))
                    {
                        var context = new DependencyContextJsonReader().Read(stream);
                        var testhostPackage = context.RuntimeLibraries.Where(lib => lib.Name.Equals(testHostPackageName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                        if (testhostPackage != null)
                        {
                            foreach (var runtimeAssemblyGroup in testhostPackage.RuntimeAssemblyGroups)
                            {
                                foreach (var path in runtimeAssemblyGroup.AssetPaths)
                                {
                                    if (path.EndsWith("testhost.dll", StringComparison.OrdinalIgnoreCase))
                                    {
                                        testHostPath = path;
                                        break;
                                    }
                                }
                            }

                            testHostPath = Path.Combine(testhostPackage.Path, testHostPath);
                            this.hostPackageVersion = testhostPackage.Version;
                            this.IsVersionCheckRequired = !this.hostPackageVersion.StartsWith("15.0.0");
                            EqtTrace.Verbose("DotnetTestHostmanager: Relative path of testhost.dll with respect to package folder is {0}", testHostPath);
                        }
                    }

                    // Get probing path
                    using (StreamReader file = new StreamReader(this.fileHelper.GetStream(runtimeConfigDevPath, FileMode.Open, FileAccess.Read)))
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        JObject context = (JObject)JToken.ReadFrom(reader);
                        JObject runtimeOptions = (JObject)context.GetValue("runtimeOptions");
                        JToken additionalProbingPaths = runtimeOptions.GetValue("additionalProbingPaths");
                        foreach (var x in additionalProbingPaths)
                        {
                            EqtTrace.Verbose("DotnetTestHostmanager: Looking for path {0} in folder {1}", testHostPath, x.ToString());
                            string testHostFullPath;
                            try
                            {
                                testHostFullPath = Path.Combine(x.ToString(), testHostPath);
                            }
                            catch (ArgumentException)
                            {
                                // https://github.com/Microsoft/vstest/issues/847
                                // skip any invalid paths and continue checking the others
                                continue;
                            }

                            if (this.fileHelper.Exists(testHostFullPath))
                            {
                                return testHostFullPath;
                            }
                        }
                    }
                }
            }
            else
            {
                errorMessage = string.Format(CultureInfo.CurrentCulture, Resources.UnableToFindDepsFile, depsFilePath);
            }

            // If we are here it means it couldn't resolve testhost.dll from nuget cache.
            // Try resolving testhost from output directory of test project. This is required if user has published the test project
            // and is running tests in an isolated machine. A second scenario is self test: test platform unit tests take a project
            // dependency on testhost (instead of nuget dependency), this drops testhost to output path.
            testHostPath = Path.Combine(sourceDirectory, "testhost.dll");
            EqtTrace.Verbose("DotnetTestHostManager: Assume published test project, with test host path = {0}.", testHostPath);

            if (!this.fileHelper.Exists(testHostPath))
            {
                // If dependency file is not found, suggest adding Microsoft.Net.Test.Sdk reference to the project
                // Otherwise, suggest publishing the test project so that test host gets dropped next to the test source.
                errorMessage = errorMessage ?? string.Format(CultureInfo.CurrentCulture, Resources.SuggestPublishTestProject, testHostPath);
                throw new TestPlatformException(errorMessage);
            }

            return testHostPath;
        }
    }
}
