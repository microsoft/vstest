// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
public class DotnetTestTests : AcceptanceTestBase
{
    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource]
    public void RunDotnetTestWithCsproj(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var projectPath = GetIsolatedTestAsset("SimpleTestProject.csproj");
        InvokeDotnetTest($@"{projectPath} --logger:""Console;Verbosity=normal""");

        // ensure our dev version is used
        StdOutputContains("-dev");
        ValidateSummaryStatus(1, 1, 1);
        ExitCodeEquals(1);
    }

    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource]
    public void RunDotnetTestWithDll(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var assemblyPath = BuildMultipleAssemblyPath("SimpleTestProject.dll");
        InvokeDotnetTest($@"{assemblyPath} --logger:""Console;Verbosity=normal""");

        // ensure our dev version is used
        StdOutputContains("-dev");
        ValidateSummaryStatus(1, 1, 1);
        ExitCodeEquals(1);
    }

    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource]
    public void RunDotnetTestWithCsprojPassInlineSettings(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var projectPath = GetIsolatedTestAsset("ParametrizedTestProject.csproj");
        InvokeDotnetTest($@"{projectPath} --logger:""Console;Verbosity=normal"" -- TestRunParameters.Parameter(name =\""weburl\"", value=\""http://localhost//def\"")");

        ValidateSummaryStatus(1, 0, 0);
        ExitCodeEquals(0);
    }

    [TestMethod]
    // patched dotnet is not published on non-windows systems
    [TestCategory("Windows-Review")]
    [NetCoreTargetFrameworkDataSource]
    public void RunDotnetTestWithDllPassInlineSettings(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var assemblyPath = BuildMultipleAssemblyPath("ParametrizedTestProject.dll");
        InvokeDotnetTest($@"{assemblyPath} --logger:""Console;Verbosity=normal"" -- TestRunParameters.Parameter(name=\""weburl\"", value=\""http://localhost//def\"")");

        ValidateSummaryStatus(1, 0, 0);
        ExitCodeEquals(0);
    }

    private string GetIsolatedTestAsset(string assetName)
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
