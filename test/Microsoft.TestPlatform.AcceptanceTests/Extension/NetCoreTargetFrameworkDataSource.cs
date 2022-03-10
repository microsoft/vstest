// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

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
    private readonly bool _useDesktopRunner;
    private readonly bool _useCoreRunner;
    private readonly bool _useNetCore21Target;
    private readonly bool _useNetCore31Target;

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
        _useDesktopRunner = useDesktopRunner;
        _useCoreRunner = useCoreRunner;
        _useNetCore21Target = useNetCore21Target;
        _useNetCore31Target = useNetCore31Target;
    }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTesthost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool NoDefaultBreakpoints { get; set; } = true;

    private void AddRunnerDataRow(List<object[]> dataRows, string runnerFramework, string targetFramework)
    {
        var runnerInfo = new RunnerInfo(runnerFramework, targetFramework, inIsolation: null, DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints);
        dataRows.Add(new object[] { runnerInfo });
    }

    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        var dataRows = new List<object[]>();
        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
        if (_useDesktopRunner && isWindows)
        {
            var runnerFramework = IntegrationTestBase.DesktopRunnerFramework;
            if (_useNetCore21Target)
            {
                AddRunnerDataRow(dataRows, runnerFramework, AcceptanceTestBase.Core21TargetFramework);
            }

            if (_useNetCore31Target)
            {
                AddRunnerDataRow(dataRows, runnerFramework, AcceptanceTestBase.Core31TargetFramework);
            }
        }

        if (_useCoreRunner)
        {
            var runnerFramework = IntegrationTestBase.CoreRunnerFramework;
            if (_useNetCore21Target)
            {
                AddRunnerDataRow(dataRows, runnerFramework, AcceptanceTestBase.Core21TargetFramework);
            }

            if (_useNetCore31Target)
            {
                AddRunnerDataRow(dataRows, runnerFramework, AcceptanceTestBase.Core31TargetFramework);
            }
        }

        return dataRows;
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
    }
}
