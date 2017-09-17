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
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunTestExecutionWithPlatformx64(RunnnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var platformArg = " /Platform:x64";
            string testhostProcessName = string.Empty;
            int expectedNumOfProcessCreated = 0;
            string desktopHostProcessName = "testhost";

            SetExpectedParams(ref expectedNumOfProcessCreated, ref testhostProcessName, desktopHostProcessName);
            this.RunTestExecutionWithPlatform(platformArg, testhostProcessName, expectedNumOfProcessCreated);
        }

        /// <summary>
        /// The run test execution with platform x86.
        /// </summary>
        [CustomDataTestMethod]
        [NETFullTargetFramework]
        [NETCORETargetFramework]
        public void RunTestExecutionWithPlatformx86(RunnnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var platformArg = " /Platform:x86";
            string testhostProcessName = string.Empty;
            int expectedNumOfProcessCreated = 0;
            string desktopHostProcessName = "testhost.x86";

            SetExpectedParams(ref expectedNumOfProcessCreated, ref testhostProcessName, desktopHostProcessName);
            this.RunTestExecutionWithPlatform(platformArg, testhostProcessName, expectedNumOfProcessCreated);
        }

        private void SetExpectedParams(ref int expectedNumOfProcessCreated, ref string testhostProcessName, string desktopHostProcessName)
        {
            if (this.IsDesktopTargetFramework())
            {
                testhostProcessName = desktopHostProcessName;
                expectedNumOfProcessCreated = 1;
            }
            else
            {
                testhostProcessName = "dotnet";
                if (this.IsDesktopRunner())
                {
                    expectedNumOfProcessCreated = 1;
                }
                else
                {
                    expectedNumOfProcessCreated = 2;
                }
            }
        }

        private void RunTestExecutionWithPlatform(string platformArg, string testhostProcessName, int expectedNumOfProcessCreated)
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
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
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {expectedNumOfProcessCreated} actual: {numOfProcessCreatedTask.Result} args: {arguments} runner path: {this.GetConsoleRunnerPath()}");
            this.ValidateSummaryStatus(1, 1, 1);
        }
    }
}
