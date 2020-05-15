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
            Assert.IsTrue(mockOutput.Messages.Count > 0, "Executor must print at least copyright info");
            Assert.IsNotNull(mockOutput.Messages.First().Message, "First Printed Message cannot be null or empty");

            // Just check first 20 characters - don't need to check whole thing as assembly version is variable
            Assert.IsTrue(
                mockOutput.Messages.First()
                    .Message.Contains(CommandLineResources.MicrosoftCommandLineTitle.Substring(0, 20)),
                "First Printed message must be Microsoft Copyright");

            Assert.IsTrue(mockOutput.Messages.First().Message.EndsWith(assemblyVersion));
        }

        [TestMethod]
        public void ExecutorShouldNotPrintsSplashScreenIfNoLogoPassed()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute("--nologo");

            Assert.AreEqual(1, exitCode, "Exit code must be One for bad arguments");

            // Verify that messages exist
            Assert.IsTrue(mockOutput.Messages.Count == 1, "Executor should not print no valid arguments provided");

            // Just check first 20 characters - don't need to check whole thing as assembly version is variable
            Assert.IsFalse(
                mockOutput.Messages.First()
                    .Message.Contains(CommandLineResources.MicrosoftCommandLineTitle.Substring(0, 20)),
                "First Printed message must be Microsoft Copyright");
        }

        [TestMethod]
        public void ExecutorShouldSanitizeNoLogoInput()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute("--nologo");

            Assert.AreEqual(1, exitCode, "Exit code must be One when no arguments are provided.");

            Assert.IsTrue(mockOutput.Messages.Any(message => message.Message.Contains(CommandLineResources.NoArgumentsProvided)));
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

        [TestMethod]
        public void ExecutorWithInvalidArgsShouldPrintErrorMessage()
        {
            var mockOutput = new MockOutput();
            string badArg = "/badArgument";
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(badArg);

            Assert.AreEqual(1, exitCode, "Exit code must be One when no arguments are provided.");

            Assert.IsTrue(mockOutput.Messages.Any(message => message.Message.Contains(string.Format(CommandLineResources.InvalidArgument, badArg))));
        }

        [TestMethod]
        public void ExecutorWithInvalidArgsShouldPrintHowToUseHelpOption()
        {
            var mockOutput = new MockOutput();
            string badArg = "--invalidArg";
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(badArg);

            Assert.AreEqual(1, exitCode, "Exit code must be One when no arguments are provided.");

            Assert.IsTrue(mockOutput.Messages.Any(message => message.Message.Contains(string.Format(CommandLineResources.InvalidArgument, badArg))));
        }

        [TestMethod]
        public void ExecutorWithInvalidArgsAndValueShouldPrintErrorMessage()
        {
            var mockOutput = new MockOutput();
            string badArg = "--invalidArg:xyz";
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(badArg);

            Assert.AreEqual(1, exitCode, "Exit code must be One when no arguments are provided.");

            Assert.IsTrue(mockOutput.Messages.Any(message => message.Message.Contains(string.Format(CommandLineResources.InvalidArgument, badArg))));
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
            Assert.AreEqual(Constants.DefaultResultsDirectory, runConfiguration.ResultsDirectory);
            Assert.AreEqual(Framework.DefaultFramework.ToString(), runConfiguration.TargetFramework.ToString());
            Assert.AreEqual(Constants.DefaultPlatform, runConfiguration.TargetPlatform);
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
        public void ExecuteShouldNotThrowSettingsExceptionButLogOutput()
        {
            var activeRunSetting = RunSettingsManager.Instance.ActiveRunSettings;
            var runSettingsFile = Path.Combine(Path.GetTempPath(), "ExecutorShouldShowRightErrorMessage.runsettings");

            try
            {
                if (File.Exists(runSettingsFile))
                {
                    File.Delete(runSettingsFile);
                }

                var fileContents = @"<RunSettings>
                                    <LoggerRunSettings>
                                        <Loggers>
                                            <Logger invalidName=""trx"" />
                                        </Loggers>
                                    </LoggerRunSettings>
                                </RunSettings>";

                File.WriteAllText(runSettingsFile, fileContents);

                var testSourceDllPath = Path.GetTempFileName();
                string[] args = { testSourceDllPath, "/settings:" + runSettingsFile };
                var mockOutput = new MockOutput();

                var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(args);

                var result = mockOutput.Messages.Any(o => o.Level == OutputLevel.Error && o.Message.Contains("Invalid settings 'Logger'. Unexpected XmlAttribute: 'invalidName'."));
                Assert.IsTrue(result, "expecting error message : Invalid settings 'Logger'.Unexpected XmlAttribute: 'invalidName'.");
            }
            finally
            {
                File.Delete(runSettingsFile);
                RunSettingsManager.Instance.SetActiveRunSettings(activeRunSetting);
            }
        }

        [TestMethod]
        public void ExecuteShouldReturnNonZeroExitCodeIfSettingsException()
        {
            var activeRunSetting = RunSettingsManager.Instance.ActiveRunSettings;
            var runSettingsFile = Path.Combine(Path.GetTempPath(), "ExecutorShouldShowRightErrorMessage.runsettings");

            try
            {
                if (File.Exists(runSettingsFile))
                {
                    File.Delete(runSettingsFile);
                }

                var fileContents = @"<RunSettings>
                                    <LoggerRunSettings>
                                        <Loggers>
                                            <Logger invalidName=""trx"" />
                                        </Loggers>
                                    </LoggerRunSettings>
                                </RunSettings>";

                File.WriteAllText(runSettingsFile, fileContents);

                string[] args = { "/settings:" + runSettingsFile };
                var mockOutput = new MockOutput();

                var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(args);

                Assert.AreEqual(1, exitCode, "Exit code should be one because it throws exception");
            }
            finally
            {
                File.Delete(runSettingsFile);
                RunSettingsManager.Instance.SetActiveRunSettings(activeRunSetting);
            }
        }

        [TestMethod]
        public void ExecutorShouldShowRightErrorMessage()
        {
            var activeRunSetting = RunSettingsManager.Instance.ActiveRunSettings;
            var runSettingsFile = Path.Combine(Path.GetTempPath(), "ExecutorShouldShowRightErrorMessage.runsettings");

            try
            {
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
            }
            finally
            {
                File.Delete(runSettingsFile);
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
