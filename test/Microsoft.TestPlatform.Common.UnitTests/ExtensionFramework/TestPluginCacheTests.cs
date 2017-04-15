// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider;
    using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;
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
            this.testablePluginCache = new TestableTestPluginCache(this.mockFileHelper.Object);
            
        }

        #region Properties tests

        [TestMethod]
        public void InstanceShouldNotReturnANull()
        {
            Assert.IsNotNull(TestPluginCache.Instance);
        }

        [TestMethod]
        public void AreExtensionsDiscoveredShouldBeFalseByDefault()
        {
            Assert.IsFalse(TestPluginCache.Instance.AreDefaultExtensionsDiscovered);
        }

        [TestMethod]
        public void TestExtensionsShouldBeNullByDefault()
        {
            Assert.IsNull(TestPluginCache.Instance.TestExtensions);
        }

        [TestMethod]
        public void PathToAdditionalExtensionsShouldBeNullByDefault()
        {
            Assert.IsNull(TestPluginCache.Instance.PathToAdditionalExtensions);
        }

        [TestMethod]
        public void LoadOnlyWellKnownExtensionsShouldBeFalseByDefault()
        {
            Assert.IsFalse(TestPluginCache.Instance.LoadOnlyWellKnownExtensions);
        }

        #endregion

        #region UpdateAdditionalExtensions tests

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldUpdateLoadOnlyWellKnownExtensions()
        {
            TestPluginCache.Instance.UpdateAdditionalExtensions(null, true);
            Assert.IsTrue(TestPluginCache.Instance.LoadOnlyWellKnownExtensions);
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldNotThrowIfExtenionPathIsNull()
        {
            TestPluginCache.Instance.UpdateAdditionalExtensions(null, true);
            Assert.IsNull(TestPluginCache.Instance.PathToAdditionalExtensions);
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldNotThrowIfExtensionPathIsEmpty()
        {
            TestPluginCache.Instance.UpdateAdditionalExtensions(new List<string>(), true);
            Assert.IsNull(TestPluginCache.Instance.PathToAdditionalExtensions);
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldUpdateAdditionalExtensions()
        {
            var additionalExtensions = new List<string> { typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location };
            TestPluginCache.Instance.UpdateAdditionalExtensions(additionalExtensions, true);
            var updatedExtensions = TestPluginCache.Instance.PathToAdditionalExtensions;

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
            TestPluginCache.Instance.UpdateAdditionalExtensions(additionalExtensions, true);
            var updatedExtensions = TestPluginCache.Instance.PathToAdditionalExtensions.ToList();

            Assert.IsNotNull(updatedExtensions);
            Assert.AreEqual(1, updatedExtensions.Count);
            CollectionAssert.AreEqual(new List<string> { additionalExtensions.First() }, updatedExtensions);
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldUpdatePathsThatDoNotExist()
        {
            var additionalExtensions = new List<string> { "foo.dll" };
            TestPluginCache.Instance.UpdateAdditionalExtensions(additionalExtensions, true);
            var updatedExtensions = TestPluginCache.Instance.PathToAdditionalExtensions;

            Assert.IsNotNull(updatedExtensions);
            Assert.AreEqual(1, updatedExtensions.Count());
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldResetExtensionsDiscoveredFlag()
        {
            
        }

        #endregion

        #region GetDefaultResolutionPaths tests

        [TestMethod]
        public void GetDefaultResolutionPathsShouldReturnCurrentDirectoryByDefault()
        {
            var currentDirectory = Path.GetDirectoryName(typeof(TestPluginCache).GetTypeInfo().Assembly.Location);
            var defaultExtensionsDirectory = Path.Combine(currentDirectory, "Extensions");
            var expectedDirectories = new List<string> { currentDirectory };
            if (Directory.Exists(defaultExtensionsDirectory))
            {
                expectedDirectories.Add(defaultExtensionsDirectory);
            }

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

            TestPluginCache.Instance.UpdateAdditionalExtensions(extensionPaths, true);
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
            CollectionAssert.AreEqual(new List<string> { Path.GetDirectoryName(defaultExtensionsFile) }, resolutionPaths.ToList());
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

            TestPluginCache.Instance.GetTestExtensions(typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location);
            
            Assert.IsNotNull(TestPluginCache.Instance.TestExtensions);
            Assert.IsTrue(TestPluginCache.Instance.TestExtensions.TestDiscoverers.Count > 0);
        }

        [TestMethod]
        public void GetTestExtensionsShouldAddTestExtensionsDiscoveredToCache()
        {
            // Setup mocks.
            var testExtensions = new TestExtensions();
            testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            testExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));

            var extensionAssembly = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;

            this.testablePluginCache.TestExtensionsSetter = (IEnumerable<string> extensionAssemblies) =>
                {
                    if (extensionAssemblies.Count() == 1 && extensionAssemblies.ToArray()[0] == extensionAssembly)
                    {
                        return testExtensions;
                    }
                    return null;
                };

            this.testablePluginCache.GetTestExtensions(extensionAssembly);

            CollectionAssert.AreEqual(
                testExtensions.TestDiscoverers.Keys,
                this.testablePluginCache.TestExtensions.TestDiscoverers.Keys);
        }

        [TestMethod]
        public void GetTestExtensionsShouldGetTestExtensionsFromCache()
        {
            // Setup mocks.
            var testExtensions = new TestExtensions();
            testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            testExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));

            var extensionAssembly = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;
            var callCount = 0;

            this.testablePluginCache.TestExtensionsSetter = (IEnumerable<string> extensionAssemblies) =>
            {
                if (extensionAssemblies.Count() == 1 && extensionAssemblies.ToArray()[0] == extensionAssembly)
                {
                    callCount++;
                    return testExtensions;
                }
                return null;
            };

            this.testablePluginCache.GetTestExtensions(extensionAssembly);

            this.testablePluginCache.GetTestExtensions(extensionAssembly);

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void GetTestExtensionsShouldAddTestExtensionsToExistingCache()
        {
            // Setup mocks.
            var testExtensions = new TestExtensions[2];
            for (var i =0; i < 2; i++)
            {
                testExtensions[i] = new TestExtensions();
                testExtensions[i].TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
                testExtensions[i].TestDiscoverers.Add("td" + i, new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));
            }
            
            this.testablePluginCache.TestExtensionsSetter = (IEnumerable<string> extensionAssemblies) =>
            {
                if (extensionAssemblies.Count() == 1 && string.Equals(extensionAssemblies.ToArray()[0], "foo1.dll"))
                {
                    return testExtensions[0];
                }
                else if (extensionAssemblies.Count() == 1 && string.Equals(extensionAssemblies.ToArray()[0], "foo2.dll"))
                {
                    return testExtensions[1];
                }

                return null;
            };

            var extensions1 = this.testablePluginCache.GetTestExtensions("foo1.dll");

            var extensions2 = this.testablePluginCache.GetTestExtensions("foo2.dll");

            // Validate if the inidividual extension are returned appropriately.
            CollectionAssert.AreEqual(testExtensions[0].TestDiscoverers.Keys, extensions1.TestDiscoverers.Keys);
            CollectionAssert.AreEqual(testExtensions[1].TestDiscoverers.Keys, extensions2.TestDiscoverers.Keys);

            var expectedDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            expectedDiscoverers.Add("td0" , new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));
            expectedDiscoverers.Add("td1", new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));

            CollectionAssert.AreEqual(
                expectedDiscoverers.Keys,
                this.testablePluginCache.TestExtensions.TestDiscoverers.Keys);
        }

        [TestMethod]
        public void GetTestExtensionsShouldShouldThrowIfDiscovererThrows()
        {
            // Setup mocks.
            var testExtensions = new TestExtensions();
            testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            testExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));

            var extensionAssembly = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;
            
            this.testablePluginCache.TestExtensionsSetter = (IEnumerable<string> extensionAssemblies) =>
                { throw new ArgumentException(); };

            Assert.ThrowsException<ArgumentException>(() => this.testablePluginCache.GetTestExtensions(extensionAssembly));
        }

        #endregion

        #region DiscoverAllTestExtensions tests

        [TestMethod]
        public void DiscoverAllTestExtensionsShouldSetPropertyAreDefaultExtensionsDiscoveredToTrue()
        {
            // Setup the testable instance.
            TestPluginCache.Instance = this.testablePluginCache;
            TestPluginCache.Instance.DefaultExtensionPaths = new List<string>() { typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location };

            Assert.IsFalse(TestPluginCache.Instance.AreDefaultExtensionsDiscovered);

            TestPluginCache.Instance.DiscoverAllTestExtensions();

            Assert.IsTrue(TestPluginCache.Instance.AreDefaultExtensionsDiscovered);
        }

        [TestMethod]
        public void DiscoverAllTestExtensionsShouldDiscoverExtensionsFromDefaultExtensionsFolder()
        {
            // Setup the testable instance.
            TestPluginCache.Instance = this.testablePluginCache;
            TestPluginCache.Instance.DefaultExtensionPaths = new List<string>() { typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location };

            TestPluginCache.Instance.DiscoverAllTestExtensions();

            Assert.IsTrue(TestPluginCache.Instance.AreDefaultExtensionsDiscovered);
            Assert.IsNotNull(TestPluginCache.Instance.TestExtensions);
            
            // Validate the discoverers to be absolutely certain.
            Assert.IsTrue(TestPluginCache.Instance.TestExtensions.TestDiscoverers.Count > 0);
        }

        [TestMethod]
        public void DiscoverAllTestExtensionsShouldDiscoverExtensionsFromAdditionalExtensionsFolder()
        {
            SetupMockAdditionalPathExtensions();

            TestPluginCache.Instance.DiscoverAllTestExtensions();

            Assert.IsTrue(TestPluginCache.Instance.AreDefaultExtensionsDiscovered);
            Assert.IsNotNull(TestPluginCache.Instance.TestExtensions);

            // Validate the discoverers to be absolutely certain.
            Assert.IsTrue(TestPluginCache.Instance.TestExtensions.TestDiscoverers.Count > 0);
        }

        [TestMethod]
        public void DiscoverAllTestExtensionsShouldAddToTheExtensionsAlreadyDiscovered()
        {
            // Setup mocks.
            var testableTestPluginCache = SetupMockAdditionalPathExtensions();

            var testExtensions = new TestExtensions();
            testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            testExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));

            var extensionAssembly = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;

            testableTestPluginCache.TestExtensionsSetter = (IEnumerable<string> extensionAssemblies) =>
            {
                if (extensionAssemblies.Count() == 1 && extensionAssemblies.ToArray()[0] == "foo.dll")
                {
                    return testExtensions;
                }
                else
                {
                    var discoverer = new TestPluginDiscoverer();
                    return discoverer.GetTestExtensionsInformation(extensionAssemblies, loadOnlyWellKnownExtensions: false);
                }
            };

            testableTestPluginCache.GetTestExtensions("foo.dll");

            testableTestPluginCache.DiscoverAllTestExtensions();

            CollectionAssert.Contains(testableTestPluginCache.TestExtensions.TestDiscoverers.Keys, "td");
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

            TestPluginCache.Instance.UpdateAdditionalExtensions(extensions, true);

            return testPluginCache;
        }

        public static void SetupMockExtensions()
        {
            SetupMockExtensions(() => { });
        }

        public static void SetupMockExtensions(Action callback)
        {
            SetupMockExtensions(new[] { typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location }, callback);
        }

        public static void SetupMockExtensions(string[] extensions, Action callback)
        {
            // Setup mocks.
            var mockFileHelper = new Mock<IFileHelper>();
            var testableTestPluginCache = new TestableTestPluginCache(mockFileHelper.Object);

            testableTestPluginCache.DefaultExtensionPaths = extensions;

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
        public TestableTestPluginCache(IFileHelper fileHelper) : base(fileHelper)
        {
        }
            
        public Func<IEnumerable<string>, TestExtensions> TestExtensionsSetter { get; set; }

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

    #endregion 
}
