﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestCaseFilterTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void RunSelectedTestsWithAndOperatorTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA&Priority=3)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSelectedTestsWithCategoryTraitInMixCase(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"TestCategory=Categorya\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSelectedTestsWithClassNameTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"ClassName=SampleUnitTestProject.UnitTest1\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSelectedTestsWithFullyQualifiedNameTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
            arguments = string.Concat(
                arguments,
                " /TestCaseFilter:\"FullyQualifiedName=SampleUnitTestProject.UnitTest1.FailingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSelectedTestsWithNameTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Name=PassingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSelectedTestsWithOrOperatorTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA|Priority=2)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunSelectedTestsWithPriorityTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Priority=2\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
            TryRemoveDirectory(resultsDir);
        }

        /// <summary>
        /// In case TestCaseFilter is provide without any property like Name or ClassName. ex. /TestCaseFilter:"UnitTest1"
        /// this command should provide same results as /TestCaseFilter:"FullyQualifiedName~UnitTest1".
        /// </summary>
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void TestCaseFilterShouldWorkIfOnlyPropertyValueGivenInExpression(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(
                this.testEnvironment.GetTestAsset("SimpleTestProject2.dll"),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " /TestCaseFilter:UnitTest1");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
            TryRemoveDirectory(resultsDir);
        }
    }
}
