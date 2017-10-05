﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OrderedTests : AcceptanceTestBase
    {

        /// <summary>
        /// Ordered Tests created using earlier versions of Visual Studio(i.e. before VS2017) should work fine.
        /// </summary>
        [CustomDataTestMethod]
        [NETFullTargetFramework(inIsolation: true, inProcess: true)]
        public void OlderOrderedTestsShouldWorkFine(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            if (runnerInfo.RunnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive(" Ordered tests are not supported with .Netcore runner.");
                return;
            }

            var orderedTestFileAbsolutePath = Path.Combine(this.testEnvironment.TestAssetsPath, "MstestV1UnitTestProject", "MixedTests.orderedtest");
            var arguments = PrepareArguments(
                orderedTestFileAbsolutePath,
                this.GetTestAdapterPath(),
                string.Empty,
                runnerInfo.InIsolationValue);

            this.InvokeVsTest(arguments);
            this.ValidatePassedTests("PassingTest1");
            this.ValidatePassedTests("PassingTest2");
            this.ValidateFailedTests("FailingTest1");
            this.ValidateSkippedTests("FailingTest2");
            this.ValidateSummaryStatus(2, 1, 1);
        }

    }
}
