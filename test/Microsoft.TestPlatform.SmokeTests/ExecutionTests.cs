﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.SmokeTests
{
    using Microsoft.TestPlatform.TestUtilities;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ExecutionTests : IntegrationTestBase
    {
        [TestMethod]
        public void RunAllTestExecution()
        {
            this.InvokeVsTestForExecution(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), ".NETFramework,Version=v4.5.1");
            this.ValidateSummaryStatus(1, 1, 1);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
            this.ValidateFailedTests("SampleUnitTestProject.UnitTest1.FailingTest");
            this.ValidateSkippedTests("SampleUnitTestProject.UnitTest1.SkippingTest");
        }

        [TestMethod]
        public void RunSelectedTests()
        {
            var resultsDir = GetResultsDirectory();
            var arguments = PrepareArguments(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, ".NETFramework,Version=v4.5.1", resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, " /Tests:PassingTest");
            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
            this.ValidatePassedTests("SampleUnitTestProject.UnitTest1.PassingTest");
            TryRemoveDirectory(resultsDir);
        }
    }
}
