// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.AcceptanceTests
{
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PortableNugetPackageTests : AcceptanceTestBase
    {
        private static string portablePackageFolder;

        [ClassInitialize]
        public static void ClassInit(TestContext testContext)
        {
            var packageLocation = Path.Combine(IntegrationTestEnvironment.TestPlatformRootDirectory, "artifacts", IntegrationTestEnvironment.BuildConfiguration, "packages");
            var nugetPackage = Directory.EnumerateFiles(packageLocation, "Microsoft.TestPlatform.Portable.*.nupkg").ToList();
            portablePackageFolder = Path.Combine(packageLocation, Path.GetFileNameWithoutExtension(nugetPackage[0]));
            ZipFile.ExtractToDirectory(nugetPackage[0], portablePackageFolder);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Directory.Delete(portablePackageFolder, true);
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void RunMultipleTestAssemblies(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            var assemblyPaths = this.BuildMultipleAssemblyPath("SimpleTestProject.dll", "SimpleTestProject2.dll").Trim('\"');

            this.InvokeVsTestForExecution(assemblyPaths, this.GetTestAdapterPath(), this.FrameworkArgValue, string.Empty);

            this.ValidateSummaryStatus(2, 2, 2);
            this.ExitCodeEquals(1); // failing tests
        }

        [TestMethod]
        [NetFullTargetFrameworkDataSource(inIsolation: true, inProcess: true)]
        [NetCoreTargetFrameworkDataSource]
        public void DiscoverAllTests(RunnerInfo runnerInfo)
        {
            AcceptanceTestBase.SetTestEnvironment(this.testEnvironment, runnerInfo);

            this.InvokeVsTestForDiscovery(this.GetSampleTestAssembly(), this.GetTestAdapterPath(), string.Empty, this.FrameworkArgValue);

            var listOfTests = new[] { "SampleUnitTestProject.UnitTest1.PassingTest", "SampleUnitTestProject.UnitTest1.FailingTest", "SampleUnitTestProject.UnitTest1.SkippingTest" };
            this.ValidateDiscoveredTests(listOfTests);
            this.ExitCodeEquals(0);
        }

        public override string GetConsoleRunnerPath()
        {
            string consoleRunnerPath = string.Empty;

            if (this.IsDesktopRunner())
            {
                consoleRunnerPath = Path.Combine(portablePackageFolder, "tools", "net472", "vstest.console.exe");
            }
            else if (this.IsNetCoreRunner())
            {
                consoleRunnerPath = Path.Combine(this.testEnvironment.ToolsDirectory, @"dotnet\dotnet.exe");
            }
            else
            {
                Assert.Fail("Unknown Runner framework - [{0}]", this.testEnvironment.RunnerFramework);
            }

            Assert.IsTrue(File.Exists(consoleRunnerPath), "GetConsoleRunnerPath: Path not found: {0}", consoleRunnerPath);
            return consoleRunnerPath;
        }

        protected override string SetVSTestConsoleDLLPathInArgs(string args)
        {
            var vstestConsoleDll = Path.Combine(portablePackageFolder, "tools", "netcoreapp2.1", "vstest.console.dll");
            vstestConsoleDll = vstestConsoleDll.AddDoubleQuote();
            args = string.Concat(
                vstestConsoleDll,
                " ",
                args);
            return args;
        }
    }
}
