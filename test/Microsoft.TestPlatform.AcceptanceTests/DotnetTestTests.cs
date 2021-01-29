// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
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

            this.InvokeDotnetTest($@"{projectPath} --logger:""Console;Verbosity=normal""");
            
            // ensure our dev version is used
            this.StdOutputContains("-dev");
            this.ValidateSummaryStatus(1, 1, 1);
            this.ExitCodeEquals(1);
        }


        [TestMethod]
        // patched dotnet is not published on non-windows systems
        [TestCategory("Windows-Review")]
        [NetCoreTargetFrameworkDataSource]
        public void RunDotnetTestWithDll(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPath = this.BuildMultipleAssemblyPath("SimpleTestProject.dll").Trim('\"');
            this.InvokeDotnetTest($@"{assemblyPath} --logger:""Console;Verbosity=normal""");

            // ensure our dev version is used
            this.StdOutputContains("-dev");
            this.ValidateSummaryStatus(1, 1, 1);
            this.ExitCodeEquals(1);
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
    }
}