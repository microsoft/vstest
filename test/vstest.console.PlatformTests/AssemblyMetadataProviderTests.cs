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

    [TestClass]
    public class AssemblyMetadataProviderTests : IntegrationTestBase
    {
        private const int ExpectedTimeForFindingArchForDotNetAssembly = 10; // In milliseconds.
        private const string PerfAssertMessageFormat = "Expected Elapsed Time: {0} ms, Actual Elapsed Time: {1} ms";

        private IAssemblyMetadataProvider assemblyMetadataProvider;

        public AssemblyMetadataProviderTests()
        {
            this.assemblyMetadataProvider = new AssemblyMetadataProvider();
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp1.0")]
        [DataRow("netcoreapp2.0")]
        public void GetArchitectureShouldReturnCorrentArchForx64Assembly(string framework)
        {
            this.TestDotnetAssemblyArch("SimpleTestProject3", framework, Architecture.X64, expectedElapsedTime: ExpectedTimeForFindingArchForDotNetAssembly);
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp1.0")]
        [DataRow("netcoreapp2.0")]
        public void GetArchitectureShouldReturnCorrentArchForx86Assembly(string framework)
        {
            this.TestDotnetAssemblyArch("SimpleTestProjectx86", framework, Architecture.X86, expectedElapsedTime: ExpectedTimeForFindingArchForDotNetAssembly);
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp1.0")]
        [DataRow("netcoreapp2.0")]
        public void GetArchitectureShouldReturnCorrentArchForAnyCPUAssembly(string framework)
        {
            this.TestDotnetAssemblyArch("SimpleTestProject", framework, Architecture.AnyCPU, expectedElapsedTime: ExpectedTimeForFindingArchForDotNetAssembly);
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp1.0")]
        [DataRow("netcoreapp2.0")]
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

            Assert.AreEqual(Enum.Parse(typeof(Architecture), platform, ignoreCase: true), arch);
            Console.WriteLine("Platform:{0}, {1}", platform, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < expectedElapsedTime, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp1.0")]
        [DataRow("netcoreapp2.0")]
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
                Assert.AreEqual(actualFx.FullName, Constants.DotNetFramework451);
            }
            else if (framework.Equals("netcoreapp1.0"))
            {
                Assert.AreEqual(actualFx.FullName, Constants.DotNetFrameworkCore10);
            }
            else
            {
                Assert.AreEqual(actualFx.FullName, ".NETCoreApp,Version=v2.0");
            }

            Console.WriteLine("Framework:{0}, {1}", framework, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < expectedElapsedTime, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
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
            Assert.AreEqual(Framework.DefaultFramework.Name, fx.FullName);

            Console.WriteLine(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds);
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < expectedElapsedTime, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
        }

        private void TestDotnetAssemblyArch(string projectName, string framework, Architecture expectedArch, long expectedElapsedTime)
        {
            var assemblyPath = this.testEnvironment.GetTestAsset(projectName + ".dll", framework);
            this.LoadAssemblyIntoMemory(assemblyPath);
            var stopWatch = Stopwatch.StartNew();
            var arch = this.assemblyMetadataProvider.GetArchitecture(assemblyPath);
            stopWatch.Stop();
            Assert.AreEqual(expectedArch, arch, $"Expected: {expectedArch} Actual: {arch}");
            Console.WriteLine("Framework:{0}, {1}", framework, string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
            Assert.IsTrue(
                stopWatch.ElapsedMilliseconds < expectedElapsedTime,
                string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
        }

        private void LoadAssemblyIntoMemory(string assemblyPath)
        {
            // Load the file into RAM in ahead to avoid perf number(expectedElapsedTime) dependence on disk read time.
            File.ReadAllBytes(assemblyPath);
        }
    }
}
