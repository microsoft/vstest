// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using FluentAssertions.Extensions;

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

        actualExecutionTime.Should().BeLessOrEqualTo(500.Milliseconds());
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

        actualDiscoveryTime.Should().BeLessOrEqualTo(500.Milliseconds());
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

        actualVsTestTime.Should().BeLessOrEqualTo(1500.Milliseconds());
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

        actualTestHostTime.Should().BeLessOrEqualTo(1000.Milliseconds());
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

        actualAdapterTimeTaken.Should().BeLessOrEqualTo(300.Milliseconds());
    }
}
