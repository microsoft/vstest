// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Hosting;

/// <summary>
/// The default test host launcher for the engine.
/// This works for Desktop local scenarios
/// </summary>
[ExtensionUri(DefaultTestHostUri)]
[FriendlyName(DefaultTestHostFriendlyName)]
public class DefaultTestHostManager : ITestRuntimeProvider2
{
    private const string DefaultTestHostUri = "HostProvider://DefaultTestHost";
    private const string DefaultTestHostFriendlyName = "DefaultTestHost";
    private const string TestAdapterEndsWithPattern = @"TestAdapter.dll";

    // Any version (older or newer) that is not in this list will use the default testhost.exe that is built using net451.
    // TODO: Add net481 when it is published, if it uses a new moniker.
    private static readonly ImmutableArray<string> SupportedTargetFrameworks = ImmutableArray.Create("net452", "net46", "net461", "net462", "net47", "net471", "net472", "net48");

    private Architecture _architecture;
    private Framework _targetFramework;
    private readonly IProcessHelper _processHelper;
    private readonly IFileHelper _fileHelper;
    private readonly IEnvironment _environment;
    private readonly IDotnetHostHelper _dotnetHostHelper;

    private ITestHostLauncher _customTestHostLauncher;
    private Process _testHostProcess;
    private StringBuilder _testHostProcessStdError;
    private IMessageLogger _messageLogger;
    private bool _hostExitedEventRaised;

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
        _processHelper = processHelper;
        _fileHelper = fileHelper;
        _environment = environment;
        _dotnetHostHelper = dotnetHostHelper;
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
    private Action<object> ExitCallBack => (process) => TestHostManagerCallbacks.ExitCallBack(_processHelper, process, _testHostProcessStdError, OnHostExited);

    /// <summary>
    /// Gets callback to read from process error stream
    /// </summary>
    private Action<object, string> ErrorReceivedCallback => (process, data) => TestHostManagerCallbacks.ErrorReceivedCallback(_testHostProcessStdError, data);

