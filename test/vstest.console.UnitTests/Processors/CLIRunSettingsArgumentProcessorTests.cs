﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.console.UnitTests.Processors
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommandLine;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;
    using Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    [TestClass]
    public class CLIRunSettingsArgumentProcessorTests
    {
        private TestableRunSettingsProvider settingsProvider;
        private CLIRunSettingsArgumentExecutor executor;
        private CommandLineOptions commandLineOptions;
        private readonly string DefaultRunSettings = string.Join(Environment.NewLine, 
        "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
"<RunSettings>",
"  <DataCollectionRunSettings>",
"    <DataCollectors />",
"  </DataCollectionRunSettings>",
"</RunSettings>");

        private readonly string RunSettingsWithDeploymentDisabled = string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors />",
            "  </DataCollectionRunSettings>",
            "  <MSTest>",
            "    <DeploymentEnabled>False</DeploymentEnabled>",
            "  </MSTest>",
            "</RunSettings>");

        private readonly string RunSettingsWithDeploymentEnabled = string.Join(Environment.NewLine,
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
            "<RunSettings>",
            "  <DataCollectionRunSettings>",
            "    <DataCollectors />",
            "  </DataCollectionRunSettings>",
            "  <MSTest>",
            "    <DeploymentEnabled>True</DeploymentEnabled>",
            "  </MSTest>",
            "</RunSettings>");

        [TestInitialize]
        public void Init()
        {
            this.commandLineOptions = CommandLineOptions.Instance;
            this.settingsProvider = new TestableRunSettingsProvider();
            this.executor = new CLIRunSettingsArgumentExecutor(this.settingsProvider, this.commandLineOptions);
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.commandLineOptions.Reset();
        }

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
            var expected = "RunSettings arguments:\r\n      Arguments to pass runsettings configurations through commandline. Arguments may be specified as name-value pair of the form [name]=[value] after \"-- \". Note the space after --. \r\n      Use a space to separate multiple [name]=[value].\r\n      More info on RunSettings arguments support: https://aka.ms/vstest-runsettings-arguments";
            Assert.AreEqual(expected.NormalizeLineEndings().ShowWhiteSpace(), capabilities.HelpContentResourceName.NormalizeLineEndings().ShowWhiteSpace());

            Assert.AreEqual(HelpContentPriority.CLIRunSettingsArgumentProcessorHelpPriority, capabilities.HelpPriority);
            Assert.IsFalse(capabilities.IsAction);
            Assert.AreEqual(ArgumentProcessorPriority.CLIRunSettings, capabilities.Priority);

            Assert.IsFalse(capabilities.AllowMultiple);
            Assert.IsFalse(capabilities.AlwaysExecute);
            Assert.IsFalse(capabilities.IsSpecialCommand);
        }

        #endregion

        #region CLIRunSettingsArgumentExecutor tests

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfArgumentIsNull()
        {
            this.executor.Initialize((string[])null);

            Assert.IsNull(this.settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldNotThrowExceptionIfArgumentIsEmpty()
        {
            this.executor.Initialize(new string[0]);

            Assert.IsNull(this.settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldCreateEmptyRunSettingsIfArgumentsHasOnlyWhiteSpace()
        {
            this.executor.Initialize(new string[] { " " });

            Assert.IsNull(this.settingsProvider.ActiveRunSettings);
        }

        [TestMethod]
        public void InitializeShouldSetValueInRunSettings()
        {
            var args = new string[] { "MSTest.DeploymentEnabled=False" };

            this.executor.Initialize(args);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(RunSettingsWithDeploymentDisabled, settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldIgnoreKeyIfValueIsNotPassed()
        {
            var args = new string[] { "MSTest.DeploymentEnabled=False", "MSTest1" };

            this.executor.Initialize(args);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(RunSettingsWithDeploymentDisabled, settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [DataRow("Testameter.Parameter(name=\"asf\",value=\"rgq\")")]
        [DataRow("TestRunParameter.Parameter(name=\"asf\",value=\"rgq\")")]
        [TestMethod]
        public void InitializeShouldThrowErrorIfArgumentIsInValid(string arg)
        {
            var args = new string[] { arg };
            var str = string.Format(CommandLineResources.MalformedRunSettingsKey);

            CommandLineException ex = Assert.ThrowsException<CommandLineException>(() => this.executor.Initialize(args));

            Assert.AreEqual(str, ex.Message);
        }

        [TestMethod]
        public void InitializeShouldIgnoreWhiteSpaceInBeginningOrEndOfKey()
        {
            var args = new string[] { " MSTest.DeploymentEnabled =False" };

            this.executor.Initialize(args);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(RunSettingsWithDeploymentDisabled, settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldIgnoreThrowExceptionIfKeyHasWhiteSpace()
        {
            var args = new string[] { "MST est.DeploymentEnabled=False" };

            Action action = () => this.executor.Initialize(args);

            ExceptionUtilities.ThrowsException<CommandLineException>(
                action,
                "One or more runsettings provided contain invalid token");
        }

        [TestMethod]
        public void InitializeShouldEncodeXMLIfInvalidXMLCharsArePresent()
        {
            var args = new string[] { "MSTest.DeploymentEnabled=F>a><l<se" };

            this.executor.Initialize(args);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(string.Join(Environment.NewLine, "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
"<RunSettings>",
"  <DataCollectionRunSettings>",
"    <DataCollectors />",
"  </DataCollectionRunSettings>",
"  <MSTest>",
"    <DeploymentEnabled>F&gt;a&gt;&lt;l&lt;se</DeploymentEnabled>",
"  </MSTest>",
"</RunSettings>"), settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldIgnoreIfKeyIsNotPassed()
        {
            var args = new string[] { "MSTest.DeploymentEnabled=False", "=value" };

            this.executor.Initialize(args);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(RunSettingsWithDeploymentDisabled, settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldIgnoreIfEmptyValueIsPassed()
        {

            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(DefaultRunSettings);
            this.settingsProvider.SetActiveRunSettings(runSettings);

            var args = new string[] { "MSTest.DeploymentEnabled=" };
            this.executor.Initialize(args);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(DefaultRunSettings, this.settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldOverwriteValueIfNodeAlreadyExists()
        {

            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(DefaultRunSettings);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = new string[] { "MSTest.DeploymentEnabled=True" };
            this.executor.Initialize(args);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(RunSettingsWithDeploymentEnabled, settingsProvider.ActiveRunSettings.SettingsXml);
        }


        [TestMethod]
        public void InitializeShouldOverwriteValueIfWhitSpaceIsPassedAndNodeAlreadyExists()
        {

            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(DefaultRunSettings);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = new string[] { "MSTest.DeploymentEnabled= " };
            this.executor.Initialize(args);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(string.Join(Environment.NewLine, "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
"<RunSettings>",
"  <DataCollectionRunSettings>",
"    <DataCollectors />",
"  </DataCollectionRunSettings>",
"  <MSTest>",
"    <DeploymentEnabled>",
"    </DeploymentEnabled>",
"  </MSTest>",
"</RunSettings>"), settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [TestMethod]
        public void InitializeShouldUpdateCommandLineOptionsFrameworkIfProvided()
        {

            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(DefaultRunSettings);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = new string[] { $"RunConfiguration.TargetFrameworkVersion={Constants.DotNetFramework46}" };
            this.executor.Initialize(args);

            Assert.IsTrue(this.commandLineOptions.FrameworkVersionSpecified);
            Assert.AreEqual(Constants.DotNetFramework46, this.commandLineOptions.TargetFrameworkVersion.Name);
        }

        [TestMethod]
        public void InitializeShouldUpdateCommandLineOptionsArchitectureIfProvided()
        {

            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(DefaultRunSettings);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = new string[] { $"RunConfiguration.TargetPlatform={nameof(Architecture.ARM)}" };
            this.executor.Initialize(args);

            Assert.IsTrue(this.commandLineOptions.ArchitectureSpecified);
            Assert.AreEqual(Architecture.ARM, this.commandLineOptions.TargetArchitecture);
        }

        [TestMethod]
        public void InitializeShouldNotUpdateCommandLineOptionsArchitectureAndFxIfNotProvided()
        {

            var runSettings = new RunSettings();
            runSettings.LoadSettingsXml(DefaultRunSettings);
            settingsProvider.SetActiveRunSettings(runSettings);

            var args = new string[] { };
            this.executor.Initialize(args);

            Assert.IsFalse(this.commandLineOptions.ArchitectureSpecified);
            Assert.IsFalse(this.commandLineOptions.FrameworkVersionSpecified);
        }

        [DynamicData(nameof(TestRunParameterArgValidTestCases), DynamicDataSourceType.Method)]
        [DataTestMethod]
        public void InitializeShouldValidateTestRunParameter(string arg, string runSettingsWithTestRunParameters)
        {
            var args = new string[] { arg };

            this.executor.Initialize(args);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(runSettingsWithTestRunParameters, settingsProvider.ActiveRunSettings.SettingsXml);
        }

        [DynamicData(nameof(TestRunParameterArgInvalidTestCases), DynamicDataSourceType.Method)]
        [DataTestMethod]
        public void InitializeShouldThrowErrorIfTestRunParameterNodeIsInValid(string arg)
        {
            var args = new string[] { arg };
            var str = string.Format(CommandLineResources.InvalidTestRunParameterArgument, arg);

            CommandLineException ex = Assert.ThrowsException<CommandLineException>(() => this.executor.Initialize(args));

            Assert.AreEqual(str, ex.Message);
        }

        public static IEnumerable<object[]> TestRunParameterArgInvalidTestCases()
        {
            return invalidTestCases;
        }

        private static readonly List<object[]> invalidTestCases = new List<object[]>
        {
            new object[] { "TestRunParameters.Parameter(name=asf,value=rgq)" },
            new object[] { "TestRunParameters.Parameter(name=\"asf\",value=\"rgq\" )"},
            new object[] { "TestRunParameters.Parameter( name=\"asf\",value=\"rgq\")" },
            new object[] { "TestRunParametersParameter(name=\"asf\",value=\"rgq\")" },
            new object[] { "TestRunParameters.Paramete(name=\"asf\",value=\"rgq\")" },
            new object[] { "TestRunParameters.Parametername=\"asf\",value=\"rgq\")" },
            new object[] { "TestRunParameters.Parameter(ame=\"asf\",value=\"rgq\")" },
            new object[] { "TestRunParameters.Parameter(name\"asf\",value=\"rgq\")" },
            new object[] { "TestRunParameters.Parameter(name=\"asf\" value=\"rgq\")" },
            new object[] { "TestRunParameters.Parameter(name=\"asf\",alue=\"rgq\")" },
            new object[] { "TestRunParameters.Parameter(name=\"asf\",value\"rgq\")" },
            new object[] { "TestRunParameters.Parameter(name=\"asf\",value=\"rgq\"" },
            new object[] { "TestRunParameters.Parameter(name=\"asf\",value=\"rgq\")wfds" },
            new object[] { "TestRunParameters.Parameter(name=\"\",value=\"rgq\")" },
            new object[] { "TestRunParameters.Parameter(name=\"asf\",value=\"\")" },
            new object[] { "TestRunParameters.Parameter(name=asf\",value=\"rgq\")" },
            new object[] { "TestRunParameters.Parameter(name=\"asf,value=\"rgq\")" },
            new object[] { "TestRunParameters.Parameter(name=\"asf\",value=rgq\")" },
            new object[] { "TestRunParameters.Parameter(name=\"asf\",value=\"rgq)" },
            new object[] { "TestRunParameters.Parameter(name=\"asf@#!\",value=\"rgq\")" },
            new object[] { "TestRunParameters.Parameter(name=\"\",value=\"fgf\")" },
            new object[] { "TestRunParameters.Parameter(name=\"gag\",value=\"\")" },
            new object[] { "TestRunParameters.Parameter(name=\"gag\")" }
        };

        public static IEnumerable<object[]> TestRunParameterArgValidTestCases()
        {
            return validTestCases;
        }

        private static readonly List<object[]> validTestCases = new List<object[]>
        {
            new object[] { "TestRunParameters.Parameter(name=\"weburl\",value=\"&><\")" ,
             string.Join(Environment.NewLine, "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
"<RunSettings>",
"  <DataCollectionRunSettings>",
"    <DataCollectors />",
"  </DataCollectionRunSettings>",
"  <TestRunParameters>",
"    <Parameter name=\"weburl\" value=\"&amp;&gt;&lt;\" />",
"  </TestRunParameters>",
"</RunSettings>")
            },
            new object[] { "TestRunParameters.Parameter(name=\"weburl\",value=\"http://localhost//abc\")" ,
             string.Join(Environment.NewLine, "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
"<RunSettings>",
"  <DataCollectionRunSettings>",
"    <DataCollectors />",
"  </DataCollectionRunSettings>",
"  <TestRunParameters>",
"    <Parameter name=\"weburl\" value=\"http://localhost//abc\" />",
"  </TestRunParameters>",
"</RunSettings>")
            },
            new object[] { "TestRunParameters.Parameter(name= \"a_sf123_12\",value= \"2324346a!@#$%^*()_+-=':;.,/?{}[]|\")" ,
             string.Join(Environment.NewLine, "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
"<RunSettings>",
"  <DataCollectionRunSettings>",
"    <DataCollectors />",
"  </DataCollectionRunSettings>",
"  <TestRunParameters>",
"    <Parameter name=\"a_sf123_12\" value=\"2324346a!@#$%^*()_+-=':;.,/?{}[]|\" />",
"  </TestRunParameters>",
"</RunSettings>")
            },
            new object[] { "TestRunParameters.Parameter(name = \"weburl\" , value = \"http://localhost//abc\")" ,
             string.Join(Environment.NewLine, "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
"<RunSettings>",
"  <DataCollectionRunSettings>",
"    <DataCollectors />",
"  </DataCollectionRunSettings>",
"  <TestRunParameters>",
"    <Parameter name=\"weburl\" value=\"http://localhost//abc\" />",
"  </TestRunParameters>",
"</RunSettings>")
            },
        };
        #endregion

        [TestMethod]
        public void InitializeShouldMergeTestRunParametersWithSpaces()
        {
            // in powershell call: ConsoleApp1.exe --% --TestRunParameters.Parameter(name =\"myParam\", value=\"myValue\")
            // args:
            //--
            //TestRunParameters.Parameter(name = "myParam",
            //value = "myValue")

            // in cmd: ConsoleApp1.exe -- TestRunParameters.Parameter(name=\"myParam\", value=\"myValue\")
            // args:
            //--
            //TestRunParameters.Parameter(name = "myParam",
            //value = "myValue")

            // in ubuntu wsl without escaping the space: ConsoleApp1.exe-- TestRunParameters.Parameter\(name =\"myParam\", value=\"myValue\"\)
            // args:
            //--
            //TestRunParameters.Parameter(name = "myParam",
            //value = "myValue")

            // in ubuntu wsl with escaping the space: ConsoleApp1.exe-- TestRunParameters.Parameter\(name =\"myParam\",\ value=\"myValue\"\)
            // args:
            //--
            //TestRunParameters.Parameter(name = "myParam", value = "myValue")

            var args = new string[] {
                "--",
                "TestRunParameters.Parameter(name=\"myParam\",",
                "value=\"myValue\")",
                "TestRunParameters.Parameter(name=\"myParam2\",",
                "value=\"myValue 2\")",
            };

            var runsettings = string.Join(Environment.NewLine, new[]{
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<RunSettings>",
                "  <DataCollectionRunSettings>",
                "    <DataCollectors />",
                "  </DataCollectionRunSettings>",
                "  <TestRunParameters>",
                "    <Parameter name=\"myParam\" value=\"myValue\" />",
                "    <Parameter name=\"myParam2\" value=\"myValue 2\" />",
                "  </TestRunParameters>",
                "</RunSettings>"});

            this.executor.Initialize(args);

            Assert.IsNotNull(this.settingsProvider.ActiveRunSettings);
            Assert.AreEqual(runsettings, settingsProvider.ActiveRunSettings.SettingsXml);
        }
    }
}
