// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading;

    using global::TestPlatform.TestUtilities;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExecutionTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void RunMultipleTestAssemblies(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');

            this.InvokeVsTestForExecution(assemblyPaths, this.GetTestAdapterPath(), this.FrameworkArgValue, string.Empty);

            this.ValidateSummaryStatus(2, 2, 2);
            this.ExitCodeEquals(1); // failing tests
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void RunMultipleTestAssembliesWithoutTestAdapterPath(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject.dll").Trim('\"');
            var xunitAssemblyPath = this.testEnvironment.TargetFramework.Equals("net451")?
                testEnvironment.GetTestAsset("XUTestProject.dll", "net46") :
                testEnvironment.GetTestAsset("XUTestProject.dll");

            assemblyPaths = string.Concat(assemblyPaths, "\" \"", xunitAssemblyPath);
            this.InvokeVsTestForExecution(assemblyPaths, string.Empty, this.FrameworkArgValue, string.Empty);

            this.ValidateSummaryStatus(2, 2, 1);
            this.ExitCodeEquals(1); // failing tests
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        [DoNotParallelize]
        public void RunMultipleTestAssembliesInParallel(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /Parallel");
            arguments = string.Concat(arguments, " /Platform:x86");
            string testhostProcessName = string.Empty;
            // for the desktop we will run testhost.x86 in two copies, but for core 
            // we will run a combination of testhost.x86 and dotnet, where the dotnet will be 
            // the test console, and sometimes it will be the test host (e.g dotnet, dotnet, testhost.x86, or dotnet, testhost.x86, testhost.x86)
            // based on the target framework
            int expectedNumOfProcessCreated = this.IsDesktopRunner() ? 2 : 3;
            var testhostProcessNames = new [] { "testhost.x86", "dotnet" };

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessNames);

            this.InvokeVsTest(arguments);

            cts.Cancel();
            Assert.AreEqual(
                expectedNumOfProcessCreated,
                numOfProcessCreatedTask.Result.Count,
                $"Number of {testhostProcessName} process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Result.Count} ({ string.Join(", ", numOfProcessCreatedTask.Result) })");
            this.ValidateSummaryStatus(2, 2, 2);
            this.ExitCodeEquals(1); // failing tests
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void TestSessionTimeOutTests(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:TestSessionTimeoutTest");

            // set TestSessionTimeOut = 7 sec
            arguments = string.Concat(arguments, " -- RunConfiguration.TestSessionTimeout=7000");
            this.InvokeVsTest(arguments);

            this.ExitCodeEquals(1);
            this.StdErrorContains("Test Run Aborted.");
            this.StdErrorContains("Aborting test run: test run timeout of 7000 milliseconds exceeded.");
            this.StdOutputDoesNotContains("Total tests: 6");
        }

        [TestMethod]
        [NetCoreTargetFrameworkDataSource]
        public void TestPlatformShouldBeCompatibleWithOldTestHost(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths = this.BuildMultipleAssemblyPath("SampleProjectWithOldTestHost.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 0, 0);
            this.ExitCodeEquals(0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void WorkingDirectoryIsSourceDirectory(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /tests:WorkingDirectoryTest");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 0, 0);
            this.ExitCodeEquals(0);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void StackOverflowExceptionShouldBeLoggedToConsoleAndDiagLogFile(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            if (IntegrationTestEnvironment.BuildConfiguration.Equals("release", StringComparison.OrdinalIgnoreCase))
            {
                // On release, x64 builds, recursive calls may be replaced with loops (tail call optimization)
                Assert.Inconclusive("On StackOverflowException testhost not exited in release configuration.");
                return;
            }

            var diagLogFilePath = Path.Combine(Path.GetTempPath(), $"std_error_log_{Guid.NewGuid()}.txt");
            File.Delete(diagLogFilePath);

            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /testcasefilter:ExitWithStackoverFlow");
            arguments = string.Concat(arguments, $" /diag:{diagLogFilePath}");

            this.InvokeVsTest(arguments);

            var errorMessage = "Process is terminated due to StackOverflowException.";
            if (runnerInfo.TargetFramework.StartsWith("netcoreapp2."))
            {
                errorMessage = "Process is terminating due to StackOverflowException.";
            }

            this.ExitCodeEquals(1);
            FileAssert.Contains(diagLogFilePath, errorMessage);
            this.StdErrorContains(errorMessage);
            File.Delete(diagLogFilePath);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void UnhandleExceptionExceptionShouldBeLoggedToDiagLogFile(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var diagLogFilePath = Path.Combine(Path.GetTempPath(), $"std_error_log_{Guid.NewGuid()}.txt");
            File.Delete(diagLogFilePath);

            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject3.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /testcasefilter:ExitwithUnhandleException");
            arguments = string.Concat(arguments, $" /diag:{diagLogFilePath}");

            this.InvokeVsTest(arguments);

            var errorFirstLine = "Test host standard error line: Unhandled Exception: System.InvalidOperationException: Operation is not valid due to the current state of the object.";
            FileAssert.Contains(diagLogFilePath, errorFirstLine);
            File.Delete(diagLogFilePath);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void IncompatibleSourcesWarningShouldBeDisplayedInTheConsole(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var expectedWarningContains = @"Following DLL(s) do not match current settings, which are .NETFramework,Version=v4.5.1 framework and X86 platform. SimpleTestProject3.dll is built for Framework .NETFramework,Version=v4.5.1 and Platform X64";
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject3.dll", "SimpleTestProjectx86.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /testcasefilter:PassingTestx86");

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 0, 0);
            this.ExitCodeEquals(0);

            // When both x64 & x86 DLL is passed x64 dll will be ignored.
            this.StdOutputContains(expectedWarningContains);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void NoIncompatibleSourcesWarningShouldBeDisplayedInTheConsole(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var expectedWarningContains = @"Following DLL(s) do not match current settings, which are .NETFramework,Version=v4.5.1 framework and X86 platform. SimpleTestProjectx86 is built for Framework .NETFramework,Version=v4.5.1 and Platform X86";
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProjectx86.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 0, 0);
            this.ExitCodeEquals(0);
            
            this.StdOutputDoesNotContains(expectedWarningContains);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void IncompatibleSourcesWarningShouldBeDisplayedInTheConsoleOnlyWhenRunningIn32BitOS(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var expectedWarningContains = @"Following DLL(s) do not match current settings, which are .NETFramework,Version=v4.5.1 framework and X86 platform. SimpleTestProject2.dll is built for Framework .NETFramework,Version=v4.5.1 and Platform X64";
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject2.dll");
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue, runnerInfo.InIsolationValue);

            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(1, 1, 1);
            this.ExitCodeEquals(1);

            // If we are running this test on 64 bit OS, it should not output any warning
            if (Environment.Is64BitOperatingSystem)
            {
                this.StdOutputDoesNotContains(expectedWarningContains);
            }
            // If we are running this test on 32 bit OS, it should output warning message
            else
            {
                this.StdOutputContains(expectedWarningContains);
            }
        }
    }
}
