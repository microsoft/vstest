// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.ExtensionFramework
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.Common.SettingsProvider;
    [TestClass]
    public class TestPluginCacheTests
    {
        [TestInitialize]
        public void TestInit()
        {
            // Reset the singleton.
            TestPluginCache.Instance = null;
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
        public void UpdateAdditionalExtensionsShouldNotThrowIfExtenionPathIsEmpty()
        {
            TestPluginCache.Instance.UpdateAdditionalExtensions(new List<string> {}, true);
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
            var updatedExtensions = TestPluginCache.Instance.PathToAdditionalExtensions;

            Assert.IsNotNull(updatedExtensions);
            Assert.AreEqual(1, updatedExtensions.Count());
            CollectionAssert.AreEqual(new List<string> { additionalExtensions.First() }, updatedExtensions.ToList());
        }

        [TestMethod]
        public void UpdateAdditionalExtensionsShouldNotUpdateInvalidPaths()
        {
            var additionalExtensions = new List<string> { "foo.dll" };
            TestPluginCache.Instance.UpdateAdditionalExtensions(additionalExtensions, true);
            var updatedExtensions = TestPluginCache.Instance.PathToAdditionalExtensions;

            Assert.IsNotNull(updatedExtensions);
            Assert.AreEqual(0, updatedExtensions.Count());
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
            var resolutionPaths = TestPluginCache.Instance.GetDefaultResolutionPaths();

            var currentDirectory = Path.GetDirectoryName(typeof(TestPluginCache).GetTypeInfo().Assembly.Location);

            Assert.IsNotNull(resolutionPaths);
            CollectionAssert.AreEqual(new List<string> { currentDirectory }, resolutionPaths.ToList());
        }
        
        [TestMethod]
        public void GetDefaultResolutionPathsShouldReturnAdditionalExtensionPathsDirectories()
        {
            var currentDirectory = Path.GetDirectoryName(typeof(TestPluginCache).GetTypeInfo().Assembly.Location);
            var candidateDirectory = Directory.GetParent(currentDirectory).FullName;
            var extensionPaths = new List<string> { Path.Combine(candidateDirectory, "foo.dll") };
            
            // Setup mocks.
            SetupMockPathUtilities();

            TestPluginCache.Instance.UpdateAdditionalExtensions(extensionPaths, true);
            var resolutionPaths = TestPluginCache.Instance.GetDefaultResolutionPaths();
            
            var expectedExtensions = new List<string> { candidateDirectory, currentDirectory };

            Assert.IsNotNull(resolutionPaths);
            CollectionAssert.AreEqual(expectedExtensions, resolutionPaths.ToList());
        }

        [TestMethod]
        public void GetDefaultResolutionPathsShouldReturnDefaultExtensionsDirectoryIfPresent()
        {
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);
            testableTestPluginCache.DoesDirectoryExistSetter = true;
            
            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;

            var resolutionPaths = TestPluginCache.Instance.GetDefaultResolutionPaths();

            var currentDirectory = Path.GetDirectoryName(typeof(TestPluginCache).GetTypeInfo().Assembly.Location);
            var defaultExtensionsDirectory = Path.Combine(currentDirectory, "Extensions");

            Assert.IsNotNull(resolutionPaths);
            CollectionAssert.AreEqual(new List<string> { currentDirectory, defaultExtensionsDirectory }, resolutionPaths.ToList());
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

        #region DefaultExtensions tests

        [TestMethod]
        public void DefaultExtensionsShouldReturnExtensionsFolder()
        {
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);
            testableTestPluginCache.DoesDirectoryExistSetter = true;
            testableTestPluginCache.FilesInDirectory = (path, pattern) => { return new string[] { path }; };

            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;

            var extensionsFolder =
                Path.Combine(
                    Path.GetDirectoryName(typeof(TestPluginCache).GetTypeInfo().Assembly.Location),
                    "Extensions");

            Assert.IsNotNull(TestPluginCache.Instance.DefaultExtensionPaths);
            CollectionAssert.Contains(TestPluginCache.Instance.DefaultExtensionPaths.ToList(), extensionsFolder);
        }

        [TestMethod]
        public void DefaultExtensionsShouldReturnEmptyIfExtensionsDirectoryDoesNotExist()
        {
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);
            testableTestPluginCache.DoesDirectoryExistSetter = false;

            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;

            Assert.IsNotNull(TestPluginCache.Instance.DefaultExtensionPaths);
            Assert.AreEqual(0, TestPluginCache.Instance.DefaultExtensionPaths.Count());
        }

        [TestMethod]
        public void DefaultExtensionsShouldReturnAllSupportedFilesUnderExtensionsFolder()
        {
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);
            testableTestPluginCache.DoesDirectoryExistSetter = true;

            var dllFiles = new string[] { "t1.dll" };
            var exeFiles = new string[] { "t1.exe", "t2.exe" };
            var jsFiles = new string[] { "t1.js" };

            testableTestPluginCache.FilesInDirectory = (path, pattern) =>
                {
                    if (pattern.Equals("*.dll"))
                    {
                        return dllFiles;
                    }
                    else if (pattern.Equals("*.exe"))
                    {
                        return exeFiles;
                    }

                    return jsFiles;
                };

            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;

            var expectedExtensions = new List<string>(dllFiles);
            expectedExtensions.AddRange(exeFiles);

            Assert.IsNotNull(TestPluginCache.Instance.DefaultExtensionPaths);
            Assert.AreEqual(3, TestPluginCache.Instance.DefaultExtensionPaths.Count());
            CollectionAssert.AreEqual(expectedExtensions, TestPluginCache.Instance.DefaultExtensionPaths.ToList());
        }

        [TestMethod]
        public void DefaultExtensionsShouldReturnAssemblyExtensionsIfFolderContainsDllsOnly()
        {
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);
            testableTestPluginCache.DoesDirectoryExistSetter = true;

            var dllFiles = new string[] { "t1.dll", "t2.dll" };

            testableTestPluginCache.FilesInDirectory = (path, pattern) =>
            {
                if (pattern.Equals("*.dll"))
                {
                    return dllFiles;
                }

                return new string[] { };
            };

            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;
            
            Assert.IsNotNull(TestPluginCache.Instance.DefaultExtensionPaths);
            Assert.AreEqual(2, TestPluginCache.Instance.DefaultExtensionPaths.Count());
            CollectionAssert.AreEqual(dllFiles, TestPluginCache.Instance.DefaultExtensionPaths.ToList());
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
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);
            
            // Setup mocks.
            var testExtensions = new TestExtensions();
            testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            testExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));

            var extensionAssembly = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;

            testableTestPluginCache.TestExtensionsSetter = (IEnumerable<string> extensionAssemblies) =>
                {
                    if (extensionAssemblies.Count() == 1 && extensionAssemblies.ToArray()[0] == extensionAssembly)
                    {
                        return testExtensions;
                    }
                    return null;
                };

            testableTestPluginCache.GetTestExtensions(extensionAssembly);

            CollectionAssert.AreEqual(
                testExtensions.TestDiscoverers.Keys,
                testableTestPluginCache.TestExtensions.TestDiscoverers.Keys);
        }

        [TestMethod]
        public void GetTestExtensionsShouldGetTestExtensionsFromCache()
        {
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);

            // Setup mocks.
            var testExtensions = new TestExtensions();
            testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            testExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));

            var extensionAssembly = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;
            var callCount = 0;

            testableTestPluginCache.TestExtensionsSetter = (IEnumerable<string> extensionAssemblies) =>
            {
                if (extensionAssemblies.Count() == 1 && extensionAssemblies.ToArray()[0] == extensionAssembly)
                {
                    callCount++;
                    return testExtensions;
                }
                return null;
            };

            testableTestPluginCache.GetTestExtensions(extensionAssembly);

            testableTestPluginCache.GetTestExtensions(extensionAssembly);

            Assert.AreEqual(1, callCount);
        }

        [TestMethod]
        public void GetTestExtensionsShouldAddTestExtensionsToExistingCache()
        {
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);

            // Setup mocks.
            var testExtensions = new TestExtensions[2];
            for (var i =0; i < 2; i++)
            {
                testExtensions[i] = new TestExtensions();
                testExtensions[i].TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
                testExtensions[i].TestDiscoverers.Add("td" + i, new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));
            }
            
            testableTestPluginCache.TestExtensionsSetter = (IEnumerable<string> extensionAssemblies) =>
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

            var extensions1 = testableTestPluginCache.GetTestExtensions("foo1.dll");

            var extensions2 = testableTestPluginCache.GetTestExtensions("foo2.dll");

            // Validate if the inidividual extension are returned appropriately.
            CollectionAssert.AreEqual(testExtensions[0].TestDiscoverers.Keys, extensions1.TestDiscoverers.Keys);
            CollectionAssert.AreEqual(testExtensions[1].TestDiscoverers.Keys, extensions2.TestDiscoverers.Keys);

            var expectedDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            expectedDiscoverers.Add("td0" , new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));
            expectedDiscoverers.Add("td1", new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));

            CollectionAssert.AreEqual(
                expectedDiscoverers.Keys,
                testableTestPluginCache.TestExtensions.TestDiscoverers.Keys);
        }

        [TestMethod]
        public void GetTestExtensionsShouldShouldThrowIfDiscovererThrows()
        {
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);

            // Setup mocks.
            var testExtensions = new TestExtensions();
            testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            testExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestPluginCacheTests)));

            var extensionAssembly = typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location;
            
            testableTestPluginCache.TestExtensionsSetter = (IEnumerable<string> extensionAssemblies) =>
                { throw new ArgumentException(); };

            Assert.ThrowsException<ArgumentException>(() => testableTestPluginCache.GetTestExtensions(extensionAssembly));
        }

        #endregion

        #region DiscoverAllTestExtensions tests

        [TestMethod]
        public void DiscoverAllTestExtensionsShouldDiscoverExtensionsFromDefaultExtensionsFolder()
        {
            var testableTestPluginCache = new TestableTestPluginCache(new Mock<IPathUtilities>().Object);
            testableTestPluginCache.DoesDirectoryExistSetter = true;
            testableTestPluginCache.FilesInDirectory = (path, pattern) =>
                {
                    if (pattern.Equals("*.dll"))
                    {
                        return new string[] { typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location };
                    }
                    return new string[] { };
                };

            // Setup the testable instance.
            TestPluginCache.Instance = testableTestPluginCache;

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
            var testableTestPluginCache = SetupMockPathUtilities();

            // Stub the default extensions folder.
            testableTestPluginCache.DoesDirectoryExistSetter = false;

            TestPluginCache.Instance.UpdateAdditionalExtensions(extensions, true);

            return testableTestPluginCache;
        }

        public static TestableTestPluginCache SetupMockPathUtilities()
        {
            var mockPathUtilities = new Mock<IPathUtilities>();
            var testableTestPluginCache = new TestableTestPluginCache(mockPathUtilities.Object);

            mockPathUtilities.Setup(pu => pu.GetUniqueValidPaths(It.IsAny<IList<string>>()))
                .Returns((IList<string> paths) => { return new HashSet<string>(paths); });

            TestPluginCache.Instance = testableTestPluginCache;

            return testableTestPluginCache;
        }

        public static void SetupMockExtensions()
        {
            SetupMockExtensions(() => { });
        }

        public static void SetupMockExtensions(Action callback)
        {
            SetupMockExtensions(new string[] { typeof(TestPluginCacheTests).GetTypeInfo().Assembly.Location }, callback);
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
            SettingsProviderExtensionManager.Destroy();
        }

        #endregion
    }

    #region Testable implementation

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

    #endregion 
}
