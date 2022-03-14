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

public sealed class TesthostCompatibilityDataSource : TestDataSource<RunnerInfo, VSTestConsoleInfo, TesthostInfo>
{
    private static XmlDocument? s_depsXml;
    private readonly string[] _runnerFrameworks;
    private readonly string[] _targetFrameworks;
    private readonly string[] _testhostVersions;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="targetFrameworks">To run tests with desktop runner(vstest.console.exe), use AcceptanceTestBase.Net452TargetFramework or alike values.</param>
    public TesthostCompatibilityDataSource(string runners = AcceptanceTestBase.DEFAULT_RUNNER_NETFX_AND_NET, string targetFrameworks = AcceptanceTestBase.DEFAULT_HOST_NETFX_AND_NET,
        string runnerVersions = AcceptanceTestBase.LATESTPREVIEW_LEGACY, string testhostVersions = AcceptanceTestBase.LATESTPREVIEW_LEGACY)
    {
        _runnerFrameworks = runners.Split(';');
        _targetFrameworks = targetFrameworks.Split(';');
        _testhostVersions = testhostVersions.Split(';');

        // Do not generate the data rows here, properties (e.g. InProcess) are not populated until after constructor is done.
    }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTesthost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool NoDefaultBreakpoints { get; set; } = true;

    public override void CreateData(MethodInfo methodInfo)
    {
        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
        // Run .NET Framework tests only on Windows.
        Func<string, bool> filter = tfm => isWindows || tfm.StartsWith("net4");
        var onlyLatest = new[] { AcceptanceTestBase.LATEST };

        // Test different versions of vstest console together with latest testhost.
        // There is no point in proving that old testhost does not work with old vstest.console
        // when we can patch neither.
        foreach (var runner in _runnerFrameworks.Where(filter))
        {
            foreach (var fmw in _targetFrameworks.Where(filter))
            {
                // For .NET Framework generate only latest console with latest testhost,
                // we cannot control the version of testhost because it is shipped with the console.
                var testhostVersions = runner.StartsWith("net4") ? onlyLatest : _testhostVersions;
                foreach (var testhostVersion in testhostVersions)
                {
                    var runnerInfo = new RunnerInfo(runner, fmw, AcceptanceTestBase.InIsolation,
                        DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints);

                    var vstestConsoleInfo = TranslationLayerCompatibilityDataSource.GetVSTestConsoleInfo(AcceptanceTestBase.LATEST, runnerInfo);
                    var testhostInfo = GetTesthostInfo(testhostVersion);

                    AddData(runnerInfo, vstestConsoleInfo, testhostInfo);
                }
            }
        }

        // Test different versions of vstest console together with latest vstest.console.
        // There is no point in proving that old testhost does not work with old vstest.console
        // when we can patch neither.
        foreach (var runner in _runnerFrameworks.Where(filter))
        {
            foreach (var fmw in _targetFrameworks.Where(filter))
            {
                // For .NET Framework generate only latest console with latest testhost,
                // we cannot control the version of testhost because it is shipped with the console.
                var consoleVersions = runner.StartsWith("net4") ? onlyLatest : _testhostVersions;
                foreach (var consoleVersion in consoleVersions)
                {
                    var runnerInfo = new RunnerInfo(runner, fmw, AcceptanceTestBase.InIsolation,
                        DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints);

                    var vstestConsoleInfo = TranslationLayerCompatibilityDataSource.GetVSTestConsoleInfo(consoleVersion, runnerInfo);
                    // Generate only for latest testhsot, 
                    var testhostInfo = GetTesthostInfo(AcceptanceTestBase.LATEST);

                    AddData(runnerInfo, vstestConsoleInfo, testhostInfo);
                }
            }
        }
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
    }

    private TesthostInfo GetTesthostInfo(string testhostVersionType)
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

        return new TesthostInfo(testhostVersionType, version, versionSpecificBinPath);
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
