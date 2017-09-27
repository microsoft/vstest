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

        public InferHelperTests()
        {
            this.mockAssemblyHelper  = new Mock<IAssemblyMetadataProvider>();
            inferHelper = new InferHelper(this.mockAssemblyHelper.Object);
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnNullSources()
        {
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(null));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnEmptySources()
        {
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>(0)));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnNullItemInSources()
        {
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>(){null}));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnWhiteSpaceItemInSources()
        {
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { " "}));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnCorrectArchForOneSource()
        {
            this.mockAssemblyHelper.Setup(ah => ah.GetArchitecture(It.IsAny<string>())).Returns(Architecture.X86);
            Assert.AreEqual(Architecture.X86, inferHelper.AutoDetectArchitecture(new List<string>(){"1.dll"}));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnCorrectDefaultArchForNotDotNetAssembly()
        {
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { "NotDotNetAssebly.appx" }));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchForAllAnyCpuAssemblies()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU);
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "AnyCPU2.exe", "AnyCPU3.dll" }));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnX86ArchIfOneX86AssemblyAndRestAnyCPU()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.X86);
            Assert.AreEqual(Architecture.X86, inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "AnyCPU2.exe", "x86.dll" }));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnX86ArchIfOneARMAssemblyAndRestAnyCPU()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.ARM).Returns(Architecture.ARM).Returns(Architecture.ARM);
            Assert.AreEqual(Architecture.ARM, inferHelper.AutoDetectArchitecture(new List<string>() { "ARM.dll", "ARM.dll", "ARM.dll" }));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnX86ArchIfOneX64AssemblyAndRestAnyCPU()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.X64);
            Assert.AreEqual(Architecture.X64, inferHelper.AutoDetectArchitecture(new List<string>() { "x64.dll", "AnyCPU2.exe", "x64.dll" }));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchOnConflictArches()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.X64).Returns(Architecture.X86);
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "x64.exe", "x86.dll" }));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectArchitectureShouldReturnDefaultArchIfthereIsNotDotNetAssemblyInSources()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU);
            Assert.AreEqual(Constants.DefaultPlatform, inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "NotDotNetAssebly.appx" }));
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnNullSources()
        {
            Assert.AreEqual(defaultFramework, inferHelper.AutoDetectFramework(null));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnEmptySources()
        {
            Assert.AreEqual(defaultFramework, inferHelper.AutoDetectFramework(new List<string>(0)));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnNullItemInSources()
        {
            Assert.AreEqual(defaultFramework, inferHelper.AutoDetectFramework(new List<string>(){null}));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnEmptyItemInSources()
        {
            Assert.AreEqual(defaultFramework, inferHelper.AutoDetectFramework(new List<string>() { string.Empty }));
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
            Assert.AreEqual(frameworkNet47, inferHelper.AutoDetectFramework(new List<string>() { "net46.dll", "net47.exe", "net45.dll" }));
            this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()),Times.Exactly(3));
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnHighestVersionFxOnEvenManyLowerVersionFxNameExists()
        {
            this.mockAssemblyHelper.SetupSequence(sh => sh.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(frameworkCore10.Name))
                .Returns(new FrameworkName(frameworkCore11.Name))
                .Returns(new FrameworkName(frameworkCore10.Name));
            Assert.AreEqual(frameworkCore11, inferHelper.AutoDetectFramework(new List<string>() { "netcore10_1.dll", "netcore11.dll", "netcore10_2.dll" }));
            this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()), Times.Exactly(3));
        }

        private void SetupAndValidateForSingleAssembly(string assemblyName, Framework fx, bool verify)
        {
            this.mockAssemblyHelper.Setup(sh => sh.GetFrameWork(assemblyName))
                .Returns(new FrameworkName(fx.Name));
            Assert.AreEqual(fx, inferHelper.AutoDetectFramework(new List<string>() { assemblyName }));
            if (verify)
            {
                this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(assemblyName));
            }
        }
    }
}
