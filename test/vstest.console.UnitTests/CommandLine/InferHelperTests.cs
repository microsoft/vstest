// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.CommandLine
{
    using Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class InferHelperTests
    {
        private readonly Mock<IAssemblyMetadataProvider> mockAssemblyHelper;
        private InferHelper inferHelper;
        private readonly Framework defaultFramework = Framework.DefaultFramework;
        private readonly Framework frameworkNet45 = Framework.FromString(".NETFramework,Version=4.5");
        private readonly Framework frameworkNet46 = Framework.FromString(".NETFramework,Version=4.6");
        private readonly Framework frameworkNet47 = Framework.FromString(".NETFramework,Version=4.7");
        private readonly Framework frameworkCore10 = Framework.FromString(".NETCoreApp,Version=1.0");
        private readonly Framework frameworkCore11 = Framework.FromString(".NETCoreApp,Version=1.1");
        private IDictionary<string, Framework> sourceFrameworks;
        private IDictionary<string, Architecture> sourceArchitectures;

        public InferHelperTests()
        {
            this.mockAssemblyHelper  = new Mock<IAssemblyMetadataProvider>();
            inferHelper = new InferHelper(this.mockAssemblyHelper.Object);
            sourceFrameworks = new Dictionary<string, Framework>();
            sourceArchitectures = new Dictionary<string, Architecture>();
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnNullSources()
        {
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(null, sourceArchitectures));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnEmptySources()
        {
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>(0), sourceArchitectures));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnNullItemInSources()
        {
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>(){null}, sourceArchitectures));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnWhiteSpaceItemInSources()
        {
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { " "}, sourceArchitectures));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnCorrectArchForOneSource()
        {
            this.mockAssemblyHelper.Setup(ah => ah.GetArchitecture(It.IsAny<string>())).Returns(Architecture.X86);
            Assert.AreEqual(Architecture.X86, inferHelper.AutoDetectArchitecture(new List<string>(){"1.dll"}, sourceArchitectures));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnCorrectDefaultArchForNotDotNetAssembly()
        {
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { "NotDotNetAssebly.appx" }, sourceArchitectures));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldSetAnyCpuArchForNotDotNetAssembly()
        {
            inferHelper.AutoDetectArchitecture(new List<string>() { "NotDotNetAssebly.appx" }, sourceArchitectures);
            Assert.AreEqual(Architecture.AnyCPU, sourceArchitectures["NotDotNetAssebly.appx"]);
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchForAllAnyCpuAssemblies()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU);
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "AnyCPU2.exe", "AnyCPU3.dll" }, sourceArchitectures));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnX86ArchIfOneX86AssemblyAndRestAnyCPU()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.X86);
            Assert.AreEqual(Architecture.X86, inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "AnyCPU2.exe", "x86.dll" }, sourceArchitectures));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnARMArchIfOneARMAssemblyAndRestAnyCPU()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.ARM).Returns(Architecture.ARM).Returns(Architecture.ARM);
            Assert.AreEqual(Architecture.ARM, inferHelper.AutoDetectArchitecture(new List<string>() { "ARM1.dll", "ARM2.dll", "ARM3.dll" }, sourceArchitectures));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnX64ArchIfOneX64AssemblyAndRestAnyCPU()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.X64);
            Assert.AreEqual(Architecture.X64, inferHelper.AutoDetectArchitecture(new List<string>() { "x64.dll", "AnyCPU2.exe", "x64.dll" }, sourceArchitectures));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchOnConflictArches()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.X64).Returns(Architecture.X86);
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "x64.exe", "x86.dll" }, sourceArchitectures));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldPoulateSourceArchitectureDictionary()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.X64).Returns(Architecture.X86);

            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "x64.exe", "x86.dll" }, sourceArchitectures));
            Assert.AreEqual(3, sourceArchitectures.Count);
            Assert.AreEqual(Architecture.AnyCPU, sourceArchitectures["AnyCPU1.dll"]);
            Assert.AreEqual(Architecture.X64, sourceArchitectures["x64.exe"]);
            Assert.AreEqual(Architecture.X86, sourceArchitectures["x86.dll"]);

            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchIfthereIsNotDotNetAssemblyInSources()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU);
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "NotDotNetAssebly.appx" }, sourceArchitectures));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnNullSources()
        {
            Assert.AreEqual(defaultFramework, inferHelper.AutoDetectFramework(null, sourceFrameworks));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnEmptySources()
        {
            Assert.AreEqual(defaultFramework, inferHelper.AutoDetectFramework(new List<string>(0), sourceFrameworks));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnNullItemInSources()
        {
            Assert.AreEqual(defaultFramework, inferHelper.AutoDetectFramework(new List<string>(){null}, sourceFrameworks));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnEmptyItemInSources()
        {
            Assert.AreEqual(defaultFramework.Name, inferHelper.AutoDetectFramework(new List<string>() { string.Empty }, sourceFrameworks).Name);
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnFrameworkCore10OnCore10Sources()
        {
            var fx = frameworkCore10;
            var assemblyName = "netcoreapp.dll";
            SetupAndValidateForSingleAssembly(assemblyName, fx, true);
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnFramework46On46Sources()
        {
            var fx = frameworkNet46;
            var assemblyName = "net46.dll";
            SetupAndValidateForSingleAssembly(assemblyName, fx, true);
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnFrameworkUap10ForAppxFiles()
        {
            var fx = Framework.FromString(Constants.DotNetFrameworkUap10);
            var assemblyName = "uwp10.appx";
            SetupAndValidateForSingleAssembly(assemblyName, fx, false);
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnDefaultFullFrameworkForJsFiles()
        {
            var fx = Framework.FromString(Constants.DotNetFramework40);
            var assemblyName = "vstests.js";
            SetupAndValidateForSingleAssembly(assemblyName, fx, false);
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnHighestVersionFxOnSameFxName()
        {
            this.mockAssemblyHelper.SetupSequence(sh => sh.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(frameworkNet46.Name))
                .Returns(new FrameworkName(frameworkNet47.Name))
                .Returns(new FrameworkName(frameworkNet45.Name));
            Assert.AreEqual(frameworkNet47.Name, inferHelper.AutoDetectFramework(new List<string>() { "net46.dll", "net47.exe", "net45.dll" }, sourceFrameworks).Name);
            this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()),Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldPopulatetheDictionaryForAllTheSources()
        {
            this.mockAssemblyHelper.SetupSequence(sh => sh.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(frameworkNet46.Name))
                .Returns(new FrameworkName(frameworkNet47.Name))
                .Returns(new FrameworkName(frameworkNet45.Name));

            Assert.AreEqual(frameworkNet47.Name, inferHelper.AutoDetectFramework(new List<string>() { "net46.dll", "net47.exe", "net45.dll" }, sourceFrameworks).Name);

            Assert.AreEqual(3, sourceFrameworks.Count);
            Assert.AreEqual(frameworkNet46.Name, sourceFrameworks["net46.dll"].Name);
            Assert.AreEqual(frameworkNet47.Name, sourceFrameworks["net47.exe"].Name);
            Assert.AreEqual(frameworkNet45.Name, sourceFrameworks["net45.dll"].Name);
            this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnHighestVersionFxOnEvenManyLowerVersionFxNameExists()
        {
            this.mockAssemblyHelper.SetupSequence(sh => sh.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(frameworkCore10.Name))
                .Returns(new FrameworkName(frameworkCore11.Name))
                .Returns(new FrameworkName(frameworkCore10.Name));
            Assert.AreEqual(frameworkCore11.Name, inferHelper.AutoDetectFramework(new List<string>() { "netcore10_1.dll", "netcore11.dll", "netcore10_2.dll" }, sourceFrameworks).Name);
            this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()), Times.Exactly(3));
        }

        private void SetupAndValidateForSingleAssembly(string assemblyName, Framework fx, bool verify)
        {
            this.mockAssemblyHelper.Setup(sh => sh.GetFrameWork(assemblyName))
                .Returns(new FrameworkName(fx.Name));
            Assert.AreEqual(fx.Name, inferHelper.AutoDetectFramework(new List<string>() { assemblyName }, sourceFrameworks).Name);
            if (verify)
            {
                this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(assemblyName));
            }
        }
    }
}
