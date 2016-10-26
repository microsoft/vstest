// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.SmokeTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    using global::TestPlatform.TestUtilities;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExecutionTests : IntegrationTestBase
    {
        [TestMethod]
        public void RunAllTestExecution()
        {
            this.InvokeVsTestForExecution(this.GetSampleTestAssembly(), this.GetTestAdapterPath());
            this.ValidateSummaryStatus(1, 1, 1);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
            this.ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
            this.ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");
        }

        [TestMethod]
        public void RunAllTestExecutionWithNETCoreApp()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);
            arguments = string.Concat(arguments, " /Framework:.NETCoreApp,Version=v1.0");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);

            // TODO validate it is running .netcore
        }

        #region Multiple Projects TestCases

        [TestMethod]
        public void RunMultipleTestAssemblies()
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            this.InvokeVsTestForExecution(assemblyPaths, this.GetTestAdapterPath());
            this.ValidateSummaryStatus(2, 2, 2);
        }
        #endregion

        #region Parallel TestCases

        /// <summary>
        /// The run multiple test assemblies in parallel.
        /// </summary>
        [TestMethod]
        public void RunMultipleTestAssembliesInParallel()
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty);
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
        #endregion

        #region MethodName Match
        [TestMethod]
        public void RunSelectedTests()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);
            arguments = string.Concat(arguments, " /Tests:PassingTest");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
        }
        #endregion

        #region Trait TestCases
        [TestMethod]
        public void RunSelectedTestsWithPriorityTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Priority=2\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithCategoryTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"TestCategory=CategoryA\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithTestPropertyTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Property1=Value1\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 0, 1);
        }

        [TestMethod]
        public void RunSelectedTestsWithOrOperatorTrait()
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA|Priority=2)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithAndOperatorTrait()
        {
            Debugger.Launch();
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA&Priority=3)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }
        #endregion

        #region Platform TestCases
        /// <summary>
        /// The run test execution with platform x64.
        /// </summary>
        [TestMethod]
        public void RunTestExecutionWithPlatformx64()
        {
            var platformArg = " /Platform:x64";
            var testhostProcessName = "testhost";
            this.RunTestExecutionWithPlatform(platformArg, testhostProcessName);
        }

        /// <summary>
        /// The run test execution with platform x86.
        /// </summary>
        [TestMethod]
        public void RunTestExecutionWithPlatformx86()
        {
            var platformArg = " /Platform:x86";
            var testhostProcessName = "testhost.x86";
            this.RunTestExecutionWithPlatform(platformArg, testhostProcessName);
        }
        #endregion

        #region RunSettings TestCases

        [TestMethod]
        public void RunTestExecutionWithRunSettingsWithoutParallelAndPlatformX86()
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var runsettingsPath = Path.Combine(
                this.testEnvironment.TestAssetsPath,
                "test_" + Guid.NewGuid() + ".runsettings");
            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                   { "MaxCpuCount", "1" },
                                                   { "TargetPlatform", "x86" },
                                                   { "TargetFrameworkVersion", "Framework45" },
                                                   { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                           };
            IntegrationTestBase.CreateRunSettingsFile(runsettingsPath, runConfigurationDictionary);
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runsettingsPath);
            var testhostProcessName = "testhost.x86";
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);
            cts.Cancel();

            var expectedProcessCreated = 1;

            Assert.AreEqual(
                expectedProcessCreated,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {expectedProcessCreated} actual: {numOfProcessCreatedTask.Result}");
            this.ValidateSummaryStatus(2, 2, 2);

            File.Delete(runsettingsPath);
        }

        [TestMethod]
        public void RunTestExecutionWithRunSettingsWithParallelAndPlatformX64()
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var runsettingsPath = Path.Combine(
                this.testEnvironment.TestAssetsPath,
                "test_" + Guid.NewGuid() + ".runsettings");
            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                   { "MaxCpuCount", "2" },
                                                   { "TargetPlatform", "x64" },
                                                   { "TargetFrameworkVersion", "Framework45" },
                                                   { "TestAdaptersPaths", this.GetTestAdapterPath() }
                                           };
            IntegrationTestBase.CreateRunSettingsFile(runsettingsPath, runConfigurationDictionary);
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), runsettingsPath);
            var testhostProcessName = "testhost";
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);
            cts.Cancel();

            var expectedProcessCreated = 2;
            Assert.AreEqual(
                expectedProcessCreated,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {expectedProcessCreated} actual: {numOfProcessCreatedTask.Result}");
            this.ValidateSummaryStatus(2, 2, 2);
        }
        #endregion

        private string BuildMultipleAssemblyPath(params string[] assetNames)
        {
            var assertFullPaths = new string[assetNames.Length];
            for (var i = 0; i < assetNames.Length; i++)
            {
                assertFullPaths[i] = IntegrationTestBase.EncloseString(this.GetAssetFullPath(assetNames[i]), "\"");
            }

            return string.Join(" ", assertFullPaths);
        }

        private void RunTestExecutionWithPlatform(string platformArg, string testhostProcessName)
        {
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);
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
    }
}