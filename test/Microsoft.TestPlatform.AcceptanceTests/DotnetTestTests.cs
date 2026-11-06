// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.Diagnostics;
    using System.IO;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DotnetTestTests : AcceptanceTestBase
    {
        [TestMethod]
        // patched dotnet is not published on non-windows systems
        [TestCategory("Windows-Review")]
        [NetCoreTargetFrameworkDataSource]
        public void RunDotnetTestWithCsproj(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var projectName = "SimpleTestProject.csproj";
            var projectPath = this.GetProjectFullPath(projectName);

            var diagnosticDirectory = GetResultsDirectory();
            try
            {
                var diagnosticPath = Path.Combine(diagnosticDirectory, "dotnet-test.log");
                this.InvokeDotnetTest($@"{projectPath} -p:VSTestNoLogo= --diag:""{diagnosticPath}"" --logger:""Console;Verbosity=normal""");

                this.StdOutputContains(GetPatchedConsoleVersion());
                this.ValidateSummaryStatus(1, 1, 1);
                this.ExitCodeEquals(1);
            }
            finally
            {
                TryRemoveDirectory(diagnosticDirectory);
            }
        }


        [TestMethod]
        // patched dotnet is not published on non-windows systems
        [TestCategory("Windows-Review")]
        [NetCoreTargetFrameworkDataSource]
        public void RunDotnetTestWithDll(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPath = this.BuildMultipleAssemblyPath("SimpleTestProject.dll").Trim('\"');
            var diagnosticDirectory = GetResultsDirectory();
            try
            {
                var diagnosticPath = Path.Combine(diagnosticDirectory, "dotnet-test.log");
                this.InvokeDotnetTest($@"{assemblyPath} --diag:""{diagnosticPath}"" --logger:""Console;Verbosity=normal""");

                this.StdOutputContains(GetPatchedConsoleVersion());
                this.ValidateSummaryStatus(1, 1, 1);
                this.ExitCodeEquals(1);
            }
            finally
            {
                TryRemoveDirectory(diagnosticDirectory);
            }
        }

        [TestMethod]
        // patched dotnet is not published on non-windows systems
        [TestCategory("Windows-Review")]
        [NetCoreTargetFrameworkDataSource]
        public void PassInlineSettings(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var projectName = "ParametrizedTestProject.csproj";
            var projectPath = this.GetProjectFullPath(projectName);
            this.InvokeDotnetTest($@"{projectPath} --logger:""Console;Verbosity=normal"" -- TestRunParameters.Parameter(name =\""weburl\"", value=\""http://localhost//def\"")");
            this.ValidateSummaryStatus(1, 0, 0);
            this.ExitCodeEquals(0);
        }

        [TestMethod]
        // patched dotnet is not published on non-windows systems
        [TestCategory("Windows-Review")]
        [NetCoreTargetFrameworkDataSource]
        public void PassInlineSettingsToDll(RunnerInfo runnerInfo)
        {

            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPath = this.BuildMultipleAssemblyPath("ParametrizedTestProject.dll").Trim('\"');
            this.InvokeDotnetTest($@"{assemblyPath} --logger:""Console;Verbosity=normal"" -- TestRunParameters.Parameter(name=\""weburl\"", value=\""http://localhost//def\"")");

            this.ValidateSummaryStatus(1, 0, 0);
            this.ExitCodeEquals(0);
        }

        private static string GetPatchedConsoleVersion()
        {
            var console = Path.Combine(
                IntegrationTestEnvironment.TestPlatformRootDirectory,
                "artifacts",
                IntegrationTestEnvironment.BuildConfiguration,
                CoreRunnerFramework,
                "vstest.console.dll");
            var version = FileVersionInfo.GetVersionInfo(console).ProductVersion;
            Assert.IsFalse(string.IsNullOrEmpty(version), "The locally built console has no product version.");
            return version;
        }
    }
}