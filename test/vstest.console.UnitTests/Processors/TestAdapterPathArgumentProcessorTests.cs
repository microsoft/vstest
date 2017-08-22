// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestAdapterPathArgumentProcessorTests
    {
        [TestMethod]
        public void GetMetadataShouldReturnTestAdapterPathArgumentProcessorCapabilities()
        {
            var processor = new TestAdapterPathArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is TestAdapterPathArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnTestAdapterPathArgumentProcessorCapabilities()
        {
            var processor = new TestAdapterPathArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is TestAdapterPathArgumentExecutor);
        }

        #region TestAdapterPathArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new TestAdapterPathArgumentProcessorCapabilities();
            Assert.AreEqual("/TestAdapterPath", capabilities.CommandName);
            Assert.AreEqual("--TestAdapterPath|/TestAdapterPath\n      This makes vstest.console.exe process use custom test adapters\n      from a given path (if any) in the test run. \n      Example  /TestAdapterPath:<pathToCustomAdapters>", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.TestAdapterPathArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.AutoUpdateRunSettings, capabilities.Priority);

            Assert.AreEqual(true, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region TestAdapterPathArgumentExecutor tests

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsNull()
        {
            var mockRunSettingsProvider = new Mock<IRunSettingsProvider>();
            var mockOutput = new Mock<IOutput>();
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, mockRunSettingsProvider.Object, mockOutput.Object, new FileHelper());

            var message =
                @"The /TestAdapterPath parameter requires a value, which is path of a location containing custom test adapters. Example:  /TestAdapterPath:c:\MyCustomAdapters";

            var isExceptionThrown = false;

            try
            {
                executor.Initialize(null);
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void InitializeShouldThrowIfArgumentIsAWhiteSpace()
        {
            var mockRunSettingsProvider = new Mock<IRunSettingsProvider>();
            var mockOutput = new Mock<IOutput>();
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, mockRunSettingsProvider.Object, mockOutput.Object, new FileHelper());

            var message =
                @"The /TestAdapterPath parameter requires a value, which is path of a location containing custom test adapters. Example:  /TestAdapterPath:c:\MyCustomAdapters";

            var isExceptionThrown = false;

            try
            {
                executor.Initialize("  ");
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void InitializeShouldThrowIfPathDoesNotExist()
        {
            var mockRunSettingsProvider = new Mock<IRunSettingsProvider>();
            var mockOutput = new Mock<IOutput>();
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, mockRunSettingsProvider.Object, mockOutput.Object, new FileHelper());

            var folder = "C:\\temp\\thisfolderdoesnotexist";

            var message = string.Format(
                @"The path '{0}' specified in the 'TestAdapterPath' is invalid. Error: {1}",
                folder,
                "The custom test adapter search path provided was not found, provide a valid path and try again.");

            var isExceptionThrown = false;

            try
            {
                executor.Initialize(folder);
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        [TestMethod]
        public void InitializeShouldUpdateTestAdapterPathInRunSettings()
        {
            RunSettingsManager.Instance.AddDefaultRunSettings();

            var mockOutput = new Mock<IOutput>();
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance, mockOutput.Object, new FileHelper());

            var currentAssemblyPath = typeof(TestAdapterPathArgumentExecutor).GetTypeInfo().Assembly.Location;
            var currentFolder = Path.GetDirectoryName(currentAssemblyPath);

            executor.Initialize(currentFolder);
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(RunSettingsManager.Instance.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(currentFolder, runConfiguration.TestAdaptersPaths);
        }

        [TestMethod]
        public void InitializeShouldUpdateTestAdapterPathsInRunSettings()
        {
            RunSettingsManager.Instance.AddDefaultRunSettings();

            var mockOutput = new Mock<IOutput>();
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance, mockOutput.Object, new FileHelper());

            var currentAssemblyPath = typeof(TestAdapterPathArgumentExecutor).GetTypeInfo().Assembly.Location;
            var currentFolder = Path.GetDirectoryName(currentAssemblyPath);

            executor.Initialize(currentFolder);
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(RunSettingsManager.Instance.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(currentFolder, runConfiguration.TestAdaptersPaths);
        }

        [TestMethod]
        public void InitializeShouldMergeTestAdapterPathsInRunSettings()
        {
            var runSettingsXml = "<RunSettings><RunConfiguration><TestAdaptersPaths>d:\\users;f:\\users</TestAdaptersPaths></RunConfiguration></RunSettings>";
            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(runSettingsXml);
            RunSettingsManager.Instance.SetActiveRunSettings(runSettings);
            var mockFileHelper = new Mock<IFileHelper>();
            var mockOutput = new Mock<IOutput>();

            mockFileHelper.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance, mockOutput.Object, mockFileHelper.Object);

            executor.Initialize("c:\\users");
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(RunSettingsManager.Instance.ActiveRunSettings.SettingsXml);
            Assert.AreEqual("d:\\users;f:\\users;c:\\users", runConfiguration.TestAdaptersPaths);
        }

        [TestMethod]
        public void InitializeShouldMergeTestAdapterPathsInRunSettingsIgnoringDuplicatePaths()
        {
            var runSettingsXml = "<RunSettings><RunConfiguration><TestAdaptersPaths>d:\\users;c:\\users</TestAdaptersPaths></RunConfiguration></RunSettings>";
            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(runSettingsXml);
            RunSettingsManager.Instance.SetActiveRunSettings(runSettings);
            var mockFileHelper = new Mock<IFileHelper>();
            var mockOutput = new Mock<IOutput>();

            mockFileHelper.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance, mockOutput.Object, mockFileHelper.Object);

            executor.Initialize("c:\\users");
            var runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(RunSettingsManager.Instance.ActiveRunSettings.SettingsXml);
            Assert.AreEqual("d:\\users;c:\\users", runConfiguration.TestAdaptersPaths);
        }

        [TestMethod]
        public void InitializeShouldAddRightAdapterPathInErrorMessage()
        {
            var runSettingsXml = "<RunSettings><RunConfiguration><TestAdaptersPaths>d:\\users</TestAdaptersPaths></RunConfiguration></RunSettings>";
            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(runSettingsXml);
            RunSettingsManager.Instance.SetActiveRunSettings(runSettings);
            var mockFileHelper = new Mock<IFileHelper>();
            var mockOutput = new Mock<IOutput>();

            mockFileHelper.Setup(x => x.DirectoryExists("d:\\users")).Returns(false);
            mockFileHelper.Setup(x => x.DirectoryExists("c:\\users")).Returns(true);
            var executor = new TestAdapterPathArgumentExecutor(CommandLineOptions.Instance, RunSettingsManager.Instance, mockOutput.Object, mockFileHelper.Object);

            var message = string.Format(
                @"The path '{0}' specified in the 'TestAdapterPath' is invalid. Error: {1}",
                "d:\\users",
                "The custom test adapter search path provided was not found, provide a valid path and try again.");

            var isExceptionThrown = false;
            try
            {
                executor.Initialize("c:\\users");
            }
            catch (Exception ex)
            {
                isExceptionThrown = true;
                Assert.IsTrue(ex is CommandLineException);
                Assert.AreEqual(message, ex.Message);
            }

            Assert.IsTrue(isExceptionThrown);
        }

        #endregion

        #region Testable implementations

        private class TestableTestAdapterPathArgumentExecutor : TestAdapterPathArgumentExecutor
        {
            internal TestableTestAdapterPathArgumentExecutor(CommandLineOptions options, IRunSettingsProvider runSettingsProvider, IOutput output, IFileHelper fileHelper)
                : base(options, runSettingsProvider, output, fileHelper)
            {
            }

            internal Func<string, IEnumerable<string>> TestAdapters { get; set; }
        }

        #endregion
    }
}