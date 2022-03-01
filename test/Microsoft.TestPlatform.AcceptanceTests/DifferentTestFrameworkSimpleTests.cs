﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.AcceptanceTests;

[TestClass]
[TestCategory("Windows-Review")]
public class DifferentTestFrameworkSimpleTests : AcceptanceTestBase
{
    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    public void ChutzpahRunAllTestExecution(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        using var tempDir = new TempDirectory();
        var testJSFileAbsolutePath = Path.Combine(_testEnvironment.TestAssetsPath, "test.js");
        var arguments = PrepareArguments(testJSFileAbsolutePath, GetTestAdapterPath(UnitTestFramework.Chutzpah), string.Empty, FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: tempDir.Path);

        InvokeVsTest(arguments);
        ValidateSummaryStatus(1, 1, 0);
    }

    [TestMethod]
    // vstest.console is x64 now, but x86 run "in process" run should still succeed by being run in x86 testhost
    // Skip .NET (Core) tests because we test them below.
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true, useCoreRunner: false)]
    public void CPPRunAllTestExecutionNetFramework(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        CppRunAllTests(runnerInfo.RunnerFramework, "x86");
    }


    [TestMethod]
    [TestCategory("Windows-Review")]
    // vstest.console is 64-bit now, run in process to test the 64-bit native dll
    // Skip .NET (Core) tests because we test them below.
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true, useCoreRunner: false)]
    public void CPPRunAllTestExecutionPlatformx64NetFramework(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        CppRunAllTests(runnerInfo.RunnerFramework, "x64");
    }

    [TestMethod]
    // C++ tests cannot run in .NET Framework host under .NET Core, because we only ship .NET Standard CPP adapter in .NET Core
    // We also don't test x86 for .NET Core, because the resolver there does not switch between x86 and x64 correctly, it just uses the parent process bitness.
    // We run this on netcore31 and not the default netcore21 because netcore31 is the minimum tfm that has the runtime features we need, such as additionaldeps.
    [NetCoreTargetFrameworkDataSource(useDesktopRunner: false, useCoreRunner: true, useNetCore21Target: false, useNetCore31Target: true)]
    public void CPPRunAllTestExecutionPlatformx64Net(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        CppRunAllTests(runnerInfo.RunnerFramework, "x64");
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource]
    public void WebTestRunAllTestsWithRunSettings(RunnerInfo runnerInfo)
    {
        using var tempDir = new TempDirectory();
        SetTestEnvironment(_testEnvironment, runnerInfo);
        var runSettingsFilePath = Path.Combine(tempDir.Path, Guid.NewGuid() + ".runsettings");

        //test the iterationCount setting for WebTestRunConfiguration in run settings
        var runSettingsXml = $@"<?xml version='1.0' encoding='utf-8'?>
                                <RunSettings>
                                    <WebTestRunConfiguration iterationCount='5' />
                                    <RunConfiguration>
                                        <TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion>
                                    </RunConfiguration>
                                </RunSettings>";

        CreateRunSettingsFile(runSettingsFilePath, runSettingsXml);

        //minWebTestResultFileSizeInKB is set to 150 here as the web test has a iteration count set to 5
        //therefore, the test will run for 5 iterations resulting in web test result file size of at least 150 KB
        WebTestRunAllTests(runnerInfo.RunnerFramework, runSettingsFilePath, 150);
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource]
    public void CodedWebTestRunAllTests(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        CodedWebTestRunAllTests(runnerInfo.RunnerFramework);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    [NetCoreTargetFrameworkDataSource]
    public void NUnitRunAllTestExecution(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);

        using var tempDir = new TempDirectory();
        var arguments = PrepareArguments(
            GetAssetFullPath("NUTestProject.dll"),
            GetTestAdapterPath(UnitTestFramework.NUnit),
            string.Empty, FrameworkArgValue,
            runnerInfo.InIsolationValue, tempDir.Path);
        InvokeVsTest(arguments);
        ValidateSummaryStatus(1, 1, 0);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
    [NetCoreTargetFrameworkDataSource]
    public void XUnitRunAllTestExecution(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        using var tempDir = new TempDirectory();

        string testAssemblyPath;
        // Xunit >= 2.2 won't support net451, Minimum target framework it supports is net452.
        if (_testEnvironment.TargetFramework.Equals("net451"))
        {
            testAssemblyPath = _testEnvironment.GetTestAsset("XUTestProject.dll", "net46");
        }
        else
        {
            testAssemblyPath = _testEnvironment.GetTestAsset("XUTestProject.dll");
        }

        var arguments = PrepareArguments(
            testAssemblyPath,
            GetTestAdapterPath(UnitTestFramework.XUnit),
            string.Empty, FrameworkArgValue,
            runnerInfo.InIsolationValue, tempDir.Path);
        InvokeVsTest(arguments);
        ValidateSummaryStatus(1, 1, 0);
    }

    private void CppRunAllTests(string runnerFramework, string platform)
    {
        using var tempDir = new TempDirectory();
        string assemblyRelativePathFormat =
            @"microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\{0}\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
        var assemblyRelativePath = platform.Equals("x64", StringComparison.OrdinalIgnoreCase)
            ? string.Format(assemblyRelativePathFormat, platform)
            : string.Format(assemblyRelativePathFormat, "");
        var assemblyAbsolutePath = Path.Combine(_testEnvironment.PackageDirectory, assemblyRelativePath);
        var arguments = PrepareArguments(assemblyAbsolutePath, string.Empty, string.Empty, FrameworkArgValue, _testEnvironment.InIsolationValue, resultsDirectory: tempDir.Path);

        InvokeVsTest(arguments);
        ValidateSummaryStatus(1, 1, 0);
    }

    private void WebTestRunAllTests(string runnerFramework, string runSettingsFilePath = null, int minWebTestResultFileSizeInKB = 0)
    {
        if (runnerFramework.StartsWith("netcoreapp"))
        {
            Assert.Inconclusive("WebTests tests not supported with .Netcore runner.");
            return;
        }

        string assemblyRelativePath =
            @"microsoft.testplatform.qtools.assets\2.0.0\contentFiles\any\any\WebTestAssets\WebTest1.webtest";

        var assemblyAbsolutePath = Path.Combine(_testEnvironment.PackageDirectory, assemblyRelativePath);
        using var resultsDirectory = new TempDirectory();
        var arguments = PrepareArguments(
            assemblyAbsolutePath,
            string.Empty,
            runSettingsFilePath, FrameworkArgValue, string.Empty, resultsDirectory.Path);

        InvokeVsTest(arguments);
        ValidateSummaryStatus(1, 0, 0);

        if (minWebTestResultFileSizeInKB > 0)
        {
            var dirInfo = new DirectoryInfo(resultsDirectory.Path);
            var webtestResultFile = "WebTest1.webtestResult";
            var files = dirInfo.GetFiles(webtestResultFile, SearchOption.AllDirectories);
            Assert.IsTrue(files.Length > 0, $"File {webtestResultFile} not found under results directory {resultsDirectory}");

            var fileSizeInKB = files[0].Length / 1024;
            Assert.IsTrue(fileSizeInKB > minWebTestResultFileSizeInKB, $"Size of the file {webtestResultFile} is {fileSizeInKB} KB. It is not greater than {minWebTestResultFileSizeInKB} KB indicating iterationCount in run settings not honored.");
        }
    }

    private void CodedWebTestRunAllTests(string runnerFramework)
    {
        if (runnerFramework.StartsWith("netcoreapp"))
        {
            Assert.Inconclusive("WebTests tests not supported with .Netcore runner.");
            return;
        }

        using var tempDir = new TempDirectory();
        string assemblyRelativePath =
            @"microsoft.testplatform.qtools.assets\2.0.0\contentFiles\any\any\WebTestAssets\BingWebTest.dll";
        var assemblyAbsolutePath = Path.Combine(_testEnvironment.PackageDirectory, assemblyRelativePath);
        var arguments = PrepareArguments(
            assemblyAbsolutePath,
            string.Empty,
            string.Empty, FrameworkArgValue, resultsDirectory: tempDir.Path);

        InvokeVsTest(arguments);
        ValidateSummaryStatus(1, 0, 0);
    }
}
