// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.UnitTests
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using CoreUtilities.Tracing.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    using Moq;

    using Utilities;

    using CommandLineResources = Microsoft.VisualStudio.TestPlatform.CommandLine.Resources.Resources;

    [TestClass]
    public class ExecutorUnitTests
    {
        private Mock<ITestPlatformEventSource> mockTestPlatformEventSource;

        [TestInitialize]
        public void TestInit()
        {
            this.mockTestPlatformEventSource = new Mock<ITestPlatformEventSource>();
        }

        /// <summary>
        /// Executor should Print splash screen first
        /// </summary>
        [TestMethod]
        public void ExecutorPrintsSplashScreenTest()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute("/badArgument");
            var assemblyVersion = typeof(Executor).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            Assert.AreEqual(1, exitCode, "Exit code must be One for bad arguments");

            // Verify that messages exist
            Assert.IsTrue(mockOutput.Messages.Count > 0, "Executor must print atleast copyright info");
            Assert.IsNotNull(mockOutput.Messages.First().Message, "First Printed Message cannot be null or empty");

            // Just check first 20 characters - don't need to check whole thing as assembly version is variable
            Assert.IsTrue(
                mockOutput.Messages.First()
                    .Message.Contains(CommandLineResources.MicrosoftCommandLineTitle.Substring(0, 20)),
                "First Printed message must be Microsoft Copyright");

            Assert.IsTrue(mockOutput.Messages.First().Message.EndsWith(assemblyVersion));
        }


        /// <summary>
        /// Executor should Print Error message and Help contents when no arguments are provided.
        /// </summary>
        [TestMethod]
        public void ExecutorEmptyArgsPrintsErrorAndHelpMessage()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(null);

            Assert.AreEqual(1, exitCode, "Exit code must be One when no arguments are provided.");

            Assert.IsTrue(mockOutput.Messages.Any(message => message.Message.Contains(CommandLineResources.NoArgumentsProvided)));
        }

        /// <summary>
        /// Executor should set default runsettings value even there is no processor
        /// </summary>
        [TestMethod]
        public void ExecuteShouldInitializeDefaultRunsettings()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(null);
            RunConfiguration runConfiguration = XmlRunSettingsUtilities.GetRunConfigurationNode(RunSettingsManager.Instance.ActiveRunSettings.SettingsXml);
            Assert.AreEqual(runConfiguration.ResultsDirectory, Constants.DefaultResultsDirectory);
            Assert.AreEqual(runConfiguration.TargetFrameworkVersion.ToString(), Framework.DefaultFramework.ToString());
            Assert.AreEqual(runConfiguration.TargetPlatform, Constants.DefaultPlatform);
        }

        [TestMethod]
        public void ExecuteShouldInstrumentVsTestConsoleStart()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(It.IsAny<string[]>());

            this.mockTestPlatformEventSource.Verify(x => x.VsTestConsoleStart(), Times.Once);
        }

        [TestMethod]
        public void ExecuteShouldInstrumentVsTestConsoleStop()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(It.IsAny<string[]>());

            this.mockTestPlatformEventSource.Verify(x => x.VsTestConsoleStop(), Times.Once);
        }

        [TestMethod]
        public void ExecuteShouldExitWithErrorOnInvalidArgumentCombination()
        {
            // Create temp file for testsource dll to pass FileUtil.Exits()
            var testSourceDllPath = Path.GetTempFileName();
            string[] args = { testSourceDllPath, "/tests:Test1", "/testCasefilter:Test" };
            var mockOutput = new MockOutput();

            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(args);

            var errorMessageCount = mockOutput.Messages.Count(msg => msg.Level == OutputLevel.Error && msg.Message.Contains(CommandLineResources.InvalidTestCaseFilterValueForSpecificTests));
            Assert.AreEqual(1, errorMessageCount, "Invalid Arguments Combination should display error.");
            Assert.AreEqual(1, exitCode, "Invalid Arguments Combination execution should exit with error.");
            File.Delete(testSourceDllPath);
        }

        [TestMethod]
        public void ExecuteShouldExitWithErrorOnResponseFileException()
        {
            string[] args = { "@FileDoesNotExist.rsp" };
            var mockOutput = new MockOutput();

            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(args);

            var errorMessageCount = mockOutput.Messages.Count(msg => msg.Level == OutputLevel.Error && msg.Message.Contains(
            string.Format(CultureInfo.CurrentCulture, CommandLineResources.OpenResponseFileError, args[0].Substring(1))));
            Assert.AreEqual(1, errorMessageCount, "Response File Exception should display error.");
            Assert.AreEqual(1, exitCode, "Response File Exception execution should exit with error.");
        }

        [TestMethod]
        public void ExecutorShouldShowRightErrorMessage()
        {
            var activeRunSetting = RunSettingsManager.Instance.ActiveRunSettings;

            try
            {
                var runSettingsFile = Path.Combine(Path.GetTempPath(), "ExecutorShouldShowRightErrorMessage.runsettings");

                if (File.Exists(runSettingsFile))
                {
                    File.Delete(runSettingsFile);
                }

                var fileContents = @"<RunSettings>
                                    <RunConfiguration>
                                        <TargetPlatform>Invalid</TargetPlatform>
                                    </RunConfiguration>
                                </RunSettings>";

                File.WriteAllText(runSettingsFile, fileContents);

                string[] args = { "/settings:" + runSettingsFile };
                var mockOutput = new MockOutput();

                var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(args);

                var result = mockOutput.Messages.Any(o => o.Level == OutputLevel.Error && o.Message.Contains("Invalid setting 'RunConfiguration'. Invalid value 'Invalid' specified for 'TargetPlatform'."));
                Assert.AreEqual(1, exitCode, "Exit code should be one because it throws exception");
                Assert.IsTrue(result, "expecting error message : Invalid setting 'RunConfiguration'. Invalid value 'Invalid' specified for 'TargetPlatform'.");

                File.Delete(runSettingsFile);
            }
            finally
            {
                RunSettingsManager.Instance.SetActiveRunSettings(activeRunSetting);
            }
        }

        private class MockOutput : IOutput
        {
            public List<OutputMessage> Messages { get; set; } = new List<OutputMessage>();

            public void Write(string message, OutputLevel level)
            {
                Messages.Add(new OutputMessage() { Message = message, Level = level });
            }

            public void WriteLine(string message, OutputLevel level)
            {
                Messages.Add(new OutputMessage() { Message = message, Level = level });
            }
        }

        private class OutputMessage
        {
            public string Message { get; set; }
            public OutputLevel Level { get; set; }
        }
    }
}
