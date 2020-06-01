// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TraceDataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using Coverage;
    using Coverage.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using TestPlatform.ObjectModel.DataCollection;
    using TraceCollector;
    using TraceCollector.Interfaces;

    [TestClass]
    public class DynamicCoverageDataCollectorImplTests
    {
        private const string DefaultConfigFileName = "CodeCoverage.config";
        private const string DefaultCoverageFileName = "abc.coverage";

        private static XmlElement sampleConfigurationElement =
            DynamicCoverageDataCollectorImplTests.CreateXmlElement($@"<Configuration>
                                                                        <CoverageFileName>{DefaultCoverageFileName}</CoverageFileName>
                                                                        <CodeCoverage>
                                                                        </CodeCoverage>
                                                                      </Configuration>");

        private DynamicCoverageDataCollectorImpl collectorImpl;
        private Mock<IVanguard> vanguardMock;
        private Mock<TraceCollector.IDataCollectionSink> dataCollectionSinkMock;
        private Mock<IDataCollectionLogger> dataCollectionLoggerMock;
        private Mock<IDirectoryHelper> directoryHelperMock;
        private Mock<IFileHelper> fileHelperMock;

        private string aConfigFileName;
        private string atempDirectory;
        private string tempSessionDir;

        public DynamicCoverageDataCollectorImplTests()
        {
            this.vanguardMock = new Mock<IVanguard>();
            this.dataCollectionSinkMock = new Mock<TraceCollector.IDataCollectionSink>();
            this.dataCollectionLoggerMock = new Mock<IDataCollectionLogger>();
            this.directoryHelperMock = new Mock<IDirectoryHelper>();
            this.fileHelperMock = new Mock<IFileHelper>();
            this.tempSessionDir = null;
            this.collectorImpl = new DynamicCoverageDataCollectorImpl(this.vanguardMock.Object, this.directoryHelperMock.Object, this.fileHelperMock.Object);
            this.SetupForInitialize();
            this.collectorImpl.Initialize(DynamicCoverageDataCollectorImplTests.sampleConfigurationElement, this.dataCollectionSinkMock.Object, this.dataCollectionLoggerMock.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (this.tempSessionDir != null)
            {
                if (Directory.Exists(this.tempSessionDir))
                {
                    Directory.Delete(this.tempSessionDir, true);
                }
            }
        }

        #region Initialize Tests
        [TestMethod]
        public void InitializeShouldCreateDefaultCodeCoverageSettingsIfConfigElementIsNull()
        {
            this.directoryHelperMock.Setup(d => d.CreateDirectory(It.IsAny<string>()))
                .Callback<string>((path) => Directory.CreateDirectory(path));

            this.fileHelperMock.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((path, content) => { File.WriteAllText(path, content); });

            this.collectorImpl.Initialize(null, this.dataCollectionSinkMock.Object, this.dataCollectionLoggerMock.Object);

            Assert.AreEqual(DynamicCoverageDataCollectorImplTests.DefaultConfigFileName, Path.GetFileName(this.aConfigFileName));
            StringAssert.Contains(this.aConfigFileName, Path.GetTempPath());
            this.CompareWithDefaultConfig();
        }

        [TestMethod]
        public void InitializeShouldInitializeVanguardWithRightCoverageSettings()
        {
            XmlElement configElement =
                DynamicCoverageDataCollectorImplTests.CreateXmlElement(@"<Configuration><CodeCoverage></CodeCoverage></Configuration>");

            this.directoryHelperMock.Setup(d => d.CreateDirectory(It.IsAny<string>()))
                .Callback<string>((path) =>
                {
                    this.tempSessionDir = path;
                    Directory.CreateDirectory(path);
                });

            this.fileHelperMock.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((path, content) => { File.WriteAllText(path, content); });

            this.collectorImpl.Initialize(configElement, this.dataCollectionSinkMock.Object, this.dataCollectionLoggerMock.Object);

            XmlDocument defaultDocument = new XmlDocument();
            defaultDocument.LoadXml(DynamicCoverageDataCollectorImplTests.GetDefaultCodeCoverageConfig());

            Assert.AreEqual(DynamicCoverageDataCollectorImplTests.DefaultConfigFileName, Path.GetFileName(this.aConfigFileName));

            XmlDocument currentDocument = new XmlDocument();
            currentDocument.LoadXml(File.ReadAllText(this.aConfigFileName));

            var codeCoverageNodes = new Tuple<XmlNode, XmlNode>(currentDocument.DocumentElement, defaultDocument.DocumentElement);

            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./ModulePaths/Exclude");
            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./Functions/Exclude");
            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./Attributes/Exclude");
            this.CompareResults(codeCoverageNodes.Item1, codeCoverageNodes.Item2, "./Sources/Exclude");
        }

        [TestMethod]
        public void InitializeShouldInitializeDefaultConfigIfNoCodeCoverageConfigExists()
        {
            XmlElement configElement =
                DynamicCoverageDataCollectorImplTests.CreateXmlElement($"<Configuration><Framework>.NETCoreApp,Version=v1.1</Framework></Configuration>");

            this.directoryHelperMock.Setup(d => d.CreateDirectory(It.IsAny<string>()))
                .Callback<string>((path) =>
                {
                    this.tempSessionDir = path;
                    Directory.CreateDirectory(path);
                });

            this.fileHelperMock.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((path, content) => { File.WriteAllText(path, content); });

            this.collectorImpl.Initialize(configElement, this.dataCollectionSinkMock.Object, this.dataCollectionLoggerMock.Object);

            this.CompareWithDefaultConfig();
        }

        [TestMethod]
        public void InitializeShouldRegisterForSendFileCompleteEvent()
        {
            this.directoryHelperMock.Setup(d => d.Exists(this.atempDirectory)).Returns(true);
            this.dataCollectionSinkMock.Raise(s => s.SendFileCompleted += null, new AsyncCompletedEventArgs(null, false, null));
            this.directoryHelperMock.Verify(d => d.Exists(this.atempDirectory));
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
            this.vanguardMock.Verify(v => v.Stop());
        }

        [TestMethod]
        public void DisposeShouldDisposeVanguard()
        {
            this.collectorImpl.Dispose();
            this.vanguardMock.Verify(v => v.Dispose());
        }

        [TestMethod]
        public void DisposeShouldDeleteTempDirectory()
        {
            this.directoryHelperMock.Setup(d => d.Exists(this.atempDirectory)).Returns(true);
            this.collectorImpl.Dispose();
            this.directoryHelperMock.Verify(d => d.Delete(this.atempDirectory, true));
        }

        [TestMethod]
        public void DisposeShouldNotDeleteTempDirectoryIfNotExists()
        {
            this.directoryHelperMock.Setup(d => d.Exists(this.atempDirectory)).Returns(false);
            this.collectorImpl.Dispose();
            this.directoryHelperMock.Verify(d => d.Delete(this.atempDirectory, true), Times.Never);
        }

        [TestMethod]
        public void DisposeShouldUnregisterFileCompleteEvent()
        {
            this.collectorImpl.Dispose();
            this.dataCollectionSinkMock.Raise(s => s.SendFileCompleted += null, new AsyncCompletedEventArgs(null, false, null));
            this.directoryHelperMock.Verify(d => d.Exists(this.atempDirectory), Times.Once);
        }

        #endregion

        #region SessionStart Tests

        [TestMethod]
        public void SessionStartShouldCreateDirectoryForCoverageFile()
        {
            var sessionStartEventArgs = new SessionStartEventArgs();
            var coverageFilePath = string.Empty;

            this.vanguardMock.Setup(v => v.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()))
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
            this.vanguardMock.Setup(v => v.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()))
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

            this.vanguardMock.Verify(v => v.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()));
        }

        [TestMethod]
        public void SessionStartShouldLogErrorOnException()
        {
            var sessionStartEventArgs = new SessionStartEventArgs();
            var exceptionMessage = "Vanguard not found";
            Exception expectedEx = null;
            this.vanguardMock.Setup(d => d.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()))
                .Throws(new VanguardException(exceptionMessage));
            this.dataCollectionLoggerMock
                .Setup(l => l.LogError(It.IsAny<DataCollectionContext>(), It.IsAny<Exception>()))
                .Callback<DataCollectionContext, Exception>((context, ex) =>
                {
                    expectedEx = ex;
                });
            var actualEx = Assert.ThrowsException<VanguardException>(() => this.collectorImpl.SessionStart(null, sessionStartEventArgs));

            this.vanguardMock.Verify(v => v.Start(It.IsAny<string>(), It.IsAny<DataCollectionContext>()));
            Assert.AreEqual(expectedEx, actualEx);
            StringAssert.Contains(actualEx.Message, exceptionMessage);
        }

        #endregion

        #region SessionStart Tests

        [TestMethod]
        public void SessionEndShouldStopVanguard()
        {
            var sessionEndEventArgs = new SessionEndEventArgs();

            this.collectorImpl.SessionEnd(null, sessionEndEventArgs);

            this.vanguardMock.Verify(v => v.Stop());
        }

        [TestMethod]
        public void SessionEndShouldSendCoverageFile()
        {
            string tempFile = Path.GetTempFileName();
            var sessionEndEventArgs = new SessionEndEventArgs();
            this.fileHelperMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.collectorImpl.SessionEnd(null, sessionEndEventArgs);

            this.dataCollectionSinkMock.Verify(s => s.SendFileAsync(It.IsAny<DataCollectionContext>(), It.IsAny<string>(), false));
        }

        #endregion

        internal static XmlElement CreateXmlElement(string xmlString)
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

        #region private methods

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

        private static string GetDefaultCodeCoverageConfig()
        {
            string result = string.Empty;

            using (Stream stream = typeof(DynamicCoverageDataCollectorImplTests).Assembly.
                GetManifestResourceStream("Microsoft.VisualStudio.TraceDataCollector.UnitTests.DefaultCodeCoverageConfig.xml"))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    result = sr.ReadToEnd();
                }
            }

            return result;
        }

        private void SetupForInitialize()
        {
            this.vanguardMock.Setup(v => v.Initialize(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDataCollectionLogger>()))
                .Callback<string, string, IDataCollectionLogger>(
                    (sessionName, configFileName, logger) =>
                    {
                        this.aConfigFileName = configFileName;
                    });

            this.directoryHelperMock.Setup(d => d.CreateDirectory(It.IsAny<string>())).Callback<string>(
                (directoryPath) => { this.atempDirectory = directoryPath; });
        }

        private void CompareWithDefaultConfig()
        {
            Assert.AreEqual(
                DynamicCoverageDataCollectorImplTests.GetDefaultCodeCoverageConfig().Replace(" ", string.Empty)
                    .Replace(Environment.NewLine, string.Empty),
                File.ReadAllText(this.aConfigFileName).Replace(" ", string.Empty).Replace(Environment.NewLine, string.Empty));
        }

        private XmlNode ExtractNode(XmlNode node, string path)
        {
            try
            {
                return node.SelectSingleNode(path);
            }
            catch
            {
            }

            return null;
        }

        private Tuple<XmlNode, XmlNode> ExtractNodes(XmlNode currentSettingsRoot, XmlNode defaultSettingsRoot, string path)
        {
            var currentNode = this.ExtractNode(currentSettingsRoot, path);
            var defaultNode = this.ExtractNode(defaultSettingsRoot, path);
            Assert.IsNotNull(currentNode);
            Assert.IsNotNull(defaultNode);

            return new Tuple<XmlNode, XmlNode>(currentNode, defaultNode);
        }

        private void CompareResults(XmlNode currentSettingsRoot, XmlNode defaultSettingsRoot, string path)
        {
            var nodes = this.ExtractNodes(currentSettingsRoot, defaultSettingsRoot, path);

            Assert.AreEqual(nodes.Item1.ChildNodes.Count, nodes.Item2.ChildNodes.Count);

            var set = new HashSet<string>();
            foreach (XmlNode child in nodes.Item1.ChildNodes)
            {
                if (!set.Contains(child.OuterXml))
                {
                    set.Add(child.OuterXml);
                }
            }

            foreach (XmlNode child in nodes.Item2.ChildNodes)
            {
                if (!set.Contains(child.OuterXml))
                {
                    set.Add(child.OuterXml);
                    continue;
                }

                set.Remove(child.OuterXml);
            }

            Assert.AreEqual(set.Count, 0);
        }

        #endregion
    }
}
