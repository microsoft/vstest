// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests;

using global::TestPlatform.TestUtilities;

using VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

/// <summary>
/// The Run Tests using VsTestConsoleWrapper API's
/// </summary>
[TestClass]
public class RunTestsWithDifferentConfigurationTests : AcceptanceTestBase
{
    private const string Netcoreapp = "netcoreapp";
    private const string Message = "VsTestConsoleWrapper does not support .Net Core Runner";

    private IVsTestConsoleWrapper _vstestConsoleWrapper;
    private RunEventHandler _runEventHandler;

    private void Setup()
    {
        _vstestConsoleWrapper = GetVsTestConsoleWrapper();
        _runEventHandler = new RunEventHandler();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _vstestConsoleWrapper?.EndSession();
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void RunTestsWithTestAdapterPath(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        var testAdapterPath = Directory.EnumerateFiles(GetTestAdapterPath(), "*.TestAdapter.dll").ToList();
        _vstestConsoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath.FirstOrDefault() });

        _vstestConsoleWrapper.RunTests(
            GetTestAssemblies(),
            GetDefaultRunSettings(),
            _runEventHandler);

        // Assert
        Assert.AreEqual(6, _runEventHandler.TestResults.Count);
        Assert.AreEqual(2, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
        Assert.AreEqual(2, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        Assert.AreEqual(2, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void RunTestsWithRunSettingsWithParallel(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        string runSettingsXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <RunSettings>
                                        <RunConfiguration>
                                        <TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion>
                                        <MaxCpuCount>2</MaxCpuCount>
                                        </RunConfiguration>
                                    </RunSettings>";

        var testHostNames = new[] { "testhost", "testhost.x86", "dotnet" };
        int expectedNumOfProcessCreated = 2;

        var cts = new CancellationTokenSource();
        var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
            cts,
            testHostNames);

        _vstestConsoleWrapper.RunTests(
            GetTestAssemblies(),
            runSettingsXml,
            _runEventHandler);

        cts.Cancel();

        // Assert
        _runEventHandler.EnsureSuccess();
        Assert.AreEqual(6, _runEventHandler.TestResults.Count);
        Assert.AreEqual(2, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
        Assert.AreEqual(2, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        Assert.AreEqual(2, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));
        Assert.AreEqual(
            expectedNumOfProcessCreated,
            numOfProcessCreatedTask.Result.Count,
            $"Number of '{ string.Join(", ", testHostNames) }' process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Result.Count} ({ string.Join(", ", numOfProcessCreatedTask.Result) })");
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource]
    public void RunTestsWithTestSettings(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        ExecuteNotSupportedRunnerFrameworkTests(runnerInfo.RunnerFramework, Netcoreapp, Message);
        Setup();

        var testsettingsFile = Path.Combine(Path.GetTempPath(), "tempsettings.testsettings");
        string testSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?><TestSettings></TestSettings>";

        File.WriteAllText(testsettingsFile, testSettingsXml, Encoding.UTF8);
        var runSettings = $"<RunSettings><RunConfiguration><TargetFrameworkVersion>{FrameworkArgValue}</TargetFrameworkVersion></RunConfiguration><MSTest><SettingsFile>" + testsettingsFile + "</SettingsFile></MSTest></RunSettings>";
        var sources = new List<string>
        {
            GetAssetFullPath("MstestV1UnitTestProject.dll")
        };

        _vstestConsoleWrapper.RunTests(
            sources,
            runSettings,
            _runEventHandler);

        // Assert
        Assert.AreEqual(5, _runEventHandler.TestResults.Count);
        Assert.AreEqual(2, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
        Assert.AreEqual(2, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        Assert.AreEqual(1, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Skipped));

        File.Delete(testsettingsFile);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void RunTestsWithX64Source(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        var sources = new List<string>
        {
            GetAssetFullPath("SimpleTestProject3.dll")
        };


        int expectedNumOfProcessCreated = 1;
        var testhostProcessNames = new[] { "testhost", "dotnet" };

        var cts = new CancellationTokenSource();
        var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
            cts, testhostProcessNames);

        _vstestConsoleWrapper.RunTests(
            sources,
            GetDefaultRunSettings(),
            new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName = SampleUnitTestProject3.UnitTest1.WorkingDirectoryTest" },
            _runEventHandler);

        cts.Cancel();

        // Assert
        Assert.AreEqual(1, _runEventHandler.TestResults.Count);
        Assert.AreEqual(1, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
        var numberOfProcessCreated = numOfProcessCreatedTask.Result;
        Assert.AreEqual(
            expectedNumOfProcessCreated,
            numberOfProcessCreated.Count,
            $"Number of { string.Join(" ,", testhostProcessNames) } process created, expected: {expectedNumOfProcessCreated} actual: {numberOfProcessCreated.Count} ({ string.Join(", ", numberOfProcessCreated) })");
    }

    private IList<string> GetTestAssemblies()
    {
        var testAssemblies = new List<string>
        {
            GetAssetFullPath("SimpleTestProject.dll"),
            GetAssetFullPath("SimpleTestProject2.dll")
        };

        return testAssemblies;
    }
}