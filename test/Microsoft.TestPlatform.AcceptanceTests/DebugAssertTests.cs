// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DebugAssertTests : AcceptanceTestBase
    {
        [TestMethod]
        // this is core only, there is nothing we can do about Debug.Assert crashing the process on framework
        [NetCoreTargetFrameworkDataSource(useDesktopRunner: false)]
        public void RunningTestWithAFailingDebugAssertDoesNotCrashTheHostingProcess(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var projectName = "CrashingOnDebugAssertTestProject.csproj";
            var projectPath = this.GetProjectFullPath(projectName);
            this.InvokeDotnetTest(projectPath);

            // this will have failed tests when it works and crash the process when it does not
            // because crashin processes is what a failed Debug.Assert does by default
            this.ValidateSummaryStatus(1, 0, 0);
        }
    }
}
