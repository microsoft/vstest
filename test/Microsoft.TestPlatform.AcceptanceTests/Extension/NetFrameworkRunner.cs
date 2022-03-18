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
/// Runs tests using the dotnet vstest.console.dll built against .NET Core 2.1.
/// Provide a list of target frameworks to run the tests from given as a ';' separated list, or using a constant containing that range such as 
/// AcceptanceTestBase.NETFX452_NET50 = "net452;net472;net48;netcoreapp2.1;netcoreapp3.1;net5.0" to determine which target framework of the project
/// to test. The target project must list those TFMs in the TargetFrameworks property in csproj.
/// </summary>
public class NetFrameworkRunner : Attribute, ITestDataSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetCoreTargetFrameworkDataSource"/> class.
    /// </summary>
    /// <param name="targetFrameworks">To run tests with desktop runner(vstest.console.exe), use AcceptanceTestBase.Net452TargetFramework or alike values.</param>
    public NetFrameworkRunner(string targetFrameworks = AcceptanceTestBase.NETFX452_NET50)
    {
        var isWindows = Environment.OSVersion.Platform.ToString().StartsWith("Win");
        if (!isWindows)
        {
            return;
        }

        foreach (var fmw in targetFrameworks.Split(';'))
        {
            _dataRows.Add(new object[] { new RunnerInfo(IntegrationTestBase.DesktopRunnerFramework, fmw, AcceptanceTestBase.InIsolation) });
        }

    }

    private readonly List<object[]> _dataRows = new();

    public IEnumerable<object[]> GetData(MethodInfo methodInfo)
    {
        return _dataRows;
    }

    public string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));
    }
}
