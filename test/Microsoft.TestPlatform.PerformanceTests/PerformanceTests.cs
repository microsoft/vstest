// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TestPlatform.TestUtilities.PerfInstrumentation;

using Microsoft.VisualStudio.TestTools.UnitTesting;

#nullable disable

namespace Microsoft.TestPlatform.PerformanceTests;

/// <summary>
/// The performance tests.
/// </summary>
[TestClass]
public class PerformanceTests : PerformanceTestBase
{
    [TestMethod]
    public void ExecutionPerformanceTest()
    {
        RunExecutionPerformanceTests(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty);

        ValidateSummaryStatus(1, 1, 1);
        ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
        ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
        ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");

        AnalyzePerfData();
        var actualExecutionTime = GetExecutionTime();

        // Sample Assert statement to verify the performance. 500 will be replaced by the actual threshold value.
        Assert.IsTrue(actualExecutionTime < 500);
    }

    [TestMethod]
    public void DiscoveryPerformanceTest()
    {
        RunDiscoveryPerformanceTests(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty);

        ValidateDiscoveredTests(
            "SampleUnitTestProject.UnitTest1.PassingTest",
            "SampleUnitTestProject.UnitTest1.FailingTest",
            "SampleUnitTestProject.UnitTest1.SkippingTest");

        AnalyzePerfData();
        var actualDiscoveryTime = GetDiscoveryTime();

        // Sample Assert statement to verify the performance. 500 will be replaced by the actual threshold value.
        Assert.IsTrue(actualDiscoveryTime < 500);
    }

    [TestMethod]
    public void VsTestConsolePerformanceTest()
    {
        RunExecutionPerformanceTests(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty);

        ValidateSummaryStatus(1, 1, 1);
        ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
        ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
        ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");

        AnalyzePerfData();
        var actualVsTestTime = GetVsTestTime();

        // Sample Assert statement to verify the performance. 1500 will be replaced by the actual threshold value.
        Assert.IsTrue(actualVsTestTime < 1500);
    }

    [TestMethod]
    public void TestHostPerformanceTest()
    {
        RunExecutionPerformanceTests(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty);

        ValidateSummaryStatus(1, 1, 1);
        ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
        ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
        ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");

        AnalyzePerfData();
        var actualTestHostTime = GetTestHostTime();

        // Sample Assert statement to verify the performance. 1000 will be replaced by the actual threshold value.
        Assert.IsTrue(actualTestHostTime < 1000);
    }

    [TestMethod]
    public void MsTestV2AdapterPerformanceTest()
    {
        RunExecutionPerformanceTests(GetSampleTestAssembly(), GetTestAdapterPath(), string.Empty);

        ValidateSummaryStatus(1, 1, 1);
        ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
        ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
        ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");

        AnalyzePerfData();

        var actualAdapterTimeTaken = GetAdapterExecutionTime("executor://mstestadapter/v2");

        // Sample Assert statement to verify the performance. 300 will be replaced by the actual threshold value.
        Assert.IsTrue(actualAdapterTimeTaken < 300);
    }
}
