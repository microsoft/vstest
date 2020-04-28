// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceDataCollector.UnitTests
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using Coverage;
    using Coverage.Interfaces;
    using global::TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using TestPlatform.CoreUtilities.Helpers;
    using TestPlatform.ObjectModel;
    using TestPlatform.ObjectModel.DataCollection;
    using TraceCollector;
    using TraceCollector.Interfaces;

    [TestClass]
    public class VanguardTests
    {
        private const string CodeCoverageExeFileName = "CodeCoverage";
        private const string ConfigFileNameFormat =
            @"{0}\{1}\CodeCoverage.config"; // {TempDirPath}\{Session_GUID}\CodeCoverage.config

        private const string ConfigXml =
            @"<CodeCoverage>
                 <ModulePaths>
                 <Exclude>
                   <ModulePath>.*Tests.dll</ModulePath>
                 </Exclude>
                </ModulePaths>
                <UseVerifiableInstrumentation>True</UseVerifiableInstrumentation>
                <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>
                <CollectFromChildProcesses>True</CollectFromChildProcesses>
                <CollectAspDotNet>False</CollectAspDotNet>
               </CodeCoverage>
              ";

        private Vanguard vanguard;
        private string sessionName;
        private string configFileName;
        private Mock<IDataCollectionLogger> dataCollectionLoggerMock;
        private Mock<IVanguardLocationProvider> vanguardLocationProviderMock;
        private string outputFileName;
        private string outputDir;
        private DataCollectionContext dataCollectionContext;
        private Mock<IVanguardCommandBuilder> vanguardCommandBuilderMock;
        private IProcessJobObject processJobObject;

        public VanguardTests()
        {
            TestCase testcase = new TestCase { Id = Guid.NewGuid() };
            this.dataCollectionContext = new DataCollectionContext(testcase);
            this.dataCollectionLoggerMock = new Mock<IDataCollectionLogger>();
            this.processJobObject = new ProcessJobObject();
            this.vanguardCommandBuilderMock = new Mock<IVanguardCommandBuilder>();
            this.vanguardLocationProviderMock = new Mock<IVanguardLocationProvider>();

            this.vanguard = new Vanguard(this.vanguardLocationProviderMock.Object, this.vanguardCommandBuilderMock.Object, this.processJobObject);
            this.sessionName = Guid.NewGuid().ToString();
            this.configFileName = string.Format(VanguardTests.ConfigFileNameFormat, Path.GetTempPath(), this.sessionName);
            this.outputDir = Path.GetDirectoryName(this.configFileName);
            Directory.CreateDirectory(this.outputDir);
            File.WriteAllText(this.configFileName, VanguardTests.ConfigXml);
            this.outputFileName = Path.Combine(this.outputDir, Guid.NewGuid() + ".coverage");
            this.vanguardCommandBuilderMock.Setup(c =>
                    c.GenerateCommandLine(VanguardCommand.Shutdown, this.sessionName, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(VanguardTests.GetShutdownCommand(this.sessionName));
            this.vanguard.Initialize(this.sessionName, this.configFileName, this.dataCollectionLoggerMock.Object);
            this.vanguardLocationProviderMock.Setup(c => c.GetVanguardPath()).Returns(Path.Combine(Directory.GetCurrentDirectory(), "CodeCoverage", "CodeCoverage.exe"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, string.Empty);
            this.vanguard.Stop();
            File.Delete(this.configFileName);
            Directory.Delete(this.outputDir, true);
        }

        [TestMethod]
        public void InitializeShouldCreateConfigFile()
        {
            Assert.IsTrue(File.Exists(this.configFileName));
            StringAssert.Contains(
                VanguardTests.ConfigXml.Replace(" ", string.Empty).Replace(Environment.NewLine, string.Empty),
                File.ReadAllText(this.configFileName).Replace(" ", string.Empty).Replace(Environment.NewLine, string.Empty));
        }

        [Ignore]
        [TestMethod]
        public void StartShouldStartVanguardProcessWithCollectCommand()
        {
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                VanguardTests.CodeCoverageExeFileName);

            this.vanguardCommandBuilderMock.Setup(c =>
                    c.GenerateCommandLine(VanguardCommand.Collect, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(VanguardTests.GetCollectCommand(this.sessionName, this.outputFileName, this.configFileName));

            this.vanguard.Start(this.outputFileName, this.dataCollectionContext);
            cts.Cancel();

            var numOfProcessCreated = numOfProcessCreatedTask.Result.Count;

            // TODO find the reason why additional process launched when collecting code coverage.
            Assert.IsTrue(numOfProcessCreated == 1 || numOfProcessCreated == 2, $"Number of process created:{numOfProcessCreated} expected is 1 or 2.");
        }

        [TestMethod]
        [ExpectedException(typeof(Win32Exception))]
        public void StartShouldThrowOnInvalidVarguardPath()
        {
            this.vanguardLocationProviderMock.Setup(c => c.GetVanguardPath()).Returns(Path.Combine(Directory.GetCurrentDirectory(), "WrongExePath.exe"));
            this.vanguard.Start(this.outputFileName, this.dataCollectionContext);
        }

        [TestMethod]
        public void StartShouldThrowOnInvalidCommandLine()
        {
            var expectedErrorMessage =
                "Running event not received from CodeCoverage.exe. Check eventlogs for failure reason.";
            this.vanguardCommandBuilderMock
                .Setup(c => c.GenerateCommandLine(
                    VanguardCommand.Collect,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())).Returns("invalid command");
            var exception = Assert.ThrowsException<VanguardException>(() => this.vanguard.Start(this.outputFileName, this.dataCollectionContext));
            Assert.AreEqual(expectedErrorMessage, exception.Message);
        }

        [TestMethod]
        [Ignore("This test is flaky")]
        public void StartShouldThrowOnTimeout()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "0");
            var expectedErrorMessage =
                "Failed to receive running event from CodeCoverage.exe in 0 seconds, This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.";
            this.vanguardCommandBuilderMock
                .Setup(c => c.GenerateCommandLine(
                    VanguardCommand.Collect,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())).Returns(VanguardTests.GetCollectCommand(this.sessionName, this.outputFileName, this.configFileName));
            var exception = Assert.ThrowsException<VanguardException>(() => this.vanguard.Start(this.outputFileName, this.dataCollectionContext));
            Assert.AreEqual(expectedErrorMessage, exception.Message);
        }

        [Ignore]
        [TestMethod]
        public void StopShouldLaunchVarguardWithShutdownCommand()
        {
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                VanguardTests.CodeCoverageExeFileName);
            this.vanguardCommandBuilderMock
                .Setup(c => c.GenerateCommandLine(
                    VanguardCommand.Collect,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())).Returns(VanguardTests.GetCollectCommand(this.sessionName, this.outputFileName, this.configFileName));
            this.vanguard.Start(this.outputFileName, this.dataCollectionContext);
            this.vanguard.Stop();
            cts.Cancel();

            var numOfProcessCreated = numOfProcessCreatedTask.Result.Count;

            // TODO find the reason why additional process launched when collecting code coverage.
            Assert.IsTrue(numOfProcessCreated == 2 || numOfProcessCreated == 4, $"Number of process created:{numOfProcessCreated} expected is 2 or 4.");
        }

        private static string GetCollectCommand(string sessionName, string outputName, string configurationFileName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "collect /session:{0}  /output:\"{1}\"  /config:\"{2}\"",
                sessionName,
                outputName,
                configurationFileName);

            return builder.ToString();
        }

        private static string GetShutdownCommand(string sessionName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat(CultureInfo.InvariantCulture, "shutdown /session:{0}", sessionName);

            return builder.ToString();
        }
    }
}
