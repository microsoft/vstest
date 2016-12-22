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
            executor.Initialize(null);

            Assert.IsNull(settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfArgumentIsWhiteSpace()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(" ");

            Assert.IsNull(settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfArgumentIsEmpty()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(string.Empty);

            Assert.IsNull(settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldSetActiveRunSettings()
        {
            var args = "MSTest.DeploymentEnabled=False";
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldIgnoreKeyIfValueIsNotPassed()
        {
            var args = "MSTest.DeploymentEnabled=False MSTest1";
            var settingsProvider = new TestableRunSettingsProvider();
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldAddNodeIfNotPresent()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var runSettings = new RunSettings();
            var defaultSettingsXml = ((XmlDocument)XmlRunSettingsUtilities.CreateDefaultRunSettings()).OuterXml;
            runSettings.LoadSettingsXml(defaultSettingsXml);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = "MSTest.DeploymentEnabled=False ";
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldOverriteValueIfNotAlreadyExists()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var defaultSettingsXml = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>";
            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(defaultSettingsXml);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = "MSTest.DeploymentEnabled=True  ";
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>True</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldHandleEmptyStringsPassedAsArguments()
        {
            var settingsProvider = new TestableRunSettingsProvider();
            var runSettings = new RunSettings();
            var defaultSettingsXml = ((XmlDocument)XmlRunSettingsUtilities.CreateDefaultRunSettings()).OuterXml;
            runSettings.LoadSettingsXml(defaultSettingsXml);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = "MSTest.DeploymentEnabled=False   ";
            var executor = new CLIRunSettingsArgumentExecutor(settingsProvider);
            executor.Initialize(args);

            Assert.IsNotNull(settingsProvider.ActiveRunSettings);
            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors />\r\n  </DataCollectionRunSettings>\r\n  <MSTest>\r\n    <DeploymentEnabled>False</DeploymentEnabled>\r\n  </MSTest>\r\n</RunSettings>", settingsProvider.ActiveRunSettings.SettingsXml);
        }

        #endregion

        #region Regex tests

        [TestMethod]
        public void ParseArgumentShouldRunSettingsArguments()
        {
            var str1 = "MSTest.DeploymentEnabled=False MSTest.CaptureTraceOutput=\"False\" MSTest.DeleteDeploymentDirectoryAfterTestRunIsComplete='True'";

            var executor = new CLIRunSettingsArgumentExecutor(null);

            var result = executor.ParseArgument(str1);

            Assert.AreEqual(3, result.Length);
            Assert.AreEqual("MSTest.CaptureTraceOutput=\"False\"", result[0]);
            Assert.AreEqual("MSTest.DeleteDeploymentDirectoryAfterTestRunIsComplete='True'", result[1]);
            Assert.AreEqual("MSTest.DeploymentEnabled=False", result[2]);
        }

        [TestMethod]
        public void ParseArgumentShouldHandleWhiteSpacesWithinQuotes()
        {
            var str1 = "MSTest.DeploymentEnabled='False '";
            var str2 = "MSTest.DeploymentEnabled=\"False \"";
            var str3 = "MSTest.DeploymentEnabled=False ";

            var executor = new CLIRunSettingsArgumentExecutor(null);

            var result = executor.ParseArgument(str1);
            Assert.AreEqual(result[0], "MSTest.DeploymentEnabled='False '");

            result = executor.ParseArgument(str2);
            Assert.AreEqual(result[0], "MSTest.DeploymentEnabled=\"False \"");

            result = executor.ParseArgument(str3);
            Assert.AreEqual(result[0], "MSTest.DeploymentEnabled=False");
        }

        [TestMethod]
        public void ParseArgumentShouldIgnoreNumberAtTheStartingOfKey()
        {
            var str1 = "1MSTest.DeploymentEnabled='False '";
            var str2 = "2MSTest.DeploymentEnabled=\"False \"";
            var str3 = "3MSTest.DeploymentEnabled=False ";

            var executor = new CLIRunSettingsArgumentExecutor(null);

            var result = executor.ParseArgument(str1);
            Assert.AreEqual("MSTest.DeploymentEnabled='False '", result[0]);

            result = executor.ParseArgument(str2);
            Assert.AreEqual("MSTest.DeploymentEnabled=\"False \"", result[0]);

            result = executor.ParseArgument(str3);
            Assert.AreEqual("MSTest.DeploymentEnabled=False", result[0]);
        }

        [TestMethod]
        public void ParseArgumentShouldHandleWhiteSpacesAndQuotesInValue()
        {
            var str1 = "MSTest.DeploymentEnabled='Fal\" \"se '";
            var str2 = "MSTest.DeploymentEnabled=\"Fa' 'lse \"";
            var str3 = "MSTest.DeploymentEnabled=False";

            var executor = new CLIRunSettingsArgumentExecutor(null);

            var result = executor.ParseArgument(str1);
            Assert.AreEqual("MSTest.DeploymentEnabled='Fal\" \"se '", result[0]);

            result = executor.ParseArgument(str2);
            Assert.AreEqual("MSTest.DeploymentEnabled=\"Fa' 'lse \"", result[0]);

            result = executor.ParseArgument(str3);
            Assert.AreEqual("MSTest.DeploymentEnabled=False", result[0]);
        }

        [TestMethod]
        public void ParseArgumentShouldHandleNumbersInValue()
        {
            var str1 = "MSTest.DeploymentEnabled='Fal123se '";
            var str2 = "MSTest.DeploymentEnabled=\"Fal123se \"";
            var str3 = "MSTest.DeploymentEnabled=Fal123se ";

            var executor = new CLIRunSettingsArgumentExecutor(null);

            var result = executor.ParseArgument(str1);
            Assert.AreEqual("MSTest.DeploymentEnabled='Fal123se '", result[0]);

            result = executor.ParseArgument(str2);
            Assert.AreEqual("MSTest.DeploymentEnabled=\"Fal123se \"", result[0]);

            result = executor.ParseArgument(str3);
            Assert.AreEqual("MSTest.DeploymentEnabled=Fal123se", result[0]);
        }

        [TestMethod]
        public void ParseArgumentShouldHandleNumbersInKey()
        {
            var str1 = "MS1Test.DeploymentEnabled='Fal123se '";
            var str2 = "MS2Test.DeploymentEnabled=\"Fal123se \"";
            var str3 = "MS3Test.DeploymentEnabled=Fal123se ";

            var executor = new CLIRunSettingsArgumentExecutor(null);

            var result = executor.ParseArgument(str1);
            Assert.AreEqual("MS1Test.DeploymentEnabled='Fal123se '", result[0]);

            result = executor.ParseArgument(str2);
            Assert.AreEqual("MS2Test.DeploymentEnabled=\"Fal123se \"", result[0]);

            result = executor.ParseArgument(str3);
            Assert.AreEqual("MS3Test.DeploymentEnabled=Fal123se", result[0]);
        }

        [TestMethod]
        public void ParseArgumentShouldHandleWhiteSpaceAfterKey()
        {
            var str1 = "MSTest.DeploymentEnabled= 'False '";
            var str2 = "MSTest.DeploymentEnabled= \"False \"";
            var str3 = "MSTest.DeploymentEnabled= False ";

            var executor = new CLIRunSettingsArgumentExecutor(null);

            var result = executor.ParseArgument(str1);
            Assert.AreEqual(0, result.Length);

            result = executor.ParseArgument(str2);
            Assert.AreEqual(0, result.Length);

            result = executor.ParseArgument(str3);
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ParseArgumentShouldHandleQuotesInsideQuotesInValue()
        {
            var str1 = "MSTest.DeploymentEnabled='Fa''lse '";
            var str2 = "MSTest.DeploymentEnabled=\"Fa\"\"lse \"";
            var str3 = "MSTest.DeploymentEnabled=Fa\"\"''lse ";

            var executor = new CLIRunSettingsArgumentExecutor(null);

            var result = executor.ParseArgument(str1);
            Assert.AreEqual("MSTest.DeploymentEnabled='Fa'", result[0]);

            result = executor.ParseArgument(str2);
            Assert.AreEqual("MSTest.DeploymentEnabled=\"Fa\"", result[0]);

            result = executor.ParseArgument(str3);
            Assert.AreEqual("MSTest.DeploymentEnabled=Fa\"\"''lse", result[0]);
        }

        [TestMethod]
        public void ParseArgumentShouldHandleSpaceInTheBeginningAndEnd()
        {
            var str1 = " MS1Test.DeploymentEnabled='Fal123se ' ";
            var str2 = " MS2Test.DeploymentEnabled=\"Fal123se \" ";
            var str3 = " MS3Test.DeploymentEnabled=Fal123se ";

            var executor = new CLIRunSettingsArgumentExecutor(null);

            var result = executor.ParseArgument(str1);
            Assert.AreEqual("MS1Test.DeploymentEnabled='Fal123se '", result[0]);

            result = executor.ParseArgument(str2);
            Assert.AreEqual("MS2Test.DeploymentEnabled=\"Fal123se \"", result[0]);

            result = executor.ParseArgument(str3);
            Assert.AreEqual("MS3Test.DeploymentEnabled=Fal123se", result[0]);
        }

        [TestMethod]
        public void ParseArgumentShouldHandleAbnormalEnding()
        {
            var str1 = "1MSTest.DeploymentEnabled='False";
            var str2 = "2MSTest.DeploymentEnabled=\"False";
            var str3 = "3MSTest.DeploymentEnabled=False\"";

            var executor = new CLIRunSettingsArgumentExecutor(null);

            var result = executor.ParseArgument(str1);
            Assert.AreEqual(0, result.Length);

            result = executor.ParseArgument(str2);
            Assert.AreEqual(0, result.Length);

            result = executor.ParseArgument(str3);
            Assert.AreEqual("MSTest.DeploymentEnabled=False", result[0]);
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
