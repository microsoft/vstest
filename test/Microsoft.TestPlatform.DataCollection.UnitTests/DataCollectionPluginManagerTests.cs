// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.TestPlatform.DataCollection.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.Execution;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using IMessageSink = Microsoft.VisualStudio.TestPlatform.DataCollection.Interfaces.IMessageSink;

    [TestClass]
    public class DataCollectionManagerTests
    {
        private DataCollectionManager dataCollectionManager;
        private DummyMessageSink mockMessageSink;
        private RunSettings runSettings;
        private string xmlSettings =
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<RunSettings>\r\n  <RunConfiguration>\r\n    <MaxCpuCount>1</MaxCpuCount>\r\n    <ResultsDirectory>.\\TestResults</ResultsDirectory>\r\n    <TargetPlatform>x86</TargetPlatform>\r\n  </RunConfiguration>\r\n  <DataCollectionRunSettings>\r\n    <DataCollectors>\r\n      <DataCollector friendlyName=\"Custom DataCollector\" uri=\"datacollector://Company/Product/Version\" assemblyQualifiedName=\"Microsoft.TestPlatform.DataCollection.UnitTests.MockDataCollector, Microsoft.TestPlatform.DataCollection.UnitTests, Version=15.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a\">\r\n      </DataCollector>\r\n    </DataCollectors>\r\n  </DataCollectionRunSettings>\r\n</RunSettings>";

        [TestInitialize]
        public void Init()
        {
            SetupMockExtensions(new string[] { typeof(DataCollectorsSettingsProvider).GetTypeInfo().Assembly.Location }, () => { });

            this.mockMessageSink = new DummyMessageSink();
            this.dataCollectionManager = new DataCollectionManager(this.mockMessageSink);
            this.runSettings = new RunSettings();
            this.runSettings.LoadSettingsXml(this.xmlSettings);
        }

        [TestCleanup]
        public void Cleanup()
        {
            MockDataCollector.EnvVarList = null;
            MockDataCollector.ThrowExceptionWhenInitialized = false;
            ResetExtensionsCache();
        }

        [TestMethod]
        public void LoadDataCollectorShouldLoadDataCollectorAndReturnEnvironmentVariables()
        {
            var envVarList = new List<KeyValuePair<string, string>>();
            envVarList.Add(new KeyValuePair<string, string>("key", "value"));
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

    [DataCollectorTypeUri("datacollector://Company/Product/Version")]
    [DataCollectorFriendlyName("Collect Log Files", false)]
    public class MockDataCollector : DataCollector, ITestExecutionEnvironmentSpecifier
    {
        public static bool IsInitializeInvoked;
        public static bool ThrowExceptionWhenInitialized;
        public static bool IsDisposeInvoked;
        public static bool IsGetTestExecutionEnvironmentVariablesInvoked;
        public static bool GetTestExecutionEnvironmentVariablesThrowException;
        public static bool DisposeShouldThrowException;

        public static IEnumerable<KeyValuePair<string, string>> EnvVarList;


        public override void Initialize(XmlElement configurationElement, DataCollectionEvents events, DataCollectionSink dataSink, DataCollectionLogger logger, DataCollectionEnvironmentContext environmentContext)
        {
            if (ThrowExceptionWhenInitialized)
            {
                throw new Exception("DataCollectorException");
            }
            IsInitializeInvoked = true;
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposeInvoked = true;
            if (DisposeShouldThrowException)
            {
                throw new Exception("DataCollectorException");
            }
        }

        public IEnumerable<KeyValuePair<string, string>> GetTestExecutionEnvironmentVariables()
        {
            if (GetTestExecutionEnvironmentVariablesThrowException)
            {
                throw new Exception("DataCollectorException");
            }

            IsGetTestExecutionEnvironmentVariablesInvoked = true;
            return EnvVarList;
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
            this.IsSendMessageInvoked = true;
        }
    }
}

