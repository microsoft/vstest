// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.CommandLine;

using CommandLineUtilities;
using Utilities.Helpers.Interfaces;
using TestTools.UnitTesting;
using Moq;

[TestClass]
public class GenerateFakesUtilitiesTests
{
    private readonly Mock<IFileHelper> _fileHelper;
    private readonly string _currentDirectory = @"C:\\Temp";
    private readonly string _runSettings = string.Empty;

    public GenerateFakesUtilitiesTests()
    {
        _fileHelper = new Mock<IFileHelper>();
        CommandLineOptions.Instance.Reset();
        CommandLineOptions.Instance.FileHelper = _fileHelper.Object;
        _fileHelper.Setup(fh => fh.GetCurrentDirectory()).Returns(_currentDirectory);
        _runSettings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.netstandard,Version=5.0</TargetFrameworkVersion></RunConfiguration ></RunSettings>";
    }

    [TestMethod]
    public void CommandLineOptionsDefaultDisableAutoFakesIsFalse()
    {
        Assert.IsFalse(CommandLineOptions.Instance.DisableAutoFakes);
    }

    [TestMethod]
    public void FakesShouldNotBeGeneratedIfDisableAutoFakesSetToTrue()
    {
        CommandLineOptions.Instance.DisableAutoFakes = true;
        string runSettingsXml = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.netstandard,Version=5.0</TargetFrameworkVersion></RunConfiguration ></RunSettings>";

        GenerateFakesUtilities.GenerateFakesSettings(CommandLineOptions.Instance, new string[] { }, ref runSettingsXml);
        Assert.AreEqual(runSettingsXml, _runSettings);
    }

}
