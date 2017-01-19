// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.IO;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.DataCollector.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    [TestClass]
    public class DataCollectionManagerTests
    {
        private DataCollectionManager dataCollectorManager;
        private string defaultRunSettings = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors >{0}</DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";
        private string dataCollectorSettings;
        private string dataCollectorSettingsWithWrongUri, dataCollectorSettingsWithoutUri;


        [TestInitialize]
        public void Init()
        {
            this.dataCollectorManager = new DataCollectionManager(new DataCollectionAttachmentManager(), new DummyMessageSink());
            CustomDataCollector.IsInitialized = false;
            CustomDataCollector.IsSessionStartedInvoked = false;
            CustomDataCollector.IsSessionEndedInvoked = false;

            this.dataCollectorSettings = string.Format("<DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom/datacollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollector, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" codebase=\"{0}\" />", typeof(CustomDataCollector).GetTypeInfo().Assembly.Location);
            this.dataCollectorSettingsWithWrongUri = string.Format("<DataCollector friendlyName=\"CustomDataCollector\" uri=\"my://custom1/datacollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollector, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" codebase=\"{0}\" />", typeof(CustomDataCollector).GetTypeInfo().Assembly.Location);
            this.dataCollectorSettingsWithoutUri = string.Format("<DataCollector friendlyName=\"CustomDataCollector\" assemblyQualifiedName=\"Microsoft.VisualStudio.TestPlatform.DataCollector.UnitTests.CustomDataCollector, datacollector.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\" codebase=\"{0}\" />", typeof(CustomDataCollector).GetTypeInfo().Assembly.Location);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldThrowExceptionIfSettingsXmlIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.dataCollectorManager.InitializeDataCollectors(null);
            });
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldReturnEmptyDictionaryIfDataCollectorsAreNotConfigured()
        {
            string RunSettings = string.Format(this.defaultRunSettings, string.Empty);
            var result = this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldLoadDataCollector()
        {
            string RunSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            var result = this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(1, this.dataCollectorManager.RunDataCollectors.Count);
            Assert.IsTrue(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void InitializeDataCollectorsShouldNotLoadDataCollectorIfUriIsNotCorrect()
        {
            string RunSettings = string.Format(defaultRunSettings, dataCollectorSettingsWithWrongUri);

            var result = this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(0, this.dataCollectorManager.RunDataCollectors.Count);
            Assert.IsFalse(CustomDataCollector.IsInitialized);
        }

        public void InitializeDataCollectorShouldNotAddSameDataCollectorMoreThanOnce()
        {
            string RunSettings = string.Format(defaultRunSettings, dataCollectorSettings + dataCollectorSettings);

            var result = this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(1, this.dataCollectorManager.RunDataCollectors.Count);
            Assert.IsTrue(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void InitializeDataCollectorShouldNotAddDataCollectorIfUriIsNotSpecifiedByDataCollector()
        {
            string RunSettings = string.Format(defaultRunSettings, dataCollectorSettingsWithoutUri);

            Assert.ThrowsException<SettingsException>(() =>
            {
                this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            });
        }

        // todo : add tests for verifying logger after implementing logger functionality.

        [TestMethod]
        public void SessionStartedShouldReturnFalseIfDataCollectionIsNotEnabled()
        {
            string RunSettings = string.Format(defaultRunSettings, string.Empty);
            this.dataCollectorManager.InitializeDataCollectors(RunSettings);

            var result = this.dataCollectorManager.SessionStarted();

            Assert.IsFalse(result);
            Assert.IsFalse(CustomDataCollector.IsInitialized);
        }

        [TestMethod]
        public void SessionStartedShouldSendEventToDataCollector()
        {
            string RunSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            var result = this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            this.dataCollectorManager.SessionStarted();

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
            Assert.AreEqual(1, this.dataCollectorManager.RunDataCollectors.Count);
            Assert.IsTrue(CustomDataCollector.IsInitialized);
            Assert.IsTrue(CustomDataCollector.IsSessionStartedInvoked);
        }

        [TestMethod]
        public void SessionEndedShouldReturnNullIfDataCollectionIsNotEnabled()
        {
            string RunSettings = string.Format(defaultRunSettings, string.Empty);
            this.dataCollectorManager.InitializeDataCollectors(RunSettings);

            var result = this.dataCollectorManager.SessionEnded(false);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void SessionEndedShouldReturnAttachments()
        {
            string RunSettings = string.Format(defaultRunSettings, dataCollectorSettings);

            this.dataCollectorManager.InitializeDataCollectors(RunSettings);
            this.dataCollectorManager.SessionStarted();

            var result = this.dataCollectorManager.SessionEnded(false);

            Assert.AreEqual(1, this.dataCollectorManager.RunDataCollectors.Count);
            Assert.IsTrue(CustomDataCollector.IsInitialized);
            Assert.IsTrue(CustomDataCollector.IsSessionEndedInvoked);
            Assert.IsNotNull(result);
        }
    }

    [DataCollectorFriendlyName("CustomDataCollector")]
    [DataCollectorTypeUri("my://custom/datacollector")]
    public class CustomDataCollector : DataCollector
    {
        public static bool IsInitialized = false;
        public static bool IsSessionStartedInvoked = false;
        public static bool IsSessionEndedInvoked = false;
        public DataCollectionEnvironmentContext dataCollectionEnvironmentContext { get; set; }
        public DataCollectionSink dataSink { get; set; }

        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
            IsInitialized = true;
            this.dataCollectionEnvironmentContext = environmentContext;
            this.dataSink = dataSink;
            events.SessionStart += new EventHandler<SessionStartEventArgs>(SessionStarted_Handler);
            events.SessionEnd += new EventHandler<SessionEndEventArgs>(SessionEnded_Handler);
        }

        private void SessionStarted_Handler(object sender, SessionStartEventArgs args)
        {
            IsSessionStartedInvoked = true;
            var filename = Path.Combine(AppContext.BaseDirectory, "filename.txt");
            File.WriteAllText(filename, string.Empty);
            this.dataSink.SendFileAsync(dataCollectionEnvironmentContext.SessionDataCollectionContext, filename, true);
        }

        private void SessionEnded_Handler(object sender, SessionEndEventArgs args)
        {
            IsSessionEndedInvoked = true;
        }
    }

    [DataCollectorFriendlyName("CustomDataCollector")]
    public class CustomDataCollectorWithoutUri : DataCollector
    {
        public static bool IsInitialized = false;

        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {
            IsInitialized = true;
        }
    }

    public class DummyMessageSink : IMessageSink
    {
        public EventHandler<DataCollectionMessageEventArgs> OnDataCollectionMessage { get; set; }

        /// <summary>
        /// Data collection message as sent by DataCollectionLogger.
        /// </summary>
        /// <param name="args">Data collection message event args.</param>
        public void SendMessage(DataCollectionMessageEventArgs args)
        {
            //System.Diagnostics.Debugger.Launch();
        }
    }
}
