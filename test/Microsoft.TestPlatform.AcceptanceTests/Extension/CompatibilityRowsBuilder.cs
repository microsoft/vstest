﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using Microsoft.TestPlatform.TestUtilities;

using Semver;

namespace Microsoft.TestPlatform.AcceptanceTests;

public class CompatibilityRowsBuilder
{
    private static XmlDocument? s_depsXml;
    private readonly string[] _runnerFrameworks;
    private readonly string[] _runnerVersions;
    private readonly string[] _hostFrameworks;
    private readonly string[] _adapterVersions;
    private readonly string[] _adapters;
    private readonly string[] _hostVersions;

    public CompatibilityRowsBuilder(string runnerFrameworks = AcceptanceTestBase.DEFAULT_HOST_NETFX_AND_NET,
    string runnerVersions = AcceptanceTestBase.LATEST_TO_LEGACY,
    string hostFrameworks = AcceptanceTestBase.DEFAULT_HOST_NETFX_AND_NET,
    string hostVersions = AcceptanceTestBase.LATEST_TO_LEGACY,
    string adapterVersions = AcceptanceTestBase.LATESTPREVIEW_TO_LEGACY,
    string adapters = AcceptanceTestBase.MSTEST)
    {
        _runnerFrameworks = runnerFrameworks.Split(';');
        _runnerVersions = runnerVersions.Split(';');
        _hostFrameworks = hostFrameworks.Split(';');
        _hostVersions = hostVersions.Split(';');
        _adapterVersions = adapterVersions.Split(';');
        _adapters = adapters.Split(';');
    }

    /// <summary>
    /// Add run for in-process using the selected .NET Framework runners, and and all selected adapters.
    /// </summary>
    public bool WithInProcess { get; set; } = true;
    public bool WithEveryVersionOfRunner { get; set; } = true;
    public bool WithEveryVersionOfHost { get; set; } = true;
    public bool WithEveryVersionOfAdapter { get; set; } = true;
    public bool WithOlderConfigurations { get; set; } = true;

    public string? BeforeFeature { get; set; }
    public string? AfterFeature { get; set; }
    public string? BeforeAdapterFeature { get; set; }
    public string? AfterAdapterFeature { get; set; }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTesthost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool NoDefaultBreakpoints { get; set; } = true;


    public List<RunnerInfo> CreateData()
    {
        var dataRows = new List<RunnerInfo>();

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

        var minVersion = SemVersion.Parse("0.0.0-alpha.1");
        var maxVersion = SemVersion.Parse("9999.0.0");
        SemVersion? beforeVersion = maxVersion;
        SemVersion? afterVersion = minVersion;
        SemVersion? beforeAdapterVersion = maxVersion;
        SemVersion? afterAdapterVersion = minVersion;

        if (BeforeFeature != null)
        {
            var feature = Features.TestPlatformFeatures[BeforeFeature];
            beforeVersion = SemVersion.Parse(feature.Version.TrimStart('v'));
        }

        if (AfterFeature != null)
        {
            var feature = Features.TestPlatformFeatures[AfterFeature];
            afterVersion = SemVersion.Parse(feature.Version.TrimStart('v'));
        }

        if (BeforeFeature != null)
        {
            var feature = Features.TestPlatformFeatures[BeforeFeature];
            beforeAdapterVersion = SemVersion.Parse(feature.Version.TrimStart('v'));
        }

        if (AfterAdapterFeature != null)
        {
            var feature = Features.AdapterFeatures[AfterAdapterFeature];
            afterAdapterVersion = SemVersion.Parse(feature.Version.TrimStart('v'));
        }

        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
        // Run .NET Framework tests only on Windows.
        Func<string, bool> filter = tfm => isWindows || !tfm.StartsWith("net4");

        // TODO: maybe we should throw if we don't end up generating any data
        // because none of the versions match, or some other way to identify tests that will never run because they are very outdated.
        // We probably don't have that need right now, because legacy version is 15.x.x, which is very old, and we are still keeping
        // compatibility.

        Func<SemVersion, SemVersion, SemVersion, bool> isInRange = (version, before, after) => version < before && after < version;

        var rows = dataRows.Where(r => r.VSTestConsoleInfo != null
            && isInRange(r.VSTestConsoleInfo.Version, beforeVersion, afterVersion)
            && r.DllInfos.All(d => d is NetTestSdkInfo ? isInRange(d.Version, beforeVersion, afterVersion) : isInRange(d.Version, beforeAdapterVersion, afterAdapterVersion))).ToList();

        if (rows.Count == 0)
        {
            // TODO: This needs to be way more specific about what happened.
            throw new InvalidOperationException("There were no rows that matched the specified criteria.");
        }

        return rows;
    }

