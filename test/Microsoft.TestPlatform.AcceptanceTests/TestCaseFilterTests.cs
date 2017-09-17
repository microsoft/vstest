// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestCaseFilterTests : AcceptanceTestBase
    {
        [CustomDataTestMethod]
        [NETFullTargetFramework(inIsolation: true, inProcess: true)]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithAndOperatorTrait(RunnnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA&Priority=3)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithCategoryTrait(RunnnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"TestCategory=CategoryA\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithClassNameTrait(RunnnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"ClassName=SampleUnitTestProject.UnitTest1\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithFullyQualifiedNameTrait(RunnnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
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
        public void RunSelectedTestsWithNameTrait(RunnnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Name=PassingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithOrOperatorTrait(RunnnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA|Priority=2)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunSelectedTestsWithPriorityTrait(RunnnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
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
        public void TestCaseFilterShouldWorkIfOnlyPropertyValueGivenInExpression(RunnnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var arguments = PrepareArguments(
                this.testEnvironment.GetTestAsset("SimpleTestProject2.dll"),
                this.GetTestAdapterPath(),
                string.Empty,
                runnerInfo.InIsolationValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:UnitTest1");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }
    }
}
