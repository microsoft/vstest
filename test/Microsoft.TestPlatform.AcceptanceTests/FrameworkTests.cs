// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FrameworkTests : AcceptanceTestBase
    {

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void FrameworkArgumentShouldWork(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty);
            arguments = string.Concat(arguments, " ", $"/Framework:{this.FrameworkArgValue}");

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void OnWrongFrameworkPassedTestRunShouldNotRun(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(GetSampleTestAssembly(), string.Empty, string.Empty);
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