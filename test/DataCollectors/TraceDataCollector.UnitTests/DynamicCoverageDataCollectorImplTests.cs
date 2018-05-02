// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceCollector.UnitTests
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using Microsoft.VisualStudio.Coverage;
    using Coverage.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using TestPlatform.ObjectModel;
    using TestPlatform.ObjectModel.DataCollection;

    [TestClass]
    public class DynamicCoverageDataCollectorImplTests
    {
        private static readonly string DefaultCodeCoverageConfig =
            @"            <ModulePaths>" + Environment.NewLine +
            @"              <Exclude>" + Environment.NewLine +
            @"                 <ModulePath>.*CPPUnitTestFramework.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*vstest.console.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*microsoft.intellitrace.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*testhost.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*datacollector.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*microsoft.teamfoundation.testplatform.*</ModulePath>" +
            Environment.NewLine +
            @"                 <ModulePath>.*microsoft.visualstudio.testplatform.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*microsoft.visualstudio.testwindow.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*microsoft.visualstudio.mstest.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*microsoft.visualstudio.qualitytools.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*microsoft.vssdk.testhostadapter.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*microsoft.vssdk.testhostframework.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*qtagent32.*</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*msvcr.*dll$</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*msvcp.*dll$</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*clr.dll$</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*clr.ni.dll$</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*clrjit.dll$</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*clrjit.ni.dll$</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*mscoree.dll$</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*mscoreei.dll$</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*mscoreei.ni.dll$</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*mscorlib.dll$</ModulePath>" + Environment.NewLine +
            @"                 <ModulePath>.*mscorlib.ni.dll$</ModulePath>" + Environment.NewLine +
            @"               </Exclude>" + Environment.NewLine +
            @"            </ModulePaths>" + Environment.NewLine +
            @"            <UseVerifiableInstrumentation>True</UseVerifiableInstrumentation>" + Environment.NewLine +
            @"            <AllowLowIntegrityProcesses>True</AllowLowIntegrityProcesses>" + Environment.NewLine +
            @"            <CollectFromChildProcesses>True</CollectFromChildProcesses>" + Environment.NewLine +
            @"            <CollectAspDotNet>false</CollectAspDotNet>" + Environment.NewLine +
            @"            <SymbolSearchPaths />" + Environment.NewLine +
            @"            <Functions>" + Environment.NewLine +
            @"              <Exclude>" + Environment.NewLine +
            @"                <Function>^std::.*</Function>" + Environment.NewLine +
            @"                <Function>^ATL::.*</Function>" + Environment.NewLine +
            @"                <Function>.*::__GetTestMethodInfo.*</Function>" + Environment.NewLine +
            @"                <Function>.*__CxxPureMSILEntry.*</Function>" + Environment.NewLine +
            @"                <Function>^Microsoft::VisualStudio::CppCodeCoverageFramework::.*</Function>" +
            Environment.NewLine +
            @"                <Function>^Microsoft::VisualStudio::CppUnitTestFramework::.*</Function>" +
            Environment.NewLine +
            @"                <Function>.*::YOU_CAN_ONLY_DESIGNATE_ONE_.*</Function>" + Environment.NewLine +
            @"                <Function>^__.*</Function>" + Environment.NewLine +
            @"                <Function>.*::__.*</Function>" + Environment.NewLine +
            @"              </Exclude>" + Environment.NewLine +
            @"            </Functions>" + Environment.NewLine +
            @"            <Attributes>" + Environment.NewLine +
            @"              <Exclude>" + Environment.NewLine +
            @"                <Attribute>^System.Diagnostics.DebuggerHidden.*</Attribute>" + Environment.NewLine +
            @"                <Attribute>^System.Diagnostics.DebuggerNonUserCode.*</Attribute>" + Environment.NewLine +
            @"                <Attribute>^System.Runtime.CompilerServices.CompilerGenerated.*</Attribute>" +
            Environment.NewLine +
            @"                <Attribute>^System.CodeDom.Compiler.GeneratedCode.*</Attribute>" + Environment.NewLine +
            @"                <Attribute>^System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage.*</Attribute>" +
            Environment.NewLine +
            @"              </Exclude>" + Environment.NewLine +
            @"            </Attributes>" + Environment.NewLine +
            @"            <Sources>" + Environment.NewLine +
            @"              <Exclude>" + Environment.NewLine +
            @"                <Source>.*\\atlmfc\\.*</Source>" + Environment.NewLine +
            @"                <Source>.*\\vctools\\.*</Source>" + Environment.NewLine +
            @"                <Source>.*\\public\\sdk\\.*</Source>" + Environment.NewLine +
            @"                <Source>.*\\externalapis\\.*</Source>" + Environment.NewLine +
            @"                <Source>.*\\microsoft sdks\\.*</Source>" + Environment.NewLine +
            @"                <Source>.*\\vc\\include\\.*</Source>" + Environment.NewLine +
            @"                <Source>.*\\msclr\\.*</Source>" + Environment.NewLine +
            @"                <Source>.*\\ucrt\\.*</Source>" + Environment.NewLine +
            @"              </Exclude>" + Environment.NewLine +
            @"            </Sources>" + Environment.NewLine +
            @"            <CompanyNames/>" + Environment.NewLine +
            @"            <PublicKeyTokens/>" + Environment.NewLine;

        private const string DefaultConfigFileName = "CodeCoverage.config";
        private const string SessionNamePrefix = "MTM_";

        private const string DefaultCoverageFileName = "abc.coverage";
        private static XmlElement SampleConfigurationElement =
            DynamicCoverageDataCollectorImplTests.CreateXmlElement($@"<Configuration>
                                                                        <CoverageFileName>{DefaultCoverageFileName}</CoverageFileName>
                                                                        <CodeCoverage>
                                                                        </CodeCoverage>
                                                                      </Configuration>");

        private DynamicCoverageDataCollectorImpl collectorImpl;
        private Mock<IVangurd> vangurdMock;
        private Mock<TraceCollector.IDataCollectionSink> dataCollectionSinkMock;
        private Mock<IDataCollectionLogger> dataCollectionLoggerMock;
        private Mock<IDirectoryHelper> directoryHelperMock;
        private Mock<IFileHelper> fileHelperMock;

        private XmlElement aConfig;
        private string aConfigFileName;
        private string aSessionName;
        private IDataCollectionLogger aLogger;
        private string atempDirectory;

        public DynamicCoverageDataCollectorImplTests()
        {
            this.vangurdMock = new Mock<IVangurd>();
            this.dataCollectionSinkMock = new Mock<TraceCollector.IDataCollectionSink>();
            this.dataCollectionLoggerMock = new Mock<IDataCollectionLogger>();
            this.directoryHelperMock = new Mock<IDirectoryHelper>();
            this.fileHelperMock = new Mock<IFileHelper>();
            this.collectorImpl = new DynamicCoverageDataCollectorImpl(this.vangurdMock.Object, this.directoryHelperMock.Object, this.fileHelperMock.Object);
            this.SetupForInitialize();
            this.collectorImpl.Initialize(DynamicCoverageDataCollectorImplTests.SampleConfigurationElement, this.dataCollectionSinkMock.Object, this.dataCollectionLoggerMock.Object);
        }

        #region Initialize Tests
        [TestMethod]
        public void InitializeShouldCreateDefaultCodeCoverageSettingsIfConfigElementIsNull()
        {
            this.collectorImpl.Initialize(null, this.dataCollectionSinkMock.Object, this.dataCollectionLoggerMock.Object);

            StringAssert.StartsWith(aSessionName, DynamicCoverageDataCollectorImplTests.SessionNamePrefix);
            Assert.AreEqual(DynamicCoverageDataCollectorImplTests.DefaultConfigFileName, Path.GetFileName(aConfigFileName));
            StringAssert.Contains(aConfigFileName, Path.GetTempPath());
            Assert.AreEqual(
                DefaultCodeCoverageConfig.Replace(Environment.NewLine, string.Empty).Replace(" ", String.Empty)
                , aConfig.InnerXml.Replace(Environment.NewLine, string.Empty).Replace(" ", String.Empty));
        }

        [TestMethod]
        public void InitializeShouldInitializeVanguardWithRightCoverageSettings()
        {
            var expectedContent = "CoverageSettingsContent";
            XmlElement configElement =
                DynamicCoverageDataCollectorImplTests.CreateXmlElement($@"<Configuration><CodeCoverage>{expectedContent}</CodeCoverage></Configuration>");

            this.collectorImpl.Initialize(configElement, this.dataCollectionSinkMock.Object, this.dataCollectionLoggerMock.Object);

            StringAssert.StartsWith(aSessionName, DynamicCoverageDataCollectorImplTests.SessionNamePrefix);
            Assert.AreEqual(DynamicCoverageDataCollectorImplTests.DefaultConfigFileName, Path.GetFileName(aConfigFileName));
            Assert.AreEqual(expectedContent, aConfig.InnerXml);
        }

        [TestMethod]
        public void InitializeShouldRegisterForSendFileCompleteEvent()
        {
            this.dataCollectionSinkMock.Raise(s => s.SendFileCompleted += null, new AsyncCompletedEventArgs(null, false, null));
            this.directoryHelperMock.Verify(d => d.Delete(this.atempDirectory, true));
        }

        [TestMethod]
        public void InitializeShouldCreateTempDirectoryForSession()
        {
            this.directoryHelperMock.Verify(d => d.CreateDirectory(this.atempDirectory));
        }
        #endregion

        #region Dispose Tests
        [TestMethod]
        public void DisposeShouldStopVanguard()
        {
            this.collectorImpl.Dispose();
            this.vangurdMock.Verify(v =>v.Stop());
        }

        [TestMethod]
        public void DisposeShouldDisposeVanguard()
        {
            this.collectorImpl.Dispose();
            this.vangurdMock.Verify(v => v.Stop());
        }

        [TestMethod]
        public void DisposeShouldDeleteTempDirectory()
        {
            this.collectorImpl.Dispose();
            this.directoryHelperMock.Verify(d => d.Delete(this.atempDirectory, true));
        }

        [TestMethod]
        public void DisposeShouldUnregisterFileCompleteEvent()
        {
            this.collectorImpl.Dispose();
            this.dataCollectionSinkMock.Raise(s => s.SendFileCompleted += null, new AsyncCompletedEventArgs(null, false, null));
            this.directoryHelperMock.Verify(d => d.Delete(this.atempDirectory, true), Times.Once);
        }

        #endregion

        #region SessionStart Tests

        [TestMethod]
        public void SessionStartShouldCreateDirectoryForCoverageFile()
        {
            var sessionStartEventArgs = new SessionStartEventArgs();
            var coverageFilePath = string.Empty;

            this.vangurdMock.Setup(v => v.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()))
                .Callback<string, DataCollectionContext>((filePath, dcContext) =>
                {
                    coverageFilePath = filePath;

                });
            this.collectorImpl.SessionStart(null, sessionStartEventArgs);

            StringAssert.StartsWith(Path.GetDirectoryName(coverageFilePath), this.atempDirectory);
            StringAssert.EndsWith(coverageFilePath, DynamicCoverageDataCollectorImplTests.DefaultCoverageFileName);
        }

        [TestMethod]
        public void SessionStartShouldUseAutoGenrateCoverageFileNameIfNotSpecified()
        {
            var sessionStartEventArgs = new SessionStartEventArgs();
            var coverageFilePath = string.Empty;

            this.collectorImpl.Initialize(null, this.dataCollectionSinkMock.Object, this.dataCollectionLoggerMock.Object);
            this.vangurdMock.Setup(v => v.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()))
                .Callback<string, DataCollectionContext>((filePath, dcContext) =>
                {
                    coverageFilePath = filePath;

                });
            this.collectorImpl.SessionStart(null, sessionStartEventArgs);

            StringAssert.StartsWith(Path.GetDirectoryName(coverageFilePath), this.atempDirectory);
            StringAssert.Contains(coverageFilePath, DynamicCoverageDataCollectorImplTests.GetAutoGenerageCodeCoverageFileNamePrefix());
        }

        [TestMethod]
        public void SessionStartShouldLogWarningOnFailToCreateDirectory()
        {
            var sessionStartEventArgs = new SessionStartEventArgs();

            var expectedErrorMessage = "Failed to create directory";
            var directoryPath = string.Empty;

            this.directoryHelperMock.Setup(d => d.CreateDirectory(It.IsAny<string>()))
                .Callback<string>((d) => { directoryPath = d; })
                .Throws(new Exception(expectedErrorMessage));

            var actualLoggedMessage = string.Empty;
            this.dataCollectionLoggerMock.Setup(l => l.LogError(It.IsAny<DataCollectionContext>(), It.IsAny<string>()))
                .Callback<DataCollectionContext, string>((c, m) => { actualLoggedMessage = m; });

            var actualErrorMessage = Assert.ThrowsException<Exception>(() => this.collectorImpl.SessionStart(null, sessionStartEventArgs)).Message;

            Assert.AreEqual(expectedErrorMessage, actualErrorMessage);

            var expectedLogMessage = string.Format("Failed to create directory: {0} with error:System.Exception: {1}", directoryPath, expectedErrorMessage);

            StringAssert.StartsWith(actualLoggedMessage, expectedLogMessage);
        }

        [TestMethod]
        public void SessionStartShouldStartVanguard()
        {
            var sessionStartEventArgs = new SessionStartEventArgs();

            this.collectorImpl.SessionStart(null, sessionStartEventArgs);

            this.vangurdMock.Verify(v => v.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()));
        }

        [TestMethod]
        public void SessionStartShouldLogWarningOnNonCriticalException()
        {
            var sessionStartEventArgs = new SessionStartEventArgs();
            var exceptionMessage = "Vanguard not found";
            var actualMessage = string.Empty;
            this.vangurdMock.Setup(d => d.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()))
                .Throws(new VanguardException(exceptionMessage, false));
            this.dataCollectionLoggerMock
                .Setup(l => l.LogWarning(It.IsAny<DataCollectionContext>(), It.IsAny<string>()))
                .Callback<DataCollectionContext, string>((context, msg) => { actualMessage = msg;});
             this.collectorImpl.SessionStart(null, sessionStartEventArgs);

            this.vangurdMock.Verify(v => v.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()));
            StringAssert.Contains(actualMessage, exceptionMessage);

        }

        [TestMethod]
        public void SessionStartShouldLogErrorOnCriticalException()
        {
            var sessionStartEventArgs = new SessionStartEventArgs();
            var exceptionMessage = "Vanguard not found";
            Exception expectedEx= null;
            this.vangurdMock.Setup(d => d.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()))
                .Throws(new VanguardException(exceptionMessage, true));
            this.dataCollectionLoggerMock
                .Setup(l => l.LogError(It.IsAny<DataCollectionContext>(), It.IsAny<Exception>()))
                .Callback<DataCollectionContext, Exception>((context, ex) =>
                {
                    expectedEx = ex;
                });
            var actualEx = Assert.ThrowsException<VanguardException>(() => this.collectorImpl.SessionStart(null, sessionStartEventArgs));

            this.vangurdMock.Verify(v => v.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()));
            Assert.AreEqual(expectedEx, actualEx);
            StringAssert.Contains(actualEx.Message , exceptionMessage);
        }

        #endregion

        #region SessionStart Tests

        [TestMethod]
        public void SessionEndShouldStopVanguard()
        {
            var sessionEndEventArgs = new SessionEndEventArgs();

            this.collectorImpl.SessionEnd(null, sessionEndEventArgs);

            this.vangurdMock.Verify(v => v.Stop());
        }

        [TestMethod]
        public void SessionEndShouldSendCoverageFile()
        {
            string tempFile = Path.GetTempFileName();
            var sessionEndEventArgs = new SessionEndEventArgs();
            this.vangurdMock.Setup(v => v.OutputName).Returns(tempFile);
            this.fileHelperMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.collectorImpl.SessionEnd(null, sessionEndEventArgs);

            this.dataCollectionSinkMock.Verify(s => s.SendFileAsync(It.IsAny<DataCollectionContext>(), It.IsAny<string>(), false));
        }

        #endregion

        #region private methods
        private void SetupForInitialize()
        {
            this.vangurdMock.Setup(v => v.Initialize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<XmlElement>(),
                    It.IsAny<IDataCollectionLogger>()))
                .Callback<string, string, XmlElement, IDataCollectionLogger>(
                    (sessionName, configFileName, config, logger) =>
                    {
                        this.aSessionName = sessionName;
                        this.aConfigFileName = configFileName;
                        this.aConfig = config;
                        this.aLogger = logger;
                    });

            this.directoryHelperMock.Setup(d => d.CreateDirectory(It.IsAny<string>())).Callback<string>(
                (directoryPath) => { this.atempDirectory = directoryPath; });
        }

        private static XmlElement CreateXmlElement(string xmlString)
        {
            var doc = new XmlDocument();
            using (
                var xmlReader = XmlReader.Create(
                    new StringReader(xmlString),
                    new XmlReaderSettings() { CloseInput = true, DtdProcessing = DtdProcessing.Prohibit }))
            {
                doc.Load(xmlReader);
            }

            return doc.DocumentElement;
        }

        private static string GetAutoGenerageCodeCoverageFileNamePrefix()
        {
            string GetUserName()
            {
                return Environment.GetEnvironmentVariable("USERNAME") ?? Environment.GetEnvironmentVariable("USER");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}_{1}",
                GetUserName(),
                Environment.MachineName);
        }
        #endregion
    }
}
