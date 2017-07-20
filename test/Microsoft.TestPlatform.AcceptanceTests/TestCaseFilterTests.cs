// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestCaseFilterTests : AcceptanceTestBase
    {
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithAndOperatorTrait(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA&Priority=3)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithCategoryTrait(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"TestCategory=CategoryA\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithClassNameTrait(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"ClassName=SampleUnitTestProject.UnitTest1\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithFullyQualifiedNameTrait(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(
                arguments,
                " /TestCaseFilter:\"FullyQualifiedName=SampleUnitTestProject.UnitTest1.FailingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithNameTrait(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Name=PassingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithOrOperatorTrait(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA|Priority=2)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithPriorityTrait(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Priority=2\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        /// <summary>
        /// In case TestCaseFilter is provide without any property like Name or ClassName. ex. /TestCaseFilter:"UnitTest1"
        /// this command should provide same results as /TestCaseFilter:"FullyQualifiedName~UnitTest1".
        /// </summary>
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void TestCaseFilterShouldWorkIfOnlyPropertyValueGivenInExpression(string runnerFramework, string targetFramework, string targetRuntime)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerFramework, targetFramework, targetRuntime);

            var arguments = PrepareArguments(
                this.testEnvironment.GetTestAsset("SimpleTestProject2.dll"),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:UnitTest1");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }
    }
}
