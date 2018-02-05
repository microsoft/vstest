// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ListExtensionsTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: false, inProcess: true)]
        public void ListDiscoverersShouldShowInboxDiscoverers(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.InvokeVsTest("/listDiscoverers");

            if (this.IsDesktopRunner())
            {
                this.StdOutputContains("executor://codedwebtestadapter/v1");
                this.StdOutputContains("executor://generictestadapter/v1");
                this.StdOutputContains(".generictest");
                this.StdOutputContains("executor://orderedtestadapter/v1");
                this.StdOutputContains(".orderedtest");
                this.StdOutputContains("executor://mstestadapter/v1");
                this.StdOutputContains("executor://webtestadapter/v1");
                this.StdOutputContains(".Webtest");
                this.StdOutputContains("executor://cppunittestexecutor/v1");
            }
            else
            {
                // There are no inbox adapters for dotnet core
                this.StdOutputDoesNotContains("executor://codedwebtestadapter/v1");
                this.StdOutputDoesNotContains("executor://generictestadapter/v1");
                this.StdOutputDoesNotContains(".generictest");
                this.StdOutputDoesNotContains("executor://orderedtestadapter/v1");
                this.StdOutputDoesNotContains(".orderedtest");
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
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.InvokeVsTest("/listExecutors");

            if (this.IsDesktopRunner())
            {
                this.StdOutputContains("executor://CodedWebTestAdapter/v1");
                this.StdOutputContains("executor://GenericTestAdapter/v1");
                this.StdOutputContains("executor://OrderedTestAdapter/v1");
                this.StdOutputContains("executor://MSTestAdapter/v1");
                this.StdOutputContains("executor://WebTestAdapter/v1");
                this.StdOutputContains("executor://CppUnitTestExecutor/v1");
                this.StdOutputContains("executor://UAPCppExecutorIdentifier");
            }
            else
            {
                // There are no inbox adapters for dotnet core
                this.StdOutputDoesNotContains("executor://CodedWebTestAdapter/v1");
                this.StdOutputDoesNotContains("executor://GenericTestAdapter/v1");
                this.StdOutputDoesNotContains("executor://OrderedTestAdapter/v1");
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
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.InvokeVsTest("/listLoggers");

            this.StdOutputContains("logger://Microsoft/TestPlatform/Extensions/Blame/v1");
            this.StdOutputContains("logger://Microsoft/TestPlatform/TrxLogger/v1");
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: false, inProcess: true)]
        public void ListSettingsProvidersShouldShowInboxSettingsProviders(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.InvokeVsTest("/listSettingsProviders");

            if (this.IsDesktopRunner())
            {
                this.StdOutputContains("MSTestSettingsProvider");
            }
            else
            {
                // There are no inbox adapters for dotnet core
                this.StdOutputDoesNotContains("MSTestSettingsProvider");
            }
        }
    }
}
