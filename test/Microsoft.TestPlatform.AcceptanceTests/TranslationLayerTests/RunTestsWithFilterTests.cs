// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests;

using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The Run Tests using VsTestConsoleWrapper API's
/// </summary>
[TestClass]
public class RunTestsWithFilterTests : AcceptanceTestBase
{
    private IVsTestConsoleWrapper _vstestConsoleWrapper;
    private RunEventHandler _runEventHandler;

    private void Setup()
    {
        _vstestConsoleWrapper = GetVsTestConsoleWrapper(out _);
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
    public void RunTestsWithTestCaseFilter(RunnerInfo runnerInfo)
    {
        AcceptanceTestBase.SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        var sources = new List<string>
                              {
                                  GetAssetFullPath("SimpleTestProject.dll")
                              };

        _vstestConsoleWrapper.RunTests(
            sources,
            GetDefaultRunSettings(),
            new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest" },
            _runEventHandler);

        // Assert
        Assert.AreEqual(1, _runEventHandler.TestResults.Count);
        Assert.AreEqual(TestOutcome.Passed, _runEventHandler.TestResults.FirstOrDefault().Outcome);
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void RunTestsWithFastFilter(RunnerInfo runnerInfo)
    {
        AcceptanceTestBase.SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        var sources = new List<string>
                              {
                                  GetAssetFullPath("SimpleTestProject.dll")
                              };

        _vstestConsoleWrapper.RunTests(
            sources,
            GetDefaultRunSettings(),
            new TestPlatformOptions() { TestCaseFilter = "FullyQualifiedName=SampleUnitTestProject.UnitTest1.PassingTest | FullyQualifiedName=SampleUnitTestProject.UnitTest1.FailingTest" },
            _runEventHandler);

        // Assert
        Assert.AreEqual(2, _runEventHandler.TestResults.Count);
        Assert.AreEqual(1, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
        Assert.AreEqual(1, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
    }
}
