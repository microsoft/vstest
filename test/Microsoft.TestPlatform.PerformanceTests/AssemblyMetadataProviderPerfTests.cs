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
    public class AssemblyMetadataProviderPerfTests
    {
        private IAssemblyMetadataProvider assemblyMetadataProvider;
        private static string rootDir;

        static AssemblyMetadataProviderPerfTests()
        {
            rootDir = new DirectoryInfo(typeof(AssemblyMetadataProviderPerfTests).GetTypeInfo().Assembly.Location).Parent?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;
        }

        public AssemblyMetadataProviderPerfTests()
        {
            this.assemblyMetadataProvider = new AssemblyMetadataProvider();
        }

        [TestMethod]
        public void GetArchitectureForDotNetAssembly()
        {
            var frameworks = new[] { "net451", "netcoreapp1.0"};
            foreach (var fx in frameworks)
            {
                var assemblyPath =
                    $@"{rootDir}\test\TestAssets\SimpleTestProject3\bin\Debug\{fx}\SimpleTestProject3.dll";
                var stopWatch = Stopwatch.StartNew();
                var arch = assemblyMetadataProvider.GetArchitecture(assemblyPath);
                stopWatch.Stop();
                Assert.AreEqual(Architecture.X64, arch);
                Console.WriteLine($"Framework:{fx}, Elapsed time:{stopWatch.ElapsedMilliseconds} ms");
                Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30);
            }
        }

        [TestMethod]
        public void GetArchitectureForNativeDll()
        {
            var platforms = new[] { "", "x64" };
            foreach (var platform in platforms)
            {
                var assemblyPath =
                    $@"{rootDir}\packages\microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\{platform}\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
                var stopWatch = Stopwatch.StartNew();
                var arch = assemblyMetadataProvider.GetArchitecture(assemblyPath);
                stopWatch.Stop();
                if (platform.Equals(platforms[1]))
                {
                    Assert.AreEqual(Architecture.X64, arch);
                }
                else
                {
                    Assert.AreEqual(Architecture.X86, arch);
                }

                Console.WriteLine($"Platform:{platform}, Elapsed time:{stopWatch.ElapsedMilliseconds} ms");
                Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30);
            }
        }

        [TestMethod]
        public void GetFrameWorkForDotNetAssembly()
        {
            var frameworks = new[] { "net451", "netcoreapp1.0" };
            foreach (var fx in frameworks)
            {
                var assemblyPath =
                    $@"{rootDir}\test\TestAssets\SimpleTestProject3\bin\Debug\{fx}\SimpleTestProject3.dll";
                var stopWatch = Stopwatch.StartNew();
                var actualFx= assemblyMetadataProvider.GetFrameWork(assemblyPath);
                stopWatch.Stop();

                if (fx.Equals(frameworks[0]))
                {
                    Assert.AreEqual(actualFx.FullName, Constants.DotNetFramework451);
                }
                else
                {
                    Assert.AreEqual(actualFx.FullName, Constants.DotNetFrameworkCore10);
                }

                Console.WriteLine($"Framework:{fx}, Elapsed time:{stopWatch.ElapsedMilliseconds} ms");
                Assert.IsTrue(stopWatch.ElapsedMilliseconds < 130);
            }
        }

        [TestMethod]
        public void GetFrameWorkForNativeDll()
        {
                var assemblyPath =
                    $@"{rootDir}\packages\microsoft.testplatform.testasset.nativecpp\2.0.0\contentFiles\any\any\Microsoft.TestPlatform.TestAsset.NativeCPP.dll";
                var stopWatch = Stopwatch.StartNew();
                var fx = assemblyMetadataProvider.GetFrameWork(assemblyPath);
                stopWatch.Stop();
                Assert.AreEqual(Framework.DefaultFramework.Name, fx.FullName);

                Console.WriteLine($"Elapsed time:{stopWatch.ElapsedMilliseconds} ms");
                Assert.IsTrue(stopWatch.ElapsedMilliseconds < 30);
        }
    }
}
