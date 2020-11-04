// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using static AcceptanceTestBase;

    [TestClass]
    public class MultitargetingTestHostTests : AcceptanceTestBase
    {
        [TestMethod]
        // the underlying test is using xUnit to avoid AppDomain enhancements in MSTest that make this pass even without multitargetting
        // xUnit supports net452 onwards, so that is why this starts at net452, I also don't test all framework versions
        [NetCoreRunner(NETFX452_48)]
        [NetFrameworkRunner(NETFX452_48)]
        public void RunningTestWithAFailingDebugAssertDoesNotCrashTheHostingProcess(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPath = this.BuildMultipleAssemblyPath("MultitargetedNetFrameworkProject.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPath, null, null, this.FrameworkArgValue, runnerInfo.InIsolationValue);
            this.InvokeVsTest(arguments);

            this.ValidateSummaryStatus(passedTestsCount: 1, failedTestsCount: 0, 0);
        }
    }
}