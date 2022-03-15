// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

using Microsoft.TestPlatform.TestUtilities;

using Semver;

namespace Microsoft.TestPlatform.AcceptanceTests;

public sealed class TestPlatformCompatibilityDataSource : TestDataSource<RunnerInfo, VSTestConsoleInfo, TesthostInfo, MSTestInfo>
{
    private static XmlDocument? s_depsXml;
    private readonly string[] _runnerFrameworks;
    private readonly string[] _runnerVersions;
    private readonly string[] _hostFrameworks;
    private readonly string[] _adapterVersions;
    private readonly string[] _adapters;
    private readonly string[] _hostVersions;

    public TestPlatformCompatibilityDataSource(
        string runnerFrameworks = AcceptanceTestBase.DEFAULT_HOST_NETFX_AND_NET,
        string runnerVersions = AcceptanceTestBase.LATEST_TO_LEGACY,
        string hostFrameworks = AcceptanceTestBase.DEFAULT_HOST_NETFX_AND_NET,
        string hostVersions = AcceptanceTestBase.LATEST_TO_LEGACY,
        string adapterVersions = AcceptanceTestBase.LATESTPREVIEW_TO_LEGACY,
        string adapters = AcceptanceTestBase.MSTEST)
    {
        // TODO: We actually don't generate values to use different translation layers, because we don't have a good way to do
        // that right now. Translation layer is loaded directly into the acceptance test, and so we don't have easy way to substitute it.
        // I am keeping this source separate from vstest console compatibility data source, to be able to easily add this feature later.
        _runnerFrameworks = runnerFrameworks.Split(';');
        _runnerVersions = runnerVersions.Split(';');
        _hostFrameworks = hostFrameworks.Split(';');
        _hostVersions = hostVersions.Split(';');
        _adapterVersions = adapterVersions.Split(';');
        _adapters = adapters.Split(';');

        // Do not generate the data rows here, properties (e.g. DebugVSTestConsole) are not populated until after constructor is done.
    }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTesthost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool NoDefaultBreakpoints { get; set; } = true;

    /// <summary>
    /// Add run for in-process using the selected .NET Framework runners, and and all selected adapters.
    /// </summary>
    public bool WithInProcess { get; set; }

    public bool WithEveryVersionOfRunner { get; set; } = true;

    public bool WithEveryVersionOfHost { get; set; } = true;

    public bool WithEveryVersionOfAdapter { get; set; } = true;

    public bool WithOlderConfigurations { get; set; } = true;


    public string? BeforeFeature { get; set; }
    public string? AfterFeature { get; set; }
    public string? BeforeAdapterFeature { get; set; }
    public string? AfterAdapterFeature { get; set; }

    public override void CreateData(MethodInfo methodInfo)
    {
        var dataRows = new List<(RunnerInfo runnerInfo, VSTestConsoleInfo vstestConsoleInfo, TesthostInfo testhostInfo, MSTestInfo mstestInfo)>();

        if (WithEveryVersionOfRunner)
            AddEveryVersionOfRunner(dataRows);

        if (WithEveryVersionOfHost)
            AddEveryVersionOfHost(dataRows);

        if (WithEveryVersionOfAdapter)
            AddEveryVersionOfAdapter(dataRows);

        if (WithOlderConfigurations)
            AddOlderConfigurations(dataRows);

        if (WithInProcess)
            AddInProcess(dataRows);

        var c = dataRows.Count();

        if (BeforeFeature != null && AfterFeature != null)
        {
            throw new InvalidOperationException($"You cannot specify {nameof(BeforeFeature)} and {nameof(AfterFeature)} at the same time");
        }

        var minVersion = SemVersion.Parse("0.0.0-alpha.1");
        SemVersion? beforeVersion = null;
        SemVersion? afterVersion = null;
        if (BeforeFeature != null)
        {
            var feature = Features.Table[BeforeFeature];
            beforeVersion = SemVersion.Parse(feature.Version.TrimStart('v'));
        }
        if (AfterFeature != null)
        {
            var feature = Features.Table[AfterFeature];
            afterVersion = SemVersion.Parse(feature.Version.TrimStart('v'));
        }

        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
        // Run .NET Framework tests only on Windows.
        Func<string, bool> filter = tfm => isWindows || !tfm.StartsWith("net4");

        // TODO: maybe we should throw if we don't end up generating any data
        // because none of the versions match, or some other way to identify tests that will never run because they are very outdated.
        // We probably don't have that need right now, because legacy version is 15.x.x, which is very old, and we are still keeping
        // compatibility.

        foreach (var dataRow in dataRows)
        {
            AddData(dataRow.runnerInfo, dataRow.vstestConsoleInfo, dataRow.testhostInfo, dataRow.mstestInfo);
        }
    }

