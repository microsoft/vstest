// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionManagerTests
    {
        private readonly DataCollectionManager dataCollectionManager;
        private readonly string defaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";
        private readonly string defaultDataCollectionSettings = "<DataCollector friendlyName=\"{0}\" uri=\"{1}\" assemblyQualifiedName=\"{2}\" codebase=\"{3}\" {4} />";
        private string dataCollectorSettings;
        private readonly string friendlyName;
        private readonly string uri;
        private readonly Mock<IMessageSink> mockMessageSink;
        private readonly Mock<DataCollector2> mockDataCollector;
        private readonly Mock<CodeCoverageDataCollector> mockCodeCoverageDataCollector;
        private readonly List<KeyValuePair<string, string>> envVarList;
        private readonly List<KeyValuePair<string, string>> codeCoverageEnvVarList;
        private readonly Mock<IDataCollectionAttachmentManager> mockDataCollectionAttachmentManager;
        private readonly Mock<IDataCollectionTelemetryManager> mockDataCollectionTelemetryManager;

        public DataCollectionManagerTests()
        {
            friendlyName = "CustomDataCollector";
            uri = "my://custom/datacollector";
            envVarList = new List<KeyValuePair<string, string>>();
            codeCoverageEnvVarList = new List<KeyValuePair<string, string>>();
            mockDataCollector = new Mock<DataCollector2>();
            mockDataCollector.As<ITestExecutionEnvironmentSpecifier>().Setup(x => x.GetTestExecutionEnvironmentVariables()).Returns(envVarList);
            mockCodeCoverageDataCollector = new Mock<CodeCoverageDataCollector>();
            mockCodeCoverageDataCollector.As<ITestExecutionEnvironmentSpecifier>().Setup(x => x.GetTestExecutionEnvironmentVariables()).Returns(codeCoverageEnvVarList);
            dataCollectorSettings = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, friendlyName, uri, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty));
            mockMessageSink = new Mock<IMessageSink>();
            mockDataCollectionAttachmentManager = new Mock<IDataCollectionAttachmentManager>();
            mockDataCollectionAttachmentManager.SetReturnsDefault(new List<AttachmentSet>());
            mockDataCollectionTelemetryManager = new Mock<IDataCollectionTelemetryManager>();

            dataCollectionManager = new TestableDataCollectionManager(mockDataCollectionAttachmentManager.Object, mockMessageSink.Object, mockDataCollector.Object, mockCodeCoverageDataCollector.Object, mockDataCollectionTelemetryManager.Object);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldThrowExceptionIfSettingsXmlIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => dataCollectionManager.InitializeDataCollectors(null));
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldReturnEmptyDictionaryIfDataCollectorsAreNotConfigured()
        {
            var runSettings = string.Format(defaultRunSettings, string.Empty);
            dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.AreEqual(0, dataCollectionManager.RunDataCollectors.Count);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollector()
        {
            var dataCollectorSettings = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, friendlyName, uri, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty));
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            Assert.IsTrue(dataCollectionManager.RunDataCollectors.ContainsKey(mockDataCollector.Object.GetType()));
            Assert.AreEqual(typeof(AttachmentProcessorDataCollector2), dataCollectionManager.RunDataCollectors[mockDataCollector.Object.GetType()].DataCollectorConfig.AttachmentsProcessorType);
            Assert.IsTrue(dataCollectionManager.RunDataCollectors[mockDataCollector.Object.GetType()].DataCollectorConfig.Metadata.Contains(true));
            mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldNotAddDataCollectorIfItIsDisabled()
        {
            var dataCollectorSettingsDisabled = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, friendlyName, uri, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, "enabled=\"false\""));
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettingsDisabled);

            Assert.AreEqual(0, dataCollectionManager.RunDataCollectors.Count);
            mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Never);
        }

        [TestMethod]
        public void InitializeShouldAddDataCollectorIfItIsEnabled()
        {
            var dataCollectorSettingsEnabled = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, friendlyName, uri, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, "enabled=\"true\""));
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettingsEnabled);

            Assert.IsTrue(dataCollectionManager.RunDataCollectors.ContainsKey(mockDataCollector.Object.GetType()));
            mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollectorIfFriendlyNameIsNotCorrectAndUriIsCorrect()
        {
            var dataCollectorSettingsWithWrongFriendlyName = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, "anyFriendlyName", uri, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty));
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettingsWithWrongFriendlyName);

            Assert.AreEqual(1, dataCollectionManager.RunDataCollectors.Count);
            mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollectorIfFriendlyNameIsCorrectAndUriIsNotCorrect()
        {
            var dataCollectorSettingsWithWrongUri = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, friendlyName, "my://custom/WrongDatacollector", mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty));
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettingsWithWrongUri);

            Assert.AreEqual(1, dataCollectionManager.RunDataCollectors.Count);
            mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollectorIfFriendlyNameIsCorrectAndUriIsNull()
        {
            var dataCollectorSettingsWithNullUri = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, friendlyName, string.Empty, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty).Replace("uri=\"\"", string.Empty));
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettingsWithNullUri);

            Assert.AreEqual(0, dataCollectionManager.RunDataCollectors.Count);
            mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Never);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollectorIfFriendlyNameIsNullAndUriIsCorrect()
        {
            var dataCollectorSettingsWithNullFriendlyName = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, string.Empty, uri, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty).Replace("friendlyName=\"\"", string.Empty));
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettingsWithNullFriendlyName);

            Assert.AreEqual(1, dataCollectionManager.RunDataCollectors.Count);
            mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollectorIfFriendlyNameIsCorrectAndUriIsEmpty()
        {
            var dataCollectorSettingsWithEmptyUri = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, friendlyName, string.Empty, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty));
            Assert.ThrowsException<ArgumentNullException>(() => dataCollectionManager.InitializeDataCollectors(dataCollectorSettingsWithEmptyUri));
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollectorIfFriendlyNameIsEmptyAndUriIsCorrect()
        {
            var dataCollectorSettingsWithEmptyFriendlyName = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, friendlyName, string.Empty, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty));
            Assert.ThrowsException<ArgumentNullException>(() => dataCollectionManager.InitializeDataCollectors(dataCollectorSettingsWithEmptyFriendlyName));
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldNotLoadDataCollectorIfFriendlyNameIsNotCorrectAndUriIsNotCorrect()
        {
            var dataCollectorSettingsWithWrongFriendlyNameAndWrongUri = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, "anyFriendlyName", "datacollector://data", mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty));
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettingsWithWrongFriendlyNameAndWrongUri);

            Assert.AreEqual(0, dataCollectionManager.RunDataCollectors.Count);
            mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Never);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldNotAddSameDataCollectorMoreThanOnce()
        {
            var datacollecterSettings = string.Format(defaultDataCollectionSettings, "CustomDataCollector", "my://custom/datacollector", mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, "enabled =\"true\"");
            var runSettings = string.Format(defaultRunSettings, datacollecterSettings + datacollecterSettings);

            dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.IsTrue(dataCollectionManager.RunDataCollectors.ContainsKey(mockDataCollector.Object.GetType()));
            mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollectorAndReturnEnvironmentVariables()
        {
            envVarList.Add(new KeyValuePair<string, string>("key", "value"));

            var result = dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            Assert.AreEqual("value", result["key"]);

            mockDataCollectionTelemetryManager.Verify(tm => tm.RecordEnvironmentVariableAddition(It.IsAny<DataCollectorInformation>(), "key", "value"));
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLogExceptionToMessageSinkIfInitializationFails()
        {
            mockDataCollector.Setup(
                x =>
                    x.Initialize(
                        It.IsAny<XmlElement>(),
                        It.IsAny<DataCollectionEvents>(),
                        It.IsAny<DataCollectionSink>(),
                        It.IsAny<DataCollectionLogger>(),
                        It.IsAny<DataCollectionEnvironmentContext>())).Throws<Exception>();

            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            Assert.AreEqual(0, dataCollectionManager.RunDataCollectors.Count);
            mockMessageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLogExceptionToMessageSinkIfSetEnvironmentVariableFails()
        {
            mockDataCollector.As<ITestExecutionEnvironmentSpecifier>().Setup(x => x.GetTestExecutionEnvironmentVariables()).Throws<Exception>();

            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            Assert.AreEqual(0, dataCollectionManager.RunDataCollectors.Count);
            mockMessageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldReturnFirstEnvironmentVariableIfMoreThanOneVariablesWithSameKeyIsSpecified()
        {
            envVarList.Add(new KeyValuePair<string, string>("key", "value"));
            envVarList.Add(new KeyValuePair<string, string>("key", "value1"));

            var result = dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            Assert.AreEqual("value", result["key"]);

            mockDataCollectionTelemetryManager.Verify(tm => tm.RecordEnvironmentVariableAddition(It.IsAny<DataCollectorInformation>(), "key", "value"));
            mockDataCollectionTelemetryManager.Verify(tm => tm.RecordEnvironmentVariableConflict(It.IsAny<DataCollectorInformation>(), "key", "value1", "value"));
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldReturnOtherThanCodeCoverageEnvironmentVariableIfMoreThanOneVariablesWithSameKeyIsSpecified()
        {
            envVarList.Add(new KeyValuePair<string, string>("cor_profiler", "clrie"));
            envVarList.Add(new KeyValuePair<string, string>("same_key", "same_value"));
            codeCoverageEnvVarList.Add(new KeyValuePair<string, string>("cor_profiler", "direct"));
            codeCoverageEnvVarList.Add(new KeyValuePair<string, string>("clrie_profiler_vanguard", "path"));
            codeCoverageEnvVarList.Add(new KeyValuePair<string, string>("same_key", "same_value"));

            dataCollectorSettings = string.Format(defaultRunSettings,
                string.Format(defaultDataCollectionSettings, "Code Coverage", "my://custom/ccdatacollector", mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty) +
                string.Format(defaultDataCollectionSettings, friendlyName, uri, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty));

            var result = dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("clrie", result["cor_profiler"]);
            Assert.AreEqual("path", result["clrie_profiler_vanguard"]);
            Assert.AreEqual("same_value", result["same_key"]);

            mockDataCollectionTelemetryManager.Verify(tm => tm.RecordEnvironmentVariableAddition(It.Is<DataCollectorInformation>(i => i.DataCollectorConfig.FriendlyName == friendlyName), "cor_profiler", "clrie"));
            mockDataCollectionTelemetryManager.Verify(tm => tm.RecordEnvironmentVariableConflict(It.Is<DataCollectorInformation>(i => i.DataCollectorConfig.FriendlyName == "Code Coverage"), "cor_profiler", "direct", "clrie"));
            mockDataCollectionTelemetryManager.Verify(tm => tm.RecordEnvironmentVariableAddition(It.Is<DataCollectorInformation>(i => i.DataCollectorConfig.FriendlyName == "Code Coverage"), "clrie_profiler_vanguard", "path"));
            mockDataCollectionTelemetryManager.Verify(tm => tm.RecordEnvironmentVariableAddition(It.Is<DataCollectorInformation>(i => i.DataCollectorConfig.FriendlyName == friendlyName), "same_key", "same_value"));
            mockDataCollectionTelemetryManager.Verify(tm => tm.RecordEnvironmentVariableConflict(It.Is<DataCollectorInformation>(i => i.DataCollectorConfig.FriendlyName == "Code Coverage"), "same_key", "same_value", "same_value"));
        }

        [TestMethod]
        public void SessionStartedShouldReturnFalseIfDataCollectionIsNotConfiguredInRunSettings()
        {
            var runSettings = string.Format(defaultRunSettings, string.Empty);
            dataCollectionManager.InitializeDataCollectors(runSettings);

            var sessionStartEventArgs = new SessionStartEventArgs();
            var result = dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SessionStartedShouldSendEventToDataCollector()
        {
            var isStartInvoked = false;
            SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => b.SessionStart += (sender, eventArgs) => isStartInvoked = true);

            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            var sessionStartEventArgs = new SessionStartEventArgs();
            var areTestCaseEventsSubscribed = dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.IsTrue(isStartInvoked);
            Assert.IsFalse(areTestCaseEventsSubscribed);
        }

        [TestMethod]
        public void SessionStartedShouldReturnTrueIfTestCaseStartIsSubscribed()
        {
            SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => b.TestCaseStart += (sender, eventArgs) => { });

            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            var sessionStartEventArgs = new SessionStartEventArgs();
            var areTestCaseEventsSubscribed = dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.IsTrue(areTestCaseEventsSubscribed);
        }

        [TestMethod]
        public void SessionStaretedShouldContinueDataCollectionIfExceptionIsThrownWhileSendingEventsToDataCollector()
        {
            SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => b.SessionStart += (sender, eventArgs) => throw new Exception());

            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            var sessionStartEventArgs = new SessionStartEventArgs();
            var result = dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SessionStartedShouldReturnFalseIfDataCollectorsAreNotInitialized()
        {
            var sessionStartEventArgs = new SessionStartEventArgs();
            var result = dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SessionStartedShouldHaveCorrectSessionContext()
        {
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            var sessionStartEventArgs = new SessionStartEventArgs();

            Assert.AreEqual(new SessionId(Guid.Empty), sessionStartEventArgs.Context.SessionId);

            dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.AreNotEqual(new SessionId(Guid.Empty), sessionStartEventArgs.Context.SessionId);
        }

        [TestMethod]
        public void SessionEndedShouldReturnEmptyCollectionIfDataCollectionIsNotEnabled()
        {
            var runSettings = string.Format(defaultRunSettings, string.Empty);
            dataCollectionManager.InitializeDataCollectors(runSettings);

            var result = dataCollectionManager.SessionEnded();

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetInvokedDataCollectorsShouldReturnDataCollector()
        {
            var dataCollectorSettingsWithNullFriendlyName = string.Format(defaultRunSettings, string.Format(defaultDataCollectionSettings, string.Empty, uri, mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty).Replace("friendlyName=\"\"", string.Empty));
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettingsWithNullFriendlyName);
            var invokedDataCollector = dataCollectionManager.GetInvokedDataCollectors();
            Assert.AreEqual(1, invokedDataCollector.Count);
            Assert.IsTrue(invokedDataCollector[0].HasAttachmentProcessor);
        }

        [TestMethod]
        public void SessionEndedShouldReturnAttachments()
        {
            var attachment = new AttachmentSet(new Uri("my://custom/datacollector"), "CustomDataCollector");
            attachment.Attachments.Add(new UriDataAttachment(new Uri("my://filename.txt"), "filename.txt"));

            mockDataCollectionAttachmentManager.Setup(x => x.GetAttachments(It.IsAny<DataCollectionContext>())).Returns(new List<AttachmentSet>() { attachment });

            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);
            var sessionStartEventArgs = new SessionStartEventArgs();
            dataCollectionManager.SessionStarted(sessionStartEventArgs);

            var result = dataCollectionManager.SessionEnded();

            Assert.IsTrue(result[0].Attachments[0].Uri.ToString().Contains("filename.txt"));
        }

        [TestMethod]
        public void SessionEndedShouldNotReturnAttachmentsIfExceptionIsThrownWhileGettingAttachments()
        {
            mockDataCollectionAttachmentManager.Setup(x => x.GetAttachments(It.IsAny<DataCollectionContext>())).Throws<Exception>();
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);

            var result = dataCollectionManager.SessionEnded();

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void SessionEndedShouldContinueDataCollectionIfExceptionIsThrownWhileSendingSessionEndEventToDataCollector()
        {
            var attachment = new AttachmentSet(new Uri("my://custom/datacollector"), "CustomDataCollector");
            attachment.Attachments.Add(new UriDataAttachment(new Uri("my://filename.txt"), "filename.txt"));

            mockDataCollectionAttachmentManager.Setup(x => x.GetAttachments(It.IsAny<DataCollectionContext>())).Returns(new List<AttachmentSet>() { attachment });

            SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => b.SessionEnd += (sender, ev) =>
    {
        c.SendFileAsync(e.SessionDataCollectionContext, "filename.txt", true);
        throw new Exception();
    });

            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);
            var sessionStartEventArgs = new SessionStartEventArgs();
            dataCollectionManager.SessionStarted(sessionStartEventArgs);

            var result = dataCollectionManager.SessionEnded();

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void SessionEndedShouldCancelProcessingAttachmentRequestsIfSessionIsCancelled()
        {
            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);
            var sessionStartEventArgs = new SessionStartEventArgs();
            dataCollectionManager.SessionStarted(sessionStartEventArgs);

            var result = dataCollectionManager.SessionEnded(true);

            mockDataCollectionAttachmentManager.Verify(x => x.Cancel(), Times.Once);
        }

        #region TestCaseEventsTest

        [TestMethod]
        public void TestCaseStartedShouldSendEventToDataCollector()
        {
            var isStartInvoked = false;
            SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => b.TestCaseStart += (sender, eventArgs) => isStartInvoked = true);

            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);
            var args = new TestCaseStartEventArgs(new TestCase());
            dataCollectionManager.TestCaseStarted(args);

            Assert.IsTrue(isStartInvoked);
        }

        [TestMethod]
        public void TestCaseStartedShouldNotSendEventToDataCollectorIfDataColletionIsNotEnbled()
        {
            var isStartInvoked = false;
            SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => b.TestCaseStart += (sender, eventArgs) => isStartInvoked = true);

            var args = new TestCaseStartEventArgs(new TestCase());
            dataCollectionManager.TestCaseStarted(args);

            Assert.IsFalse(isStartInvoked);
        }

        [TestMethod]
        public void TestCaseEndedShouldSendEventToDataCollector()
        {
            var isEndInvoked = false;
            SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => b.TestCaseEnd += (sender, eventArgs) => isEndInvoked = true);

            dataCollectionManager.InitializeDataCollectors(dataCollectorSettings);
            var args = new TestCaseEndEventArgs();
            args.TestElement = new TestCase();
            dataCollectionManager.TestCaseEnded(args);

            Assert.IsTrue(isEndInvoked);
        }

        [TestMethod]
        public void TestCaseEndedShouldNotSendEventToDataCollectorIfDataColletionIsNotEnbled()
        {
            var isEndInvoked = false;
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettings);
            SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => b.TestCaseEnd += (sender, eventArgs) => isEndInvoked = true);

            var args = new TestCaseEndEventArgs();
            Assert.IsFalse(isEndInvoked);
        }

        private void SetupMockDataCollector(Action<XmlElement, DataCollectionEvents, DataCollectionSink, DataCollectionLogger, DataCollectionEnvironmentContext> callback)
        {
            mockDataCollector.Setup(
                x =>
                    x.Initialize(
                        It.IsAny<XmlElement>(),
                        It.IsAny<DataCollectionEvents>(),
                        It.IsAny<DataCollectionSink>(),
                        It.IsAny<DataCollectionLogger>(),
                        It.IsAny<DataCollectionEnvironmentContext>())).Callback<XmlElement, DataCollectionEvents, DataCollectionSink, DataCollectionLogger, DataCollectionEnvironmentContext>((a, b, c, d, e) => callback.Invoke(a, b, c, d, e));
        }

        #endregion
    }

    internal class TestableDataCollectionManager : DataCollectionManager
    {
        readonly DataCollector dataCollector;
        readonly DataCollector ccDataCollector;

        public TestableDataCollectionManager(IDataCollectionAttachmentManager datacollectionAttachmentManager, IMessageSink messageSink, DataCollector dataCollector, DataCollector ccDataCollector, IDataCollectionTelemetryManager dataCollectionTelemetryManager) : this(datacollectionAttachmentManager, messageSink, dataCollectionTelemetryManager)
        {
            this.dataCollector = dataCollector;
            this.ccDataCollector = ccDataCollector;
        }

        internal TestableDataCollectionManager(IDataCollectionAttachmentManager datacollectionAttachmentManager, IMessageSink messageSink, IDataCollectionTelemetryManager dataCollectionTelemetryManager) : base(datacollectionAttachmentManager, messageSink, dataCollectionTelemetryManager)
        {
        }

        protected override bool TryGetUriFromFriendlyName(string friendlyName, out string dataCollectorUri)
        {
            if (friendlyName.Equals("CustomDataCollector"))
            {
                dataCollectorUri = "my://custom/datacollector";
                return true;
            }
            else if (friendlyName.Equals("Code Coverage"))
            {
                dataCollectorUri = "my://custom/ccdatacollector";
                return true;
            }
            else
            {
                dataCollectorUri = string.Empty;
                return false;
            }
        }

        protected override bool IsUriValid(string uri)
        {
            return uri.Equals("my://custom/datacollector") || uri.Equals("my://custom/ccdatacollector");
        }

        protected override DataCollector TryGetTestExtension(string extensionUri)
        {
            if (extensionUri.Equals("my://custom/datacollector"))
            {
                return dataCollector;
            }

            return extensionUri.Equals("my://custom/ccdatacollector") ? ccDataCollector : null;
        }

        protected override DataCollectorConfig TryGetDataCollectorConfig(string extensionUri)
        {
            if (extensionUri.Equals("my://custom/datacollector"))
            {
                var dc = new DataCollectorConfig(dataCollector.GetType());
                dc.FilePath = Path.GetTempFileName();
                return dc;
            }

            if (extensionUri.Equals("my://custom/ccdatacollector"))
            {
                var dc = new DataCollectorConfig(ccDataCollector.GetType());
                dc.FilePath = Path.GetTempFileName();
                return dc;
            }

            return null;
        }
    }

    [DataCollectorFriendlyName("CustomDataCollector")]
    [DataCollectorTypeUri("my://custom/datacollector")]
    [DataCollectorAttachmentProcessor(typeof(AttachmentProcessorDataCollector2))]
    public abstract class DataCollector2 : DataCollector
    {
    }

    [DataCollectorFriendlyName("Code Coverage")]
    [DataCollectorTypeUri("my://custom/ccdatacollector")]
    public abstract class CodeCoverageDataCollector : DataCollector
    {
    }

    public class AttachmentProcessorDataCollector2 : IDataCollectorAttachmentProcessor
    {
        public bool SupportsIncrementalProcessing => throw new NotImplementedException();

        public IEnumerable<Uri> GetExtensionUris()
        {
            throw new NotImplementedException();
        }

        public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement, ICollection<AttachmentSet> attachments, IProgress<int> progressReporter, IMessageLogger logger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
