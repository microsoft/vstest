// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    [TestClass]
    public class CUITTest : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void CUITRunAllTests(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(this.testEnvironment, runnerInfo);
            CUITRunAll(runnerInfo.RunnerFramework);
        }

        private void CUITRunAll(string runnerFramework)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("CUIT tests are not supported with .Netcore runner.");
                return;
            }

            var assemblyAbsolutePath = testEnvironment.GetTestAsset("CUITTestProject.dll", "net461");
            var arguments = PrepareArguments(
                assemblyAbsolutePath,
                string.Empty,
                string.Empty,
                this.FrameworkArgValue);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(1, 0, 0);
        }
    }
}
