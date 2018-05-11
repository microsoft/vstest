// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector.UnitTests
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using Coverage;
    using Coverage.Interfaces;
    using global::TestPlatform.TestUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using TestPlatform.CoreUtilities.Helpers;
    using TestPlatform.ObjectModel;
    using TestPlatform.ObjectModel.DataCollection;

    [TestClass]
    public class VanguardTests
    {
        private const string CodeCoverageExeFileName = "CodeCoverage";
        private const string SessionNamePrefix = "MTM_";
        private const string ConfigFileNameFormat =
            @"{0}\MTM_{1}\CodeCoverage.config"; // {TempDirPath}\MTM_{GUID}\CodeCoverage.config

        private const string ConfigXml =
            @"  <Configuration>
                <CodeCoverage>
                <ModulePaths>
                 <Exclude>
                   <ModulePath>.*Tests.dll</ModulePath>
                 </Exclude>
                </ModulePaths>
                <UseVerifiableInstrumentation>True</UseVerifiableInstrumentation>
                <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>
                <CollectFromChildProcesses>True</CollectFromChildProcesses>
                <CollectAspDotNet>False</CollectAspDotNet>
              </CodeCoverage></Configuration>";

        private XmlElement configXmlElement;
        private Vanguard vanguard;
        private string sessionName;
        private string configFileName;
        private Mock<IDataCollectionLogger> dataCollectionLoggerMock;
        private Mock<ICollectorUtility> collectorUtilityMock;
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
            this.collectorUtilityMock  = new Mock<ICollectorUtility>();

            this.vanguard = new Vanguard(this.collectorUtilityMock.Object, this.vanguardCommandBuilderMock.Object, this.processJobObject);
            var guid = Guid.NewGuid();
            this.sessionName = VanguardTests.SessionNamePrefix + guid;
            this.configFileName = string.Format(VanguardTests.ConfigFileNameFormat, Path.GetTempPath(), guid);
            this.outputDir = Path.GetDirectoryName(this.configFileName);
            Directory.CreateDirectory(outputDir);
            this.outputFileName = Path.Combine(this.outputDir, Guid.NewGuid() + ".coverage");
            this.configXmlElement = DynamicCoverageDataCollectorImplTests.CreateXmlElement(ConfigXml)["CodeCoverage"];
            this.vanguardCommandBuilderMock.Setup(c =>
                    c.GenerateCommandLine(VanguardCommand.Shutdown, this.sessionName, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(VanguardTests.GetShutdownCommand(this.sessionName));
            this.vanguard.Initialize(this.sessionName, this.configFileName, this.configXmlElement, this.dataCollectionLoggerMock.Object);
            this.collectorUtilityMock.Setup(c => c.GetVanguardPath()).Returns(Path.Combine(Directory.GetCurrentDirectory(), "CodeCoverage.exe"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "");
            this.vanguard.Stop();
            File.Delete(this.configFileName);
            Directory.Delete(this.outputDir, true);
        }

        [TestMethod]
        public void InitializeShouldCreateConfigFile()
        {
            Assert.IsTrue(File.Exists(this.configFileName));
            StringAssert.Contains(
                VanguardTests.ConfigXml.Replace(" ", string.Empty).Replace(Environment.NewLine, String.Empty),
                File.ReadAllText(this.configFileName).Replace(" ", string.Empty).Replace(Environment.NewLine, String.Empty));
        }

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

            // TODO find the reason why more than one processes launched.
            Assert.IsTrue(numOfProcessCreatedTask.Result >= 1);
        }

        [TestMethod]
        [ExpectedException(typeof(Win32Exception))]
        public void StartShouldThrowOnInvalidVarguardPath()
        {
            this.collectorUtilityMock.Setup(c => c.GetVanguardPath()).Returns(Path.Combine(Directory.GetCurrentDirectory(), "WrongExePath.exe"));
            this.vanguard.Start(this.outputFileName, this.dataCollectionContext);
        }

        [TestMethod]
        public void StartShouldThrowOnInvalidCommandLine()
        {
            var expectedErrorMessage =
                "Running event not received from CodeCoverage.exe. Check eventlogs for failure reason.";
            this.vanguardCommandBuilderMock
                .Setup(c => c.GenerateCommandLine(VanguardCommand.Collect, It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>())).Returns("invalid command");
            var exception = Assert.ThrowsException<VanguardException>(() => this.vanguard.Start(this.outputFileName, this.dataCollectionContext));
            Assert.AreEqual(expectedErrorMessage, exception.Message);
        }

        [TestMethod]
        public void StartShouldThrowOnTimeout()
        {
            Environment.SetEnvironmentVariable(EnvironmentHelper.VstestConnectionTimeout, "0");
            var expectedErrorMessage =
                "Failed to receive running event from CodeCoverage.exe in 0 seconds, This may occur due to machine slowness, please set environment variable VSTEST_CONNECTION_TIMEOUT to increase timeout.";
            this.vanguardCommandBuilderMock
                .Setup(c => c.GenerateCommandLine(VanguardCommand.Collect, It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>())).Returns(VanguardTests.GetCollectCommand(this.sessionName, this.outputFileName, this.configFileName));
            var exception = Assert.ThrowsException<VanguardException>(() => this.vanguard.Start(this.outputFileName, this.dataCollectionContext));
            Assert.AreEqual(expectedErrorMessage, exception.Message);
        }

        [TestMethod]
        public void StopShouldLaunchVarguardWithShutdownCommand()
        {
            var cts = new CancellationTokenSource();
            var numOfProcessCreatedTask = NumberOfProcessLaunchedUtility.NumberOfProcessCreated(
                cts,
                VanguardTests.CodeCoverageExeFileName);
            this.vanguardCommandBuilderMock
                .Setup(c => c.GenerateCommandLine(VanguardCommand.Collect, It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>())).Returns(VanguardTests.GetCollectCommand(this.sessionName, this.outputFileName, this.configFileName));
            this.vanguard.Start(this.outputFileName, this.dataCollectionContext);
            this.vanguard.Stop();
            cts.Cancel();

            // TODO find the reason why more than two processes launched.
            Assert.IsTrue(numOfProcessCreatedTask.Result >= 2);
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
