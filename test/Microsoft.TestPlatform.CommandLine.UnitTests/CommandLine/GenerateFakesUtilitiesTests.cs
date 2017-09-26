// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine.UnitTests.CommandLine
{
    using Microsoft.TestPlatform.CommandLineUtilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class GenerateFakesUtilitiesTests
    {
        private readonly Mock<IFileHelper> fileHelper;
        private readonly string currentDirectory = @"C:\\Temp";
        private string runSettings = string.Empty;

        public GenerateFakesUtilitiesTests()
        {
            this.fileHelper = new Mock<IFileHelper>();
            CommandLineOptions.Instance.Reset();
            CommandLineOptions.Instance.FileHelper = this.fileHelper.Object;
            this.fileHelper.Setup(fh => fh.GetCurrentDirectory()).Returns(currentDirectory);
            this.runSettings = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.netstandard,Version=5.0</TargetFrameworkVersion></RunConfiguration ></RunSettings>";
        }

        [TestMethod]
        public void CommandLineOptionsDefaultDisableAutoFakesIsFalse()
        {
            Assert.AreEqual(false, CommandLineOptions.Instance.DisableAutoFakes);
        }

        [TestMethod]
        public void FakesShouldNotBeGeneratedIfDisableAutoFakesSetToTrue()
        {
            CommandLineOptions.Instance.DisableAutoFakes = true;
            string runSettingsXml = @"<RunSettings><RunConfiguration><TargetFrameworkVersion>.netstandard,Version=5.0</TargetFrameworkVersion></RunConfiguration ></RunSettings>";

            GenerateFakesUtilities.GenerateFakesSettings(CommandLineOptions.Instance, new string[] { }, ref runSettingsXml);
            Assert.AreEqual(runSettingsXml, this.runSettings);
        }

    }
}
