// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FrameworkTests : AcceptanceTestBase
    {

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void FrameworkArgumentShouldWork(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, this.FrameworkArgValue);
            arguments = string.Concat(arguments, " ", $"/Framework:{this.FrameworkArgValue}");

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void OnWrongFrameworkPassedTestRunShouldNotRun(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty, this.FrameworkArgValue);
            if (runnerInfo.TargetFramework.Contains("netcore"))
            {
                arguments = string.Concat(arguments, " ", "/Framework:Framework45");
            }
            else
            {
                arguments = string.Concat(arguments, " ", "/Framework:FrameworkCore10");
            }
            this.InvokeVsTest(arguments);

            if (runnerInfo.TargetFramework.Contains("netcore"))
            {
                this.StdOutputContains("No test is available");
            }
            else
            {
                this.StdErrorContains("Test Run Aborted.");
            }
        }
    }
}