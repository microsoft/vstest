// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using vstest.console.UnitTests.Processors;

    using Moq;
    using System.Text;

    [TestClass]
    public class RunSettingsArgumentProcessorTests
    {
        private IRunSettingsProvider settingsProvider;

        [TestInitialize]
        public void Init()
        {
            this.settingsProvider = new TestableRunSettingsProvider();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            CommandLineOptions.Instance.Reset();
        }

        [TestMethod]
        public void GetMetadataShouldReturnRunSettingsArgumentProcessorCapabilities()
        {
            var processor = new RunSettingsArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is RunSettingsArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnRunSettingsArgumentExecutor()
        {
            var processor = new RunSettingsArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is RunSettingsArgumentExecutor);
        }

        #region RunSettingsArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new RunSettingsArgumentProcessorCapabilities();
            Assert.AreEqual("/Settings", capabilities.CommandName);
            Assert.AreEqual("--Settings|/Settings:<Settings File>\n      Settings to use when running tests.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.RunSettingsArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.RunSettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region RunSettingsArgumentExecutor tests

        [TestMethod]
        public void InitializeShouldThrowExceptionIfArgumentIsNull()
        {
            Action action = () => new RunSettingsArgumentExecutor(CommandLineOptions.Instance, null).Initialize(null);

            ExceptionUtilities.ThrowsException<CommandLineException>(
                action,
                "The /Settings parameter requires a settings file to be provided.");
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfArgumentIsWhiteSpace()
        {
            Action action = () => new RunSettingsArgumentExecutor(CommandLineOptions.Instance, null).Initialize("  ");

            ExceptionUtilities.ThrowsException<CommandLineException>(
                action,
                "The /Settings parameter requires a settings file to be provided.");
        }

        [TestMethod]
        public void InitializeShouldThrowExceptionIfFileDoesNotExist()
        {
            var fileName = "C:\\Imaginary\\nonExistentFile.txt";

            var executor = new RunSettingsArgumentExecutor(CommandLineOptions.Instance, null);
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(false);

            executor.FileHelper = mockFileHelper.Object;

            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize(fileName),
                "The Settings file '{0}' could not be found.",
                fileName);
        }

        [TestMethod]
        public void InitializeShouldThrowIfRunSettingsSchemaDoesNotMatch()
        {
            // Arrange.
            var fileName = "C:\\temp\\r.runsettings";
            var settingsXml = "<BadRunSettings></BadRunSettings>";

            var executor = new TestableRunSettingsArgumentExecutor(
                CommandLineOptions.Instance,
                this.settingsProvider,
                settingsXml);

            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);

            executor.FileHelper = mockFileHelper.Object;

            // Act and Assert.
            ExceptionUtilities.ThrowsException<SettingsException>(
                () => executor.Initialize(fileName),
                "Settings file provided does not conform to required format.");
        }

        [TestMethod]
        public void InitializeShouldSetActiveRunSettings()
        {
            // Arrange.
            var fileName = "C:\\temp\\r.runsettings";
            var settingsXml = "<RunSettings></RunSettings>";

            var executor = new TestableRunSettingsArgumentExecutor(
                CommandLineOptions.Instance,
                this.settingsProvider,
                settingsXml);

            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            executor.FileHelper = mockFileHelper.Object;

            // Act.
            executor.Initialize(fileName);

            // Assert.
            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(fileName, CommandLineOptions.Instance.SettingsFile);
        }

        [TestMethod]
        public void InitializeShouldSetSettingsFileForCommandLineOptions()
        {
            // Arrange.
            var fileName = "C:\\temp\\r.runsettings";
            var settingsXml = "<RunSettings></RunSettings>";

            var executor = new TestableRunSettingsArgumentExecutor(
                CommandLineOptions.Instance,
                this.settingsProvider,
                settingsXml);

            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            executor.FileHelper = mockFileHelper.Object;

            // Act.
            executor.Initialize(fileName);

            // Assert.
            Assert.AreEqual(fileName, CommandLineOptions.Instance.SettingsFile);
        }

        [TestMethod]
        public void InitializeShouldAddDefaultSettingsIfNotPresent()
        {
            // Arrange.
            var fileName = "C:\\temp\\r.runsettings";
            var settingsXml = "<RunSettings></RunSettings>";

            var executor = new TestableRunSettingsArgumentExecutor(
                CommandLineOptions.Instance,
                this.settingsProvider,
                settingsXml);

            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            executor.FileHelper = mockFileHelper.Object;

            // Act.
            executor.Initialize(fileName);

            // Assert.
            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            RunConfiguration runConfiguration =
                XmlRunSettingsUtilities.GetRunConfigurationNode(this.settingsProvider.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.ResultsDirectory, Constants.DefaultResultsDirectory);
            Assert.AreEqual(runConfiguration.TargetFrameworkVersion.ToString(), Framework.DefaultFramework.ToString());
            Assert.AreEqual(runConfiguration.TargetPlatform, Constants.DefaultPlatform);

        }

        [TestMethod]
        public void InitializeShouldSetActiveRunSettingsForTestSettingsFiles()
        {
            // Arrange.
            var fileName = "C:\\temp\\r.testsettings";
            var settingsXml = "<TestSettings></TestSettings>";

            var executor = new TestableRunSettingsArgumentExecutor(
                CommandLineOptions.Instance,
                this.settingsProvider,
                settingsXml);

            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            executor.FileHelper = mockFileHelper.Object;

            // Act.
            executor.Initialize(fileName);

            // Assert.
            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            StringAssert.Contains(this.settingsProvider.ActiveRunSettings.SettingsXml, $"<RunSettings>\r\n  <RunConfiguration>\r\n    <TargetPlatform>{Constants.DefaultPlatform}</TargetPlatform>\r\n    <TargetFrameworkVersion>{Framework.FromString(FrameworkVersion.Framework45.ToString()).Name}</TargetFrameworkVersion>\r\n    <ResultsDirectory>{Constants.DefaultResultsDirectory}</ResultsDirectory>\r\n  </RunConfiguration>\r\n  <MSTest>\r\n    <SettingsFile>C:\\temp\\r.testsettings</SettingsFile>\r\n    <ForcedLegacyMode>true</ForcedLegacyMode>\r\n  </MSTest>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n</RunSettings>");
        }


        [TestMethod]
        public void InitializeShouldUpdateCommandLineOptionsArchitectureAndFxIfProvided()
        {
            // Arrange.
            var fileName = "C:\\temp\\r.runsettings";
            var settingsXml = $"<RunSettings><RunConfiguration><TargetPlatform>{Architecture.X64.ToString()}</TargetPlatform><TargetFrameworkVersion>{Constants.DotNetFramework46}</TargetFrameworkVersion></RunConfiguration></RunSettings>";

            var executor = new TestableRunSettingsArgumentExecutor(
                CommandLineOptions.Instance,
                this.settingsProvider,
                settingsXml);

            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            executor.FileHelper = mockFileHelper.Object;

            // Act.
            executor.Initialize(fileName);

            // Assert.
            Assert.IsTrue(CommandLineOptions.Instance.ArchitectureSpecified);
            Assert.IsTrue(CommandLineOptions.Instance.FrameworkVersionSpecified);
            Assert.AreEqual(Architecture.X64, CommandLineOptions.Instance.TargetArchitecture);
            Assert.AreEqual(Constants.DotNetFramework46, CommandLineOptions.Instance.TargetFrameworkVersion.Name);
        }

        [TestMethod]
        public void InitializeShouldNotUpdateCommandLineOptionsArchitectureAndFxIfNotProvided()
        {
            // Arrange.
            var fileName = "C:\\temp\\r.runsettings";
            var settingsXml = "<RunSettings><RunConfiguration></RunConfiguration></RunSettings>";

            var executor = new TestableRunSettingsArgumentExecutor(
                CommandLineOptions.Instance,
                this.settingsProvider,
                settingsXml);

            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);
            executor.FileHelper = mockFileHelper.Object;

            // Act.
            executor.Initialize(fileName);

            // Assert.
            Assert.IsFalse(CommandLineOptions.Instance.ArchitectureSpecified);
            Assert.IsFalse(CommandLineOptions.Instance.FrameworkVersionSpecified);
        }

        [TestMethod]
        public void InitializeShouldPreserveActualJapaneseString()
        {
            var runsettingsFile = Path.Combine(Path.GetTempPath(), "InitializeShouldPreserveActualJapaneseString.runsettings");
            var settingsXml = @"<RunSettings><RunConfiguration><ResultsDirectory>C:\新しいフォルダー</ResultsDirectory></RunConfiguration></RunSettings>";

            File.WriteAllText(runsettingsFile, settingsXml, Encoding.UTF8);

            var executor = new TestableRunSettingsArgumentExecutor(
                CommandLineOptions.Instance,
                this.settingsProvider,
                null);

            executor.Initialize(runsettingsFile);
            Assert.IsTrue(this.settingsProvider.ActiveRunSettings.SettingsXml.Contains(@"C:\新しいフォルダー"));
            File.Delete(runsettingsFile);
        }

        #endregion

        #region Testable Implementations

        private class TestableRunSettingsArgumentExecutor : RunSettingsArgumentExecutor
        {
            private string runSettingsString;

            internal TestableRunSettingsArgumentExecutor(
                CommandLineOptions commandLineOptions,
                IRunSettingsProvider runSettingsManager,
                string runSettings)
                : base(commandLineOptions, runSettingsManager)

            {
                this.runSettingsString = runSettings;
            }

            protected override XmlReader GetReaderForFile(string runSettingsFile)
            {
                if (this.runSettingsString == null)
                {
                    return base.GetReaderForFile(runSettingsFile);
                }

                var reader = new StringReader(this.runSettingsString);
                var xmlReader = XmlReader.Create(reader, XmlRunSettingsUtilities.ReaderSettings);

                return xmlReader;
            }
        }

        #endregion
    }
}
