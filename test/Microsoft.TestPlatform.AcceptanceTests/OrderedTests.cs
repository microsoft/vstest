// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OrderedTests : AcceptanceTestBase
    {
        /// <summary>
        /// Ordered Tests created using earlier versions of Visual Studio(i.e. before VS2017) should work fine.
        /// </summary>
        [TestMethod]
        [TestCategory("Windows-Review")]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        public void OlderOrderedTestsShouldWorkFine(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            if (runnerInfo.RunnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive(" Ordered tests are not supported with .Netcore runner.");
                return;
            }

            var orderedTestFileAbsolutePath = Path.Combine(this.testEnvironment.TestAssetsPath, "MstestV1UnitTestProject");

            if (IntegrationTestEnvironment.BuildConfiguration.Equals("release", StringComparison.OrdinalIgnoreCase))
            {
                orderedTestFileAbsolutePath = Path.Combine(orderedTestFileAbsolutePath, "MixedTestsRelease.orderedtest");
            }
            else
            {
                orderedTestFileAbsolutePath = Path.Combine(orderedTestFileAbsolutePath, "MixedTestsDebug.orderedtest");
            }

            var arguments = PrepareArguments(
                orderedTestFileAbsolutePath,
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                runnerInfo.InIsolationValue, resultsDirectory: resultsDir);

            this.InvokeVsTest(arguments);
            this.ValidatePassedTests("PassingTest1");
            this.ValidatePassedTests("PassingTest2");
            this.ValidateFailedTests("FailingTest1");
            this.ValidateSkippedTests("FailingTest2");
            // Parent test result should fail as inner results contain failing test.
            this.ValidateSummaryStatus(2, 1, 1);
            TryRemoveDirectory(resultsDir);
        }
    }
}
