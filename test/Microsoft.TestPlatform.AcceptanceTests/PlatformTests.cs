﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.Threading;
    using System.Threading.Tasks;
    using global::TestPlatform.TestUtilities;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    // monitoring the processes does not work correctly
    [TestCategory("Windows-Review")]
    public class PlatformTests : AcceptanceTestBase
    {
        /// <summary>
        /// The run test execution with platform x64.
        /// </summary>
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public async Task RunTestExecutionWithPlatformx64(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var platformArg = " /Platform:x64";
            await RunTestExecutionWithPlatform(platformArg, "testhost", 1);
        }

        /// <summary>
        /// The run test execution with platform x86.
        /// </summary>
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public async Task RunTestExecutionWithPlatformx86(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var platformArg = " /Platform:x86";
            await RunTestExecutionWithPlatform(platformArg, "testhost.x86", 1);
        }

        private void SetExpectedParams(ref int expectedNumOfProcessCreated, ref string testhostProcessName, string desktopHostProcessName)
        {
            testhostProcessName = desktopHostProcessName;
            expectedNumOfProcessCreated = 1;
        }

        private async Task RunTestExecutionWithPlatform(string platformArg, string testhostProcessName, int expectedNumOfProcessCreated)
        {
            var resultsDir = GetResultsDirectory();

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                this.testEnvironment.InIsolationValue, resultsDirectory: resultsDir);
            arguments = string.Concat(arguments, platformArg);

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = await NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);

            cts.Cancel();

            Assert.AreEqual(
                expectedNumOfProcessCreated,
                numOfProcessCreatedTask.Count,
                $"Number of {testhostProcessName} process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Count} ({ string.Join(", ", numOfProcessCreatedTask) }) args: {arguments} runner path: {this.GetConsoleRunnerPath()}");
            this.ValidateSummaryStatus(1, 1, 1);
            TryRemoveDirectory(resultsDir);
        }
    }
}
