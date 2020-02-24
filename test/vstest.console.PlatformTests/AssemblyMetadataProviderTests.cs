// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.PlatformTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Utilities.Helpers;
    using Utilities.Helpers.Interfaces;

    [TestClass]
    public class AssemblyMetadataProviderTests : IntegrationTestBase
    {
        private const int ExpectedTimeForFindingArchForDotNetAssembly = 15; // In milliseconds.
        private const string PerfAssertMessageFormat = "Expected Elapsed Time: {0} ms, Actual Elapsed Time: {1} ms";

        private IAssemblyMetadataProvider assemblyMetadataProvider;

        private Mock<IFileHelper> fileHelperMock;

        private FileHelper fileHelper;

        private bool isManagedAssemblyArchitectureTest;

        public AssemblyMetadataProviderTests()
        {
            this.fileHelper = new FileHelper();
            this.fileHelperMock = new Mock<IFileHelper>();
            this.isManagedAssemblyArchitectureTest = false;
            this.assemblyMetadataProvider = new AssemblyMetadataProvider(this.fileHelperMock.Object);

            this.fileHelperMock.Setup(f =>
                    f.GetStream(It.IsAny<string>(), FileMode.Open, FileAccess.Read))
                .Returns<string, FileMode, FileAccess>((filePath, mode, access) => this.fileHelper.GetStream(filePath, mode, access));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (this.isManagedAssemblyArchitectureTest == false)
            {
                this.fileHelperMock.Verify(
                    f => f.GetStream(It.IsAny<string>(), FileMode.Open, FileAccess.Read), Times.Once);
            }
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp2.1")]
        public void GetArchitectureShouldReturnCorrentArchForx64Assembly(string framework)
        {
            this.TestDotnetAssemblyArch("SimpleTestProject3", framework, Architecture.X64, expectedElapsedTime: ExpectedTimeForFindingArchForDotNetAssembly);
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp2.1")]
        public void GetArchitectureShouldReturnCorrentArchForx86Assembly(string framework)
        {
            this.TestDotnetAssemblyArch("SimpleTestProjectx86", framework, Architecture.X86, expectedElapsedTime: ExpectedTimeForFindingArchForDotNetAssembly);
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp2.1")]
        public void GetArchitectureShouldReturnCorrentArchForAnyCPUAssembly(string framework)
        {
            this.TestDotnetAssemblyArch("SimpleTestProject", framework, Architecture.AnyCPU, expectedElapsedTime: ExpectedTimeForFindingArchForDotNetAssembly);
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp2.1")]
        public void GetArchitectureShouldReturnCorrentArchForARMAssembly(string framework)
        {
            this.TestDotnetAssemblyArch("SimpleTestProjectARM", framework, Architecture.ARM, expectedElapsedTime: ExpectedTimeForFindingArchForDotNetAssembly);
        }

        [TestMethod]
        [DataRow("x86")]
        [DataRow("x64")]
        public void GetArchitectureForNativeDll(string platform)
        {
            var expectedElapsedTime = 5;
            var platformPath = platform.Equals("x64") ? platform : string.Empty;
            var assemblyPath = $@"{this.testEnvironment.PackageDirectory}\microsoft.testplatform.testasset.nativecpp\2.0.0\"
                + $@"contentFiles\any\any\{platformPath}\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
            this.LoadAssemblyIntoMemory(assemblyPath);
            var stopWatch = Stopwatch.StartNew();
            var arch = this.assemblyMetadataProvider.GetArchitecture(assemblyPath);
            stopWatch.Stop();

            Console.WriteLine("Platform:{0}, {1}", platform, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
            Assert.AreEqual(Enum.Parse(typeof(Architecture), platform, ignoreCase: true), arch);

            // We should not assert on time elapsed, it will vary depending on machine, & their state, commenting below assert
            // Assert.IsTrue(stopWatch.ElapsedMilliseconds < expectedElapsedTime, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp2.1")]
        public void GetFrameWorkForDotNetAssembly(string framework)
        {
            var expectedElapsedTime = 5;
            var assemblyPath = this.testEnvironment.GetTestAsset("SimpleTestProject3.dll", framework);
            this.LoadAssemblyIntoMemory(assemblyPath);
            var stopWatch = Stopwatch.StartNew();
            var actualFx = this.assemblyMetadataProvider.GetFrameWork(assemblyPath);
            stopWatch.Stop();

            if (framework.Equals("net451"))
            {
                // Reason is unknown for why full framework it is taking more time. Need to investigate.
                expectedElapsedTime = 100;
                Assert.AreEqual(Constants.DotNetFramework451, actualFx.FullName);
            }
            else
            {
                Assert.AreEqual(".NETCoreApp,Version=v2.1", actualFx.FullName);
            }

            Console.WriteLine("Framework:{0}, {1}", framework, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));

            // We should not assert on time elapsed, it will vary depending on machine, & their state.
            // Assert.IsTrue(stopWatch.ElapsedMilliseconds < expectedElapsedTime, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
        }

        [TestMethod]
        public void GetFrameWorkForNativeDll()
        {
            var expectedElapsedTime = 5;
            var assemblyPath = $@"{this.testEnvironment.PackageDirectory}\microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
            this.LoadAssemblyIntoMemory(assemblyPath);
            var stopWatch = Stopwatch.StartNew();
            var fx = this.assemblyMetadataProvider.GetFrameWork(assemblyPath);
            stopWatch.Stop();

            Console.WriteLine(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds);
            Assert.AreEqual(Framework.DefaultFramework.Name, fx.FullName);

            // We should not assert on time elapsed, it will vary depending on machine, & their state.
            // Assert.IsTrue(stopWatch.ElapsedMilliseconds < expectedElapsedTime, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
        }

        private void TestDotnetAssemblyArch(string projectName, string framework, Architecture expectedArch, long expectedElapsedTime)
        {
            this.isManagedAssemblyArchitectureTest = true;
            var assemblyPath = this.testEnvironment.GetTestAsset(projectName + ".dll", framework);
            this.LoadAssemblyIntoMemory(assemblyPath);
            var stopWatch = Stopwatch.StartNew();
            var arch = this.assemblyMetadataProvider.GetArchitecture(assemblyPath);
            stopWatch.Stop();

            Console.WriteLine("Framework:{0}, {1}", framework, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
            Assert.AreEqual(expectedArch, arch, $"Expected: {expectedArch} Actual: {arch}");

            // We should not assert on time elapsed, it will vary depending on machine, & their state.
            // Assert.IsTrue(stopWatch.ElapsedMilliseconds < expectedElapsedTime, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
        }

        private void LoadAssemblyIntoMemory(string assemblyPath)
        {
            // Load the file into RAM in ahead to avoid performance number(expectedElapsedTime) dependence on disk read time.
            File.ReadAllBytes(assemblyPath);
        }
    }
}
