// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.Threading;

    using global::TestPlatform.TestUtilities;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PlatformTests : AcceptanceTestBase
    {
        /// <summary>
        /// The run test execution with platform x64.
        /// </summary>
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestExecutionWithPlatformx64(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var platformArg = " /Platform:x64";
            this.RunTestExecutionWithPlatform(platformArg, "testhost", 1);
        }

        /// <summary>
        /// The run test execution with platform x86.
        /// </summary>
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        [NetCoreTargetFrameworkDataSource]
        public void RunTestExecutionWithPlatformx86(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var platformArg = " /Platform:x86";
            this.RunTestExecutionWithPlatform(platformArg, "testhost.x86", 1);
        }

        private void SetExpectedParams(ref int expectedNumOfProcessCreated, ref string testhostProcessName, string desktopHostProcessName)
        {
            testhostProcessName = desktopHostProcessName;
            expectedNumOfProcessCreated = 1;
        }

        private void RunTestExecutionWithPlatform(string platformArg, string testhostProcessName, int expectedNumOfProcessCreated)
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                this.testEnvironment.InIsolationValue);
            arguments = string.Concat(arguments, platformArg);

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);

            cts.Cancel();

            Assert.AreEqual(
                expectedNumOfProcessCreated,
                numOfProcessCreatedTask.Result.Count,
                $"Number of {testhostProcessName} process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Result.Count} ({ string.Join(", ", numOfProcessCreatedTask.Result) }) args: {arguments} runner path: {this.GetConsoleRunnerPath()}");
            this.ValidateSummaryStatus(1, 1, 1);
        }
    }
}
