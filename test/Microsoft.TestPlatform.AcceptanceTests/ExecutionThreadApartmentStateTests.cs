// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExecutionThreadApartmentStateTests : AcceptanceTestBase
    {
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void UITestShouldPassIfApartmentStateIsSTA(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /testcasefilter:UITestMethod -- RunConfiguration.ExecutionThreadApartmentState=STA");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [CustomDataTestMethod]
        [NETCORETargetFramework]
        public void WarningShouldBeShownWhenValueIsSTAForNetCore(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject2.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /testcasefilter:PassingTest2 -- RunConfiguration.ExecutionThreadApartmentState=STA");
            this.InvokeVsTest(arguments);
            this.StdOutputContains("Unable to execute in STA thread");
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void UITestShouldFailWhenDefaultApartmentStateIsMTA(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /testcasefilter:UITestMethod");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [Ignore]
        // Issue with cancel, After cancellation same test counting twice. Below is example command and output.
        /* C:\Users\samadala\src\vstest\artifacts\Debug\net451\win7-x64\vstest.console.exe "C:\Users\samadala\src\vstest\test\TestAssets\SimpleTestProject3\bin\Debug\net451\SimpleTestProject3.dll" /testadapterpath:"C:\Users\samadala\src\vstest\packages\MSTest.TestAdapter\1.2.0-beta\build\_common" /Framework:".NETFramework,Version=v4.5.1" /logger:"console;verbosity=normal" /tests:TestWhichTakeSomeTime1,TestWhichTakeSomeTime2,TestWhichTakeSomeTime3 -- RunConfiguration.TestSessionTimeout=2000
            Microsoft (R) Test Execution Command Line Tool Version 15.5.0-dev
            Copyright (c) Microsoft Corporation.  All rights reserved.
            Starting test discovery, please wait...
            Canceling test run: test run timeout of 2000 milliseconds exceeded.
            Passed   SampleUnitTestProject3.TestSessionTimeoutTest.TestWhichTakeSomeTime1
            Passed   SampleUnitTestProject3.TestSessionTimeoutTest.TestWhichTakeSomeTime1
            Total tests: 2. Passed: 2. Failed: 0. Skipped: 0.
            Test Run Canceled.
            Test execution time: 4.4512 Seconds
        */
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void CancelTestExectionShouldWorkWhenApartmentStateIsSTA(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /tests:UITestMethod,UITestWithSleep -- RunConfiguration.ExecutionThreadApartmentState=STA RunConfiguration.TestSessionTimeout=5000");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }
    }
}