    private void AddInProcess(List<(RunnerInfo runnerInfo, VSTestConsoleInfo vstestConsoleInfo, TesthostInfo testhostInfo, MSTestInfo mstestInfo)> dataRows)
    {
        foreach (var runnerFramework in _runnerFrameworks)
        {
            if (!runnerFramework.StartsWith("net4"))
            {
                continue;
            }

            foreach (var runnerVersion in _runnerVersions)
            {
                foreach (var adapter in _adapters)
                {
                    foreach (var adapterVersion in _adapterVersions)
                    {
                        AddRow(dataRows, runnerVersion, runnerFramework, runnerVersion, runnerFramework, adapter, adapterVersion, inIsolation: false);
                    }
                }
            }
        }
    }

    private void AddOlderConfigurations(List<(RunnerInfo, VSTestConsoleInfo, TesthostInfo, MSTestInfo)> dataRows)
    {
        // Older configurations where the runner, host and adapter version are the same.
        // We already added the row where all are newest when adding combination with all runners.
        foreach (var runnerVersion in _runnerVersions.Skip(1))
        {
            foreach (var runnerFramework in _runnerFrameworks)
            {
                foreach (var hostFramework in _hostFrameworks)
                {
                    var isNetFramework = hostFramework.StartsWith("net4");
                    var hostVersion = runnerVersion;
                    foreach (var adapter in _adapters)
                    {
                        var adapterVersion = runnerVersion;
                        AddRow(dataRows, runnerVersion, runnerFramework, hostVersion, hostFramework, adapter, adapterVersion, inIsolation: true);
                    }
                }
            }
        }
    }

    private void AddEveryVersionOfAdapter(List<(RunnerInfo, VSTestConsoleInfo, TesthostInfo, MSTestInfo)> dataRows)
    {
        var runnerVersion = _runnerVersions[0];
        foreach (var runnerFramework in _runnerFrameworks)
        {
            foreach (var hostFramework in _hostFrameworks)
            {
                var isNetFramework = hostFramework.StartsWith("net4");
                // .NET Framework testhost ships with the runner, and the version from the
                // runner directory is always selected, otherwise select the newest version from _hostFrameworks.
                var hostVersion = isNetFramework ? runnerVersion : _hostVersions[0];
                foreach (var adapter in _adapters)
                {
                    // We already used the newest when adding combination with every runner
                    foreach (var adapterVersion in _adapterVersions.Skip(1))
                    {
                        AddRow(dataRows, runnerVersion, runnerFramework, hostVersion, hostFramework, adapter, adapterVersion, inIsolation: true);
                    }
                }
            }
        }
    }

    private void AddEveryVersionOfHost(List<(RunnerInfo, VSTestConsoleInfo, TesthostInfo, MSTestInfo)> dataRows)
    {
        var runnerVersion = _runnerVersions[0];

        foreach (var runnerFramework in _runnerFrameworks)
        {
            foreach (var hostFramework in _hostFrameworks)
            {
                var isNetFramework = hostFramework.StartsWith("net4");
                // .NET Framework testhost ships with the runner, and the version from the
                // runner directory is always the same as the runner. There are no variations
                // so we just need to add host versions for .NET testhosts. We also skip the
                // newest version because we already added it when AddEveryVersionOfRunner
                var hostVersions = isNetFramework ? Array.Empty<string>() : _hostVersions.Skip(1).ToArray();
                foreach (var hostVersion in hostVersions)
                {
                    foreach (var adapter in _adapters)
                    {
                        // use the newest
                        var adapterVersion = _adapterVersions[0];
                        AddRow(dataRows, runnerVersion, runnerFramework, hostVersion, hostFramework, adapter, adapterVersion, inIsolation: true);
                    }
                }
            }
        }
    }

    private void AddEveryVersionOfRunner(List<(RunnerInfo, VSTestConsoleInfo, TesthostInfo, MSTestInfo)> dataRows)
    {
        foreach (var runnerVersion in _runnerVersions)
        {
            foreach (var runnerFramework in _runnerFrameworks)
            {
                foreach (var hostFramework in _hostFrameworks)
                {
                    var isNetFramework = hostFramework.StartsWith("net4");
                    // .NET Framework testhost ships with the runner, and the version from the
                    // runner directory is always selected, otherwise select the newest version from _hostFrameworks.
                    var hostVersion = isNetFramework ? runnerVersion : _hostVersions[0];
                    foreach (var adapter in _adapters)
                    {
                        // use the newest
                        var adapterVersion = _adapterVersions[0];
                        AddRow(dataRows, runnerVersion, runnerFramework, hostVersion, hostFramework, adapter, adapterVersion, inIsolation: true);
                    }
                }
            }
        }
    }

