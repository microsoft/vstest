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

        private string dataCollectorSettingsWithWrongUri, dataCollectorSettingsWithoutUri, dataCollectorSettingsEnabled, dataCollectorSettingsDisabled;

        private Mock<IMessageSink> mockMessageSink;

        public DataCollectionManagerTests()
        {
            var friendlyName = "CustomDataCollector";
            var uri = "my://custom/datacollector";
            this.dataCollectorSettings = string.Format(defaultDataCollectionSettings, friendlyName, uri, typeof(CustomDataCollector).AssemblyQualifiedName, typeof(CustomDataCollector).GetTypeInfo().Assembly.Location, string.Empty);
            this.dataCollectorSettingsWithWrongUri = string.Format(defaultDataCollectionSettings, friendlyName, "my://custom1/datacollector", typeof(CustomDataCollector).AssemblyQualifiedName, typeof(CustomDataCollector).GetTypeInfo().Assembly.Location, string.Empty);
            this.dataCollectorSettingsWithoutUri = string.Format(defaultDataCollectionSettings, friendlyName, string.Empty, typeof(CustomDataCollector).AssemblyQualifiedName, typeof(CustomDataCollector).GetTypeInfo().Assembly.Location, string.Empty).Replace("uri=\"\"", string.Empty);
            this.dataCollectorSettingsEnabled = string.Format(defaultDataCollectionSettings, friendlyName, uri, typeof(CustomDataCollector).AssemblyQualifiedName, typeof(CustomDataCollector).GetTypeInfo().Assembly.Location, "enabled=\"true\"");
            this.dataCollectorSettingsDisabled = string.Format(defaultDataCollectionSettings, friendlyName, uri, typeof(CustomDataCollector).AssemblyQualifiedName, typeof(CustomDataCollector).GetTypeInfo().Assembly.Location, "enabled=\"false\"");

            this.mockMessageSink = new Mock<IMessageSink>();
            this.dataCollectionManager = new DataCollectionManager(new DataCollectionAttachmentManager(), this.mockMessageSink.Object);
        }

        [TestInitialize]
        public void Init()
        {
            CustomDataCollector.Reset();
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
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.AreEqual(1, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.IsTrue(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void InitializeShouldNotAddDataCollectorIfItIsDisabled()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettingsDisabled);

            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.AreEqual(0, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.IsFalse(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void InitializeShouldNotAddDataCollectorIfItIsEnabled()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettingsEnabled);

            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.AreEqual(1, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.IsTrue(CustomDataCollector.IsInitialized);
        }


        [TestMethod]
        public void InitializeDataCollectorsShouldNotLoadDataCollectorIfUriIsNotCorrect()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettingsWithWrongUri);

            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.AreEqual(0, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.IsFalse(CustomDataCollector.IsInitialized);
        }

        public void InitializeDataCollectorsShouldNotAddSameDataCollectorMoreThanOnce()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettings + dataCollectorSettings);

            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.AreEqual(1, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.IsTrue(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldNotAddDataCollectorIfUriIsNotSpecifiedByDataCollector()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettingsWithoutUri);

            Assert.ThrowsException<SettingsException>(() =>
            {
                this.dataCollectionManager.InitializeDataCollectors(runSettings);
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
            Assert.IsTrue(CustomDataCollector.IsDisposeInvoked);
            this.mockMessageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Once);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldReturnOnlyOneEnvironmentVariableIfMoreThanOneVariablesWithSameKeyIsSpecified()
        {
            var runSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            var envVarList = new List<KeyValuePair<string, string>>();
            envVarList.Add(new KeyValuePair<string, string>("key", "value"));
            envVarList.Add(new KeyValuePair<string, string>("key", "value1"));
            CustomDataCollector.EnvVarList = envVarList;

            var result = this.dataCollectionManager.InitializeDataCollectors(runSettings);

            Assert.IsTrue(CustomDataCollector.IsGetTestExecutionEnvironmentVariablesInvoked);
            Assert.AreEqual("key", result.Keys.First());
            Assert.AreEqual("value", result.Values.First());

            this.mockMessageSink.Verify(x => x.SendMessage(It.IsAny<DataCollectionMessageEventArgs>()), Times.Once);

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

            var mockDataCollectionAttachmentManager = new Mock<IDataCollectionAttachmentManager>();
            mockDataCollectionAttachmentManager.Setup(x => x.GetAttachments(It.IsAny<DataCollectionContext>())).Throws<Exception>();

            this.dataCollectionManager = new DataCollectionManager(mockDataCollectionAttachmentManager.Object, this.mockMessageSink.Object);
            this.dataCollectionManager.InitializeDataCollectors(runSettings);

            var result = this.dataCollectionManager.SessionEnded();

            Assert.AreEqual(0, result.Count);
        }
    }
}
