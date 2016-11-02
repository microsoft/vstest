// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    using global::TestPlatform.TestUtilities;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExecutionTests : IntegrationTestBase
    {
        protected string framework = ".NETFramework,Version=v4.6";

        [Ignore]
        [TestMethod]
        public void RunAllTestExecutionWithNETCoreApp()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(arguments, " /Framework:.NETCoreApp,Version=v1.0");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);

            // TODO validate it is running .netcore
        }

        [TestMethod]
        public void RunMultipleTestAssemblies()
        {
            var assemblyPaths =
            this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            this.InvokeVsTestForExecution(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.framework);
            this.ValidateSummaryStatus(2, 2, 2);
        }

        /// <summary>
        ///     The run multiple test assemblies in parallel.
        /// </summary>
        [TestMethod]
        public void RunMultipleTestAssembliesInParallel()
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(arguments, " /Parallel");
            arguments = string.Concat(arguments, " /Platform:x86");
            var testhostProcessName = "testhost.x86";
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);
            cts.Cancel();

            Assert.AreEqual(
                2,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {2} actual: {numOfProcessCreatedTask.Result}");
            this.ValidateSummaryStatus(2, 2, 2);
        }

        [TestMethod]
        public void RunSelectedTests()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(arguments, " /Tests:PassingTest");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
        }

        [TestMethod]
        public void RunSelectedTestsWithAndOperatorTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA&Priority=3)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithCategoryTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"TestCategory=CategoryA\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [Ignore]
        [TestMethod]
        public void RunSelectedTestsWithClassNameTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"ClassName=UnitTest1\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [TestMethod]
        public void RunSelectedTestsWithFullyQualifiedNameTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(
                arguments,
                " /TestCaseFilter:\"FullyQualifiedName=SampleUnitTestProject.UnitTest1.FailingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithNameTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Name=PassingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithOrOperatorTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA|Priority=2)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithPriorityTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Priority=2\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        /// <summary>
        ///     The run test execution with platform x64.
        /// </summary>
        [TestMethod]
        public void RunTestExecutionWithPlatformx64()
        {
            var platformArg = " /Platform:x64";
            var testhostProcessName = "testhost";
            this.RunTestExecutionWithPlatform(platformArg, testhostProcessName);
        }

        /// <summary>
        ///     The run test execution with platform x86.
        /// </summary>
        [TestMethod]
        public void RunTestExecutionWithPlatformx86()
        {
            var platformArg = " /Platform:x86";
            var testhostProcessName = "testhost.x86";
            this.RunTestExecutionWithPlatform(platformArg, testhostProcessName);
        }

        [TestMethod]
        public void RunTestExecutionWithRunSettingsWithoutParallelAndPlatformX86()
        {
            var testhostProcessName = "testhost.x86";
            var expectedProcessCreated = 1;
            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "MaxCpuCount", "1" },
                                                         { "TargetPlatform", "x86" },
                                                         { "TargetFrameworkVersion", "Framework45" },
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            this.RunTestWithRunSettings(runConfigurationDictionary, testhostProcessName, expectedProcessCreated);
        }

        [TestMethod]
        public void RunTestExecutionWithRunSettingsWithParallelAndPlatformX64()
        {
            var testhostProcessName = "testhost";
            var expectedProcessCreated = 2;
            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "MaxCpuCount", "2" },
                                                         { "TargetPlatform", "x64" },
                                                         { "TargetFrameworkVersion", "Framework45" },
                                                         { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                                 };
            this.RunTestWithRunSettings(runConfigurationDictionary, testhostProcessName, expectedProcessCreated);
        }

        private void RunTestExecutionWithPlatform(string platformArg, string testhostProcessName)
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.framework);
            arguments = string.Concat(arguments, platformArg);

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);

            cts.Cancel();

            Assert.AreEqual(
                1,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {1} actual: {numOfProcessCreatedTask.Result}");
            this.ValidateSummaryStatus(1, 1, 1);
        }

        private void RunTestWithRunSettings(
            Dictionary<string, string> runConfigurationDictionary,
            string testhostProcessName,
            int expectedProcessCreated)
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var runsettingsPath = Path.Combine(
                this.testEnvironment.TestAssetsPath,
                "test_" + Guid.NewGuid() + ".runsettings");
            IntegrationTestBase.CreateRunSettingsFile(runsettingsPath, runConfigurationDictionary);
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runsettingsPath, this.framework);
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);
            cts.Cancel();

            Assert.AreEqual(
                expectedProcessCreated,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {expectedProcessCreated} actual: {numOfProcessCreatedTask.Result}");
            this.ValidateSummaryStatus(2, 2, 2);
            File.Delete(runsettingsPath);
        }

        [TestMethod]
        public void XUnitRunAllTestExecution()
        {
            this.InvokeVsTestForExecution(this.GetAssetFullPath("XUTestProject.dll"), this.GetTestAdapterPath(UnitTestFramework.XUnit));
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        public void NUnitRunAllTestExecution()
        {
            this.InvokeVsTestForExecution(this.GetAssetFullPath("NUTestProject.dll"), this.GetTestAdapterPath(UnitTestFramework.NUnit));
            this.ValidateSummaryStatus(1, 1, 0);
        }

        /*
        [TestMethod]
        public void ChutzpahRunAllTestExecution()
        {
            this.InvokeVsTestForExecution(this.GetAssetFullPath("ChutzpahTestProject.dll"), this.GetTestAdapterPath(UnitTestFramework.Chutzpah));
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        public void CPPRunAllTestExecution()
        {
            this.InvokeVsTestForExecution(this.GetAssetFullPath("CPPUnitTestProject.dll"), this.GetTestAdapterPath(UnitTestFramework.CPP));
            this.ValidateSummaryStatus(1, 1, 0);
        }*/
    }
}