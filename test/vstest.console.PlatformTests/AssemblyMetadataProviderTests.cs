// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.PerformanceTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AssemblyMetadataProviderTests
    {
        private const string AssetPathFormat = @"{0}\test\TestAssets\{1}\bin\Debug\{2}\{1}.dll";
        private const string PerfAssertMessageFormat = "Perf test failed. Expected Elapsed Time: {0} ms, Actual Elapsed Time: {1} ms";
        private static readonly string VstestRepoRootDir;

        private IAssemblyMetadataProvider assemblyMetadataProvider;

        static AssemblyMetadataProviderTests()
        {
            VstestRepoRootDir = new DirectoryInfo(typeof(AssemblyMetadataProviderTests).GetTypeInfo().Assembly.Location).Parent?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;
        }

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
            this.TestDonetAssemblyArch("SimpleTestProject3", framework, Architecture.X64, expectedElapsedTime: 30);
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp1.0")]
        [DataRow("netcoreapp2.0")]
        public void GetArchitectureShouldReturnCorrentArchForx86Assembly(string framework)
        {
            this.TestDonetAssemblyArch("SimpleTestProject2", framework, Architecture.X86, expectedElapsedTime: 30);
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp1.0")]
        [DataRow("netcoreapp2.0")]
        public void GetArchitectureShouldReturnCorrentArchForAnyCPUAssembly(string framework)
        {
            this.TestDonetAssemblyArch("SimpleTestProject", framework, Architecture.AnyCPU, expectedElapsedTime: 30);
        }

        [TestMethod]
        [DataRow("x86")]
        [DataRow("x64")]
        public void GetArchitectureForNativeDll(string platform)
        {
            var platformPath = platform.Equals("x64") ? platform : string.Empty;
            var assemblyPath = $@"{VstestRepoRootDir}\packages\microsoft.testplatform.testasset.nativecpp\2.0.0\"
                + $@"contentFiles\any\any\{platformPath}\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
            var stopWatch = Stopwatch.StartNew();
            var arch = this.assemblyMetadataProvider.GetArchitecture(assemblyPath);
            stopWatch.Stop();

            Assert.AreEqual(Enum.Parse(typeof(Architecture), platform, ignoreCase: true), arch);
            Console.WriteLine($"Platform:{platform}, Elapsed time:{stopWatch.ElapsedMilliseconds} ms");
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30, string.Format(PerfAssertMessageFormat, 30, stopWatch.ElapsedMilliseconds));
        }

        [TestMethod]
        [DataRow("net451")]
        [DataRow("netcoreapp1.0")]
        [DataRow("netcoreapp2.0")]
        public void GetFrameWorkForDotNetAssembly(string framework)
        {
            var assemblyPath = string.Format(AssetPathFormat, VstestRepoRootDir, "SimpleTestProject3", framework);
            var stopWatch = Stopwatch.StartNew();
            var actualFx = this.assemblyMetadataProvider.GetFrameWork(assemblyPath);
            stopWatch.Stop();

            if (framework.Equals("net451"))
            {
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

            Console.WriteLine($"Framework:{framework}, Elapsed time:{stopWatch.ElapsedMilliseconds} ms");
            Assert.IsTrue(stopWatch.ElapsedMilliseconds < 130, string.Format(PerfAssertMessageFormat, 130, stopWatch.ElapsedMilliseconds));
        }

        [TestMethod]
        public void GetFrameWorkForNativeDll()
        {
                var assemblyPath =
                    $@"{VstestRepoRootDir}\packages\microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
                var stopWatch = Stopwatch.StartNew();
                var fx = this.assemblyMetadataProvider.GetFrameWork(assemblyPath);
                stopWatch.Stop();
                Assert.AreEqual(Framework.DefaultFramework.Name, fx.FullName);

                Console.WriteLine($"Elapsed time:{stopWatch.ElapsedMilliseconds} ms");
                Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30, string.Format(PerfAssertMessageFormat, 30, stopWatch.ElapsedMilliseconds));
        }

        private void TestDonetAssemblyArch(string projectName, string framework, Architecture expectedArch, long expectedElapsedTime)
        {
            var assemblyPath = string.Format(AssetPathFormat, VstestRepoRootDir, projectName, framework);
            var stopWatch = Stopwatch.StartNew();
            var arch = this.assemblyMetadataProvider.GetArchitecture(assemblyPath);
            stopWatch.Stop();
            Assert.AreEqual(expectedArch, arch, $"Expected: {expectedArch} Actual: {arch}");
            Console.WriteLine($"Framework:{framework}, Elapsed time:{stopWatch.ElapsedMilliseconds} ms");
            Assert.IsTrue(
                stopWatch.ElapsedMilliseconds < expectedElapsedTime,
                string.Format(PerfAssertMessageFormat, expectedElapsedTime, stopWatch.ElapsedMilliseconds));
        }
    }
}
