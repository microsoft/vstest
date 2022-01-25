﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

using TestUtilities;

using VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TestPlatformNugetPackageTests : CodeCoverageAcceptanceTestBase
{
    private static string s_nugetPackageFolder;
    private string _resultsDirectory;

    [ClassInitialize]
    public static void ClassInit(TestContext testContext)
    {
        var packageLocation = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "artifacts", IntegrationTestEnvironment.BuildConfiguration, "packages");
        var nugetPackage = Directory.EnumerateFiles(packageLocation, "Microsoft.TestPlatform.*.nupkg").OrderBy(a => a).FirstOrDefault();
        s_nugetPackageFolder = Path.Combine(packageLocation, Path.GetFileNameWithoutExtension(nugetPackage));
        ZipFile.ExtractToDirectory(nugetPackage, s_nugetPackageFolder);

        TryMoveDirectory(
            sourceDirName: Path.Combine(s_nugetPackageFolder, "tools", "net451", "Team%20Tools"),
            destDirName: Path.Combine(s_nugetPackageFolder, "tools", "net451", "Team Tools")
        );

        TryMoveDirectory(
            sourceDirName: Path.Combine(s_nugetPackageFolder, "tools", "net451", "Team Tools", "Dynamic%20Code%20Coverage%20Tools"),
            destDirName: Path.Combine(s_nugetPackageFolder, "tools", "net451", "Team Tools", "Dynamic Code Coverage Tools")
        );
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        Directory.Delete(s_nugetPackageFolder, true);
    }

    [TestInitialize]
    public void SetUp()
    {
        _resultsDirectory = GetResultsDirectory();
    }

    [TestCleanup]
    public void CleanUp()
    {
        TryRemoveDirectory(_resultsDirectory);
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource(useCoreRunner: false)]
    [NetCoreTargetFrameworkDataSource(useCoreRunner: false)]
    public void RunMultipleTestAssembliesWithCodeCoverage(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        var assemblyPaths = BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');

        var arguments = CreateCodeCoverageArguments(runnerInfo, assemblyPaths, out var trxFilePath);
        InvokeVsTest(arguments);

        ExitCodeEquals(1); // failing tests

        var actualCoverageFile = GetCoverageFileNameFromTrx(trxFilePath, _resultsDirectory);
        Console.WriteLine($@"Coverage file: {actualCoverageFile}  Results directory: {_resultsDirectory} trxfile: {trxFilePath}");
        Assert.IsTrue(File.Exists(actualCoverageFile), "Coverage file not found: {0}", actualCoverageFile);
    }

    public override string GetConsoleRunnerPath()
    {
        string consoleRunnerPath = string.Empty;

        if (IsDesktopRunner())
        {
            consoleRunnerPath = Path.Combine(s_nugetPackageFolder, "tools", "net451", "Common7", "IDE", "Extensions", "TestPlatform", "vstest.console.exe");
        }

        Assert.IsTrue(File.Exists(consoleRunnerPath), "GetConsoleRunnerPath: Path not found: {0}", consoleRunnerPath);
        return consoleRunnerPath;
    }

    private string CreateCodeCoverageArguments(
        RunnerInfo runnerInfo,
        string assemblyPaths,
        out string trxFilePath)
    {
        string diagFileName = Path.Combine(_resultsDirectory, "diaglog.txt");

        var arguments = PrepareArguments(assemblyPaths, GetTestAdapterPath(), string.Empty,
            FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: _resultsDirectory);

        arguments = string.Concat(arguments, $" /Diag:{diagFileName}", $" /EnableCodeCoverage");

        trxFilePath = Path.Combine(_resultsDirectory, Guid.NewGuid() + ".trx");
        arguments = string.Concat(arguments, " /logger:trx;logfilename=" + trxFilePath);

        return arguments;
    }

    private static void TryMoveDirectory(string sourceDirName, string destDirName)
    {
        if (Directory.Exists(sourceDirName))
        {
            Directory.Move(sourceDirName, destDirName);
        }
    }
}