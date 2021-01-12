// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System;
    using System.IO;
    using System.Threading;

    using global::TestPlatform.TestUtilities;

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
            // TODO: figure out this path relative to the project
            this.InvokeDotnetTest($@"{projectPath} --logger:""Console;Verbosity=normal"" -p:VSTestConsolePath=""C:\p\vstest\artifacts\Debug\netcoreapp2.1\vstest.console.dll""");
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
            // TODO: figure out this path relative to the project
            var vsTestConsolePath = Environment.GetEnvironmentVariable("VSTEST_CONSOLE_PATH");
            try
            {
                Environment.SetEnvironmentVariable("VSTEST_CONSOLE_PATH", @"C:\p\vstest\artifacts\Debug\netcoreapp2.1\vstest.console.dll");
                this.InvokeDotnetTest($@"{assemblyPath} --logger:""Console;Verbosity=normal""");
            }
            finally
            {
                Environment.SetEnvironmentVariable("VSTEST_CONSOLE_PATH", vsTestConsolePath);
            }

            //this.StdOutputContains("-dev");
            this.ValidateSummaryStatus(1, 1, 1);
            this.ExitCodeEquals(1);
        }
    }
}