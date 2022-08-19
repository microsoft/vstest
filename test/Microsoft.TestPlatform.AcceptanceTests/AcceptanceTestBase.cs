﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;

using Microsoft.TestPlatform.TestUtilities;

namespace Microsoft.TestPlatform.AcceptanceTests;

public class AcceptanceTestBase : IntegrationTestBase
{
    public const string DesktopTargetFramework = Net462TargetFramework;
    public const string Net462TargetFramework = "net462";
    public const string Net47TargetFramework = "net47";
    public const string Net471TargetFramework = "net471";
    public const string Net472TargetFramework = "net472";
    public const string Net48TargetFramework = "net48";
    public const string Core31TargetFramework = "netcoreapp3.1";
    public const string Core50TargetFramework = "net5.0";
    public const string Core60TargetFramework = "net6.0";

    public const string DesktopFrameworkArgValue = Net462FrameworkArgValue;
    public const string Net462FrameworkArgValue = ".NETFramework,Version=v4.6.2";
    public const string Net47FrameworkArgValue = ".NETFramework,Version=v4.7";
    public const string Net471FrameworkArgValue = ".NETFramework,Version=v4.7.1";
    public const string Net472FrameworkArgValue = ".NETFramework,Version=v4.7.2";
    public const string Net48FrameworkArgValue = ".NETFramework,Version=v4.8";

    public const string Core31FrameworkArgValue = ".NETCoreApp,Version=v3.1";
    public const string Core50FrameworkArgValue = ".NETCoreApp,Version=v5.0";
    public const string Core60FrameworkArgValue = ".NETCoreApp,Version=v6.0";

    public const string DesktopRunnerTargetRuntime = "win7-x64";
    public const string CoreRunnerTargetRuntime = "";
    public const string InIsolation = "/InIsolation";

    public const string NETFX462_48 = "net462;net472;net48";
    public const string NETCORE21_50 = "netcoreapp3.1;net5.0";
    public const string NETFX462_NET50 = "net462;net472;net48;netcoreapp3.1;net5.0";
    public const string NETFX462_NET31 = "net462;net472;net48;netcoreapp3.1";
    public const string DEFAULT_RUNNER_NETFX = Net462TargetFramework;
    public const string DEFAULT_HOST_NETFX = Net462TargetFramework;
    public const string DEFAULT_RUNNER_NETCORE = Core31TargetFramework;
    public const string DEFAULT_HOST_NETCORE = Core31TargetFramework;
    /// <summary>
    /// Our current defaults for .NET and .NET Framework.
    /// </summary>
    public const string DEFAULT_RUNNER_NETFX_AND_NET = $"{DEFAULT_RUNNER_NETFX};{DEFAULT_RUNNER_NETCORE}";
    public const string DEFAULT_HOST_NETFX_AND_NET = "net462;netcoreapp3.1";
    public const string LATEST_TO_LEGACY = "Latest;LatestPreview;LatestStable;RecentStable;MostDownloaded;PreviousStable;LegacyStable";
    public const string LATESTPREVIEW_TO_LEGACY = "LatestPreview;LatestStable;RecentStable;MostDownloaded;PreviousStable;LegacyStable";
    public const string LATEST = "Latest";
    public const string LATESTSTABLE = "LatestStable";
    internal const string MSTEST = "MSTest";

    public static string And(string left, string right)
    {
        return string.Join(";", left, right);
    }

    protected string FrameworkArgValue => DeriveFrameworkArgValue(_testEnvironment);

    protected static void SetTestEnvironment(IntegrationTestEnvironment testEnvironment, RunnerInfo runnerInfo)
    {
        testEnvironment.VSTestConsoleInfo = runnerInfo.VSTestConsoleInfo;
        // The order here matters, it changes how the resulting path is built when we resolve test dlls and other assets.
        testEnvironment.DllInfos = new[] { runnerInfo.TestHostInfo, runnerInfo.AdapterInfo }.Where(d => d != null).Select(x => x!).ToList();
        testEnvironment.DebugInfo = runnerInfo.DebugInfo;

        testEnvironment.RunnerFramework = runnerInfo.RunnerFramework!;
        testEnvironment.TargetFramework = runnerInfo.TargetFramework;
        testEnvironment.InIsolationValue = runnerInfo.InIsolationValue;
    }

    protected static string DeriveFrameworkArgValue(IntegrationTestEnvironment testEnvironment)
        => testEnvironment.TargetFramework switch
        {
            Core31TargetFramework => Core31FrameworkArgValue,
            Core50TargetFramework => Core50FrameworkArgValue,
            Core60TargetFramework => Core60FrameworkArgValue,
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
    /// Empty runsettings, just with the RunSettings tag that we require.
    /// </summary>
    /// <returns></returns>
    public static string GetEmptyRunsettings()
    {
        return "<RunSettings></RunSettings>";
    }

    /// <summary>
    /// Almost empty runsettings, just specifying the target framework from the currently set test environment.
    /// </summary>
    public string GetDefaultRunSettings()
    {
        return GetRunSettingsWithTargetFramework(FrameworkArgValue);
    }

    /// <summary>
    /// Almost empty runsettings, just specifying the given target framework.
    /// Use the overload without any parameters to get the target framework from the currently set test environment.
    /// </summary>
    /// <returns></returns>
    public static string GetRunSettingsWithTargetFramework(string targetFramework)
    {
        string runSettingsXml =
            $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <RunSettings>
                <RunConfiguration>
                    <TargetFrameworkVersion>{targetFramework}</TargetFrameworkVersion>
                </RunConfiguration>
            </RunSettings>";

        return runSettingsXml;
    }

    protected string GetIsolatedTestAsset(string assetName)
    {
        var projectPath = GetProjectFullPath(assetName);

        foreach (var file in new FileInfo(projectPath).Directory!.EnumerateFiles())
        {
            var newFilePath = Path.Combine(TempDirectory.Path, file.Name);

            if (file.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                const string testAssetsProps = "TestAssets.props";
                const string relativePathToRoot = @"..\..\..";
                const string relativePathToBuild = relativePathToRoot + @"\scripts\build\";
                const string testAssetsPropsFullPath = relativePathToBuild + testAssetsProps;

                // Copy csproj and edit path to TestAssets.props
                var content = File.ReadAllText(file.FullName).Replace(testAssetsPropsFullPath, testAssetsProps);
                File.WriteAllText(newFilePath, content);

                // Copy TestAssets.props and update path to TestPlatform.Dependencies.props
                const string tpDependenciesPropsFileName = "TestPlatform.Dependencies.props";
                var assetPropsContent = File.ReadAllText(Path.Combine(file.DirectoryName!, testAssetsPropsFullPath))
                    .Replace("$(MSBuildThisFileDirectory)" + tpDependenciesPropsFileName, Path.Combine(file.DirectoryName!, relativePathToBuild, tpDependenciesPropsFileName));
                File.WriteAllText(Path.Combine(TempDirectory.Path, testAssetsProps), assetPropsContent);

                // Copy nuget.config and make packages folder point to vstest packages folder
                const string nugetFileName = "NuGet.config";
                var nugetContent = File.ReadAllText(Path.Combine(file.DirectoryName!, relativePathToRoot, nugetFileName))
                    .Replace("\"packages\"", "\"" + Path.Combine(file.DirectoryName!, relativePathToRoot, "packages") + "\"");
                File.WriteAllText(Path.Combine(TempDirectory.Path, nugetFileName), nugetContent);
            }
            else
            {
                File.Copy(file.FullName, newFilePath);
            }
        }

        return Path.Combine(TempDirectory.Path, assetName);
    }
}
