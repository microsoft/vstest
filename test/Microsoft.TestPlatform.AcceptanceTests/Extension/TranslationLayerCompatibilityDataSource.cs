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

public sealed class TranslationLayerCompatibilityDataSource : TestDataSource<RunnerInfo, VSTestConsoleInfo>
{
    private static XmlDocument? s_depsXml;
    private readonly string[] _runnerFrameworks;
    private readonly string[] _targetFrameworks;
    // private readonly string[] _translationLayerVersions;
    private readonly string[] _vstestConsoleVersions;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="targetFrameworks">To run tests with desktop runner(vstest.console.exe), use AcceptanceTestBase.Net452TargetFramework or alike values.</param>
    public TranslationLayerCompatibilityDataSource(
        string runners = AcceptanceTestBase.DEFAULT_RUNNER_NETFX_AND_NET,
        string targetFrameworks = AcceptanceTestBase.DEFAULT_HOST_NETFX_AND_NET,
        string vstestConsoleVersions = AcceptanceTestBase.LATEST_TO_LEGACY)
    {
        // TODO: We actually don't generate values to use different translation layers, because we don't have a good way to do
        // that right now. Translation layer is loaded directly into the acceptance test, and so we don't have easy way to substitute it.
        // I am keeping this source separate from vstest console compatibility data source, to be able to easily add this feature later.
        _runnerFrameworks = runners.Split(';');
        _targetFrameworks = targetFrameworks.Split(';');
        _vstestConsoleVersions = vstestConsoleVersions.Split(';');

        // Do not generate the data rows here, properties (e.g. DebugVSTestConsole) are not populated until after constructor is done.
    }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTesthost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool NoDefaultBreakpoints { get; set; } = true;

    public string? BeforeFeature { get; set; }
    public string? AfterFeature { get; set; }

    public override void CreateData(MethodInfo methodInfo)
    {
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
        foreach (var runner in _runnerFrameworks.Where(filter))
        {
            foreach (var fmw in _targetFrameworks.Where(filter))
            {
                foreach (var vstestConsoleVersion in _vstestConsoleVersions)
                {
                    var runnerInfo = new RunnerInfo(runner, fmw, AcceptanceTestBase.InIsolation,
                        DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints);
                    var vsTestConsoleInfo = GetVSTestConsoleInfo(vstestConsoleVersion, runnerInfo);

                    if (beforeVersion != null && vsTestConsoleInfo.Version > beforeVersion)
                        continue;

                    if (afterVersion != null && vsTestConsoleInfo.Version < afterVersion)
                        continue;

                    AddData(runnerInfo, vsTestConsoleInfo);
                }
            }
        }
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
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

public readonly record struct Feature(string Version, string Issue);

public static class Features
{
    public const string ATTACH_DEBUGGER = nameof(ATTACH_DEBUGGER);

    public static Dictionary<string, Feature> Table { get; } = new Dictionary<string, Feature>
    {
        [ATTACH_DEBUGGER] = new(Version: "v16.7.0-preview-20200519-01", Issue: "https://github.com/microsoft/vstest/pull/2325")
    };
}
