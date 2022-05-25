﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NuGet.Versioning;

using Microsoft.TestPlatform.TestUtilities;

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

    public string? BeforeRunnerFeature { get; set; }
    public string? AfterRunnerFeature { get; set; }
    public string? BeforeTestHostFeature { get; set; }
    public string? AfterTestHostFeature { get; set; }
    public string? BeforeAdapterFeature { get; set; }
    public string? AfterAdapterFeature { get; set; }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTestHost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool DebugStopAtEntrypoint { get; set; }
    public int? JustRow { get; internal set; }

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

        var minVersion = ParseAndPatchSemanticVersion("0.0.0-alpha.1");
        var maxVersion = ParseAndPatchSemanticVersion("9999.0.0");
        SemanticVersion? beforeRunnerVersion = maxVersion;
        SemanticVersion? afterRunnerVersion = minVersion;
        SemanticVersion? beforeTestHostVersion = maxVersion;
        SemanticVersion? afterTestHostVersion = minVersion;
        SemanticVersion? beforeAdapterVersion = maxVersion;
        SemanticVersion? afterAdapterVersion = minVersion;

        if (BeforeRunnerFeature != null)
        {
            var feature = Features.TestPlatformFeatures[BeforeRunnerFeature];
            beforeRunnerVersion = ParseAndPatchSemanticVersion(feature.Version);
        }

        if (AfterRunnerFeature != null)
        {
            var feature = Features.TestPlatformFeatures[AfterRunnerFeature];
            afterRunnerVersion = ParseAndPatchSemanticVersion(feature.Version);
        }

        if (BeforeTestHostFeature != null)
        {
            var feature = Features.TestPlatformFeatures[BeforeTestHostFeature];
            beforeTestHostVersion = ParseAndPatchSemanticVersion(feature.Version);
        }

        if (AfterTestHostFeature != null)
        {
            var feature = Features.TestPlatformFeatures[AfterTestHostFeature];
            afterTestHostVersion = ParseAndPatchSemanticVersion(feature.Version);
        }

        if (BeforeAdapterFeature != null)
        {
            var feature = Features.TestPlatformFeatures[BeforeAdapterFeature];
            beforeAdapterVersion = ParseAndPatchSemanticVersion(feature.Version);
        }

        if (AfterAdapterFeature != null)
        {
            var feature = Features.AdapterFeatures[AfterAdapterFeature];
            afterAdapterVersion = ParseAndPatchSemanticVersion(feature.Version);
        }

        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
        // Run .NET Framework tests only on Windows.
        Func<string, bool> filter = tfm => isWindows || !tfm.StartsWith("net4");

        // TODO: maybe we should throw if we don't end up generating any data
        // because none of the versions match, or some other way to identify tests that will never run because they are very outdated.
        // We probably don't have that need right now, because legacy version is 15.x.x, which is very old, and we are still keeping
        // compatibility.

        Func<SemanticVersion, SemanticVersion, SemanticVersion, bool> isInRange = (version, before, after) => version < before && after <= version;

        var rows = dataRows.Where(r => r.VSTestConsoleInfo != null
            && isInRange(ParseAndPatchSemanticVersion(r.VSTestConsoleInfo.Version), beforeRunnerVersion, afterRunnerVersion)
            && r.TestHostInfo != null && isInRange(ParseAndPatchSemanticVersion(r.TestHostInfo.Version), beforeTestHostVersion, afterTestHostVersion)
            && r.AdapterInfo != null && isInRange(ParseAndPatchSemanticVersion(r.AdapterInfo.Version), beforeAdapterVersion, afterAdapterVersion)).ToList();

        // We use ToString to determine which values are unique. Not great solution, but works better than using records.
        var distinctRows = new Dictionary<string, RunnerInfo>();
        rows.ForEach(r => distinctRows[r.ToString()] = r);

        if (distinctRows.Count == 0)
        {
            // TODO: This needs to be way more specific about what happened. And possibly propagate as inconclusive state if we decide to update versions automatically?
            throw new InvalidOperationException("There were no rows that matched the specified criteria.");
        }

        var allRows = distinctRows.Values.ToList();

        return JustRow == null ? allRows : new List<RunnerInfo> { allRows[JustRow.Value] };
    }

    private static SemanticVersion ParseAndPatchSemanticVersion(string? version)
    {
        // Our developer version is 17.2.0-dev, but we release few preview, that are named 17.2.0-preview or 17.2.0-release, yet we still
        // want 17.2.0-dev to be considered the latest version. So we patch it.
        var v = version != null && version.EndsWith("-dev") ? version?.Substring(0, version.Length - 4) + "-ZZZZZZZZZZ" : version;
        return SemanticVersion.Parse(v?.TrimStart('v'));
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
                        AddRow(dataRows, "In process", runnerVersion, runnerFramework, runnerVersion, runnerFramework, adapter, adapterVersion, inIsolation: false);
                    }
                }
            }
        }
    }

    private void AddOlderConfigurations(List<RunnerInfo> dataRows)
    {
        // Older configurations where the runner, host and adapter version are the same.
        // We already added the row where all are newest when adding combination with all runners.
        foreach (var runnerVersion in _runnerVersions)
        {
            foreach (var runnerFramework in _runnerFrameworks)
            {
                foreach (var hostFramework in _hostFrameworks)
                {
                    var isNetFramework = hostFramework.StartsWith("net4");
                    var hostVersion = runnerVersion;
                    foreach (var adapter in _adapters)
                    {
                        var adapterVersion = _adapterVersions[0];
                        AddRow(dataRows, "Older", runnerVersion, runnerFramework, hostVersion, hostFramework, adapter, adapterVersion, inIsolation: true);
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
                    foreach (var adapterVersion in _adapterVersions)
                    {
                        AddRow(dataRows, "Every adapter", runnerVersion, runnerFramework, hostVersion, hostFramework, adapter, adapterVersion, inIsolation: true);
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
                // so we just need to add host versions for .NET testhosts.
                var hostVersions = isNetFramework ? Array.Empty<string>() : _hostVersions.ToArray();
                foreach (var hostVersion in hostVersions)
                {
                    foreach (var adapter in _adapters)
                    {
                        // use the newest
                        var adapterVersion = _adapterVersions[0];
                        AddRow(dataRows, "Every host", runnerVersion, runnerFramework, hostVersion, hostFramework, adapter, adapterVersion, inIsolation: true);
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
                        AddRow(dataRows, "Every runner", runnerVersion, runnerFramework, hostVersion, hostFramework, adapter, adapterVersion, inIsolation: true);
                    }
                }
            }
        }
    }

    private void AddRow(List<RunnerInfo> dataRows, string batch,
        string runnerVersion, string runnerFramework, string hostVersion, string hostFramework, string adapter, string adapterVersion, bool inIsolation)
    {
        RunnerInfo runnerInfo = GetRunnerInfo(batch, runnerFramework, hostFramework, inIsolation);
        runnerInfo.DebugInfo = GetDebugInfo();
        runnerInfo.VSTestConsoleInfo = GetVSTestConsoleInfo(runnerVersion, runnerInfo);

        // The order in which we add them matters. We end up both modifying the same path
        // and adding to it. So the first one added will be later in the path. E.g.:
        // Adding testSdk first:
        // C:\p\vstest\test\TestAssets\MSTestProject1\bin\MSTestLatestPreview-2.2.9-preview-20220210-07\NETTestSdkLatest-17.2.0-dev\Debug\net451\MSTestProject1.dll
        // versus adding testSdk second:
        // C:\p\vstest\test\TestAssets\MSTestProject1\bin\NETTestSdkLatest-17.2.0-dev\MSTestLatestPreview-2.2.9-preview-20220210-07\Debug\net451\MSTestProject1.dll
        runnerInfo.TestHostInfo = GetNetTestSdkInfo(hostVersion);
        runnerInfo.AdapterInfo = GetMSTestInfo(adapterVersion);
        dataRows.Add(runnerInfo);
    }

    private DebugInfo GetDebugInfo()
    {
        return new DebugInfo
        {
            DebugDataCollector = DebugDataCollector,
            DebugTestHost = DebugTestHost,
            DebugVSTestConsole = DebugVSTestConsole,
            DebugStopAtEntrypoint = DebugStopAtEntrypoint
        };
    }

    private RunnerInfo GetRunnerInfo(string batch, string runnerFramework, string hostFramework, bool inIsolation)
    {
        return new RunnerInfo
        {
            Batch = batch,
            RunnerFramework = runnerFramework,
            TargetFramework = hostFramework,
            InIsolationValue = inIsolation ? AcceptanceTestBase.InIsolation : null
        };
    }

    private DllInfo GetMSTestInfo(string msTestVersion)
    {
        var depsXml = GetDependenciesXml();

        // It is okay when node is null, we check that Version has value when we update paths by using MSTestInfo, and throw.
        // This way it throws in the body of the test which has better error reporting than throwing in the data source.
        XmlNode? node = depsXml.DocumentElement?.SelectSingleNode($"PropertyGroup/MSTestFramework{msTestVersion}Version");
        var version = node?.InnerText.Replace("[", "").Replace("]", "");
        var slash = Path.DirectorySeparatorChar;
        var versionSpecificBinPath = $"{slash}bin{slash}MSTest{msTestVersion}-{version}{slash}";

        return new DllInfo
        {
            Name = "MSTest",
            PropertyName = "MSTest",
            VersionType = msTestVersion,
            Version = version,
            Path = versionSpecificBinPath,
        };
    }

    private static VSTestConsoleInfo GetVSTestConsoleInfo(string vstestConsoleVersion, RunnerInfo runnerInfo)
    {
        var depsXml = GetDependenciesXml();

        // When version is Latest, we built it locally, but it gets restored into our nuget cache on build
        // same as other versions, we just need to grab the version from a different property. 

        var propertyName = vstestConsoleVersion == AcceptanceTestBase.LATEST
            ? "NETTestSdkVersion"
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

        return new VSTestConsoleInfo
        {
            VersionType = vstestConsoleVersion,
            Version = version,
            Path = vstestConsolePath,
        };
    }

    private static NetTestSdkInfo GetNetTestSdkInfo(string testhostVersionType)
    {
        var depsXml = GetDependenciesXml();

        // When version is Latest, we built it locally, but it gets restored into our nuget cache on build
        // same as other versions, we just need to grab the version from a different property. 

        var propertyName = testhostVersionType == AcceptanceTestBase.LATEST
            ? "NETTestSdkVersion"
            : $"VSTestConsole{testhostVersionType}Version";

        // It is okay when node is null, we check that Version has value when we update paths by using TesthostInfo, and throw.
        // This way it throws in the body of the test which has better error reporting than throwing in the data source.
        //
        // We use the VSTestConsole properties to figure out testhost version, for now.
        XmlNode? node = depsXml.DocumentElement?.SelectSingleNode($"PropertyGroup/{propertyName}");
        var version = node?.InnerText.Replace("[", "").Replace("]", "");
        var slash = Path.DirectorySeparatorChar;
        var versionSpecificBinPath = $"{slash}bin{slash}NETTestSdk{testhostVersionType}-{version}{slash}";

        return new NetTestSdkInfo
        {
            VersionType = testhostVersionType,
            Version = version,
            Path = versionSpecificBinPath
        };
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

