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
        public void TryGetArchitectureShouldReturnDefaultArchitectureOnNullSources()
        {
            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(null, sourceArchitectures, out var inferredArchitecture);

            Assert.AreEqual(false, isArchitectureIncompatible);
            Assert.AreEqual(Constants.DefaultPlatform, inferredArchitecture);
        }

        [TestMethod]
        public void TryGetArchitectureShouldReturnDefaultArchitectureOnEmptySources()
        {
            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>(0), sourceArchitectures, out var inferredArchitecture);

            Assert.AreEqual(false, isArchitectureIncompatible);
            Assert.AreEqual(Constants.DefaultPlatform, inferredArchitecture);
        }

        [TestMethod]
        public void TryGetArchitectureShouldReturnDefaultArchitectureOnNullItemInSources()
        {
            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>() { null }, sourceArchitectures, out var inferredArchitecture);

            Assert.AreEqual(false, isArchitectureIncompatible);
            Assert.AreEqual(Constants.DefaultPlatform, inferredArchitecture);
        }

        [TestMethod]
        public void TryGetArchitectureShouldReturnDefaultArchitectureOnWhiteSpaceItemInSources()
        {
            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>() { " " }, sourceArchitectures, out var inferredArchitecture);

            Assert.AreEqual(false, isArchitectureIncompatible);
            Assert.AreEqual(Constants.DefaultPlatform, inferredArchitecture);
        }

        [TestMethod]
        public void TryGetArchitectureShouldReturnCorrectArchForOneSource()
        {
            this.mockAssemblyHelper.Setup(ah => ah.GetArchitecture(It.IsAny<string>())).Returns(Architecture.X86);

            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>() { "1.dll" }, sourceArchitectures, out var inferredArchitecture);

            Assert.AreEqual(false, isArchitectureIncompatible);
            Assert.AreEqual(Constants.DefaultPlatform, inferredArchitecture);
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()));
        }

        [TestMethod]
        public void TryGetArchitectureShouldReturnCorrectDefaultArchForNotDotNetAssembly()
        {
            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>() { "NotDotNetAssebly.appx" }, sourceArchitectures, out var inferredArchitecture);

            Assert.AreEqual(false, isArchitectureIncompatible);
            Assert.AreEqual(Constants.DefaultPlatform, inferredArchitecture);
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TryGetArchitectureShouldSetAnyCpuArchForNotDotNetAssembly()
        {
            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>() { "NotDotNetAssebly.appx" }, sourceArchitectures, out var inferredArchitecture);

            Assert.AreEqual(false, isArchitectureIncompatible);
            Assert.AreEqual(Architecture.AnyCPU, sourceArchitectures["NotDotNetAssebly.appx"]);
        }

        [TestMethod]
        public void TryArchitectureShouldReturnDefaultArchForAllAnyCpuAssemblies()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU);

            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>() { "AnyCPU1.dll", "AnyCPU2.exe", "AnyCPU3.dll" }, sourceArchitectures, out var inferredArchitecture);
            Assert.AreEqual(Constants.DefaultPlatform, inferredArchitecture);
            Assert.AreEqual(false, isArchitectureIncompatible);
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void TryGetArchitectureShouldReturnX86ArchIfOneX86AssemblyAndRestAnyCPU()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.X86);
            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>() { "AnyCPU1.dll", "AnyCPU2.exe", "x86.dll" }, sourceArchitectures, out var inferredArchitecture);
            Assert.AreEqual(false, isArchitectureIncompatible);
            Assert.AreEqual(Architecture.X86, inferredArchitecture);
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void TryGetArchitectureShouldReturnARMArchIfOneARMAssemblyAndRestAnyCPU()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.ARM).Returns(Architecture.ARM).Returns(Architecture.ARM);

            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>() { "ARM1.dll", "ARM2.dll", "ARM3.dll" }, sourceArchitectures, out var inferredArchitecture);
            Assert.AreEqual(false, isArchitectureIncompatible);
            Assert.AreEqual(Architecture.ARM, inferredArchitecture);
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void TryGetArchitectureShouldReturnX64ArchIfOneX64AssemblyAndRestAnyCPU()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.X64);

            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>() { "x64.dll", "AnyCPU2.exe", "x64.dll" }, sourceArchitectures, out var inferredArchitecture);
            Assert.AreEqual(false, isArchitectureIncompatible);
            Assert.AreEqual(Architecture.X64, inferredArchitecture);
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void TryGetArchitectureShouldReturnDefaultArchOnConflictArches()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.X64).Returns(Architecture.X86);

            bool isArchitectureIncompatible = inferHelper.TryGetCompatibleArchitecture(new List<string>() { "AnyCPU1.dll", "x64.exe", "x86.dll" }, sourceArchitectures, out var inferredArchitecture);
            Assert.AreEqual(true, isArchitectureIncompatible);
            Assert.AreEqual(Constants.DefaultPlatform, inferredArchitecture);
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void TryGetArchitectureShouldPoulateSourceArchitectureDictionary()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU).Returns(Architecture.X64).Returns(Architecture.X86);

            inferHelper.TryGetCompatibleArchitecture(new List<string>() { "AnyCPU1.dll", "x64.exe", "x86.dll" }, sourceArchitectures, out var inferredArchitecture);
            Assert.AreEqual(Constants.DefaultPlatform, inferredArchitecture);
            Assert.AreEqual(3, sourceArchitectures.Count);
            Assert.AreEqual(Architecture.AnyCPU, sourceArchitectures["AnyCPU1.dll"]);
            Assert.AreEqual(Architecture.X64, sourceArchitectures["x64.exe"]);
            Assert.AreEqual(Architecture.X86, sourceArchitectures["x86.dll"]);

            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void TryGetArchitectureShouldReturnDefaultArchIfthereIsNotDotNetAssemblyInSources()
        {
            this.mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
                .Returns(Architecture.AnyCPU);

            inferHelper.TryGetCompatibleArchitecture(new List<string>() { "AnyCPU1.dll", "NotDotNetAssebly.appx" }, sourceArchitectures, out var inferredArchitecture);
            Assert.AreEqual(Constants.DefaultPlatform, inferredArchitecture);
            this.mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(1));
        }

        [TestMethod]
        public void TryGetFrameworkShouldReturnDefaultFrameworkOnNullSources()
        {
            inferHelper.TryGetCompatibleFramework(null, sourceFrameworks, out var inferredFramework);
            Assert.AreEqual(defaultFramework, inferredFramework);
        }

        [TestMethod]
        public void TryGetFrameworkShouldReturnDefaultFrameworkOnEmptySources()
        {
            inferHelper.TryGetCompatibleFramework(new List<string>(0), sourceFrameworks, out var inferredFramework);
            Assert.AreEqual(defaultFramework, inferredFramework);
        }

        [TestMethod]
        public void TryGetFrameworkShouldReturnDefaultFrameworkOnNullItemInSources()
        {
            inferHelper.TryGetCompatibleFramework(new List<string>() { null }, sourceFrameworks, out var inferredFramework);
            Assert.AreEqual(defaultFramework, inferredFramework);
        }

        [TestMethod]
        public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnEmptyItemInSources()
        {
            inferHelper.TryGetCompatibleFramework(new List<string>() { string.Empty }, sourceFrameworks, out var inferredFramework);
            Assert.AreEqual(defaultFramework.Name, inferredFramework.Name);
        }

        [TestMethod]
        public void TryGetFrameworkShouldReturnFrameworkCore10OnCore10Sources()
        {
            var fx = frameworkCore10;
            var assemblyName = "netcoreapp.dll";
            SetupAndValidateForSingleAssembly(assemblyName, fx, true);
        }

        [TestMethod]
        public void TryGetFrameworkShouldReturnFramework46On46Sources()
        {
            var fx = frameworkNet46;
            var assemblyName = "net46.dll";
            SetupAndValidateForSingleAssembly(assemblyName, fx, true);
        }

        [TestMethod]
        public void TryGetFrameworkShouldReturnFrameworkUap10ForAppxFiles()
        {
            var fx = Framework.FromString(Constants.DotNetFrameworkUap10);
            var assemblyName = "uwp10.appx";
            SetupAndValidateForSingleAssembly(assemblyName, fx, false);
        }

        [TestMethod]
        public void TryGetFrameworkShouldReturnFrameworkUap10ForAppxrecipeFiles()
        {
            var fx = Framework.FromString(Constants.DotNetFrameworkUap10);
            var assemblyName = "uwp10.appxrecipe";
            SetupAndValidateForSingleAssembly(assemblyName, fx, false);
        }

        [TestMethod]
        public void TryGetFrameworkShouldReturnDefaultFullFrameworkForJsFiles()
        {
            var fx = Framework.FromString(Constants.DotNetFramework40);
            var assemblyName = "vstests.js";
            SetupAndValidateForSingleAssembly(assemblyName, fx, false);
        }

        [TestMethod]
        public void TryGetFrameworkShouldReturnHighestVersionFxOnSameFxName()
        {
            this.mockAssemblyHelper.SetupSequence(sh => sh.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(frameworkNet46.Name))
                .Returns(new FrameworkName(frameworkNet47.Name))
                .Returns(new FrameworkName(frameworkNet45.Name));

            inferHelper.TryGetCompatibleFramework(new List<string>() { "net46.dll", "net47.exe", "net45.dll" }, sourceFrameworks, out var inferredFramework);
            Assert.AreEqual(frameworkNet47.Name, inferredFramework.Name);
            this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()),Times.Exactly(3));
        }

        [TestMethod]
        public void TryGetFrameworkShouldPopulatetheDictionaryForAllTheSources()
        {
            this.mockAssemblyHelper.SetupSequence(sh => sh.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(frameworkNet46.Name))
                .Returns(new FrameworkName(frameworkNet47.Name))
                .Returns(new FrameworkName(frameworkNet45.Name));

            inferHelper.TryGetCompatibleFramework(new List<string>() { "net46.dll", "net47.exe", "net45.dll" }, sourceFrameworks, out var inferredFramework);
            Assert.AreEqual(frameworkNet47.Name, inferredFramework.Name);

            Assert.AreEqual(3, sourceFrameworks.Count);
            Assert.AreEqual(frameworkNet46.Name, sourceFrameworks["net46.dll"].Name);
            Assert.AreEqual(frameworkNet47.Name, sourceFrameworks["net47.exe"].Name);
            Assert.AreEqual(frameworkNet45.Name, sourceFrameworks["net45.dll"].Name);
            this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()), Times.Exactly(3));
        }

        [TestMethod]
        public void TryGetFrameworkShouldReturnHighestVersionFxOnEvenManyLowerVersionFxNameExists()
        {
            this.mockAssemblyHelper.SetupSequence(sh => sh.GetFrameWork(It.IsAny<string>()))
                .Returns(new FrameworkName(frameworkCore10.Name))
                .Returns(new FrameworkName(frameworkCore11.Name))
                .Returns(new FrameworkName(frameworkCore10.Name));
            inferHelper.TryGetCompatibleFramework(new List<string>() { "netcore10_1.dll", "netcore11.dll", "netcore10_2.dll" }, sourceFrameworks, out var inferredFramework);
            Assert.AreEqual(frameworkCore11.Name, inferredFramework.Name);
            this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()), Times.Exactly(3));
        }

        private void SetupAndValidateForSingleAssembly(string assemblyName, Framework fx, bool verify)
        {
            this.mockAssemblyHelper.Setup(sh => sh.GetFrameWork(assemblyName))
                .Returns(new FrameworkName(fx.Name));
            inferHelper.TryGetCompatibleFramework(new List<string>() { assemblyName }, sourceFrameworks, out var inferredFramework);
            Assert.AreEqual(fx.Name, inferredFramework.Name);
            if (verify)
            {
                this.mockAssemblyHelper.Verify(ah => ah.GetFrameWork(assemblyName));
            }
        }
    }
}