    private void AddInProcess(List<RunnerInfo> dataRows)
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

    private void AddOlderConfigurations(List<RunnerInfo> dataRows)
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


    private void AddEveryVersionOfAdapter(List<RunnerInfo> dataRows)
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

    private void AddEveryVersionOfHost(List<RunnerInfo> dataRows)
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

    private void AddEveryVersionOfRunner(List<RunnerInfo> dataRows)
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

    private void AddRow(List<RunnerInfo> dataRows,
string runnerVersion, string runnerFramework, string hostVersion, string hostFramework, string adapter, string adapterVersion, bool inIsolation)
    {
        RunnerInfo runnerInfo = GetRunnerInfo(runnerFramework, hostFramework, inIsolation);
        runnerInfo.DebugInfo = GetDebugInfo();
        runnerInfo.VSTestConsoleInfo = GetVSTestConsoleInfo(runnerVersion, runnerInfo);

        // The order in which we add them matters. We end up both modifying the same path
        // and adding to it. So the first one added will be later in the path. E.g.:
        // Adding testSdk first:
        // C:\p\vstest\test\TestAssets\MSTestProject1\bin\MSTestLatestPreview-2.2.9-preview-20220210-07\NETTestSdkLatest-17.2.0-dev\Debug\net451\MSTestProject1.dll
        // versus adding testSdk second:
        // C:\p\vstest\test\TestAssets\MSTestProject1\bin\NETTestSdkLatest-17.2.0-dev\MSTestLatestPreview-2.2.9-preview-20220210-07\Debug\net451\MSTestProject1.dll
        runnerInfo.DllInfos.Add(GetMSTestInfo(adapterVersion));
        runnerInfo.DllInfos.Add(GetNetTestSdkInfo(hostVersion));
        dataRows.Add(runnerInfo);
    }

    private DebugInfo GetDebugInfo()
    {
        return new DebugInfo
        {
            DebugDataCollector = DebugDataCollector,
            DebugTesthost = DebugTesthost,
            DebugVSTestConsole = DebugVSTestConsole,
            NoDefaultBreakpoints = NoDefaultBreakpoints
        };
    }


    private RunnerInfo GetRunnerInfo(string runnerFramework, string hostFramework, bool inIsolation)
    {
        return new RunnerInfo
        {
            RunnerFramework = runnerFramework,
            TargetFramework = hostFramework,
            InIsolationValue = inIsolation ? AcceptanceTestBase.InIsolation : null
        };
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

    private static VSTestConsoleInfo GetVSTestConsoleInfo(string vstestConsoleVersion, RunnerInfo runnerInfo)
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

    private static NetTestSdkInfo GetNetTestSdkInfo(string testhostVersionType)
    {
        var depsXml = GetDependenciesXml();

        // When version is Latest, we built it locally, but it gets restored into our nuget cache on build
        // same as other versions, we just need to grab the version from a different property. 

        var propertyName = testhostVersionType == AcceptanceTestBase.LATEST
            ? $"NETTestSdkVersion"
            : $"VSTestConsole{testhostVersionType}Version";

        // It is okay when node is null, we check that Version has value when we update paths by using TesthostInfo, and throw.
        // This way it throws in the body of the test which has better error reporting than throwing in the data source.
        //
        // We use the VSTestConsole properties to figure out testhost version, for now.
        XmlNode? node = depsXml.DocumentElement?.SelectSingleNode($"PropertyGroup/{propertyName}");
        var version = node?.InnerText.Replace("[", "").Replace("]", "");
        var slash = Path.DirectorySeparatorChar;
        var versionSpecificBinPath = $"{slash}bin{slash}NETTestSdk{testhostVersionType}-{version}{slash}";

        return new NetTestSdkInfo(testhostVersionType, version, versionSpecificBinPath);
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

