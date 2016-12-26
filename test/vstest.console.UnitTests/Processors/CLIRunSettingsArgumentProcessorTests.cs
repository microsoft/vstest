// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CLIRunSettingsArgumentProcessorTests
    {
        private const string DefaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";
        private const string RunSettingsWithDeploymentDisabled = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>";
        private const string RunSettingsWithDeploymentEnabled = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>True</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>";

        [TestMethod]
        public void GetMetadataShouldReturnRunSettingsArgumentProcessorCapabilities()
        {
            var processor = new CLIRunSettingsArgumentProcessor();
            Assert.IsTrue(processor.Metadata.Value is CLIRunSettingsArgumentProcessorCapabilities);
        }

        [TestMethod]
        public void GetExecuterShouldReturnRunSettingsArgumentProcessorCapabilities()
        {
            var processor = new CLIRunSettingsArgumentProcessor();
            Assert.IsTrue(processor.Executor.Value is CLIRunSettingsArgumentExecutor);
        }

        #region CLIRunSettingsArgumentProcessorCapabilities tests

        [TestMethod]
        public void CapabilitiesShouldReturnAppropriateProperties()
        {
            var capabilities = new CLIRunSettingsArgumentProcessorCapabilities();

            Assert.AreEqual("--", capabilities.CommandName);
            Assert.AreEqual("Args:\n      Any extra arguments that should be passed to adapter. Arguments may be specified as name-value pair of the form <n>=<v>, where <n> is the argument name, and <v> is the argument value. Use a space to separate multiple arguments.", capabilities.HelpContentResourceName);

            Assert.AreEqual(HelpContentPriority.CLIRunSettingsArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.AreEqual(false, capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.CLIRunSettings, capabilities.Priority);

            Assert.AreEqual(false, capabilities.AllowMultiple);
            Assert.AreEqual(false, capabilities.AlwaysExecute);
            Assert.AreEqual(false, capabilities.IsSpecialCommand);
        }

        #endregion

        #region CLIRunSettingsArgumentExecutor tests

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfArgumentIsNull()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(null);

            executor.Initialize((string[])null);

            Assert.IsNull(settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfArgumentIsEmpty()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);

            executor.Initialize(new string[0]);

            Assert.IsNull(settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldCreateDefaultRunSettingsIfArgumentsHasOnlyWhiteSpace()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);

            executor.Initialize(new string[] { " " });

            Assert.AreEqual(DefaultRunSettings, settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldSetValueInRunSettings()
        {
            var args = new string[] { "MSTest.DeploymentEnabled=False" };
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual(RunSettingsWithDeploymentDisabled, settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldIgnoreKeyIfValueIsNotPassed()
        {
            var args = new string[]
                { "MSTest.DeploymentEnabled=False", "MSTest1"
                };
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual(RunSettingsWithDeploymentDisabled, settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldIgnoreIfKeyIsNotPassed()
        {
            var args = new string[]
                { "MSTest.DeploymentEnabled=False", "=value"
                };
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual(RunSettingsWithDeploymentDisabled, settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldIgnoreIfEmptyValueIsPassed()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(DefaultRunSettings);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = new string[] { "MSTest.DeploymentEnabled=" };
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual(DefaultRunSettings, settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldOverwriteValueIfNodeAlreadyExists()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(DefaultRunSettings);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = new string[] { "MSTest.DeploymentEnabled=True" };
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual(RunSettingsWithDeploymentEnabled, settingsProvider.ActiveRunSettings.SettingsXml);
        }


        [TestMethod]
        public void InitializeShouldOverwriteValueIfWhitSpaceIsPassedAndNodeAlreadyExists()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(DefaultRunSettings);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = new string[] { "MSTest.DeploymentEnabled= " };
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>\r\n    </DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        #endregion

        #region private

        private class TestableRunSettingsProvider : IRunSettingsProvider
        {
            public RunSettings ActiveRunSettings
            {
                get;
                set;
            }

            public void SetActiveRunSettings(RunSettings runSettings)
            {
                this.ActiveRunSettings = runSettings;
            }
        }

        #endregion
    }
}
