// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestCaseFilterTests : AcceptanceTestBase
    {
        [CustomDataTestMethod]
        [NETFullTargetFramework(inIsolation: true, inProcess: true)]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithAndOperatorTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA&Priority=3)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithCategoryTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"TestCategory=CategoryA\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithClassNameTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"ClassName=SampleUnitTestProject.UnitTest1\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithFullyQualifiedNameTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(
                arguments,
                " /TestCaseFilter:\"FullyQualifiedName=SampleUnitTestProject.UnitTest1.FailingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithNameTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Name=PassingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithOrOperatorTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA|Priority=2)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithPriorityTrait(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
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
        public void TestCaseFilterShouldWorkIfOnlyPropertyValueGivenInExpression(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.testEnvironment.GetTestAsset("SimpleTestProject2.dll"),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:UnitTest1");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        /// <summary>
        /// Discover tests using mstest v1 adapter with test case filters.
        /// </summary>
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void DiscoverMstestV1TestsWithAndOperatorTrait(RunnerInfo runnerInfo)
        {
            if (runnerInfo.RunnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("Mstest v1 tests not supported with .Netcore runner.");
                return;
            }

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.testEnvironment.GetTestAsset("MstestV1UnitTestProject.dll"),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /listtests /TestCaseFilter:\"(TestCategory!=CategoryA&Priority!=3)\"");

            this.InvokeVsTest(arguments);
            var listOfTests = new string[] {"MstestV1UnitTestProject.UnitTest1.PassingTest", "MstestV1UnitTestProject.UnitTest1.SkippingTest" };
            var listOfNotDiscoveredTests = new string[] {"MstestV1UnitTestProject.UnitTest1.FailingTest" };
            this.ValidateDiscoveredTests(listOfTests);
            this.ValidateTestsNotDiscovered(listOfNotDiscoveredTests);
        }

        /// <summary>
        /// Discover tests using tmi adapter with test case filters.
        /// </summary>
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void DiscoverTmiTestsWithOnlyPropertyValue(RunnerInfo runnerInfo)
        {
            if (runnerInfo.RunnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("Tmi tests not supported with .Netcore runner.");
                return;
            }

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            string testAssemblyPath = this.testEnvironment.GetTestAsset("MstestV1UnitTestProject.dll");
            var arguments = PrepareArguments(
                testAssemblyPath,
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue,
                runnerInfo.InIsolationValue);
            string testSettingsPath = Path.Combine(Path.GetDirectoryName(testAssemblyPath), "MstestV1UnitTestProjectTestSettings.testsettings");
            arguments = string.Concat(arguments, " /listtests /TestCaseFilter:PassingTest /settings:", testSettingsPath);

            this.InvokeVsTest(arguments);
            var listOfTests = new string[] {"MstestV1UnitTestProject.UnitTest1.PassingTest" };
            var listOfNotDiscoveredTests = new string[] {"MstestV1UnitTestProject.UnitTest1.FailingTest", "MstestV1UnitTestProject.UnitTest1.SkippingTest" };
            this.ValidateDiscoveredTests(listOfTests);
            this.ValidateTestsNotDiscovered(listOfNotDiscoveredTests);
        }
    }
}