    private void AddRow(List<(RunnerInfo, VSTestConsoleInfo, TesthostInfo, MSTestInfo)> dataRows,
        string runnerVersion, string runnerFramework, string hostVersion, string hostFramework, string adapter, string adapterVersion, bool inIsolation)
    {
        RunnerInfo runnerInfo = GetRunnerInfo(runnerFramework, hostFramework, inIsolation);
        var vstestConsoleInfo = GetVSTestConsoleInfo(runnerVersion, runnerInfo);
        var testhostInfo = TesthostCompatibilityDataSource.GetTesthostInfo(hostVersion);
        var mstestInfo = GetMSTestInfo(adapterVersion);
        dataRows.Add(new(runnerInfo, vstestConsoleInfo, testhostInfo, mstestInfo));
    }

    private RunnerInfo GetRunnerInfo(string runnerFramework, string hostFramework, bool inIsolation)
    {
        return new RunnerInfo(runnerFramework, hostFramework, inIsolation ? AcceptanceTestBase.InIsolation : null,
            DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints);
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
    }

    private MSTestInfo GetMSTestInfo(string msTestVersion)
    {
        var depsXml = GetDependenciesXml();

        // It is okay when node is null, we check that Version has value when we update paths by using MSTestInfo, and throw.
        // This way it throws in the body of the test which has better error reporting than throwing in the data source.
        XmlNode? node = depsXml.DocumentElement?.SelectSingleNode($"PropertyGroup/MSTestFramework{msTestVersion}Version");
        var version = node?.InnerText.Replace("[", "").Replace("]", "");
        var slash = Path.DirectorySeparatorChar;
        var versionSpecificBinPath = $"{slash}bin{slash}MSTest{msTestVersion}-{version}{slash}";

        return new MSTestInfo(msTestVersion, version, versionSpecificBinPath);
    }

    internal static VSTestConsoleInfo GetVSTestConsoleInfo(string vstestConsoleVersion, RunnerInfo runnerInfo)
    {
        var depsXml = GetDependenciesXml();

        // When version is Latest, we built it locally, but it gets restored into our nuget cache on build
        // same as other versions, we just need to grab the version from a different property. 

        var propertyName = vstestConsoleVersion == AcceptanceTestBase.LATEST
            ? $"NETTestSdkVersion"
            : $"VSTestConsole{vstestConsoleVersion}Version";

        var packageName = runnerInfo.IsNetFrameworkRunner
            ? "microsoft.testplatform"
            : "microsoft.testplatform.cli";

        // It is okay when node is null, we will fail to find the executable later, and throw.
        // This way it throws in the body of the test which has better error reporting than throwing in the data source.
        // And we can easily find out what is going on because --WRONG-VERSION-- sticks out, and is easy to find in the codebase.
        XmlNode? node = depsXml.DocumentElement?.SelectSingleNode($"PropertyGroup/{propertyName}");
        var version = node?.InnerText.Replace("[", "").Replace("]", "") ?? "--WRONG-VERSION--";
        var vstestConsolePath = runnerInfo.IsNetFrameworkRunner
            ? Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "packages", packageName, version,
                "tools", "net451", "Common7", "IDE", "Extensions", "TestPlatform", "vstest.console.exe")
            : Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "packages", packageName, version,
                "contentFiles", "any", "netcoreapp2.1", "vstest.console.dll");

        if (version.StartsWith("15."))
        {
            vstestConsolePath = vstestConsolePath.Replace("netcoreapp2.1", "netcoreapp2.0");
        }

        return new VSTestConsoleInfo(vstestConsoleVersion, version, vstestConsolePath);
    }

    private static XmlDocument GetDependenciesXml()
    {
        if (s_depsXml != null)
            return s_depsXml;

        var depsXmlPath = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "scripts", "build", "TestPlatform.Dependencies.props");
        var fileStream = File.OpenRead(depsXmlPath);
        var xmlTextReader = new XmlTextReader(fileStream) { Namespaces = false };
        var depsXml = new XmlDocument();
        depsXml.Load(xmlTextReader);

        s_depsXml = depsXml;
        return depsXml;
    }
}
