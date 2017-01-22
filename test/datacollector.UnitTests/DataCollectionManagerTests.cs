// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DataCollectionManagerTests
    {
        private DataCollectionManager dataCollectionManager;
        private string defaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";
        private string dataCollectorSettings;
        private string dataCollectorSettingsWithWrongUri, dataCollectorSettingsWithoutUri;

        private IMessageSink mockMessageSink;


        [TestInitialize]
        public void Init()
        {
            CustomDataCollector.Reset();
            DummyMessageSink.Reset();

            this.mockMessageSink = new DummyMessageSink();
            this.dataCollectionManager = new DataCollectionManager(new DataCollectionAttachmentManager(), new DummyMessageSink());

            this.dataCollectorSettings = string.Format("<DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom/datacollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollector, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" codebase=\"{0}\" />", typeof(CustomDataCollector).GetTypeInfo().Assembly.Location);
            this.dataCollectorSettingsWithWrongUri = string.Format("<DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom1/datacollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollector, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" codebase=\"{0}\" />", typeof(CustomDataCollector).GetTypeInfo().Assembly.Location);
            this.dataCollectorSettingsWithoutUri = string.Format("<DataCollector friendlyName=\"CustomDataCollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollector, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" codebase=\"{0}\" />", typeof(CustomDataCollector).GetTypeInfo().Assembly.Location);
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
            var RunSettings = string.Format(this.defaultRunSettings, string.Empty);
            this.dataCollectionManager.InitializeDataCollectors(RunSettings);

            Assert.AreEqual(0, this.dataCollectionManager.RunDataCollectors.Count);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollector()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.AreEqual(1, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.IsTrue(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldNotLoadDataCollectorIfUriIsNotCorrect()
        {
            var RunSettings = string.Format(defaultRunSettings, dataCollectorSettingsWithWrongUri);

            this.dataCollectionManager.InitializeDataCollectors(RunSettings);

            Assert.AreEqual(0, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.IsFalse(CustomDataCollector.IsInitialized);
        }

        public void InitializeDataCollectorsShouldNotAddSameDataCollectorMoreThanOnce()
        {
            var RunSettings = string.Format(defaultRunSettings, dataCollectorSettings + dataCollectorSettings);

            this.dataCollectionManager.InitializeDataCollectors(RunSettings);

            Assert.AreEqual(1, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.IsTrue(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldNotAddDataCollectorIfUriIsNotSpecifiedByDataCollector()
        {
            var RunSettings = string.Format(defaultRunSettings, dataCollectorSettingsWithoutUri);

            Assert.ThrowsException<SettingsException>(() =>
            {
                this.dataCollectionManager.InitializeDataCollectors(RunSettings);
            });
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollectorAndReturnEnvironmentVariables()
        {
            string runSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            var envVarList = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("key", "value") };
            CustomDataCollector.EnvVarList = envVarList;

            var result = this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("key", result.Keys.First());
            Assert.AreEqual("value", result.Values.First());
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLogExceptionToMessageSinkIfInitializationFails()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettings);
            CustomDataCollector.ThrowExceptionWhenInitialized = true;

            var result = this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.AreEqual(0, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.IsTrue(DummyMessageSink.IsSendMessageInvoked);
            Assert.IsTrue(CustomDataCollector.IsDisposeInvoked);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldReturnOnlyOneEnvirnmentVariableIfMoreThanOneVariablesWithSameKeyIsSpecified()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            var envVarList = new List<KeyValuePair<string, string>>();
            envVarList.Add(new KeyValuePair<string, string>("key", "value"));
            envVarList.Add(new KeyValuePair<string, string>("key", "value1"));
            CustomDataCollector.EnvVarList = envVarList;

            var result = this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.IsTrue(CustomDataCollector.IsGetTestExecutionEnvironmentVariablesInvoked);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("key", result.Keys.First());
            Assert.AreEqual("value", result.Values.First());
            Assert.IsTrue(DummyMessageSink.IsSendMessageInvoked);
        }

        [TestMethod]
        public void SessionStartedShouldReturnFalseIfDataCollectionIsNotConfiguredInRunSettings()
        {
            string runSettings = string.Format(defaultRunSettings, string.Empty);
            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            var result = this.dataCollectionManager.SessionStarted();

            Assert.IsFalse(result);
            Assert.IsFalse(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void SessionStartedShouldSendEventToDataCollector()
        {
            string runSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            this.dataCollectionManager.InitializeDataCollectors(runSettings);
            this.dataCollectionManager.SessionStarted();

            Assert.AreEqual(1, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.IsTrue(CustomDataCollector.IsInitialized);
            Assert.IsTrue(CustomDataCollector.IsSessionStartedInvoked);
        }

        [TestMethod]
        public void SessionStaretedShouldContinueDataCollectionIfExceptionIsThrownWhileSendingEventsToDataCollector()
        {
            string runSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            this.dataCollectionManager.InitializeDataCollectors(runSettings);
            CustomDataCollector.Events_SessionStartThrowException = true;

            this.dataCollectionManager.InitializeDataCollectors(runSettings);
            var count = this.dataCollectionManager.RunDataCollectors.Count;

            var result = this.dataCollectionManager.SessionStarted();

            Assert.IsTrue(CustomDataCollector.IsSessionStartedInvoked);

            this.dataCollectionManager.SessionEnded();
        }

        [TestMethod]
        public void SessionStartedShouldReturnFalseIfDataCollectorsAreNotInitialized()
        {
            var result = this.dataCollectionManager.SessionStarted();

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SessionEndedShouldReturnNullIfDataCollectionIsNotEnabled()
        {
            string runSettings = string.Format(defaultRunSettings, string.Empty);
            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            var result = this.dataCollectionManager.SessionEnded();

            Assert.IsNull(result);
        }

        [TestMethod]
        public void SessionEndedShouldReturnAttachments()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettings);
            CustomDataCollector.Attachfile = true;

            this.dataCollectionManager.InitializeDataCollectors(runSettings);
            this.dataCollectionManager.SessionStarted();

            var result = this.dataCollectionManager.SessionEnded();

            Assert.IsTrue(CustomDataCollector.IsSessionEndedInvoked);
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void SessionEndedShouldNotReturnAttachmentsIfInvokedTwice()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettings);
            CustomDataCollector.Attachfile = true;

            this.dataCollectionManager.InitializeDataCollectors(runSettings);
            this.dataCollectionManager.SessionStarted();

            var result = this.dataCollectionManager.SessionEnded();

            Assert.AreEqual(1, result.Count);

            result = this.dataCollectionManager.SessionEnded();
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void SessionEndedShouldNotReturnAttachmentsIfExceptionIsThrownWhileGettingAttachments()
        {
            string runSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            var mockDataCollectionAttachmentManager = new MockDataCollectionAttachmentManager();
            this.dataCollectionManager = new DataCollectionManager(mockDataCollectionAttachmentManager, this.mockMessageSink);
            this.dataCollectionManager.InitializeDataCollectors(runSettings);
            mockDataCollectionAttachmentManager.GetDataThrowException = true;

            var result = this.dataCollectionManager.SessionEnded();

            Assert.IsNull(result);
        }
    }

    internal class MockDataCollectionAttachmentManager : IDataCollectionAttachmentManager
    {
        public List<AttachmentSet> Attachments;
        public const string GetDataExceptionMessaage = "FileManagerExcpetion";
        public bool GetDataThrowException;

        public MockDataCollectionAttachmentManager()
        {
            this.Attachments = new List<AttachmentSet>();
        }

        public void Initialize(SessionId id, string outputDirectory, IMessageSink messageSink)
        {
        }

        public List<AttachmentSet> GetAttachments(DataCollectionContext dataCollectionContext)
        {
            if (this.GetDataThrowException)
            {
                throw new Exception(GetDataExceptionMessaage);
            }

            return this.Attachments;
        }

        public void AddAttachment(FileTransferInformationExtension fileTransferInfo)
        {
        }

        public void Dispose()
        {
        }
    }
}
