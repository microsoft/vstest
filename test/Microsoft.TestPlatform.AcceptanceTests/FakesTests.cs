// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FakesTests : AcceptanceTestBase
    {
        private const string SimpleTestSettingsContent =
            @"<?xml version='1.0' encoding='UTF-8'?>
              <TestSettings xmlns='http://microsoft.com/schemas/VisualStudio/TeamTest/2010' name='TestSettings1' id='bb640a13-3a47-4bc5-a7bf-00bfefc1d36e'>
                 <Description>These are default test settings for a local test run.</Description>
                 <Deployment enabled='false' />
                 <Execution hostProcessPlatform='MSIL'>
                    <TestTypeSpecific>
                       <UnitTestRunConfig testTypeId='13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b'>
                          <AssemblyResolution>
                             <TestDirectory useLoadContext='true' />
                          </AssemblyResolution>
                       </UnitTestRunConfig>
                    </TestTypeSpecific>
                    <AgentRule name='LocalMachineDefaultRole'>
                    </AgentRule>
                 </Execution>
                 <Properties />
              </TestSettings>";

        public FakesTests()
        {
            this.testEnvironment.portableRunner = true;
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void RunSimpleFakeTests(RunnerInfo runnerInfo)
        {
            RunFakesTests(runnerInfo, string.Empty, "FakesTestProject");
        }

        [CustomDataTestMethod]
        [NETFullTargetFramework]
        public void RunSimpleFakeTestsWithTestSettings(RunnerInfo runnerInfo)
        {
            var testsettingsFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".testsettings");
            File.AppendAllText(testsettingsFile, SimpleTestSettingsContent);

            var additionalArgs = $" /settings:{testsettingsFile}";
            RunFakesTests(runnerInfo, additionalArgs, "MSTestV1FakesProject");
            File.Delete(testsettingsFile);
        }

        private void RunFakesTests(RunnerInfo runnerInfo, string additionalArgs, string projectName)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            if (runnerInfo.RunnerFramework.StartsWith("netcoreapp"))
            {
                Assert.Inconclusive("Fakes not supported for .NET core runner");
            }
            var platforms = new string[] {"", "x64"};
            var configs = new string[] {"debug", "release"};
            foreach (var platform in platforms)
            {
                foreach (var config in configs)
                {
                    string assemblyRelativePathFormat =
                        @"microsoft.testPlatform.testassets.fakes\1.0.0\contentFiles\any\any\{0}\{1}\{2}\{0}.dll";
                    var assemblyRelativePath = platform.Equals("x64", StringComparison.OrdinalIgnoreCase)
                        ? string.Format(assemblyRelativePathFormat, projectName, platform, config)
                        : string.Format(assemblyRelativePathFormat, projectName, "", config);
                    var assemblyAbsolutePath = Path.Combine(this.testEnvironment.PackageDirectory, assemblyRelativePath);
                    var args = string.Concat(assemblyAbsolutePath, $" {additionalArgs}");
                    if (platform.Equals("x64", StringComparison.OrdinalIgnoreCase))
                    {
                        args = string.Concat(args, " /platform:x64");
                    }

                    this.InvokeVsTest(args);
                    this.ValidateSummaryStatus(1, 0, 0);
                }
            }
        }
    }
}
