// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

using Microsoft.TestPlatform.TestUtilities;


namespace Microsoft.TestPlatform.AcceptanceTests;

public sealed class TranslationLayerCompatibilityDataSource : TestDataSource<RunnerInfo, VSTestConsoleInfo>
{
    private static XmlDocument? _depsXml;
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
       // string translationLayerVersions = AcceptanceTestBase.LATEST_LEGACY,
        string vstestConsoleVersions = AcceptanceTestBase.LATEST_LEGACY)
    {
        _runnerFrameworks = runners.Split(';');
        _targetFrameworks = targetFrameworks.Split(';');
       // _translationLayerVersions = translationLayerVersions.Split(';');
        _vstestConsoleVersions = vstestConsoleVersions.Split(';');

        // Do not generate the data rows here, properties (e.g. DebugVSTestConsole) are not populated until after constructor is done.
    }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTesthost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool NoDefaultBreakpoints { get; set; } = true;

    public override void CreateData(MethodInfo methodInfo)
    {
        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
        // Only run .NET Framework tests on Windows.
        Func<string, bool> filter = tfm => isWindows || !tfm.StartsWith("net4");


        foreach (var runner in _runnerFrameworks.Where(filter))
        {
            foreach (var fmw in _targetFrameworks.Where(filter))
            {
                foreach (var vstestConsoleVersion in _vstestConsoleVersions)
                {
                    var runnerInfo = new RunnerInfo(runner, fmw, AcceptanceTestBase.InIsolation,
                        DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints);
                    var vsTestConsoleInfo = GetVSTestConsoleInfo(vstestConsoleVersion, runnerInfo);

                    AddData(runnerInfo, vsTestConsoleInfo);
                }
            }
        }
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
    }

    private VSTestConsoleInfo GetVSTestConsoleInfo(string vstestConsoleVersion, RunnerInfo runnerInfo)
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

    //private MSTestInfo GetTranslationLayerInfo(string translationLayerVersion)
    //{
    //    // TODO: replacing in the result string is lame, but I am not going to fight 20 GetAssetFullPath method overloads right now
    //    // TODO: this could also be cached of course.

    //    var depsXml = GetDependenciesXml();

    //    // It is okay when node is null, we check that Version has value when we update paths by using MSTestInfo, and throw.
    //    // This way it throws in the body of the test which has better error reporting than throwing in the data source.
    //    XmlNode? node = depsXml.DocumentElement?.SelectSingleNode($"PropertyGroup/TranslationLayer{translationLayerVersion}Version");
    //    var version = node?.InnerText.Replace("[", "").Replace("]", "");
    //    var slash = Path.DirectorySeparatorChar;
    //    var dllPath = $"{slash}bin{slash}MSTest{translationLayerVersion}-{version}{slash}";

    //    return new TranslationLayerInfo(dllPath, version, versionSpecificBinPath);
    //}

    private static XmlDocument GetDependenciesXml()
    {
        if (_depsXml != null)
            return _depsXml;

        var depsXmlPath = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "scripts", "build", "TestPlatform.Dependencies.props");
        var fileStream = File.OpenRead(depsXmlPath);
        var xmlTextReader = new XmlTextReader(fileStream) { Namespaces = false };
        var depsXml = new XmlDocument();
        depsXml.Load(xmlTextReader);

        _depsXml = depsXml;
        return depsXml;
    }
}
