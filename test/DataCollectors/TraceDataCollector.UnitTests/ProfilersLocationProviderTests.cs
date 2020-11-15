// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceDataCollector.UnitTests
{
    using System;
    using System.IO;
    using System.Reflection;
    using TestTools.UnitTesting;
    using TraceCollector;

    [TestClass]
    [TestCategory("Windows")]
    public class ProfilersLocationProviderTests
    {
        private ProfilersLocationProvider vanguardLocationProvider;

        public ProfilersLocationProviderTests()
        {
            this.vanguardLocationProvider = new ProfilersLocationProvider();
        }

        [TestCleanup]
        public void CleanEnvVariables()
        {
            Environment.SetEnvironmentVariable("CLRIEX86InstallDir", null);
            Environment.SetEnvironmentVariable("CLRIEX64InstallDir", null);
        }

        [TestMethod]
        public void GetVanguardPathShouldReturnRightPath()
        {
            var actualPath = this.vanguardLocationProvider.GetVanguardPath();

            Assert.AreEqual(Path.Join(this.GetCurrentAssemblyLocation(), @"CodeCoverage\CodeCoverage.exe"), actualPath);
        }

        [TestMethod]
        public void GetVanguardProfilerX86PathShouldReturnRightDirectory()
        {
            var actualPath = this.vanguardLocationProvider.GetVanguardProfilerX86Path();

            Assert.AreEqual(Path.Join(this.GetCurrentAssemblyLocation(), @"CodeCoverage\covrun32.dll"), actualPath);
        }

        [TestMethod]
        public void GetVanguardProfilerX64PathShouldReturnRightDirectory()
        {
            var actualPath = this.vanguardLocationProvider.GetVanguardProfilerX64Path();

            Assert.AreEqual(Path.Join(this.GetCurrentAssemblyLocation(), @"CodeCoverage\amd64\covrun64.dll"), actualPath);
        }

        [TestMethod]
        public void GetVanguardProfilerConfigX86PathShouldReturnRightDirectory()
        {
            var actualPath = this.vanguardLocationProvider.GetVanguardProfilerConfigX86Path();

            Assert.AreEqual(Path.Join(this.GetCurrentAssemblyLocation(), @"CodeCoverage\VanguardInstrumentationProfiler_x86.config"), actualPath);
        }

        [TestMethod]
        public void GetVanguardProfilerConfigX64PathShouldReturnRightDirectory()
        {
            var actualPath = this.vanguardLocationProvider.GetVanguardProfilerConfigX64Path();

            Assert.AreEqual(Path.Join(this.GetCurrentAssemblyLocation(), @"CodeCoverage\amd64\VanguardInstrumentationProfiler_x64.config"), actualPath);
        }

        [TestMethod]
        public void GetCodeCoverageShimPathShouldReturnRightDirectory()
        {
            var actualPath = this.vanguardLocationProvider.GetCodeCoverageShimPath();

            Assert.AreEqual(Path.Join(this.GetCurrentAssemblyLocation(), @"CodeCoverage\coreclr\Microsoft.VisualStudio.CodeCoverage.Shim.dll"), actualPath);
        }

        [TestMethod]
        public void GetClrInstrumentationEngineX86PathShouldReturnRightDirectory()
        {
            var actualDir = this.vanguardLocationProvider.GetClrInstrumentationEngineX86Path();

            Assert.AreEqual(Path.Join(this.GetCurrentAssemblyLocation(), @"InstrumentationEngine\x86\MicrosoftInstrumentationEngine_x86.dll"), actualDir);
        }

        [TestMethod]
        public void GetClrInstrumentationEngineX64PathShouldReturnRightDirectory()
        {
            var actualDir = this.vanguardLocationProvider.GetClrInstrumentationEngineX64Path();

            Assert.AreEqual(Path.Join(this.GetCurrentAssemblyLocation(), @"InstrumentationEngine\x64\MicrosoftInstrumentationEngine_x64.dll"), actualDir);
        }

        [TestMethod]
        public void GetClrInstrumentationEngineX86PathShouldReturnRightDirectoryIfEnvVariableSet()
        {
            Environment.SetEnvironmentVariable("CLRIEX86InstallDir", @"C:\temp");

            var actualDir = this.vanguardLocationProvider.GetClrInstrumentationEngineX86Path();

            Assert.AreEqual(@"C:\temp\MicrosoftInstrumentationEngine_x86.dll", actualDir);
        }

        [TestMethod]
        public void GetClrInstrumentationEngineX64PathShouldReturnRightDirectoryIfEnvVariableSet()
        {
            Environment.SetEnvironmentVariable("CLRIEX64InstallDir", @"C:\temp");

            var actualDir = this.vanguardLocationProvider.GetClrInstrumentationEngineX64Path();

            Assert.AreEqual(@"C:\temp\MicrosoftInstrumentationEngine_x64.dll", actualDir);
        }

        private string GetCurrentAssemblyLocation()
        {
            return Path.GetDirectoryName(typeof(ProfilersLocationProviderTests).GetTypeInfo().Assembly.Location);
        }
    }
}