    /// <inheritdoc/>
    public void SetCustomLauncher(ITestHostLauncher customLauncher)
    {
        _customTestHostLauncher = customLauncher;
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
        string testHostProcessName = GetTestHostName(_architecture, _targetFramework, _processHelper.GetCurrentProcessArchitecture());

        var currentWorkingDirectory = Path.Combine(Path.GetDirectoryName(typeof(DefaultTestHostManager).GetTypeInfo().Assembly.Location), "..//");
        var argumentsString = " " + connectionInfo.ToCommandLineOptions();

        // check in current location for testhost exe
        var testhostProcessPath = Path.Combine(currentWorkingDirectory, testHostProcessName);

        if (!File.Exists(testhostProcessPath))
        {
            // "TestHost" is the name of the folder which contain Full CLR built testhost package assemblies, in dotnet SDK.
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
        if (!_environment.OperatingSystem.Equals(PlatformOperatingSystem.Windows) &&
            !_processHelper.GetCurrentProcessFileName().EndsWith(DotnetHostHelper.MONOEXENAME, StringComparison.OrdinalIgnoreCase))
        {
            launcherPath = _dotnetHostHelper.GetMonoPath();
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

    private string GetTestHostName(Architecture architecture, Framework targetFramework, PlatformArchitecture processArchitecture)
    {
        // We ship multiple executables for testhost that follow this naming schema:
        // testhost<.tfm><.architecture>.exe
        // e.g.: testhost.net472.x86.exe -> 32-bit testhost for .NET Framework 4.7.2
        //
        // The tfm is omitted for .NET Framework 4.5.1 testhost.
        // testhost.x86.exe -> 32-bit testhost for .NET Framework 4.5.1
        //
        // The architecture is omitted for 64-bit (x64) testhost.
        // testhost.net472.exe -> 64-bit testhost for .NET Framework 4.7.2
        // testhost.exe -> 64-bit testhost for .NET Framework 4.5.1
        //
        // These omissions are done for backwards compatibility because originally there were
        // only testhost.exe and testhost.x86.exe, both built against .NET Framework 4.5.1.

        StringBuilder testHostProcessName = new("testhost");

        if (targetFramework.Name.StartsWith(".NETFramework,Version=v"))
        {
            // Transform target framework name into moniker.
            // e.g. ".NETFramework,Version=v4.7.2" -> "net472".
            var targetFrameworkMoniker = "net" + targetFramework.Name.Replace(".NETFramework,Version=v", string.Empty).Replace(".", string.Empty);

            var isSupportedTargetFramework = SupportedTargetFrameworks.Contains(targetFrameworkMoniker);
            if (isSupportedTargetFramework)
            {
                testHostProcessName.Append('.').Append(targetFrameworkMoniker);
            }
            else
            {
                // The .NET Framework 4.5.1 testhost that does not have moniker in the name is used as fallback.
            }
        }

        var processArchitectureAsArchitecture = processArchitecture switch
        {
            PlatformArchitecture.X86 => Architecture.X86,
            PlatformArchitecture.X64 => Architecture.X64,
            PlatformArchitecture.ARM => Architecture.ARM,
            PlatformArchitecture.ARM64 => Architecture.ARM64,
            PlatformArchitecture.S390x => Architecture.S390x,
            _ => throw new NotSupportedException(),
        };

        // Default architecture, or AnyCPU architecture will use the architecture of the current process,
        // so when you run from 32-bit vstest.console, or from 32-bit dotnet test, you will get 32-bit testhost
        // as the preferred testhost.
        var actualArchitecture = architecture is Architecture.Default or Architecture.AnyCPU
            ? processArchitectureAsArchitecture
            : architecture;

        if (actualArchitecture != Architecture.X64)
        {
            // Append .<architecture> to the name, such as .x86. It is possible that we are not shipping the
            // executable for the architecture with VS, and that will fail later with file not found exception,
            // which is okay.
            testHostProcessName.Append('.').Append(architecture.ToString().ToLowerInvariant());
        }
        else
        {
            // 64-bit (x64) executable, uses no architecture suffix in the name.
            // E.g.: testhost.exe or testhost.net472.exe
        }

        testHostProcessName.Append(".exe");
        return testHostProcessName.ToString();
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources, IEnumerable<string> extensions)
    {
        if (sources != null && sources.Any())
        {
            extensions = extensions.Concat(sources.SelectMany(s => _fileHelper.EnumerateFiles(Path.GetDirectoryName(s), SearchOption.TopDirectoryOnly, TestAdapterEndsWithPattern)));
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

        _messageLogger = logger;
        _architecture = runConfiguration.TargetPlatform;
        _targetFramework = runConfiguration.TargetFramework;
        _testHostProcess = null;

        Shared = !runConfiguration.DisableAppDomain;
        _hostExitedEventRaised = false;
    }

    /// <inheritdoc/>
    public Task CleanTestHostAsync(CancellationToken cancellationToken)
    {
        try
        {
            _processHelper.TerminateProcess(_testHostProcess);
        }
        catch (Exception ex)
        {
            EqtTrace.Warning("DefaultTestHostManager: Unable to terminate test host process: " + ex);
        }

        _testHostProcess?.Dispose();

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public bool AttachDebuggerToTestHost()
    {
        return _customTestHostLauncher is ITestHostLauncher2 launcher
               && launcher.AttachDebuggerToProcess(_testHostProcess.Id);
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
            _messageLogger.SendMessage(TestMessageLevel.Warning, message);
        }

        return selectedExtensions.Values;
    }

    private Version GetAndLogFileVersion(string path)
    {
        var fileVersion = _fileHelper.GetFileVersion(path);
        EqtTrace.Verbose("FileVersion for {0} : {1}", path, fileVersion);

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
        if (!_hostExitedEventRaised)
        {
            _hostExitedEventRaised = true;
            HostExited.SafeInvoke(this, e, "HostProviderEvents.OnHostExited");
        }
    }

    private bool LaunchHost(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
    {
        _testHostProcessStdError = new StringBuilder(0, CoreUtilities.Constants.StandardErrorMaxLength);
        EqtTrace.Verbose("Launching default test Host Process {0} with arguments {1}", testHostStartInfo.FileName, testHostStartInfo.Arguments);

        // We launch the test host process here if we're on the normal test running workflow.
        // If we're debugging and we have access to the newest version of the testhost launcher
        // interface we launch it here as well, but we expect to attach later to the test host
        // process by using its PID.
        // For every other workflow (e.g.: profiling) we ask the IDE to launch the custom test
        // host for us. In the profiling case this is needed because then the IDE sets some
        // additional environmental variables for us to help with probing.
        if ((_customTestHostLauncher == null)
            || (_customTestHostLauncher.IsDebug
                && _customTestHostLauncher is ITestHostLauncher2))
        {
            EqtTrace.Verbose("DefaultTestHostManager: Starting process '{0}' with command line '{1}'", testHostStartInfo.FileName, testHostStartInfo.Arguments);
            cancellationToken.ThrowIfCancellationRequested();
            _testHostProcess = _processHelper.LaunchProcess(
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
            int processId = _customTestHostLauncher.LaunchTestHost(testHostStartInfo, cancellationToken);
            _testHostProcess = Process.GetProcessById(processId);
            _processHelper.SetExitCallback(processId, ExitCallBack);
        }

        OnHostLaunched(new HostProviderEventArgs("Test Runtime launched", 0, _testHostProcess.Id));
        return _testHostProcess != null;
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
