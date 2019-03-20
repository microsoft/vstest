// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Common.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class DataCollectionManagerTests
    {
        private DataCollectionManager dataCollectionManager;
        private string defaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";
        private string defaultDataCollectionSettings = "<DataCollector friendlyName=\"{0}\" uri=\"{1}\" assemblyQualifiedName=\"{2}\" codebase=\"{3}\" {4} />";
        private string dataCollectorSettings;

        private string dataCollectorSettingsWithWrongFriendlyName, dataCollectorSettingsWithoutFriendlyName, dataCollectorSettingsEnabled, dataCollectorSettingsDisabled;

        private Mock<IMessageSink> mockMessageSink;
        private Mock<DataCollector2> mockDataCollector;
        private List<KeyValuePair<string, string>> envVarList;
        private Mock<IDataCollectionAttachmentManager> mockDataCollectionAttachmentManager;

        public DataCollectionManagerTests()
        {
            var friendlyName = "CustomDataCollector";
            var uri = "my://custom/datacollector";

            this.envVarList = new List<KeyValuePair<string, string>>();
            this.mockDataCollector = new Mock<DataCollector2>();
            this.mockDataCollector.As<ITestExecutionEnvironmentSpecifier>().Setup(x => x.GetTestExecutionEnvironmentVariables()).Returns(this.envVarList);

            this.dataCollectorSettings = string.Format(this.defaultRunSettings, string.Format(this.defaultDataCollectionSettings, friendlyName, uri, this.mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty));
            this.dataCollectorSettingsWithWrongFriendlyName = string.Format(this.defaultRunSettings, string.Format(this.defaultDataCollectionSettings, "anyFriendlyName", uri, this.mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty));
            this.dataCollectorSettingsWithoutFriendlyName = string.Format(this.defaultRunSettings, string.Format(this.defaultDataCollectionSettings, string.Empty, uri, this.mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, string.Empty).Replace("friendlyName=\"\"", string.Empty));
            this.dataCollectorSettingsEnabled = string.Format(this.defaultRunSettings, string.Format(this.defaultDataCollectionSettings, friendlyName, uri, this.mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, "enabled=\"true\""));
            this.dataCollectorSettingsDisabled = string.Format(this.defaultRunSettings, string.Format(this.defaultDataCollectionSettings, friendlyName, uri, this.mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, "enabled=\"false\""));
            this.mockMessageSink = new Mock<IMessageSink>();
            this.mockDataCollectionAttachmentManager = new Mock<IDataCollectionAttachmentManager>();
            this.mockDataCollectionAttachmentManager.SetReturnsDefault<List<AttachmentSet>>(new List<AttachmentSet>());

            this.dataCollectionManager = new TestableDataCollectionManager(this.mockDataCollectionAttachmentManager.Object, this.mockMessageSink.Object, this.mockDataCollector.Object);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldThrowExceptionIfSettingsXmlIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.dataCollectionManager.InitializeDataCollectors(null);
            });
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldReturnEmptyDictionaryIfDataCollectorsAreNotConfigured()
        {
            var runSettings = string.Format(this.defaultRunSettings, string.Empty);
            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.AreEqual(0, this.dataCollectionManager.RunDataCollectors.Count);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollector()
        {
            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);

            Assert.IsTrue(this.dataCollectionManager.RunDataCollectors.ContainsKey(this.mockDataCollector.Object.GetType()));
            this.mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Once);
        }

        [TestMethod]
        public void InitializeShouldNotAddDataCollectorIfItIsDisabled()
        {
            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettingsDisabled);

            Assert.AreEqual(0, this.dataCollectionManager.RunDataCollectors.Count);
            this.mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Never);
        }

        [TestMethod]
        public void InitializeShouldAddDataCollectorIfItIsEnabled()
        {
            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettingsEnabled);

            Assert.IsTrue(this.dataCollectionManager.RunDataCollectors.ContainsKey(this.mockDataCollector.Object.GetType()));
            this.mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Once);
        }


        [TestMethod]
        public void InitializeDataCollectorsShouldNotLoadDataCollectorIfFriendlyNameIsNotCorrect()
        {
            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettingsWithWrongFriendlyName);

            Assert.AreEqual(0, this.dataCollectionManager.RunDataCollectors.Count);
            this.mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Never);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldNotAddSameDataCollectorMoreThanOnce()
        {
            var datacollecterSettings = string.Format(this.defaultDataCollectionSettings, "CustomDataCollector", "my://custom/datacollector", this.mockDataCollector.Object.GetType().AssemblyQualifiedName, typeof(DataCollectionManagerTests).GetTypeInfo().Assembly.Location, "enabled =\"true\"");
            var runSettings = string.Format(this.defaultRunSettings, datacollecterSettings + datacollecterSettings);

            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.IsTrue(this.dataCollectionManager.RunDataCollectors.ContainsKey(this.mockDataCollector.Object.GetType()));
            this.mockDataCollector.Verify(x => x.Initialize(It.IsAny<XmlElement>(), It.IsAny<DataCollectionEvents>(), It.IsAny<DataCollectionSink>(), It.IsAny<DataCollectionLogger>(), It.IsAny<DataCollectionEnvironmentContext>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldNotAddDataCollectorIfFriendlyNameIsNotSpecifiedByDataCollector()
        {
            Assert.ThrowsException<SettingsException>(() =>
            {
                this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettingsWithoutFriendlyName);
            });
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollectorAndReturnEnvironmentVariables()
        {
            this.envVarList.Add(new KeyValuePair<string, string>("key", "value"));

            var result = this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);

            Assert.AreEqual("value", result["key"]);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLogExceptionToMessageSinkIfInitializationFails()
        {
            this.mockDataCollector.Setup(
                x =>
                    x.Initialize(
                        It.IsAny<XmlElement>(),
                        It.IsAny<DataCollectionEvents>(),
                        It.IsAny<DataCollectionSink>(),
                        It.IsAny<DataCollectionLogger>(),
                        It.IsAny<DataCollectionEnvironmentContext>())).Throws<Exception>();

            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);

            Assert.AreEqual(0, this.dataCollectionManager.RunDataCollectors.Count);
            this.mockMessageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLogExceptionToMessageSinkIfSetEnvironmentVariableFails()
        {
            this.mockDataCollector.As<ITestExecutionEnvironmentSpecifier>().Setup(x => x.GetTestExecutionEnvironmentVariables()).Throws<Exception>();

            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);

            Assert.AreEqual(0, this.dataCollectionManager.RunDataCollectors.Count);
            this.mockMessageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldReturnFirstEnvironmentVariableIfMoreThanOneVariablesWithSameKeyIsSpecified()
        {
            this.envVarList.Add(new KeyValuePair<string, string>("key", "value"));
            this.envVarList.Add(new KeyValuePair<string, string>("key", "value1"));

            var result = this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);

            Assert.AreEqual("value", result["key"]);
        }

        [TestMethod]
        public void SessionStartedShouldReturnFalseIfDataCollectionIsNotConfiguredInRunSettings()
        {
            var runSettings = string.Format(this.defaultRunSettings, string.Empty);
            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            var sessionStartEventArgs = new SessionStartEventArgs();
            var result = this.dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SessionStartedShouldSendEventToDataCollector()
        {
            var isStartInvoked = false;
            this.SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) =>
            {
                b.SessionStart += (sender, eventArgs) => isStartInvoked = true;
            });

            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);

            var sessionStartEventArgs = new SessionStartEventArgs();
            var areTestCaseEventsSubscribed = this.dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.IsTrue(isStartInvoked);
            Assert.IsFalse(areTestCaseEventsSubscribed);
        }

        [TestMethod]
        public void SessionStartedShouldReturnTrueIfTestCaseStartIsSubscribed()
        {
            this.SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) =>
            {
                b.TestCaseStart += (sender, eventArgs) => { };
            });

            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);

            var sessionStartEventArgs = new SessionStartEventArgs();
            var areTestCaseEventsSubscribed = this.dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.IsTrue(areTestCaseEventsSubscribed);
        }

        [TestMethod]
        public void SessionStaretedShouldContinueDataCollectionIfExceptionIsThrownWhileSendingEventsToDataCollector()
        {
            this.SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) =>
            {
                b.SessionStart += (sender, eventArgs) => throw new Exception();
            });

            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);

            var sessionStartEventArgs = new SessionStartEventArgs();
            var result = this.dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SessionStartedShouldReturnFalseIfDataCollectorsAreNotInitialized()
        {
            var sessionStartEventArgs = new SessionStartEventArgs();
            var result = this.dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SessionStartedShouldHaveCorrectSessionContext()
        {
            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);

            var sessionStartEventArgs = new SessionStartEventArgs();

            Assert.AreEqual(sessionStartEventArgs.Context.SessionId, new SessionId(Guid.Empty));

            this.dataCollectionManager.SessionStarted(sessionStartEventArgs);

            Assert.AreNotEqual(sessionStartEventArgs.Context.SessionId, new SessionId(Guid.Empty));
        }

        [TestMethod]
        public void SessionEndedShouldReturnEmptyCollectionIfDataCollectionIsNotEnabled()
        {
            var runSettings = string.Format(this.defaultRunSettings, string.Empty);
            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            var result = this.dataCollectionManager.SessionEnded();

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void SessionEndedShouldReturnAttachments()
        {
            var attachment = new AttachmentSet(new Uri("my://custom/datacollector"), "CustomDataCollector");
            attachment.Attachments.Add(new UriDataAttachment(new Uri("my://filename.txt"), "filename.txt"));

            this.mockDataCollectionAttachmentManager.Setup(x => x.GetAttachments(It.IsAny<DataCollectionContext>())).Returns(new List<AttachmentSet>() { attachment });

            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);
            var sessionStartEventArgs = new SessionStartEventArgs();
            this.dataCollectionManager.SessionStarted(sessionStartEventArgs);

            var result = this.dataCollectionManager.SessionEnded();

            Assert.IsTrue(result[0].Attachments[0].Uri.ToString().Contains("filename.txt"));
        }

        [TestMethod]
        public void SessionEndedShouldNotReturnAttachmentsIfExceptionIsThrownWhileGettingAttachments()
        {
            this.mockDataCollectionAttachmentManager.Setup(x => x.GetAttachments(It.IsAny<DataCollectionContext>())).Throws<Exception>();
            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);

            var result = this.dataCollectionManager.SessionEnded();

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void SessionEndedShouldContinueDataCollectionIfExceptionIsThrownWhileSendingSessionEndEventToDataCollector()
        {
            var attachment = new AttachmentSet(new Uri("my://custom/datacollector"), "CustomDataCollector");
            attachment.Attachments.Add(new UriDataAttachment(new Uri("my://filename.txt"), "filename.txt"));

            this.mockDataCollectionAttachmentManager.Setup(x => x.GetAttachments(It.IsAny<DataCollectionContext>())).Returns(new List<AttachmentSet>() { attachment });

            this.SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) =>
            {
                b.SessionEnd += (sender, ev) =>
                    {
                        c.SendFileAsync(e.SessionDataCollectionContext, "filename.txt", true);
                        throw new Exception();
                    };
            });

            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);
            var sessionStartEventArgs = new SessionStartEventArgs();
            this.dataCollectionManager.SessionStarted(sessionStartEventArgs);

            var result = this.dataCollectionManager.SessionEnded();

            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void SessionEndedShouldCancelProcessingAttachmentRequestsIfSessionIsCancelled()
        {
            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);
            var sessionStartEventArgs = new SessionStartEventArgs();
            this.dataCollectionManager.SessionStarted(sessionStartEventArgs);

            var result = this.dataCollectionManager.SessionEnded(true);

            this.mockDataCollectionAttachmentManager.Verify(x => x.Cancel(), Times.Once);
        }

        #region TestCaseEventsTest

        [TestMethod]
        public void TestCaseStartedShouldSendEventToDataCollector()
        {
            var isStartInvoked = false;
            this.SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => { b.TestCaseStart += (sender, eventArgs) => isStartInvoked = true; });

            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);
            var args = new TestCaseStartEventArgs(new TestCase());
            this.dataCollectionManager.TestCaseStarted(args);

            Assert.IsTrue(isStartInvoked);
        }

        [TestMethod]
        public void TestCaseStartedShouldNotSendEventToDataCollectorIfDataColletionIsNotEnbled()
        {
            var isStartInvoked = false;
            this.SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => { b.TestCaseStart += (sender, eventArgs) => isStartInvoked = true; });

            var args = new TestCaseStartEventArgs(new TestCase());
            this.dataCollectionManager.TestCaseStarted(args);

            Assert.IsFalse(isStartInvoked);
        }

        [TestMethod]
        public void TestCaseEndedShouldSendEventToDataCollector()
        {
            var isEndInvoked = false;
            this.SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) => { b.TestCaseEnd += (sender, eventArgs) => isEndInvoked = true; });

            this.dataCollectionManager.InitializeDataCollectors(this.dataCollectorSettings);
            var args = new TestCaseEndEventArgs();
            args.TestElement = new TestCase();
            this.dataCollectionManager.TestCaseEnded(args);

            Assert.IsTrue(isEndInvoked);
        }

        [TestMethod]
        public void TestCaseEndedShouldNotSendEventToDataCollectorIfDataColletionIsNotEnbled()
        {
            var isEndInvoked = false;
            var runSettings = string.Format(this.defaultRunSettings, this.dataCollectorSettings);
            this.SetupMockDataCollector((XmlElement a, DataCollectionEvents b, DataCollectionSink c, DataCollectionLogger d, DataCollectionEnvironmentContext e) =>
            {
                b.TestCaseEnd += (sender, eventArgs) => isEndInvoked = true;
            });

            var args = new TestCaseEndEventArgs();
            Assert.IsFalse(isEndInvoked);
        }

        private void SetupMockDataCollector(Action<XmlElement, DataCollectionEvents, DataCollectionSink, DataCollectionLogger, DataCollectionEnvironmentContext> callback)
        {
            this.mockDataCollector.Setup(
                x =>
                    x.Initialize(
                        It.IsAny<XmlElement>(),
                        It.IsAny<DataCollectionEvents>(),
                        It.IsAny<DataCollectionSink>(),
                        It.IsAny<DataCollectionLogger>(),
                        It.IsAny<DataCollectionEnvironmentContext>())).Callback<XmlElement, DataCollectionEvents, DataCollectionSink, DataCollectionLogger, DataCollectionEnvironmentContext>((a, b, c, d, e) =>
                        {
                            callback.Invoke(a, b, c, d, e);
                        });
        }

        #endregion
    }

    internal class TestableDataCollectionManager : DataCollectionManager
    {
        DataCollector dataCollector;

        public TestableDataCollectionManager(IDataCollectionAttachmentManager datacollectionAttachmentManager, IMessageSink messageSink, DataCollector dataCollector) : this(datacollectionAttachmentManager, messageSink)
        {
            this.dataCollector = dataCollector;
        }

        internal TestableDataCollectionManager(IDataCollectionAttachmentManager datacollectionAttachmentManager, IMessageSink messageSink) : base(datacollectionAttachmentManager, messageSink)
        {
        }

        protected override bool TryGetUriFromFriendlyName(string friendlyName, out string dataCollectorUri)
        {
            if (friendlyName.Equals("CustomDataCollector"))
            {
                dataCollectorUri = "my://custom/datacollector";
                return true;
            }
            else
            {
                dataCollectorUri = string.Empty;
                return false;
            }
        }

        protected override DataCollector TryGetTestExtension(string extensionUri)
        {
            if (extensionUri.Equals("my://custom/datacollector"))
            {
                return dataCollector;
            }
            return null;
        }
    }

    [DataCollectorFriendlyName("CustomDataCollector")]
    [DataCollectorTypeUri("my://custom/datacollector")]
    public abstract class DataCollector2 : DataCollector
    {
    }
}
