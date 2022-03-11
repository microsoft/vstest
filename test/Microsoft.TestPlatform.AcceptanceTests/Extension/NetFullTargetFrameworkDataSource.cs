﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
/// The attribute defining runner framework and target framework for net451.
/// First Argument (Runner framework) = This decides who will run the tests. If runner framework is netcoreapp then "dotnet vstest.console.dll" will run the tests.
/// If runner framework is net46 then vstest.console.exe will run the tests.
/// Second argument (target framework) = The framework for which test will run
/// </summary>
public class NetFullTargetFrameworkDataSource : Attribute, ITestDataSource
{
    private readonly bool _inIsolation;
    private readonly bool _inProcess;
    private readonly bool _useDesktopRunner;
    private readonly bool _useCoreRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetFullTargetFrameworkDataSource"/> class.
    /// </summary>
    /// <param name="inIsolation">Run test in isolation</param>
    /// <param name="inProcess">Run tests in process</param>
    /// <param name="useDesktopRunner">To run tests with desktop runner(vstest.console.exe)</param>
    /// <param name="useCoreRunner">To run tests with core runner(dotnet vstest.console.dll)</param>
    public NetFullTargetFrameworkDataSource(bool inIsolation = true, bool inProcess = false, bool useDesktopRunner = true, bool useCoreRunner = true)
    {
        _inIsolation = inIsolation;
        _inProcess = inProcess;
        _useDesktopRunner = useDesktopRunner;
        _useCoreRunner = useCoreRunner;
    }

    public bool DebugVSTestConsole { get; set; }
    public bool DebugTesthost { get; set; }
    public bool DebugDataCollector { get; set; }
    public bool NoDefaultBreakpoints { get; set; } = true;

    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        var dataRows = new List<object[]>();
        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
        if (_useCoreRunner && isWindows)
        {
            dataRows.Add(new object[] { new RunnerInfo(IntegrationTestBase.CoreRunnerFramework, AcceptanceTestBase.DesktopTargetFramework, InIsolationValue: null, DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints) });
        }

        if (_useDesktopRunner && isWindows)
        {
            if (_inIsolation)
            {
                dataRows.Add(new object[] { new RunnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.DesktopTargetFramework, AcceptanceTestBase.InIsolation, DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints) });
            }

            if (_inProcess)
            {
                dataRows.Add(new object[] { new RunnerInfo(IntegrationTestBase.DesktopRunnerFramework, AcceptanceTestBase.DesktopTargetFramework, InIsolationValue: null, DebugVSTestConsole, DebugTesthost, DebugDataCollector, NoDefaultBreakpoints) });
            }
        }

        return dataRows;
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
    }
}
