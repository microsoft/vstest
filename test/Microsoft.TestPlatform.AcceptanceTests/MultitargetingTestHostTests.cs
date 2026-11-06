// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using System;
    using System.Collections.Generic;
    using System.IO;

    using static AcceptanceTestBase;

    [TestClass]
    public class MultitargetingTestHostTests : AcceptanceTestBase
    {
        [TestMethod]
        [TestCategory("Windows-Review")]
        [NetCoreRunner(NETFX452_48)]
        [NetFrameworkRunner(NETFX452_48)]
        public void TestRunInATesthostThatTargetsTheirChosenNETFramework(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();

            var assemblyPath = this.BuildMultipleAssemblyPath("MultitargetedNetFrameworkProject.dll").Trim('\"');
            var arguments = PrepareArguments(assemblyPath, null, null, this.FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
            arguments += this.FrameworkDiagnosticArgument();
            this.InvokeVsTest(arguments, new Dictionary<string, string>
            {
                ["EXPECTED_TARGET_FRAMEWORK"] = runnerInfo.TargetFramework
            });

            this.ValidateSummaryStatus(passedTestsCount: 1, failedTestsCount: 0, 0);
            TryRemoveDirectory(resultsDir);
        }

        [TestMethod]
        [TestCategory("Windows-Review")]
        [NetCoreRunner(NETFX452_48)]
        [NetFrameworkRunner(NETFX452_48)]
        public void FrameworkOracleShouldRejectAnIncorrectExpectedFramework(RunnerInfo runnerInfo)
        {
            SetTestEnvironment(this.testEnvironment, runnerInfo);
            var resultsDir = GetResultsDirectory();
            try
            {
                var assemblyPath = this.BuildMultipleAssemblyPath("MultitargetedNetFrameworkProject.dll").Trim('\"');
                var arguments = PrepareArguments(assemblyPath, null, null, this.FrameworkArgValue, runnerInfo.InIsolationValue, resultsDirectory: resultsDir);
                arguments += this.FrameworkDiagnosticArgument();
                this.InvokeVsTest(arguments, new Dictionary<string, string>
                {
                    ["EXPECTED_TARGET_FRAMEWORK"] = runnerInfo.TargetFramework == Net452TargetFramework ? Net48TargetFramework : Net452TargetFramework
                });

                this.ValidateSummaryStatus(passedTestsCount: 0, failedTestsCount: 1, skippedTestsCount: 0);
                this.StdOutputContains("Assert.Equal() Failure");
                this.ExitCodeEquals(1);
            }
            finally
            {
                TryRemoveDirectory(resultsDir);
            }
        }

        private string FrameworkDiagnosticArgument()
        {
            var directory = Environment.GetEnvironmentVariable("VSTEST_ACCEPTANCE_DIAGNOSTICS_DIRECTORY")
                ?? Path.Combine(this.testEnvironment.TestArtifactsDirectory, "framework-diagnostics");
            Directory.CreateDirectory(directory);
            return " /Diag:\"" + Path.Combine(directory, "framework-" + Guid.NewGuid().ToString("N") + ".log") + "\"";
        }
    }
}