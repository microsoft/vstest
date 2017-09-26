// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CommandLine.UnitTests
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    using Moq;

    using CommandLineResources = Microsoft.TestPlatform.CommandLine.Resources.Resources;

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
        /// Executor should try find "project.json" if empty args given
        /// </summary>
        [TestMethod]
        public void ExecutorEmptyArgsCallRunTestsProcessor()
        {
            var mockOutput = new MockOutput();
            var exitCode = new Executor(mockOutput, this.mockTestPlatformEventSource.Object).Execute(null);

            // Since no projectjsons exist in current folder it should fail
            Assert.AreEqual(1, exitCode, "Exit code must be One for bad arguments");

            //// Verify that messages exist
            //Assert.IsTrue(mockOutput.Messages.Count > 0, "Executor must print atleast copyright info");
            //Assert.IsNotNull(mockOutput.Messages.First().Message, "First Printed Message cannot be null or empty");

            //// Just check first 20 characters - don't need to check whole thing as assembly version is variable
            //Assert.IsTrue(mockOutput.Messages.First().Message.Contains(
            //    Microsoft.TestPlatform.CommandLine.Resources.MicrosoftCommandLineTitle.Substring(0, 20)),
            //    "First Printed message must be Microsoft Copyright");
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
