// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExecutionThreadApartmentStateTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void UITestShouldPassIfApartmentStateIsSTA(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /testcasefilter:UITestMethod");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [TestMethod]
        [NetCoreTargetFrameworkDataSource]
        public void WarningShouldBeShownWhenValueIsSTAForNetCore(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject2.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /testcasefilter:PassingTest2 -- RunConfiguration.ExecutionThreadApartmentState=STA");
            this.InvokeVsTest(arguments);
            this.StdOutputContains("ExecutionThreadApartmentState option not supported for framework:");
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void UITestShouldFailWhenDefaultApartmentStateIsMTA(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /testcasefilter:UITestMethod -- RunConfiguration.ExecutionThreadApartmentState=MTA");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [Ignore(@"Issue with TestSessionTimeout:  https://github.com/Microsoft/vstest/issues/980")]
        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void CancelTestExectionShouldWorkWhenApartmentStateIsSTA(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /tests:UITestWithSleep1,UITestMethod -- RunConfiguration.ExecutionThreadApartmentState=STA RunConfiguration.TestSessionTimeout=2000");
            this.InvokeVsTest(arguments);
            this.StdOutputContains("Canceling test run: test run timeout of");
            this.ValidateSummaryStatus(1, 0, 0);
        }
    }
}
