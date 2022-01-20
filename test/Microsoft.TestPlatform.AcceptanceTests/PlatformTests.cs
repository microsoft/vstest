// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.TestPlatform.TestUtilities;
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

        private void RunTestExecutionWithPlatform(string platformArg, string testhostProcessName, int expectedNumOfProcessCreated)
        {
            using var tempDir = new TempDirectory();

            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty, this.FrameworkArgValue,
                this.testEnvironment.InIsolationValue, resultsDirectory: tempDir.Path);

            arguments = string.Concat(arguments, platformArg, GetDiagArg(tempDir.Path));
            this.InvokeVsTest(arguments);

            AssertExpectedNumberOfHostProcesses(expectedNumOfProcessCreated, tempDir.Path, new[] { testhostProcessName }, arguments, this.GetConsoleRunnerPath());
            this.ValidateSummaryStatus(1, 1, 1);
        }
    }
}
