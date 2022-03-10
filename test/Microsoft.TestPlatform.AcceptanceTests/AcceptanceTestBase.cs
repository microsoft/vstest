// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.TestPlatform.TestUtilities;

#nullable disable

namespace Microsoft.TestPlatform.AcceptanceTests;

public class AcceptanceTestBase : IntegrationTestBase
{
    public const string Net451TargetFramework = "net451";
    public const string Net452TargetFramework = "net452";
    public const string Net46TargetFramework = "net46";
    public const string Net461TargetFramework = "net461";
    public const string Net462TargetFramework = "net462";
    public const string Net47TargetFramework = "net47";
    public const string Net471TargetFramework = "net471";
    public const string Net472TargetFramework = "net472";
    public const string Net48TargetFramework = "net48";
    public const string DesktopTargetFramework = "net451";
    public const string Core21TargetFramework = "netcoreapp2.1";
    public const string Core31TargetFramework = "netcoreapp3.1";
    public const string Core50TargetFramework = "net5.0";
    public const string Core60TargetFramework = "net6.0";

    public const string DesktopFrameworkArgValue = ".NETFramework,Version=v4.5.1";
    public const string Net451FrameworkArgValue = ".NETFramework,Version=v4.5.1";
    public const string Net452FrameworkArgValue = ".NETFramework,Version=v4.5.2";
    public const string Net46FrameworkArgValue = ".NETFramework,Version=v4.6";
    public const string Net461FrameworkArgValue = ".NETFramework,Version=v4.6.1";
    public const string Net462FrameworkArgValue = ".NETFramework,Version=v4.6.2";
    public const string Net47FrameworkArgValue = ".NETFramework,Version=v4.7";
    public const string Net471FrameworkArgValue = ".NETFramework,Version=v4.7.1";
    public const string Net472FrameworkArgValue = ".NETFramework,Version=v4.7.2";
    public const string Net48FrameworkArgValue = ".NETFramework,Version=v4.8";

    public const string Core21FrameworkArgValue = ".NETCoreApp,Version=v2.1";
    public const string Core31FrameworkArgValue = ".NETCoreApp,Version=v3.1";
    public const string Core50FrameworkArgValue = ".NETCoreApp,Version=v5.0";
    public const string Core60FrameworkArgValue = ".NETCoreApp,Version=v6.0";

    public const string DesktopRunnerTargetRuntime = "win7-x64";
    public const string CoreRunnerTargetRuntime = "";
    public const string InIsolation = "/InIsolation";
    
    public const string NETFX452_48 = "net452;net461;net472;net48";
    public const string NETFX451_48 = "net452;net461;net472;net48";
    public const string NETCORE21_50 = "netcoreapp2.1;netcoreapp3.1;net5.0";
    public const string NETFX452_NET50 = "net452;net461;net472;net48;netcoreapp2.1;netcoreapp3.1;net5.0";
    public const string NETFX452_NET31 = "net452;net461;net472;net48;netcoreapp2.1;netcoreapp3.1";
    public const string DEFAULT_RUNNER_NETFX = "net451";
    /// <summary>
    /// Our current defaults for .NET and .NET Framework.
    /// </summary>
    public const string DEFAULT_RUNNER_NETFX_AND_NET = $"{DEFAULT_RUNNER_NETFX};netcoreapp2.1"; 
    public const string DEFAULT_HOST_NETFX_AND_NET = "net451;netcoreapp2.1";
    public const string LATEST_LEGACY = "Latest;LatestPreview;LatestStable;RecentStable;MostDownloaded;PreviousStable;LegacyStable";
    public const string LATESTPREVIEW_LEGACY = "LatestPreview;LatestStable;RecentStable;MostDownloaded;PreviousStable;LegacyStable";
    public const string LATEST = "Latest";

    public static string And(string left, string right)
    {
        return string.Join(";", left, right);
    }

    protected string FrameworkArgValue => DeriveFrameworkArgValue(_testEnvironment);

    protected static void SetTestEnvironment(IntegrationTestEnvironment testEnvironment, RunnerInfo runnerInfo)
    {
        testEnvironment.RunnerFramework = runnerInfo.RunnerFramework;
        testEnvironment.TargetFramework = runnerInfo.TargetFramework;
        testEnvironment.InIsolationValue = runnerInfo.InIsolationValue;
        testEnvironment.DebugVSTestConsole = runnerInfo.DebugVSTestConsole;
        testEnvironment.DebugTesthost = runnerInfo.DebugTesthost;
        testEnvironment.DebugDataCollector = runnerInfo.DebugDataCollector;
        testEnvironment.NoDefaultBreakpoints = runnerInfo.NoDefaultBreakpoints;
    }

    protected static string DeriveFrameworkArgValue(IntegrationTestEnvironment testEnvironment)
        => testEnvironment.TargetFramework switch
        {
            Core21TargetFramework => Core21FrameworkArgValue,
            Core31TargetFramework => Core31FrameworkArgValue,
            Core50TargetFramework => Core50FrameworkArgValue,
            Core60TargetFramework => Core60FrameworkArgValue,
            Net451TargetFramework => Net451FrameworkArgValue,
            Net452TargetFramework => Net452FrameworkArgValue,
            Net46TargetFramework => Net46FrameworkArgValue,
            Net461TargetFramework => Net461FrameworkArgValue,
            Net462TargetFramework => Net462FrameworkArgValue,
            Net47TargetFramework => Net47FrameworkArgValue,
            Net471TargetFramework => Net471FrameworkArgValue,
            Net472TargetFramework => Net472FrameworkArgValue,
            Net48TargetFramework => Net48FrameworkArgValue,
            _ => throw new NotSupportedException($"{testEnvironment.TargetFramework} is not supported TargetFramework value."),
        };

    protected bool IsDesktopTargetFramework()
        => _testEnvironment.TargetFramework == DesktopTargetFramework;

    protected string GetTargetFramworkForRunsettings()
    {
        string targetFramework = _testEnvironment.TargetFramework == DesktopTargetFramework ? "Framework45" : "FrameworkCore10";

        return targetFramework;
    }

    /// <summary>
    /// Default RunSettings
    /// </summary>
    /// <returns></returns>
    public string GetDefaultRunSettings()
    {
        string runSettingsXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion>
                                        </RunConfiguration>
                                    </RunSettings>";
        return runSettingsXml;
    }
}
