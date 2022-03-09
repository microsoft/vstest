// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Microsoft.TestPlatform.TestUtilities;

using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.AcceptanceTests;

/// <summary>
/// The attribute defining runner framework, target framework and target runtime for netcoreapp1.*
/// First Argument (Runner framework) = This decides who will run the tests. If runner framework is netcoreapp then "dotnet vstest.console.dll" will run the tests.
/// If runner framework is net46 then vstest.console.exe will run the tests.
/// Second argument (target framework) = The framework for which test will run
/// </summary>
public class NetCoreTargetFrameworkDataSource : Attribute, ITestDataSource
{
    private readonly List<object[]> _dataRows = new();
    /// <summary>
    /// Initializes a new instance of the <see cref="NetCoreTargetFrameworkDataSource"/> class.
    /// </summary>
    /// <param name="useDesktopRunner">To run tests with desktop runner(vstest.console.exe)</param>
    /// <param name="useCoreRunner">To run tests with core runner(dotnet vstest.console.dll)</param>
    public NetCoreTargetFrameworkDataSource(
        bool useDesktopRunner = true,
        // adding another runner is not necessary until we need to start building against another 
        // sdk, because the netcoreapp2.1 executable is forward compatible
        bool useCoreRunner = true,
        bool useNetCore21Target = true,
        // laying the ground work here for tests to be able to run against 3.1 but not enabling it for
        // all tests to avoid changing all acceptance tests right now
        bool useNetCore31Target = false)
    {
        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
        if (useDesktopRunner && isWindows)
        {
            var runnerFramework = IntegrationTestBase.DesktopRunnerFramework;
            if (useNetCore21Target)
            {
                AddRunnerDataRow(runnerFramework, AcceptanceTestBase.Core21TargetFramework);
            }

            if (useNetCore31Target)
            {
                AddRunnerDataRow(runnerFramework, AcceptanceTestBase.Core31TargetFramework);
            }
        }

        if (useCoreRunner)
        {
            var runnerFramework = IntegrationTestBase.CoreRunnerFramework;
            if (useNetCore21Target)
            {
                AddRunnerDataRow(runnerFramework, AcceptanceTestBase.Core21TargetFramework);
            }

            if (useNetCore31Target)
            {
                AddRunnerDataRow(runnerFramework, AcceptanceTestBase.Core31TargetFramework);
            }
        }
    }

    private void AddRunnerDataRow(string runnerFramework, string targetFramework)
    {
        var runnerInfo = new RunnerInfo(runnerFramework, targetFramework);
        _dataRows.Add(new object[] { runnerInfo });
    }

    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        return _dataRows;
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
    }
}

public class MSTestCompatibilityDataSource : Attribute, ITestDataSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetCoreTargetFrameworkDataSource"/> class.
    /// </summary>
    /// <param name="targetFrameworks">To run tests with desktop runner(vstest.console.exe), use AcceptanceTestBase.Net452TargetFramework or alike values.</param>
    public MSTestCompatibilityDataSource(string runners = AcceptanceTestBase.DEFAULT_NETFX_AND_NET, string targetFrameworks = AcceptanceTestBase.DEFAULT_NETFX_AND_NET, string msTestVersions = AcceptanceTestBase.LATESTSTABLE_LEGACY)
    {
        var runnersFrameworks = runners.Split(';');
        var testhostFrameworks = targetFrameworks.Split(';');
        var msTestVersionsToRun = msTestVersions.Split(';');

        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");

        // Only run .NET Framework tests on Windows.
        Func<string, bool> filter = tfm => isWindows || !tfm.StartsWith("net4");

        foreach (var runner in runnersFrameworks.Where(filter))
        {
            foreach (var fmw in testhostFrameworks.Where(filter))
            {
                foreach (var msTestVersion in msTestVersionsToRun)
                {
                    _dataRows.Add(new object[] { new RunnerInfo(runner, fmw), GetMSTestInfo(msTestVersion) });
                }
            }
        }
    }

    private readonly List<object[]> _dataRows = new();
    private static XmlDocument _depsXml;

    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        return _dataRows;
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

        XmlNode node = depsXml.DocumentElement.SelectSingleNode($"PropertyGroup/MSTestFramework{msTestVersion}Version");
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

