// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public abstract class TestCaseFilterTests : AcceptanceTestBase
    {
        [TestMethod]
        public void RunSelectedTestsWithAndOperatorTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA&Priority=3)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithCategoryTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"TestCategory=CategoryA\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(0, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithClassNameTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"ClassName=SampleUnitTestProject.UnitTest1\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 1);
        }

        [TestMethod]
        public void RunSelectedTestsWithFullyQualifiedNameTrait()
        {
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

        [TestMethod]
        public void RunSelectedTestsWithNameTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Name=PassingTest\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithOrOperatorTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"(TestCategory=CategoryA|Priority=2)\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 1, 0);
        }

        [TestMethod]
        public void RunSelectedTestsWithPriorityTrait()
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /TestCaseFilter:\"Priority=2\"");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }
    }
}
