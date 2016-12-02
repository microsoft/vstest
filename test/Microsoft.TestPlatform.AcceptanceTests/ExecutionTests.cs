// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.Threading;

    using global::TestPlatform.TestUtilities;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public abstract class ExecutionTests : AcceptanceTestBase
    {
        [TestMethod]
        public void RunMultipleTestAssemblies()
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            this.InvokeVsTestForExecution(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            this.ValidateSummaryStatus(2, 2, 2);
        }

        [TestMethod]
        public void RunMultipleTestAssembliesInParallel()
        {
            var assemblyPaths =
                this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPaths, this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);
            arguments = string.Concat(arguments, " /Parallel");
            arguments = string.Concat(arguments, " /Platform:x86");
            var testhostProcessName = "testhost.x86";
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                testhostProcessName);

            this.InvokeVsTest(arguments);
            cts.Cancel();

            Assert.AreEqual(
                2,
                numOfProcessCreatedTask.Result,
                $"Number of {testhostProcessName} process created, expected: {2} actual: {numOfProcessCreatedTask.Result}");
            this.ValidateSummaryStatus(2, 2, 2);
        }
    }
}