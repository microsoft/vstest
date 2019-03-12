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
    public class DisableAppdomainTests : AcceptanceTestBase
    {
        [TestMethod]
        [NetFullTargetFrameworkDataSource]
        public void DisableAppdomainTest(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(this.testEnvironment, runnerInfo);
            RunTests(runnerInfo.RunnerFramework);
        }

        private void RunTests(string runnerFramework)
        {
            if (runnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("This test is not meant for .netcore.");
                return;
            }

            var runConfigurationDictionary = new Dictionary<string, string>
                                                 {
                                                         { "DisableAppDomain", "true" }
                                                 };

            var diableAppdomainTest1 = testEnvironment.GetTestAsset("DisableAppdomainTest1.dll", "net451");
            var diableAppdomainTest2 = testEnvironment.GetTestAsset("DisableAppdomainTest2.dll", "net451");
            var arguments = PrepareArguments(
                string.Format("{0}\" \"{1}", diableAppdomainTest1, diableAppdomainTest2),
                string.Empty,
                GetRunsettingsFilePath(runConfigurationDictionary),
                this.FrameworkArgValue);

            this.InvokeVsTest(arguments);
            this.ValidateSummaryStatus(2, 0, 0);
        }

        private string GetRunsettingsFilePath(Dictionary<string, string> runConfigurationDictionary)
        {
            var runsettingsPath = Path.Combine(
                Path.GetTempPath(),
                "test_" + Guid.NewGuid() + ".runsettings");
            CreateRunSettingsFile(runsettingsPath, runConfigurationDictionary);
            return runsettingsPath;
        }
    }
}
