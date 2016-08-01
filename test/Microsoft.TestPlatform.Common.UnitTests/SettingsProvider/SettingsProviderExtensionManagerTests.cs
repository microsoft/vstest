// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.SettingsProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class SettingsProviderExtensionManagerTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            SettingsProviderExtensionManager.Destroy();
        }

        #region Constructor tests

        [TestMethod]
        public void ConstructorShouldPopulateSettingsProviderMap()
        {
            var extensions = this.GetMockExtensions("TestableSettings");
            var unfilteredExtensions = new List<LazyExtension<ISettingsProvider, Dictionary<string, object>>>
                                           {
                                               new LazyExtension<ISettingsProvider,Dictionary<string,object>>
                                                   (
                                                   new Mock<ISettingsProvider>().Object,
                                                   new Dictionary<string,object>())
                                           };
            var spm = new TestableSettingsProviderManager(extensions, unfilteredExtensions, new Mock<IMessageLogger>().Object);

            Assert.IsNotNull(spm.SettingsProvidersMap);
            Assert.AreEqual("TestableSettings", spm.SettingsProvidersMap.Keys.FirstOrDefault());
        }

        [TestMethod]
        public void ConstructorShouldLogWarningOnDuplicateSettingsProviderNames()
        {
            var extensions = this.GetMockExtensions("TestableSettings", "TestableSettings");
            var unfilteredExtensions = new List<LazyExtension<ISettingsProvider, Dictionary<string, object>>>
                                           {
                                               new LazyExtension<ISettingsProvider,Dictionary<string,object>>
                                                   (
                                                   new Mock<ISettingsProvider>().Object,
                                                   new Dictionary<string,object>())
                                           };
            var mockLogger = new Mock<IMessageLogger>();
            var spm = new TestableSettingsProviderManager(extensions, unfilteredExtensions, mockLogger.Object);

            mockLogger.Verify(
                l =>
                l.SendMessage(
                    TestMessageLevel.Error,
                    "Duplicate settings provider named 'TestableSettings'.  Ignoring the duplicate provider."));

            // Also validate the below.
            Assert.IsNotNull(spm.SettingsProvidersMap);
            Assert.AreEqual("TestableSettings", spm.SettingsProvidersMap.Keys.FirstOrDefault());
        }

        #endregion

        #region Create tests

        [TestMethod]
        public void CreateShouldDiscoverSettingsProviderExtensions()
        {
            TestPluginCacheTests.SetupMockExtensions();

            var extensionManager = SettingsProviderExtensionManager.Create();

            Assert.IsNotNull(extensionManager.SettingsProvidersMap);
            Assert.IsTrue(extensionManager.SettingsProvidersMap.Count > 0);
        }

        [TestMethod]
        public void CreateShouldCacheDiscoveredExtensions()
        {
            var discoveryCount = 0;
            TestPluginCacheTests.SetupMockExtensions(() => { discoveryCount++; });

            var extensionManager = SettingsProviderExtensionManager.Create();
            SettingsProviderExtensionManager.Create();

            Assert.IsNotNull(extensionManager.SettingsProvidersMap);
            Assert.IsTrue(extensionManager.SettingsProvidersMap.Count > 0);
            Assert.AreEqual(1, discoveryCount);
        }
        
        #endregion

        #region LoadAndInitialize tests

        [TestMethod]
        public void LoadAndInitializeShouldInitializeAllExtensions()
        {
            TestPluginCacheTests.SetupMockExtensions();

            SettingsProviderExtensionManager.LoadAndInitializeAllExtensions(false);

            var settingsProviders = SettingsProviderExtensionManager.Create().SettingsProvidersMap.Values;

            foreach (var provider in settingsProviders)
            {
                Assert.IsTrue(provider.IsExtensionCreated);
            }
        }

        #endregion

        #region GetSettingsProvider tests

        [TestMethod]
        public void GetSettingsProviderShouldThrowIfSettingsNameIsNullOrEmpty()
        {
            var extensions = this.GetMockExtensions("TestableSettings");
            var unfilteredExtensions = new List<LazyExtension<ISettingsProvider, Dictionary<string, object>>>
                                           {
                                               new LazyExtension<ISettingsProvider,Dictionary<string,object>>
                                                   (
                                                   new Mock<ISettingsProvider>().Object,
                                                   new Dictionary<string,object>())
                                           };
            var spm = new TestableSettingsProviderManager(extensions, unfilteredExtensions, new Mock<IMessageLogger>().Object);

            Assert.ThrowsException<ArgumentException>(() => spm.GetSettingsProvider(null));
            Assert.ThrowsException<ArgumentException>(() => spm.GetSettingsProvider(string.Empty));
        }

        [TestMethod]
        public void GetSettingsProviderShouldReturnNullIfSettingsProviderWithSpecifiedNameIsNotFound()
        {
            var extensions = this.GetMockExtensions("TestableSettings");
            var unfilteredExtensions = new List<LazyExtension<ISettingsProvider, Dictionary<string, object>>>
                                           {
                                               new LazyExtension<ISettingsProvider,Dictionary<string,object>>
                                                   (
                                                   new Mock<ISettingsProvider>().Object,
                                                   new Dictionary<string,object>())
                                           };
            var spm = new TestableSettingsProviderManager(extensions, unfilteredExtensions, new Mock<IMessageLogger>().Object);

            var sp = spm.GetSettingsProvider("RandomSettingsWhichDoesNotExist");

            Assert.IsNull(sp);
        }

        [TestMethod]
        public void GetSettingsProviderShouldReturnSettingsProviderInstance()
        {
            var extensions = this.GetMockExtensions("TestableSettings");
            var unfilteredExtensions = new List<LazyExtension<ISettingsProvider, Dictionary<string, object>>>
                                           {
                                               new LazyExtension<ISettingsProvider,Dictionary<string,object>>
                                                   (
                                                   new Mock<ISettingsProvider>().Object,
                                                   new Dictionary<string,object>())
                                           };
            var spm = new TestableSettingsProviderManager(extensions, unfilteredExtensions, new Mock<IMessageLogger>().Object);

            var sp = spm.GetSettingsProvider("TestableSettings");

            Assert.IsNotNull(sp);
            Assert.IsNotNull(sp.Value);
        }

        #endregion

        #region private methods

        private IEnumerable<LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>> GetMockExtensions(params string[] settingNames)
        {
            var settingsList = new List<LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>>();

            foreach (var settingName in settingNames)
            {
                var mockSettingsProvider = new Mock<ISettingsProvider>();
                var metadata = new TestSettingsProviderMetadata(settingName);

                var extension =
                    new LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>(
                        mockSettingsProvider.Object,
                        metadata);

                settingsList.Add(extension);
            }

            return  settingsList;
        }

        #endregion

        #region Testable Implementations

        private class TestableSettingsProviderManager : SettingsProviderExtensionManager
        {
            public TestableSettingsProviderManager(
                IEnumerable<LazyExtension<ISettingsProvider, ISettingsProviderCapabilities>> settingsProviders,
                IEnumerable<LazyExtension<ISettingsProvider, Dictionary<string, object>>> unfilteredSettingsProviders,
                IMessageLogger logger)
                : base(settingsProviders, unfilteredSettingsProviders, logger)
            {
            }
        }

        [SettingsName("Random")]
        private class RandomSettingsProvider : ISettingsProvider
        {
            public void Load(XmlReader reader)
            {
            }
        }

        #endregion
    }

    [TestClass]
    public class TestSettingsProviderMetadataTests
    {
        [TestMethod]
        public void ConstructorShouldSetSettingsName()
        {
            var metadata = new TestSettingsProviderMetadata("sample");
            Assert.AreEqual("sample", metadata.SettingsName);
        }
    }
}
