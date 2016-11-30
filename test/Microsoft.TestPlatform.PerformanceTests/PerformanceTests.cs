// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.PerformanceTests
{
    using Microsoft.TestPlatform.TestUtilities.PerfInstrumentation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The performance tests.
    /// </summary>
    [TestClass]
    public class PerformanceTests : PerformanceTestBase
    {
        [TestMethod]
        public void ExecutionPerformanceTest()
        {
            this.RunExecutionPerformanceTests(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);

            this.ValidateSummaryStatus(1, 1, 1);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
            this.ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
            this.ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");

            this.AnalyzePerfData();
            var actualExecutionTime = this.GetExecutionTime();

            // Sample Assert statement to verify the performance. 500 will be replaced by the actual threshold value.
            Assert.IsTrue(actualExecutionTime < 500);
        }

        [TestMethod]
        public void DiscoveryPerformanceTest()
        {
            this.RunDiscoveryPerformanceTests(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);

            this.ValidateDiscoveredTests(
                "SampleUnitTestProject.UnitTest1.PassingTest",
                "SampleUnitTestProject.UnitTest1.FailingTest",
                "SampleUnitTestProject.UnitTest1.SkippingTest");

            this.AnalyzePerfData();
            var actualDiscoveryTime = this.GetDiscoveryTime();

            // Sample Assert statement to verify the performance. 500 will be replaced by the actual threshold value.
            Assert.IsTrue(actualDiscoveryTime < 500);
        }

        [TestMethod]
        public void VsTestConsolePerformanceTest()
        {
            this.RunExecutionPerformanceTests(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);

            this.ValidateSummaryStatus(1, 1, 1);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
            this.ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
            this.ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");

            this.AnalyzePerfData();
            var actualVsTestTime = this.GetVsTestTime();

            // Sample Assert statement to verify the performance. 1500 will be replaced by the actual threshold value.
            Assert.IsTrue(actualVsTestTime < 1500);
        }

        [TestMethod]
        public void TestHostPerformanceTest()
        {
            this.RunExecutionPerformanceTests(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);

            this.ValidateSummaryStatus(1, 1, 1);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
            this.ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
            this.ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");

            this.AnalyzePerfData();
            var actualTestHostTime = this.GetTestHostTime();

            // Sample Assert statement to verify the performance. 1000 will be replaced by the actual threshold value.
            Assert.IsTrue(actualTestHostTime < 1000);
        }

        [TestMethod]
        public void MsTestV2AdapterPerformanceTest()
        {
            this.RunExecutionPerformanceTests(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty);

            this.ValidateSummaryStatus(1, 1, 1);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
            this.ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
            this.ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");

            this.AnalyzePerfData();

            var actualAdapterTimeTaken = this.GetAdapterExecutionTime("executor://mstestadapter/v2");

            // Sample Assert statement to verify the performance. 300 will be replaced by the actual threshold value.
            Assert.IsTrue(actualAdapterTimeTaken < 300);
        }
    }
}