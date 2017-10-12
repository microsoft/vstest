// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestPluginCacheTests
    {
        private readonly Mock<IFileHelper> mockFileHelper;

        private readonly TestableTestPluginCache testablePluginCache;

        public TestPluginCacheTests()
        {
            // Reset the singleton.
            TestPluginCache.Instance = null;
            this.mockFileHelper = new Mock<IFileHelper>();
            this.testablePluginCache = new TestableTestPluginCache(
                this.mockFileHelper.Object,
                new List<string>() { typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location });

            this.mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);
        }

        #region Properties tests

        [TestMethod]
        public void InstanceShouldNotReturnANull()
        {
            Assert.IsNotNull(TestPluginCache.Instance);
        }

        [TestMethod]
        public void TestExtensionsShouldBeNullByDefault()
        {
            Assert.IsNull(TestPluginCache.Instance.TestExtensions);
        }

        [TestMethod]
        public void PathToAdditionalExtensionsShouldBeNullByDefault()
        {
            Assert.IsNull(TestPluginCache.Instance.PathToExtensions);
        }

        #endregion

        #region UpdateAdditionalExtensions tests

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldNotThrowIfExtenionPathIsNull()
        {
            TestPluginCache.Instance.UpdateExtensions(null, true);
            Assert.IsNull(TestPluginCache.Instance.PathToExtensions);
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldNotThrowIfExtensionPathIsEmpty()
        {
            TestPluginCache.Instance.UpdateExtensions(new List<string>(), true);
            Assert.IsNull(TestPluginCache.Instance.PathToExtensions);
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldUpdateAdditionalExtensions()
        {
            var additionalExtensions = new List<string> { typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location };
            TestPluginCache.Instance.UpdateExtensions(additionalExtensions, false);
            var updatedExtensions = TestPluginCache.Instance.PathToExtensions;

            Assert.IsNotNull(updatedExtensions);
            CollectionAssert.AreEqual(additionalExtensions, updatedExtensions.ToList());
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldOnlyAddUniqueExtensionPaths()
        {
            var additionalExtensions = new List<string>
                                           {
                                               typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location,
                                               typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location
                                           };
            TestPluginCache.Instance.UpdateExtensions(additionalExtensions, false);
            var updatedExtensions = TestPluginCache.Instance.PathToExtensions.ToList();

            Assert.IsNotNull(updatedExtensions);
            Assert.AreEqual(1, updatedExtensions.Count);
            CollectionAssert.AreEqual(new List<string> { additionalExtensions.First() }, updatedExtensions);
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldUpdatePathsThatDoNotExist()
        {
            var additionalExtensions = new List<string> { "foo.dll" };
            TestPluginCache.Instance.UpdateExtensions(additionalExtensions, false);
            var updatedExtensions = TestPluginCache.Instance.PathToExtensions;

            Assert.IsNotNull(updatedExtensions);
            Assert.AreEqual(1, updatedExtensions.Count());
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldUpdateDefaultExtensionsWhenSkipFilteringIsTrue()
        {
            var additionalExtensions = new List<string> { "foo.dll" };
            TestPluginCache.Instance.UpdateExtensions(additionalExtensions, true);
            var updatedExtensions = TestPluginCache.Instance.DefaultExtensionPaths;

            Assert.IsNotNull(updatedExtensions);
            Assert.AreEqual(1, updatedExtensions.Count());
        }

        [Ignore]
        [TestMethod]
        public void UpdateAdditionalExtensionsShouldResetExtensionsDiscoveredFlag()
        {
        }

        #endregion

        #region ClearExtensions

        [TestMethod]
        public void ClearExtensionsShouldClearPathToExtensions()
        {
            TestPluginCache.Instance.UpdateExtensions(new List<string> { @"oldExtension.dll" }, false);

            TestPluginCache.Instance.ClearExtensions();

            Assert.AreEqual(0, TestPluginCache.Instance.PathToExtensions.Count());
        }

        #endregion

        #region GetDefaultResolutionPaths tests

        [TestMethod]
        public void GetDefaultResolutionPathsShouldReturnCurrentDirectoryByDefault()
        {
            var currentDirectory = Path.GetDirectoryName(typeof(TestPluginCache).GetTypeInfo().Assembly.Location);
            var expectedDirectories = new List<string> { currentDirectory };

            var resolutionPaths = TestPluginCache.Instance.GetDefaultResolutionPaths();

            Assert.IsNotNull(resolutionPaths);
            CollectionAssert.AreEqual(expectedDirectories, resolutionPaths.ToList());
        }

        [TestMethod]
        public void GetDefaultResolutionPathsShouldReturnAdditionalExtensionPathsDirectories()
        {
            var currentDirectory = Path.GetDirectoryName(typeof(TestPluginCache).GetTypeInfo().Assembly.Location);
            var candidateDirectory = Directory.GetParent(currentDirectory).FullName;
            var extensionPaths = new List<string> { Path.Combine(candidateDirectory, "foo.dll") };

            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(false);
            var testableTestPluginCache = new TestableTestPluginCache(mockFileHelper.Object);

            TestPluginCache.Instance = testableTestPluginCache;

            TestPluginCache.Instance.UpdateExtensions(extensionPaths, true);
            var resolutionPaths = TestPluginCache.Instance.GetDefaultResolutionPaths();

            var expectedExtensions = new List<string> { candidateDirectory, currentDirectory };

            Assert.IsNotNull(resolutionPaths);
            CollectionAssert.AreEqual(expectedExtensions, resolutionPaths.ToList());
        }

        [TestMethod]
        public void GetDefaultResolutionPathsShouldReturnDirectoryFromDefaultExtensionsPath()
        {
            // Setup the testable instance.
            TestPluginCache.Instance = this.testablePluginCache;

            var defaultExtensionsFile = typeof(TestPluginCache).GetTypeInfo().Assembly.Location;
            this.testablePluginCache.DefaultExtensionPaths = new List<string>() { defaultExtensionsFile };

            var resolutionPaths = TestPluginCache.Instance.GetDefaultResolutionPaths();

            Assert.IsNotNull(resolutionPaths);
            Assert.IsTrue(resolutionPaths.Contains(Path.GetDirectoryName(defaultExtensionsFile)));
        }

        #endregion

        #region GetResolutionPaths tests

        [TestMethod]
        public void GetResolutionPathsShouldThrowIfExtensionAssemblyIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => TestPluginCache.Instance.GetResolutionPaths(null));
        }

        [TestMethod]
        public void GetResolutionPathsShouldReturnExtensionAssemblyDirectoryAndTPCommonDirectory()
        {
            var resolutionPaths = TestPluginCache.Instance.GetResolutionPaths(@"C:\temp\Idonotexist.dll");

            var tpCommonDirectory = Path.GetDirectoryName(typeof(TestPluginCache).GetTypeInfo().Assembly.Location);
            var expectedPaths = new List<string> { "C:\\temp", tpCommonDirectory };

            CollectionAssert.AreEqual(expectedPaths, resolutionPaths.ToList());
        }

        [TestMethod]
        public void GetResolutionPathsShouldNotHaveDuplicatePathsIfExtensionIsInSameDirectory()
        {
            var tpCommonlocation = typeof(TestPluginCache).GetTypeInfo().Assembly.Location;

            var resolutionPaths = TestPluginCache.Instance.GetResolutionPaths(tpCommonlocation);

            var expectedPaths = new List<string> { Path.GetDirectoryName(tpCommonlocation) };

            CollectionAssert.AreEqual(expectedPaths, resolutionPaths.ToList());
        }

        #endregion

        #region GetTestExtensions tests

        [TestMethod]
        public void GetTestExtensionsShouldReturnExtensionsInAssembly()
        {
            SetupMockAdditionalPathExtensions();

            TestPluginCache.Instance.GetTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer>(typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location);

            Assert.IsNotNull(TestPluginCache.Instance.TestExtensions);
            Assert.IsTrue(TestPluginCache.Instance.TestExtensions.TestDiscoverers.Count > 0);
        }

        [TestMethod]
        public void GetTestExtensionsShouldAddTestExtensionsDiscoveredToCache()
        {
            var extensionAssembly = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;

            var testDiscovererPluginInfos = this.testablePluginCache.GetTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer>(extensionAssembly);

            CollectionAssert.AreEqual(
                this.testablePluginCache.TestExtensions.TestDiscoverers.Keys,
                testDiscovererPluginInfos.Keys);
        }

        [TestMethod]
        public void GetTestExtensionsShouldGetTestExtensionsFromCache()
        {
            var extensionAssembly = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;
            var testDiscovererPluginInfos = this.testablePluginCache.GetTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer>(extensionAssembly);
            Assert.IsFalse(testDiscovererPluginInfos.ContainsKey("td"));

            // Set the cache.
            this.testablePluginCache.TestExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));

            testDiscovererPluginInfos = this.testablePluginCache.GetTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer>(extensionAssembly);
            Assert.IsTrue(testDiscovererPluginInfos.ContainsKey("td"));
        }

        [Ignore]
        [TestMethod]
        public void GetTestExtensionsShouldShouldThrowIfDiscovererThrows()
        {
            //todo : make ITestDiscoverer interface and then mock it in order to make this test case pass.

            var extensionAssembly = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;
            Assert.ThrowsException<Exception>(() => this.testablePluginCache.GetTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer>(extensionAssembly));
        }

        #endregion

        #region DiscoverTestExtensions tests

        [TestMethod]
        public void DiscoverTestExtensionsShouldDiscoverExtensionsFromExtensionsFolder()
        {
            SetupMockAdditionalPathExtensions();

            TestPluginCache.Instance.DiscoverTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer>(TestPlatformConstants.TestAdapterEndsWithPattern);

            Assert.IsNotNull(TestPluginCache.Instance.TestExtensions);

            // Validate the discoverers to be absolutely certain.
            Assert.IsTrue(TestPluginCache.Instance.TestExtensions.TestDiscoverers.Count > 0);
        }

        [TestMethod]
        public void DiscoverTestExtensionsShouldSetCachedBoolToTrue()
        {
            SetupMockAdditionalPathExtensions();

            TestPluginCache.Instance.DiscoverTestExtensions<TestDiscovererPluginInformation, ITestDiscoverer>(TestPlatformConstants.TestAdapterEndsWithPattern);

            Assert.IsTrue(TestPluginCache.Instance.TestExtensions.AreTestDiscoverersCached);
            Assert.IsTrue(TestPluginCache.Instance.TestExtensions.AreTestExtensionsCached<TestDiscovererPluginInformation>());
        }

        #endregion

        #region Setup mocks

        public static TestableTestPluginCache SetupMockAdditionalPathExtensions()
        {
            return SetupMockAdditionalPathExtensions(
                new string[] { typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location });
        }

        public static TestableTestPluginCache SetupMockAdditionalPathExtensions(string[] extensions)
        {
            var mockFileHelper = new Mock<IFileHelper>();
            var testPluginCache = new TestableTestPluginCache(mockFileHelper.Object);

            TestPluginCache.Instance = testPluginCache;

            // Stub the default extensions folder.
            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(false);

            TestPluginCache.Instance.UpdateExtensions(extensions, true);

            return testPluginCache;
        }

        public static void SetupMockExtensions(Mock<IFileHelper> mockFileHelper = null)
        {
            SetupMockExtensions(() => { }, mockFileHelper);
        }

        public static void SetupMockExtensions(Action callback, Mock<IFileHelper> mockFileHelper = null)
        {
            SetupMockExtensions(new[] { typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location }, callback, mockFileHelper);
        }

        public static void SetupMockExtensions(string[] extensions, Action callback, Mock<IFileHelper> mockFileHelper = null)
        {
            // Setup mocks.
            if (mockFileHelper == null)
            {
                mockFileHelper = new Mock<IFileHelper>();
            }

            mockFileHelper.Setup(fh => fh.DirectoryExists(It.IsAny<string>())).Returns(true);

            var testableTestPluginCache = new TestableTestPluginCache(mockFileHelper.Object, extensions.ToList());
            testableTestPluginCache.Action = callback;

            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;
        }

        public static void ResetExtensionsCache()
        {
            TestPluginCache.Instance = null;
            SettingsProviderExtensionManager.Destroy();
        }

        #endregion
    }

    #region Testable implementation

    public class TestableTestPluginCache : TestPluginCache
    {
        public Action Action;
        public TestableTestPluginCache(IFileHelper fileHelper, List<string> extensionsPath) : base(fileHelper)
        {
            TestDiscoveryExtensionManager.Destroy();
            TestExecutorExtensionManager.Destroy();
            SettingsProviderExtensionManager.Destroy();
            this.UpdateExtensions(extensionsPath, skipExtensionFilters: false);
        }

        public TestableTestPluginCache(IFileHelper fileHelper) : this(fileHelper, new List<string>())
        {
        }

        internal override List<string> GetFilteredExtensions(List<string> extensions, string searchPattern)
        {
            this.Action?.Invoke();
            return extensions;
        }
    }

    #endregion 
}

