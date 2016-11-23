// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.Threading;

    using global::TestPlatform.TestUtilities;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public abstract class PlatformTests : AcceptanceTestBase
    {
        /// <summary>
        /// The run test execution with platform x64.
        /// </summary>
        [TestMethod]
        public void RunTestExecutionWithPlatformx64()
        {
            var platformArg = " /Platform:x64";
            var testhostProcessName = "testhost";
            this.RunTestExecutionWithPlatform(platformArg, testhostProcessName);
        }

        /// <summary>
        /// The run test execution with platform x86.
        /// </summary>
        [TestMethod]
        public void RunTestExecutionWithPlatformx86()
        {
            var platformArg = " /Platform:x86";
            var testhostProcessName = "testhost.x86";
            this.RunTestExecutionWithPlatform(platformArg, testhostProcessName);
        }

        private void RunTestExecutionWithPlatform(string platformArg, string testhostProcessName)
        {
            var arguments = PrepareArguments(
                this.GetSampleTestAssembly(),
                this.GetTestAdapterPath(),
                string.Empty,
                this.FrameworkArgValue);
            arguments = string.Concat(arguments, platformArg);

            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);

            cts.Cancel();

            Assert.AreEqual(
                1,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {1} actual: {numOfProcessCreatedTask.Result}");
            this.ValidateSummaryStatus(1, 1, 1);
        }
    }
}
