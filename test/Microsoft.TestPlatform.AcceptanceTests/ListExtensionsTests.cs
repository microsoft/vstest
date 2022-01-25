// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests;

using VisualStudio.TestTools.UnitTesting;

[TestClass]
// this is tested only on .NET Framework
[TestCategory("Windows-Review")]
public class ListExtensionsTests : AcceptanceTestBase
{
    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: false, inProcess: true)]
    public void ListDiscoverersShouldShowInboxDiscoverers(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(testEnvironment, runnerInfo);

        InvokeVsTest("/listDiscoverers");

        if (this.IsDesktopRunner())
        {
            this.StdOutputContains("executor://codedwebtestadapter/v1");
            this.StdOutputContains("executor://mstestadapter/v1");
            this.StdOutputContains("executor://webtestadapter/v1");
            this.StdOutputContains(".Webtest");
            this.StdOutputContains("executor://cppunittestexecutor/v1");
        }
        else
        {
            // There are no inbox adapters for dotnet core
            this.StdOutputDoesNotContains("executor://codedwebtestadapter/v1");
            this.StdOutputDoesNotContains("executor://mstestadapter/v1");
            this.StdOutputDoesNotContains("executor://webtestadapter/v1");
            this.StdOutputDoesNotContains(".Webtest");
            this.StdOutputDoesNotContains("executor://cppunittestexecutor/v1");
        }
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: false, inProcess: true)]
    public void ListExecutorsShouldShowInboxExecutors(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(testEnvironment, runnerInfo);

        InvokeVsTest("/listExecutors");

        if (this.IsDesktopRunner())
        {
            this.StdOutputContains("executor://CodedWebTestAdapter/v1");
            this.StdOutputContains("executor://MSTestAdapter/v1");
            this.StdOutputContains("executor://WebTestAdapter/v1");
            this.StdOutputContains("executor://CppUnitTestExecutor/v1");
            this.StdOutputContains("executor://UAPCppExecutorIdentifier");
        }
        else
        {
            // There are no inbox adapters for dotnet core
            this.StdOutputDoesNotContains("executor://CodedWebTestAdapter/v1");
            this.StdOutputDoesNotContains("executor://MSTestAdapter/v1");
            this.StdOutputDoesNotContains("executor://WebTestAdapter/v1");
            this.StdOutputDoesNotContains("executor://CppUnitTestExecutor/v1");
            this.StdOutputDoesNotContains("executor://UAPCppExecutorIdentifier");
        }
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: false, inProcess: true)]
    public void ListLoggersShouldShowInboxLoggers(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(testEnvironment, runnerInfo);

        InvokeVsTest("/listLoggers");

        StdOutputContains("logger://Microsoft/TestPlatform/Extensions/Blame/v1");
        StdOutputContains("logger://Microsoft/TestPlatform/TrxLogger/v1");
    }

    [TestMethod]
    [NetFullTargetFrameworkDataSource(inIsolation: false, inProcess: true)]
    public void ListSettingsProvidersShouldShowInboxSettingsProviders(RunnerInfo runnerInfo)
    {
        SetTestEnvironment(testEnvironment, runnerInfo);

        InvokeVsTest("/listSettingsProviders");

        if (IsDesktopRunner())
        {
            StdOutputContains("MSTestSettingsProvider");
        }
        else
        {
            // There are no inbox adapters for dotnet core
            StdOutputDoesNotContains("MSTestSettingsProvider");
        }
    }
}