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
    using Microsoft.Extensions.DependencyModel;
    using Microsoft.TestPlatform.TestHostProvider.Hosting;
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
        private Framework targetFramework;
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
            this.targetFramework = runConfiguration.TargetFramework;
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
            // TODO: Throw an exception when we get 0 or more than 1 source, that explains what happened, instead of .Single throwing a generic exception?
            var args = string.Empty;
            var sourcePath = sources.Single();
            var sourceFile = Path.GetFileNameWithoutExtension(sourcePath);
            var sourceDirectory = Path.GetDirectoryName(sourcePath);

            // Probe for runtime config and deps file for the test source
            var runtimeConfigPath = Path.Combine(sourceDirectory, string.Concat(sourceFile, ".runtimeconfig.json"));
            var runtimeConfigFound = false;
            if (this.fileHelper.Exists(runtimeConfigPath))
            {
                runtimeConfigFound = true;
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

            // Try find testhost.exe (or the architecture specific version). We ship those ngened executables for Windows because they have faster startup time. We ship them only for some platforms.
            // When user specified path to dotnet.exe don't try to find the exexutable, because we will always use the testhost.dll together with their dotnet.exe.
            bool testHostExeFound = false;
            if (!useCustomDotnetHostpath
                && this.platformEnvironment.OperatingSystem.Equals(PlatformOperatingSystem.Windows))
            {
                // testhost.exe is 64-bit and has no suffix other versions have architecture suffix.
                var exeName = this.architecture == Architecture.X64 || this.architecture == Architecture.Default || this.architecture == Architecture.AnyCPU
                    ? "testhost.exe"
                    : $"testhost.{this.architecture.ToString().ToLowerInvariant()}.exe";

                var fullExePath = Path.Combine(sourceDirectory, exeName);

                // check for testhost.exe in sourceDirectory. If not found, check in nuget folder.
                if (this.fileHelper.Exists(fullExePath))
                {
                    EqtTrace.Verbose($"DotnetTestHostManager: {exeName} found at path: " + fullExePath);
                    startInfo.FileName = fullExePath;
                    testHostExeFound = true;
                }
                else
                {
                    // Check if testhost.dll is found in nuget folder or next to the test.dll, and use that to locate testhost.exe that is in the build folder in the same Nuget package.
                    testHostPath = this.GetTestHostPath(runtimeConfigDevPath, depsFilePath, sourceDirectory);
                    if (!string.IsNullOrWhiteSpace(testHostPath) && testHostPath.IndexOf("microsoft.testplatform.testhost", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // testhost.dll is present in path {testHostNugetRoot}\lib\netcoreapp2.1\testhost.dll
                        // testhost.(x86).exe is present in location {testHostNugetRoot}\build\netcoreapp2.1\{x86/x64}\{testhost.x86.exe/testhost.exe}
                        var folderName = this.architecture == Architecture.X64 || this.architecture == Architecture.Default || this.architecture == Architecture.AnyCPU
                            ? Architecture.X64.ToString().ToLowerInvariant()
                            : this.architecture.ToString().ToLowerInvariant();

                        var testHostNugetRoot = new DirectoryInfo(testHostPath).Parent.Parent.Parent;

#if DOTNET_BUILD_FROM_SOURCE
                        var testHostExeNugetPath = Path.Combine(testHostNugetRoot.FullName, "build", "net6.0", folderName, exeName);
#else
                        var testHostExeNugetPath = Path.Combine(testHostNugetRoot.FullName, "build", "netcoreapp2.1", folderName, exeName);
#endif

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
                // We did not find testhost.exe, either it did not exist, or we are not on Windows, or the user forced a custom path to dotnet. So we will try
                // to find testhost.dll from the runtime config and deps.json.
                if (string.IsNullOrEmpty(testHostPath))
                {
                    testHostPath = this.GetTestHostPath(runtimeConfigDevPath, depsFilePath, sourceDirectory);
                }

                if (string.IsNullOrEmpty(testHostPath))
                {
                    // We still did not find testhost.dll. Try finding it next to vstest.console, (or in next to vstest.console ./TestHost for .NET Framework)
#if NETFRAMEWORK
                    var testHostNextToRunner = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "TestHost", "testhost.dll");
#else
                    var testHostNextToRunner = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "testhost.dll");
#endif
                    if (this.fileHelper.Exists(testHostNextToRunner))
                    {
                        EqtTrace.Verbose("DotnetTestHostManager: Found testhost.dll next to runner executable: {0}.", testHostNextToRunner);
                        testHostPath = testHostNextToRunner;

                        // Because we could not find the testhost based on the project reference, or next to the tested dll,
                        // it is most likely not referenced from it, or the .dll is unmanaged.
                        //
                        // Add additional deps, that describes the dependencies of testhost.dll, which will merge with deps.json
                        // if the process provided any, or will be used by itself if none was provided.
                        var testhostDeps = Path.Combine(Path.GetDirectoryName(testHostNextToRunner), "testhost.deps.json");
                        string argsToAdd = " --additional-deps " + testhostDeps.AddDoubleQuote();
                        args += argsToAdd;
                        EqtTrace.Verbose("DotnetTestHostmanager: Adding {0} in args", argsToAdd);

                        // Additional deps will contain relative paths, tell the process to search for the dlls also
                        // next to the testhost.dll. The additional deps file is specially crafted to keep all the
                        // .dlls in the root folder, by only referencing libraries, and setting the path to "/".
                        // Without this, e.g. using the normal deps.json that is generated when testhost.dll is built,
                        // dotnet would consider additional deps path as the root of a Nuget package source,
                        // and would try to locate the dlls in a more complicated folder structure, and would fail to
                        // find those dependencies.
                        //
                        // If they were in the base path (where the test dll is) it would work
                        // fine, because in base folder, dotnet searches directly in that folder, but not in probing paths.
                        var testHostProbingPath = Path.GetDirectoryName(testHostNextToRunner);
                        argsToAdd = " --additionalprobingpath " + testHostProbingPath.AddDoubleQuote();
                        args += argsToAdd;
                        EqtTrace.Verbose("DotnetTestHostmanager: Adding {0} in args", argsToAdd);

                        if (!runtimeConfigFound)
                        {
                            // When runtime config is not found, we don't know which version exactly should be selected for the runtime.
                            // This can happen when the test project is .NET (Core) but does not have EXE output type, or when the dll is native.
                            //
                            // When the project is .NET (Core) we can look at the TargetFramework and gather the rough version from there. We then
                            // provide a runtime config targetting that version. It rolls forward on the minor version by default, so the latest
                            // version that is present will be selected in that range. Same as if you had EXE and no special settings.
                            // E.g. the dll targets netcoreapp3.1, we get 3.1 from the attribute in the Dll, and provide testhost-3.1.runtimeconfig.json
                            // this will resolve to 3.1.17 runtime because that is the latest installed on the system.
                            //
                            //
                            // In the other case, where the Dll is native, we take the a runtime config that will roll forward to the latest version
                            // because we don't care on which version we will run, and rolling forward gives us the best chance of findind some runtime.
                            //
                            //
                            // There are 2 options how to provide the runtime version. Using --runtimeconfig, and --fx-version. The --fx-version does
                            // not roll forward even when the --roll-forward option is provided (or --roll-forward-on-no-candidate-fx for netcoreapp2.1)
                            // and we don't know the exact version we want to use. So the only option for us is to use the runtimeconfig.json.
                            //
                            //
                            // TODO: This version check is a hack, when the target framework is figured out it tries to unify to a single common framework
                            // even if there are incompatible frameworks (e.g any .NET Framwork assembly and any .NET (Core) assembly). Those incompatibilities
                            // will fall back to a common default framework. And that framework (stored in Framework.DefaultFramework) depends on compile time variables
                            // so depending on the version of vstest.console you are using, you will get a different value. This value for vstest.console.exe (under VS)
                            // is .NET Framework 4, but for vstest.console.dll (under dotnet test) is .NET Core 1.0. Those values are also valid values, so we have no idea
                            // if user actually provided a .NET Core 1.0 dll, or we are using fallback because we are running under vstest.console, and there is conflict,
                            // or if user provided native dll which does not have the attribute (that we read via PEReader).
                            //
                            // Another aspect of this is that we are unifying the dlls, so until we add per assembly data, this would be less accurate than using runtimeconfig.json
                            // but we can work around that by 1) changing how we schedule runners, to make sure we can process more that 1 type of assembly in vstest.console and
                            // 2) making sure we still make the project executable (and so we actually do get runtimeconfig unless the user tries hard to not make the test and EXE).
                            var suffix = this.targetFramework.Version == "1.0.0.0" ? "latest" : $"{new Version(this.targetFramework.Version).Major}.{new Version(this.targetFramework.Version).Minor}";
                            var testhostRuntimeConfig = Path.Combine(Path.GetDirectoryName(testHostNextToRunner), $"testhost-{suffix}.runtimeconfig.json");
                            argsToAdd = " --runtimeconfig " + testhostRuntimeConfig.AddDoubleQuote();
                            args += argsToAdd;
                            EqtTrace.Verbose("DotnetTestHostmanager: Adding {0} in args", argsToAdd);
                        }
                    }
                }

                if (string.IsNullOrEmpty(testHostPath))
                {
                    throw new TestPlatformException("Could not find testhost");
                }

                bool forceToX64 = ForceToX64();
                var currentProcessPath = this.processHelper.GetCurrentProcessFileName();
                if (useCustomDotnetHostpath)
                {
                    startInfo.FileName = this.dotnetHostPath;
                }

                // If already running with the dotnet executable and the architecture is compatible, use it; otherwise pick up the dotnet available on path.
                //
                // This allows us to pick up dotnet even when it is not present on PATH, or when we are running in custom
                // portable installation, and DOTNET_ROOT is overridden and MULTILEVEL_LOOKUP is set to 0, which would
                // normally prevent us from finding the dotnet executable.
                //
                // Wrap the paths with quotes in case dotnet executable is installed on a path with whitespace.
                else if ((currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase) ||
                         currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase)) &&
                         IsSameArchitecture(this.architecture, this.platformEnvironment.Architecture) &&
                         !forceToX64)
                {
                    EqtTrace.Verbose("DotnetTestHostmanager: valid skd architecture {0}", this.platformEnvironment.Architecture);
                    startInfo.FileName = currentProcessPath;
                }
                else
                {
                    EqtTrace.Verbose($"DotnetTestHostmanager: try to find valid dotnet muxer for the architecture {this.architecture}");
                    if (this.dotnetHostHelper.TryGetMuxerPath(
                        this.processHelper.GetProcessHandle(Process.GetCurrentProcess().Id),
                        TranslateToPlatformArchitecture(forceToX64 ? Architecture.X64 : this.architecture),
                        out string muxer))
                    {
                        startInfo.FileName = muxer;
                    }
                    else
                    {
                        throw new FileNotFoundException($"Could not find dotnet muxer for '{this.architecture}' architecture.");
                    }
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

            PlatformArchitecture TranslateToPlatformArchitecture(Architecture architecture)
            {
                switch (architecture)
                {
                    case Architecture.X86:
                        return PlatformArchitecture.X86;
                    case Architecture.X64:
                        return PlatformArchitecture.X64;
                    case Architecture.ARM:
                        return PlatformArchitecture.ARM;
                    case Architecture.ARM64:
                        return PlatformArchitecture.ARM64;
                    case Architecture.AnyCPU:
                    case Architecture.Default:
                    default:
                        break;
                }

                throw new TestPlatformException($"Invalid target architecture '{architecture}'");
            }

            bool IsSameArchitecture(Architecture targetArchitecture, PlatformArchitecture platformAchitecture)
            {
                switch (targetArchitecture)
                {
                    case Architecture.X86:
                        return platformAchitecture == PlatformArchitecture.X86;
                    case Architecture.X64:
                        return platformAchitecture == PlatformArchitecture.X64;
                    case Architecture.ARM:
                        return platformAchitecture == PlatformArchitecture.ARM;
                    case Architecture.ARM64:
                        return platformAchitecture == PlatformArchitecture.ARM64;
                    case Architecture.AnyCPU:
                    case Architecture.Default:
                    default:
                        break;
                }

                return false;
            }

            bool ForceToX64()
            {
                // We need to force x64 in some scenario
                // https://github.com/dotnet/sdk/blob/main/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.RuntimeIdentifierInference.targets#L140-L143

                // If we are running on an M1 with a native SDK and the TFM is < 6.0, we have to use a x64 apphost since there are no osx-arm64 apphosts previous to .NET 6.0.
                if (this.platformEnvironment.OperatingSystem == PlatformOperatingSystem.OSX &&
                    this.platformEnvironment.Architecture == PlatformArchitecture.ARM64 &&
                    new Version(this.targetFramework.Version).Major < 6)
                {
                    return true;
                }

                // If we are running on win-arm64 and the TFM is < 5.0, we have to use a x64 apphost since there are no win-arm64 apphosts previous to .NET 5.0.
                if (this.platformEnvironment.OperatingSystem == PlatformOperatingSystem.Windows &&
                    this.platformEnvironment.Architecture == PlatformArchitecture.ARM64 &&
                    new Version(this.targetFramework.Version).Major < 5)
                {
                    return true;
                }

                return false;
            }
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
                || framework.Name.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0
                || framework.Name.IndexOf("net5", StringComparison.OrdinalIgnoreCase) >= 0)
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
            && launcher.AttachDebuggerToProcess(this.testHostProcess.Id);
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
            string testHostPath = null;

            if (this.fileHelper.Exists(depsFilePath))
            {
                if (this.fileHelper.Exists(runtimeConfigDevPath))
                {
                    EqtTrace.Verbose("DotnetTestHostmanager: Reading file {0} to get path of testhost.dll", depsFilePath);

                    // Get testhost relative path
                    using (var stream = this.fileHelper.GetStream(depsFilePath, FileMode.Open, FileAccess.Read))
                    {
                        var context = new DependencyContextJsonReader().Read(stream);
                        var testhostPackage = context.RuntimeLibraries.FirstOrDefault(lib => lib.Name.Equals(testHostPackageName, StringComparison.OrdinalIgnoreCase));

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
                                EqtTrace.Verbose("DotnetTestHostmanager: Found testhost.dll in {0}", testHostFullPath);
                                return testHostFullPath;
                            }
                        }
                    }
                }
                else
                {
                    EqtTrace.Verbose("DotnetTestHostmanager: Runtimeconfig.dev.json {0} does not exist.", runtimeConfigDevPath);
                }
            }
            else
            {
                EqtTrace.Verbose("DotnetTestHostmanager: Deps file {0} does not exist.", depsFilePath);
            }

            // If we are here it means it couldn't resolve testhost.dll from nuget cache.
            // Try resolving testhost from output directory of test project. This is required if user has published the test project
            // and is running tests in an isolated machine. A second scenario is self test: test platform unit tests take a project
            // dependency on testhost (instead of nuget dependency), this drops testhost to output path.
            var testHostNextToTestProject = Path.Combine(sourceDirectory, "testhost.dll");
            if (this.fileHelper.Exists(testHostNextToTestProject))
            {
                EqtTrace.Verbose("DotnetTestHostManager: Found testhost.dll in source directory: {0}.", testHostNextToTestProject);
                return testHostNextToTestProject;
            }

            return testHostPath;
        }
    }
}
