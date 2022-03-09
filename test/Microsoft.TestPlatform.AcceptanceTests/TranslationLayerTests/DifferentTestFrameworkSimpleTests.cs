// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.TestPlatform.TestUtilities;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.AcceptanceTests.TranslationLayerTests;

/// <summary>
/// The Run Tests using VsTestConsoleWrapper API's
/// </summary>
[TestClass]
public class DifferentTestFrameworkSimpleTests : AcceptanceTestBase
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
    public void RunTestsWithNunitAdapter(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        var sources = new List<string>
        {
            GetAssetFullPath("NUTestProject.dll")
        };

        _vstestConsoleWrapper.RunTests(
            sources,
            GetDefaultRunSettings(),
            _runEventHandler);

        var testCase =
            _runEventHandler.TestResults.Where(tr => tr.TestCase.DisplayName.Equals("PassTestMethod1"));

        // Assert
        Assert.AreEqual(2, _runEventHandler.TestResults.Count);
        Assert.AreEqual(1, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
        Assert.AreEqual(1, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));

        // Release builds optimize code, hence line numbers are different.
        if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
        {
            Assert.AreEqual(14, testCase.FirstOrDefault().TestCase.LineNumber);
        }
        else
        {
            Assert.AreEqual(13, testCase.FirstOrDefault().TestCase.LineNumber);
        }
    }

    [TestMethod]
    // there are logs in the diagnostic log, it is failing with NullReferenceException because path is null
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource]
    [NetCoreTargetFrameworkDataSource]
    public void RunTestsWithXunitAdapter(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        // Xunit >= 2.2 won't support net451, Minimum target framework it supports is net452.
        string testAssemblyPath = _testEnvironment.TargetFramework.Equals("net451")
            ? _testEnvironment.GetTestAsset("XUTestProject.dll", "net46")
            : _testEnvironment.GetTestAsset("XUTestProject.dll");
        var sources = new List<string> { testAssemblyPath };
        var testAdapterPath = Directory.EnumerateFiles(GetTestAdapterPath(UnitTestFramework.XUnit), "*.TestAdapter.dll").ToList();
        _vstestConsoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath.FirstOrDefault() });

        _vstestConsoleWrapper.RunTests(
            sources,
            GetDefaultRunSettings(),
            _runEventHandler);

        var testCase =
            _runEventHandler.TestResults.Where(tr => tr.TestCase.DisplayName.Equals("xUnitTestProject.Class1.PassTestMethod1"));

        // Assert
        Assert.AreEqual(2, _runEventHandler.TestResults.Count);
        Assert.AreEqual(1, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
        Assert.AreEqual(1, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));

        // Release builds optimize code, hence line numbers are different.
        if (IntegrationTestEnvironment.BuildConfiguration.StartsWith("release", StringComparison.OrdinalIgnoreCase))
        {
            Assert.AreEqual(15, testCase.FirstOrDefault().TestCase.LineNumber);
        }
        else
        {
            Assert.AreEqual(14, testCase.FirstOrDefault().TestCase.LineNumber);
        }
    }

    [TestMethod]
    [TestCategory("Windows-Review")]
    [NetFullTargetFrameworkDataSource]
    public void RunTestsWithChutzpahAdapter(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(_testEnvironment, runnerInfo);
        Setup();

        var sources = new List<string>
        {
            Path.Combine(_testEnvironment.TestAssetsPath, "test.js")
        };

        var testAdapterPath = Directory.EnumerateFiles(GetTestAdapterPath(UnitTestFramework.Chutzpah), "*.TestAdapter.dll").ToList();
        _vstestConsoleWrapper.InitializeExtensions(new List<string>() { testAdapterPath.FirstOrDefault() });

        _vstestConsoleWrapper.RunTests(
            sources,
            GetDefaultRunSettings(),
            _runEventHandler);

        var testCase =
            _runEventHandler.TestResults.Where(tr => tr.TestCase.DisplayName.Equals("TestMethod1"));

        // Assert
        Assert.AreEqual(2, _runEventHandler.TestResults.Count);
        Assert.AreEqual(1, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Passed));
        Assert.AreEqual(1, _runEventHandler.TestResults.Count(t => t.Outcome == TestOutcome.Failed));
        Assert.AreEqual(1, testCase.FirstOrDefault().TestCase.LineNumber);
    }
}
