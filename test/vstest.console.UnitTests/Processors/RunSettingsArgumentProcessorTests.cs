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

            CommandLineOptions.Instance.EnableCodeCoverage = true;
            var executor = new TestableRunSettingsArgumentExecutor(
                CommandLineOptions.Instance,
                this.settingsProvider,
                settingsXml);

            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.Exists(It.IsAny<string>())).Returns(true);

            executor.FileHelper = mockFileHelper.Object;

            // Act and Assert.
            ExceptionUtilities.ThrowsException<CommandLineException>(
                () => executor.Initialize(fileName),
                "Settings file provided do not confirm to required format.");
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
        public void InitializeShouldSetActiveRunSettingsWithCodeCoverageData()
        {
            // Arrange.
            var fileName = "C:\\temp\\r.runsettings";
            var settingsXml = "<RunSettings></RunSettings>";

            CommandLineOptions.Instance.EnableCodeCoverage = true;
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
            StringAssert.Contains(this.settingsProvider.ActiveRunSettings.SettingsXml, $"<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector uri=\"datacollector://microsoft/CodeCoverage/2.0\" assemblyQualifiedName=\"Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=15.0.0.0 , Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" friendlyName=\"Code Coverage\">\r\n        <Configuration>\r\n          <CodeCoverage>\r\n            <ModulePaths>\r\n              <Exclude>\r\n                <ModulePath>.*CPPUnitTestFramework.*</ModulePath>\r\n                <ModulePath>.*vstest.console.*</ModulePath>\r\n                <ModulePath>.*microsoft.intellitrace.*</ModulePath>\r\n                <ModulePath>.*vstest.executionengine.*</ModulePath>\r\n                <ModulePath>.*vstest.discoveryengine.*</ModulePath>\r\n                <ModulePath>.*microsoft.teamfoundation.testplatform.*</ModulePath>\r\n                <ModulePath>.*microsoft.visualstudio.testplatform.*</ModulePath>\r\n                <ModulePath>.*microsoft.visualstudio.testwindow.*</ModulePath>\r\n                <ModulePath>.*microsoft.visualstudio.mstest.*</ModulePath>\r\n                <ModulePath>.*microsoft.visualstudio.qualitytools.*</ModulePath>\r\n                <ModulePath>.*microsoft.vssdk.testhostadapter.*</ModulePath>\r\n                <ModulePath>.*microsoft.vssdk.testhostframework.*</ModulePath>\r\n                <ModulePath>.*qtagent32.*</ModulePath>\r\n                <ModulePath>.*msvcr.*dll$</ModulePath>\r\n                <ModulePath>.*msvcp.*dll$</ModulePath>\r\n                <ModulePath>.*clr.dll$</ModulePath>\r\n                <ModulePath>.*clr.ni.dll$</ModulePath>\r\n                <ModulePath>.*clrjit.dll$</ModulePath>\r\n                <ModulePath>.*clrjit.ni.dll$</ModulePath>\r\n                <ModulePath>.*mscoree.dll$</ModulePath>\r\n                <ModulePath>.*mscoreei.dll$</ModulePath>\r\n                <ModulePath>.*mscoreei.ni.dll$</ModulePath>\r\n                <ModulePath>.*mscorlib.dll$</ModulePath>\r\n                <ModulePath>.*mscorlib.ni.dll$</ModulePath>\r\n              </Exclude>\r\n            </ModulePaths>\r\n            <UseVerifiableInstrumentation>True</UseVerifiableInstrumentation>\r\n            <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>\r\n            <CollectFromChildProcesses>True</CollectFromChildProcesses>\r\n            <CollectAspDotNet>false</CollectAspDotNet>\r\n            <SymbolSearchPaths />\r\n            <Functions>\r\n              <Exclude>\r\n                <Function>^std::.*</Function>\r\n                <Function>^ATL::.*</Function>\r\n                <Function>.*::__GetTestMethodInfo.*</Function>\r\n                <Function>.*__CxxPureMSILEntry.*</Function>\r\n                <Function>^Microsoft::VisualStudio::CppCodeCoverageFramework::.*</Function>\r\n                <Function>^Microsoft::VisualStudio::CppUnitTestFramework::.*</Function>\r\n                <Function>.*::YOU_CAN_ONLY_DESIGNATE_ONE_.*</Function>\r\n                <Function>^__.*</Function>\r\n                <Function>.*::__.*</Function>\r\n              </Exclude>\r\n            </Functions>\r\n            <Attributes>\r\n              <Exclude>\r\n                <Attribute>^System.Diagnostics.DebuggerHiddenAttribute$</Attribute>\r\n                <Attribute>^System.Diagnostics.DebuggerNonUserCodeAttribute$</Attribute>\r\n                <Attribute>^System.Runtime.CompilerServices.CompilerGeneratedAttribute$</Attribute>\r\n                <Attribute>^System.CodeDom.Compiler.GeneratedCodeAttribute$</Attribute>\r\n                <Attribute>^System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute$</Attribute>\r\n              </Exclude>\r\n            </Attributes>\r\n            <Sources>\r\n              <Exclude>\r\n                <Source>.*\\\\atlmfc\\\\.*</Source>\r\n                <Source>.*\\\\vctools\\\\.*</Source>\r\n                <Source>.*\\\\public\\\\sdk\\\\.*</Source>\r\n                <Source>.*\\\\externalapis\\\\.*</Source>\r\n                <Source>.*\\\\microsoft sdks\\\\.*</Source>\r\n                <Source>.*\\\\vc\\\\include\\\\.*</Source>\r\n                <Source>.*\\\\msclr\\\\.*</Source>\r\n                <Source>.*\\\\ucrt\\\\.*</Source>\r\n              </Exclude>\r\n            </Sources>\r\n            <CompanyNames />\r\n            <PublicKeyTokens />\r\n          </CodeCoverage>\r\n        </Configuration>\r\n      </DataCollector>\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n  <RunConfiguration>\r\n    <ResultsDirectory>{Constants.DefaultResultsDirectory}</ResultsDirectory>\r\n    <TargetPlatform>{Constants.DefaultPlatform}</TargetPlatform>\r\n    <TargetFrameworkVersion>{Framework.DefaultFramework.Name}</TargetFrameworkVersion>\r\n  </RunConfiguration>\r\n</RunSettings>");
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
                    return null;
                }

                var reader = new StringReader(this.runSettingsString);
                var xmlReader = XmlReader.Create(reader, XmlRunSettingsUtilities.ReaderSettings);

                return xmlReader;
            }
        }

        #endregion
    }
}
