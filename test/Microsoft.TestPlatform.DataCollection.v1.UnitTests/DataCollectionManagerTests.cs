// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.DataCollection.V1.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.DataCollection.V1;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using VisualStudio.TestPlatform.Common.DataCollection.Interfaces;
    using VisualStudio.TestPlatform.ObjectModel;

    using IMessageSink = Microsoft.VisualStudio.TestPlatform.Common.DataCollection.Interfaces.IMessageSink;
    using TestCaseStartEventArgs = Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.Events.TestCaseStartEventArgs;

    [TestClass]
    public class DataCollectionManagerTests
    {
        private DataCollectionManager dataCollectionManager;
        private DummyMessageSink mockMessageSink;
        private RunSettings runSettings;
        private MockDataCollectionFileManager mockDataCollectionFileManager;

        private string xmlSettings =
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <RunConfiguration>\r\n    <MaxCpuCount>1</MaxCpuCount>\r\n    <ResultsDirectory>.\\TestResults</ResultsDirectory>\r\n    <TargetPlatform>x86</TargetPlatform>\r\n  </RunConfiguration>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"Custom DataCollector\" uri=\"datacollector://Company/Product/Version\" assemblyQualifiedName=\"Microsoft.TestPlatform.DataCollection.V1.UnitTests.MockDataCollector, Microsoft.TestPlatform.DataCollection.v1.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\">\r\n      </DataCollector>\r\n    <DataCollector friendlyName=\"Custom DataCollector\" uri=\"datacollector://Company/Product/Version2\" assemblyQualifiedName=\"Microsoft.TestPlatform.DataCollection.V1.UnitTests.MockDataCollector2, Microsoft.TestPlatform.DataCollection.v1.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\">\r\n      </DataCollector>\r\n </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

        [TestInitialize]
        public void Init()
        {
            SetupMockExtensions(new string[] { typeof(DataCollectorsSettingsProvider).GetTypeInfo().Assembly.Location }, () => { });

            this.mockMessageSink = new DummyMessageSink();
            this.mockDataCollectionFileManager = new MockDataCollectionFileManager();
            this.dataCollectionManager = new DataCollectionManager(this.mockMessageSink, this.mockDataCollectionFileManager);
            this.runSettings = new RunSettings();
            this.runSettings.LoadSettingsXml(this.xmlSettings);
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.mockDataCollectionFileManager.GetDataThrowException = false;
            ResetExtensionsCache();
            MockDataCollector.Reset();
            MockDataCollector2.Reset();
        }

        #region LoadDataCollector
        [TestMethod]
        public void LoadDataCollectorShouldLoadDataCollectorAndReturnEnvironmentVariables()
        {
            var envVarList = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("key", "value") };
            MockDataCollector.EnvVarList = envVarList;
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);

            var result = this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("key", result.Keys.First());
            Assert.AreEqual("value", result.Values.First());
        }

        [TestMethod]
        public void LoadDataCollectorShouldThrowExceptionIfRunSettingsIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                this.dataCollectionManager.LoadDataCollectors(null);
            });
        }

        [TestMethod]
        public void LoadDataCollectorsShouldReturnEmptyDictionaryIfDataCollectionSettingsProviderIsNotRegistered()
        {
            var result = this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void LoadDataCollectorsShouldInitializeDataCollector()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);

            var result = this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            Assert.IsTrue(MockDataCollector.IsInitializeInvoked);
        }

        [TestMethod]
        public void LoadDataCollectorsShouldLogExceptionToMessageSinkIfInitializationFails()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            MockDataCollector.ThrowExceptionWhenInitialized = true;

            var result = this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            Assert.IsTrue(this.mockMessageSink.IsSendMessageInvoked);
            Assert.IsTrue(MockDataCollector.IsDisposeInvoked);
        }

        [TestMethod]
        public void LaodDataCollectorsShouldReturnEnviornmentVariables()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            var envVarList = new List<KeyValuePair<string, string>>();
            var kvp = new KeyValuePair<string, string>("key", "value");
            envVarList.Add(kvp);
            MockDataCollector.EnvVarList = envVarList;

            var result = this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            Assert.IsTrue(MockDataCollector.IsGetTestExecutionEnvironmentVariablesInvoked);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("key", result.Keys.First());
            Assert.AreEqual("value", result.Values.First());
        }

        [TestMethod]
        public void LoadDataCollectorsShouldReturnOnlyOneEnvirnmentVariableIfMoreThanOneVariablesWithSameKeyIsSpecified()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            var envVarList = new List<KeyValuePair<string, string>>();
            envVarList.Add(new KeyValuePair<string, string>("key", "value"));
            envVarList.Add(new KeyValuePair<string, string>("key", "value1"));
            MockDataCollector.EnvVarList = envVarList;

            var result = this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            Assert.IsTrue(MockDataCollector.IsGetTestExecutionEnvironmentVariablesInvoked);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("key", result.Keys.First());
            Assert.AreEqual("value", result.Values.First());
            Assert.IsTrue(this.mockMessageSink.IsSendMessageInvoked);
        }

        #endregion

        #region SessionStarted

        [TestMethod]
        public void SessionStartedShouldSendSessionStartEventArgsToDataCollectors()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            var result = this.dataCollectionManager.SessionStarted();

            Assert.IsFalse(result);
            Assert.IsTrue(MockDataCollector.IsEvents_SessionStartInvoked);
        }

        [TestMethod]
        public void SessionStartedShouldReturnTrueIfTestCaseLevelEventsAreRegistered()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            MockDataCollector.ConfigureTestCaseLevelEvents = true;
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            var result = this.dataCollectionManager.SessionStarted();

            Assert.IsTrue(result);
            Assert.IsTrue(MockDataCollector.IsEvents_SessionStartInvoked);
        }

        [TestMethod]
        public void SessionStartedShouldReturnFalseIfDataCollectorsAreNotLoaded()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);

            var result = this.dataCollectionManager.SessionStarted();

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SessionStaretedShouldRemoveDataCollectorIfExceptionIsThrownWhileSendingSessionStartEventArgs()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            MockDataCollector.ConfigureTestCaseLevelEvents = true;
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);
            MockDataCollector2.Events_SessionStartThrowException = true;

            this.dataCollectionManager.LoadDataCollectors(this.runSettings);
            var count = this.dataCollectionManager.RunDataCollectors.Count;

            var result = this.dataCollectionManager.SessionStarted();

            Assert.AreEqual(count - 1, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, Resource.DataCollectorRunError, MockDataCollector2.Events_SessionStartExceptionMessage), this.mockMessageSink.EventMessage);
            Assert.IsTrue(result);
        }

        #endregion

        #region SessionEnded

        [TestMethod]
        public void SessionEndedShouldShouldSendSessionEndEventArgsToDataCollectors()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            MockDataCollector.ConfigureTestCaseLevelEvents = true;
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);
            var attachment = new AttachmentSet(new Uri("DataCollector://Attachment"), "DataCollectorAttachment");
            this.mockDataCollectionFileManager.Attachments.Add(attachment);

            var result = this.dataCollectionManager.SessionEnded(isCancelled: false);

            Assert.IsTrue(MockDataCollector.IsEvents_SessionEndInvoked);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(attachment.DisplayName, result.First().DisplayName);
            Assert.AreEqual(attachment.Uri, result.First().Uri);
        }

        [TestMethod]
        public void SessionEndedShouldRemoveDataCollectorIfExceptionIsThrownWhileSendingSessionEndEventArgs()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);
            MockDataCollector2.Events_SessionEndThrowException = true;

            var count = this.dataCollectionManager.RunDataCollectors.Count;

            var result = this.dataCollectionManager.SessionEnded(isCancelled: false);

            Assert.AreEqual(count - 1, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, Resource.DataCollectorRunError, MockDataCollector2.Events_SessionEndExceptionMessage), this.mockMessageSink.EventMessage);
        }

        [TestMethod]
        public void SessionEndedShouldThrowExceptionIfExceptionIsThrownWhileGettingAttachments()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);
            this.mockDataCollectionFileManager.GetDataThrowException = true;

            Assert.ThrowsException<Exception>(() =>
            {
                var result = this.dataCollectionManager.SessionEnded(isCancelled: false);
            });
        }

        [TestMethod]
        public void SessionEndedShouldReturnNullIfDataCollectorsAreNotLoaded()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);

            var result = this.dataCollectionManager.SessionEnded(isCancelled: false);

            Assert.IsFalse(this.dataCollectionManager.IsDataCollectionEnabled);
            Assert.IsNull(result);
        }

        #endregion

        #region TestCaseStarted

        [TestMethod]
        public void TestCaseStartedShouldSendTestCaseStartEventArgsToDataCollectors()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            MockDataCollector.ConfigureTestCaseLevelEvents = true;
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            var tc = new TestCase("Test", new Uri("Test://Case"), "Source");
            var testCaseStartEventArgs = new TestCaseStartEventArgs(tc);

            this.dataCollectionManager.TestCaseStarted(testCaseStartEventArgs);

            Assert.IsTrue(MockDataCollector.IsEvents_TestCaseStartInvoked);
        }

        [TestMethod]
        public void TestCaseStartedShouldNotSentEventToDataCollectorsIfDataCollectorsAreNotLoaded()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            MockDataCollector.ConfigureTestCaseLevelEvents = true;

            var tc = new TestCase("Test", new Uri("Test://Case"), "Source");
            var testCaseStartEventArgs = new TestCaseStartEventArgs(tc);

            this.dataCollectionManager.TestCaseStarted(testCaseStartEventArgs);

            Assert.IsFalse(this.dataCollectionManager.IsDataCollectionEnabled);
            Assert.IsFalse(MockDataCollector.IsEvents_TestCaseStartInvoked);
        }

        [TestMethod]
        public void TestCaseStartedShouldNotSendTestCaseStartEventArgsIfTestCaseLevelEventsAreNotRequestByDataCollectors()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            MockDataCollector.ConfigureTestCaseLevelEvents = false;
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            var tc = new TestCase("Test", new Uri("Test://Case"), "Source");
            var testCaseStartEventArgs = new TestCaseStartEventArgs(tc);

            this.dataCollectionManager.TestCaseStarted(testCaseStartEventArgs);

            Assert.IsFalse(MockDataCollector.IsEvents_TestCaseStartInvoked);
        }

        [TestMethod]
        public void TestCaseStartedShouldRemoveDataCollectorIfExceptionIsThrownWhileSendingTestCaseStartEventArgs()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            MockDataCollector.ConfigureTestCaseLevelEvents = true;
            MockDataCollector2.Events_TestCaseStartThrowException = true;
            MockDataCollector2.ConfigureTestCaseLevelEvents = true;
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            this.dataCollectionManager.LoadDataCollectors(this.runSettings);
            int count = this.dataCollectionManager.RunDataCollectors.Count;

            var tc = new TestCase("Test", new Uri("Test://Case"), "Source");
            var testCaseStartEventArgs = new TestCaseStartEventArgs(tc);

            this.dataCollectionManager.TestCaseStarted(testCaseStartEventArgs);

            Assert.AreEqual(count - 1, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, Resource.DataCollectorRunError, MockDataCollector2.Events_TestCaseStartExceptionMessage), this.mockMessageSink.EventMessage);
        }

        #endregion

        #region TestCaseEnd

        [TestMethod]
        public void TestCaseEndedShouldShouldSendSessionEndEventArgsToDataCollectors()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            MockDataCollector.ConfigureTestCaseLevelEvents = true;
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);
            var attachment = new AttachmentSet(new Uri("DataCollector://Attachment"), "DataCollectorAttachment");
            this.mockDataCollectionFileManager.Attachments.Add(attachment);

            var tc = new TestCase("Test", new Uri("Test://Case"), "Source");

            var result = this.dataCollectionManager.TestCaseEnded(tc, TestOutcome.Passed);

            Assert.IsTrue(MockDataCollector.IsEvents_TestCaseEndInvoked);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(attachment.DisplayName, result.First().DisplayName);
            Assert.AreEqual(attachment.Uri, result.First().Uri);
        }

        [TestMethod]
        public void TestCaseEndedShouldRemoveDataCollectorIfExceptionIsThrownWhileSendingTestCaseEndEventArgs()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            MockDataCollector2.Events_TestCaseEndThrowException = true;
            MockDataCollector2.ConfigureTestCaseLevelEvents = true;
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);

            int count = this.dataCollectionManager.RunDataCollectors.Count;

            var tc = new TestCase("Test", new Uri("Test://Case"), "Source");

            var result = this.dataCollectionManager.TestCaseEnded(tc, TestOutcome.Passed);

            Assert.AreEqual(count - 1, this.dataCollectionManager.RunDataCollectors.Count);
            Assert.AreEqual(string.Format(CultureInfo.CurrentCulture, Resource.DataCollectorRunError, MockDataCollector2.Events_TestCaseEndExceptionMessage), this.mockMessageSink.EventMessage);
        }

        [TestMethod]
        public void TestCaseEndedShouldThrowExceptionIfExceptionIsThrownWhileGettingAttachments()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);
            this.dataCollectionManager.LoadDataCollectors(this.runSettings);
            this.mockDataCollectionFileManager.GetDataThrowException = true;

            var tc = new TestCase("Test", new Uri("Test://Case"), "Source");
            Assert.ThrowsException<Exception>(() =>
            {
                var result = this.dataCollectionManager.TestCaseEnded(tc, TestOutcome.Passed);
            });
        }

        [TestMethod]
        public void TestCaseEndedShouldReturnNullIfDataCollectorsAreNotLoaded()
        {
            this.runSettings.InitializeSettingsProviders(this.xmlSettings);

            var tc = new TestCase("Test", new Uri("Test://Case"), "Source");
            var result = this.dataCollectionManager.TestCaseEnded(tc, TestOutcome.Passed);

            Assert.IsFalse(this.dataCollectionManager.IsDataCollectionEnabled);
            Assert.IsNull(result);
        }

        #endregion
        public static void SetupMockExtensions(string[] extensions, Action callback)
        {
            // Setup mocks.
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);
            testableTestPluginCache.DoesDirectoryExistSetter = true;

            testableTestPluginCache.FilesInDirectory = (path, pattern) =>
            {
                if (pattern.Equals("*.dll"))
                {
                    callback.Invoke();
                    return extensions;
                }

                return new string[] { };
            };

            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;
        }

        public static void ResetExtensionsCache()
        {
            TestPluginCache.Instance = null;
        }
    }

    public class TestableTestPluginCache : TestPluginCache
    {
        public TestableTestPluginCache(IPathUtilities pathUtilities)
            : base(pathUtilities)
        {
        }

        internal Func<string, string, string[]> FilesInDirectory
        {
            get;
            set;
        }

        public bool DoesDirectoryExistSetter
        {
            get;
            set;
        }

        public Func<IEnumerable<string>, TestExtensions> TestExtensionsSetter { get; set; }

        internal override bool DoesDirectoryExist(string path)
        {
            return this.DoesDirectoryExistSetter;
        }

        internal override string[] GetFilesInDirectory(string path, string searchPattern)
        {
            return this.FilesInDirectory.Invoke(path, searchPattern);
        }

        internal override TestExtensions GetTestExtensions(IEnumerable<string> extensions)
        {
            if (this.TestExtensionsSetter == null)
            {
                return base.GetTestExtensions(extensions);
            }
            else
            {
                return this.TestExtensionsSetter.Invoke(extensions);
            }
        }
    }

    internal class DummyMessageSink : IMessageSink
    {
        public bool IsSendMessageInvoked;
        public string EventMessage;

        public void Reset()
        {
            this.IsSendMessageInvoked = false;
            this.EventMessage = string.Empty;
        }

        public EventHandler<DataCollectionMessageEventArgs> OnDataCollectionMessage
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void SendMessage(DataCollectorDataMessage collectorDataMessage)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(DataCollectionMessageEventArgs args)
        {
            this.EventMessage = args.Message;
            this.IsSendMessageInvoked = true;
        }
    }
}

