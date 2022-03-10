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


namespace Microsoft.TestPlatform.AcceptanceTests;

public sealed class MSTestCompatibilityDataSource : TestDataSource<RunnerInfo, MSTestInfo>
{
    private static XmlDocument? _depsXml;
    private readonly string[] _runnerFrameworks;
    private readonly string[] _targetFrameworks;
    private readonly string[] _msTestVersions;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="targetFrameworks">To run tests with desktop runner(vstest.console.exe), use AcceptanceTestBase.Net452TargetFramework or alike values.</param>
    public MSTestCompatibilityDataSource(string runners = AcceptanceTestBase.DEFAULT_RUNNER_NETFX_AND_NET, string targetFrameworks = AcceptanceTestBase.DEFAULT_HOST_NETFX_AND_NET, string msTestVersions = AcceptanceTestBase.LATESTSTABLE_LEGACY)
    {
        _runnerFrameworks = runners.Split(';');
        _targetFrameworks = targetFrameworks.Split(';');
        _msTestVersions = msTestVersions.Split(';');

        // Do not generate the data rows here, properties (e.g. InProcess) are not populated until after constructor is done.
    }

    /// <summary>
    /// Add also run for in-process using the <see cref="AcceptanceTestBase.DEFAULT_NETFX"/> runner.
    /// </summary>
    // TODO: Can we somehow assert that we actually ran in process?
    public bool InProcess { get; set; }
    public bool DebugVSTestConsole { get; set; }
    public bool DebugTesthost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool NoDefaultBreakpoints { get; set; } = true;

    public override void CreateData(MethodInfo methodInfo)
    {
        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
        // Only run .NET Framework tests on Windows.
        Func<string, bool> filter = tfm => isWindows || !tfm.StartsWith("net4");

        if (InProcess)
        {
            foreach (var msTestVersion in _msTestVersions)
            {
                var runnerInfo = new RunnerInfo(AcceptanceTestBase.DEFAULT_RUNNER_NETFX, AcceptanceTestBase.DEFAULT_RUNNER_NETFX, inIsolation: null,
                    DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints);
                var msTestInfo = GetMSTestInfo(msTestVersion);
                // We run in the .NET Framework runner process, the runner and target framework must agree.
                AddData(runnerInfo, msTestInfo);
            }
        }

        foreach (var runner in _runnerFrameworks.Where(filter))
        {
            foreach (var fmw in _targetFrameworks.Where(filter))
            {
                foreach (var msTestVersion in _msTestVersions)
                {
                    var runnerInfo = new RunnerInfo(runner, fmw, AcceptanceTestBase.InIsolation,
                        DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints);
                    var msTestInfo = GetMSTestInfo(msTestVersion);

                    AddData(runnerInfo, msTestInfo);
                }
            }
        }
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
    }

    private MSTestInfo GetMSTestInfo(string msTestVersion)
    {
        // TODO: replacing in the result string is lame, but I am not going to fight 20 GetAssetFullPath method overloads right now
        // TODO: this could also be cached of course.

        var depsXml = GetDependenciesXml();

        // It is okay when node is null, we check that Version has value when we update paths by using MSTestInfo, and throw.
        // This way it throws in the body of the test which has better error reporting than throwing in the data source.
        XmlNode? node = depsXml.DocumentElement?.SelectSingleNode($"PropertyGroup/MSTestFramework{msTestVersion}Version");
        var version = node?.InnerText.Replace("[", "").Replace("]", "");
        var slash = Path.DirectorySeparatorChar;
        var versionSpecificBinPath = $"{slash}bin{slash}MSTest{msTestVersion}-{version}{slash}";

        return new MSTestInfo(msTestVersion, version, versionSpecificBinPath);
    }

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

