// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.CommandLine;

using System.Collections.Generic;
using System.Runtime.Versioning;

using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using CommandLineUtilities;
using TestTools.UnitTesting;
using Moq;

[TestClass]
public class InferHelperTests
{
    private readonly Mock<IAssemblyMetadataProvider> _mockAssemblyHelper;
    private readonly InferHelper _inferHelper;
    private readonly Framework _defaultFramework = Framework.DefaultFramework;
    private readonly Architecture _defaultArchitecture = Architecture.X64;
    private readonly Framework _frameworkNet45 = Framework.FromString(".NETFramework,Version=4.5");
    private readonly Framework _frameworkNet46 = Framework.FromString(".NETFramework,Version=4.6");
    private readonly Framework _frameworkNet47 = Framework.FromString(".NETFramework,Version=4.7");
    private readonly Framework _frameworkCore10 = Framework.FromString(".NETCoreApp,Version=1.0");
    private readonly Framework _frameworkCore11 = Framework.FromString(".NETCoreApp,Version=1.1");
    private readonly IDictionary<string, Framework> _sourceFrameworks;
    private readonly IDictionary<string, Architecture> _sourceArchitectures;

    public InferHelperTests()
    {
        _mockAssemblyHelper = new Mock<IAssemblyMetadataProvider>();
        _inferHelper = new InferHelper(_mockAssemblyHelper.Object);
        _sourceFrameworks = new Dictionary<string, Framework>();
        _sourceArchitectures = new Dictionary<string, Architecture>();
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnNullSources()
    {
        Assert.AreEqual(_defaultArchitecture, _inferHelper.AutoDetectArchitecture(null, _sourceArchitectures, _defaultArchitecture));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnEmptySources()
    {
        Assert.AreEqual(_defaultArchitecture, _inferHelper.AutoDetectArchitecture(new List<string>(0), _sourceArchitectures, _defaultArchitecture));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnNullItemInSources()
    {
        Assert.AreEqual(_defaultArchitecture, _inferHelper.AutoDetectArchitecture(new List<string>() { null }, _sourceArchitectures, _defaultArchitecture));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnDefaultArchitectureOnWhiteSpaceItemInSources()
    {
        Assert.AreEqual(_defaultArchitecture, _inferHelper.AutoDetectArchitecture(new List<string>() { " " }, _sourceArchitectures, _defaultArchitecture));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnCorrectArchForOneSource()
    {
        _mockAssemblyHelper.Setup(ah => ah.GetArchitecture(It.IsAny<string>())).Returns(Architecture.X86);
        Assert.AreEqual(Architecture.X86, _inferHelper.AutoDetectArchitecture(new List<string>() { "1.dll" }, _sourceArchitectures, _defaultArchitecture));
        _mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnCorrectDefaultArchForNotDotNetAssembly()
    {
        Assert.AreEqual(_defaultArchitecture, _inferHelper.AutoDetectArchitecture(new List<string>() { "NotDotNetAssebly.appx" }, _sourceArchitectures, _defaultArchitecture));
        _mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldSetAnyCpuArchForNotDotNetAssembly()
    {
        _inferHelper.AutoDetectArchitecture(new List<string>() { "NotDotNetAssebly.appx" }, _sourceArchitectures, _defaultArchitecture);
        Assert.AreEqual(Architecture.AnyCPU, _sourceArchitectures["NotDotNetAssebly.appx"]);
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnDefaultArchForAllAnyCpuAssemblies()
    {
        _mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
            .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU);
        Assert.AreEqual(_defaultArchitecture, _inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "AnyCPU2.exe", "AnyCPU3.dll" }, _sourceArchitectures, _defaultArchitecture));
        _mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnX86ArchIfOneX86AssemblyAndRestAnyCPU()
    {
        _mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
            .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.X86);
        Assert.AreEqual(Architecture.X86, _inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "AnyCPU2.exe", "x86.dll" }, _sourceArchitectures, _defaultArchitecture));
        _mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnARMArchIfOneARMAssemblyAndRestAnyCPU()
    {
        _mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
            .Returns(Architecture.ARM).Returns(Architecture.ARM).Returns(Architecture.ARM);
        Assert.AreEqual(Architecture.ARM, _inferHelper.AutoDetectArchitecture(new List<string>() { "ARM1.dll", "ARM2.dll", "ARM3.dll" }, _sourceArchitectures, _defaultArchitecture));
        _mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnX64ArchIfOneX64AssemblyAndRestAnyCPU()
    {
        _mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
            .Returns(Architecture.AnyCPU).Returns(Architecture.AnyCPU).Returns(Architecture.X64);
        Assert.AreEqual(Architecture.X64, _inferHelper.AutoDetectArchitecture(new List<string>() { "x64.dll", "AnyCPU2.exe", "x64.dll" }, _sourceArchitectures, _defaultArchitecture));
        _mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnDefaultArchOnConflictArches()
    {
        _mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
            .Returns(Architecture.AnyCPU).Returns(Architecture.X64).Returns(Architecture.X86);
        Assert.AreEqual(_defaultArchitecture, _inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "x64.exe", "x86.dll" }, _sourceArchitectures, _defaultArchitecture));
        _mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldPoulateSourceArchitectureDictionary()
    {
        _mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
            .Returns(Architecture.AnyCPU).Returns(Architecture.X64).Returns(Architecture.X86);

        Assert.AreEqual(_defaultArchitecture, _inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "x64.exe", "x86.dll" }, _sourceArchitectures, _defaultArchitecture));
        Assert.AreEqual(3, _sourceArchitectures.Count);
        Assert.AreEqual(Architecture.AnyCPU, _sourceArchitectures["AnyCPU1.dll"]);
        Assert.AreEqual(Architecture.X64, _sourceArchitectures["x64.exe"]);
        Assert.AreEqual(Architecture.X86, _sourceArchitectures["x86.dll"]);

        _mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(3));
    }

    [TestMethod]
    public void AutoDetectArchitectureShouldReturnDefaultArchIfthereIsNotDotNetAssemblyInSources()
    {
        _mockAssemblyHelper.SetupSequence(ah => ah.GetArchitecture(It.IsAny<string>()))
            .Returns(Architecture.AnyCPU);
        Assert.AreEqual(_defaultArchitecture, _inferHelper.AutoDetectArchitecture(new List<string>() { "AnyCPU1.dll", "NotDotNetAssebly.appx" }, _sourceArchitectures, _defaultArchitecture));
        _mockAssemblyHelper.Verify(ah => ah.GetArchitecture(It.IsAny<string>()), Times.Exactly(1));
    }

    [TestMethod]
    public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnNullSources()
    {
        Assert.AreEqual(_defaultFramework, _inferHelper.AutoDetectFramework(null, _sourceFrameworks));
    }

    [TestMethod]
    public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnEmptySources()
    {
        Assert.AreEqual(_defaultFramework, _inferHelper.AutoDetectFramework(new List<string>(0), _sourceFrameworks));
    }

    [TestMethod]
    public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnNullItemInSources()
    {
        Assert.AreEqual(_defaultFramework, _inferHelper.AutoDetectFramework(new List<string>() { null }, _sourceFrameworks));
    }

    [TestMethod]
    public void AutoDetectFrameworkShouldReturnDefaultFrameworkOnEmptyItemInSources()
    {
        Assert.AreEqual(_defaultFramework.Name, _inferHelper.AutoDetectFramework(new List<string>() { string.Empty }, _sourceFrameworks).Name);
    }

    [TestMethod]
    public void AutoDetectFrameworkShouldReturnFrameworkCore10OnCore10Sources()
    {
        var fx = _frameworkCore10;
        var assemblyName = "netcoreapp.dll";
        SetupAndValidateForSingleAssembly(assemblyName, fx, true);
    }

    [TestMethod]
    public void AutoDetectFrameworkShouldReturnFramework46On46Sources()
    {
        var fx = _frameworkNet46;
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
    public void AutoDetectFrameworkShouldReturnFrameworkUap10ForMsixFiles()
    {
        var fx = Framework.FromString(Constants.DotNetFrameworkUap10);
        var assemblyName = "uwp10.msix";
        SetupAndValidateForSingleAssembly(assemblyName, fx, false);
    }

    [TestMethod]
    public void AutoDetectFrameworkShouldReturnFrameworkUap10ForAppxrecipeFiles()
    {
        var fx = Framework.FromString(Constants.DotNetFrameworkUap10);
        var assemblyName = "uwp10.appxrecipe";
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
        _mockAssemblyHelper.SetupSequence(sh => sh.GetFrameWork(It.IsAny<string>()))
            .Returns(new FrameworkName(_frameworkNet46.Name))
            .Returns(new FrameworkName(_frameworkNet47.Name))
            .Returns(new FrameworkName(_frameworkNet45.Name));
        Assert.AreEqual(_frameworkNet47.Name, _inferHelper.AutoDetectFramework(new List<string>() { "net46.dll", "net47.exe", "net45.dll" }, _sourceFrameworks).Name);
        _mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()), Times.Exactly(3));
    }

    [TestMethod]
    public void AutoDetectFrameworkShouldPopulatetheDictionaryForAllTheSources()
    {
        _mockAssemblyHelper.SetupSequence(sh => sh.GetFrameWork(It.IsAny<string>()))
            .Returns(new FrameworkName(_frameworkNet46.Name))
            .Returns(new FrameworkName(_frameworkNet47.Name))
            .Returns(new FrameworkName(_frameworkNet45.Name));

        Assert.AreEqual(_frameworkNet47.Name, _inferHelper.AutoDetectFramework(new List<string>() { "net46.dll", "net47.exe", "net45.dll" }, _sourceFrameworks).Name);

        Assert.AreEqual(3, _sourceFrameworks.Count);
        Assert.AreEqual(_frameworkNet46.Name, _sourceFrameworks["net46.dll"].Name);
        Assert.AreEqual(_frameworkNet47.Name, _sourceFrameworks["net47.exe"].Name);
        Assert.AreEqual(_frameworkNet45.Name, _sourceFrameworks["net45.dll"].Name);
        _mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()), Times.Exactly(3));
    }

    [TestMethod]
    public void AutoDetectFrameworkShouldReturnHighestVersionFxOnEvenManyLowerVersionFxNameExists()
    {
        _mockAssemblyHelper.SetupSequence(sh => sh.GetFrameWork(It.IsAny<string>()))
            .Returns(new FrameworkName(_frameworkCore10.Name))
            .Returns(new FrameworkName(_frameworkCore11.Name))
            .Returns(new FrameworkName(_frameworkCore10.Name));
        Assert.AreEqual(_frameworkCore11.Name, _inferHelper.AutoDetectFramework(new List<string>() { "netcore10_1.dll", "netcore11.dll", "netcore10_2.dll" }, _sourceFrameworks).Name);
        _mockAssemblyHelper.Verify(ah => ah.GetFrameWork(It.IsAny<string>()), Times.Exactly(3));
    }

    private void SetupAndValidateForSingleAssembly(string assemblyName, Framework fx, bool verify)
    {
        _mockAssemblyHelper.Setup(sh => sh.GetFrameWork(assemblyName))
            .Returns(new FrameworkName(fx.Name));
        Assert.AreEqual(fx.Name, _inferHelper.AutoDetectFramework(new List<string>() { assemblyName }, _sourceFrameworks).Name);
        if (verify)
        {
            _mockAssemblyHelper.Verify(ah => ah.GetFrameWork(assemblyName));
        }
    }
}
