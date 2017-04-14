// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework
{
    using System.Linq;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Reflection;
    [TestClass]
    public class TestExecutorExtensionManagerTests
    {
        [TestCleanup]
        public void TestCleanup()
        {
            TestExecutorExtensionManager.Destroy();
        }

        [TestMethod]
        public void CreateShouldDiscoverExecutorExtensions()
        {
            TestPluginCacheTests.SetupMockExtensions();

            var extensionManager = TestExecutorExtensionManager.Create();

            Assert.IsNotNull(extensionManager.TestExtensions);
            Assert.IsTrue(extensionManager.TestExtensions.Count() > 0);
        }

        [TestMethod]
        public void CreateShouldCacheDiscoveredExtensions()
        {
            TestPluginCacheTests.SetupMockExtensions(() => { });

            var extensionManager = TestExecutorExtensionManager.Create();
            TestExecutorExtensionManager.Create();

            Assert.IsNotNull(extensionManager.TestExtensions);
            Assert.IsTrue(extensionManager.TestExtensions.Count() > 0);
        }

        [TestMethod]
        public void GetExecutorExtensionManagerShouldReturnAnExecutionManagerWithExtensions()
        {
            var extensionManager =
                TestExecutorExtensionManager.GetExecutionExtensionManager(
                    typeof(TestExecutorExtensionManagerTests).GetTypeInfo().Assembly.Location);

            Assert.IsNotNull(extensionManager.TestExtensions);
            Assert.IsTrue(extensionManager.TestExtensions.Count() > 0);
        }

        #region LoadAndInitialize tests

        [TestMethod]
        public void LoadAndInitializeShouldInitializeAllExtensions()
        {
            TestPluginCacheTests.SetupMockExtensions();

            TestExecutorExtensionManager.LoadAndInitializeAllExtensions(false);

            var allExecutors = TestExecutorExtensionManager.Create().TestExtensions;

            foreach (var executor in allExecutors)
            {
                Assert.IsTrue(executor.IsExtensionCreated);
            }
        }

        #endregion
    }

    [TestClass]
    public class TestExecutorMetadataTests
    {
        [TestMethod]
        public void TestExecutorMetadataCtorShouldSetExtensionUri()
        {
            var metadata = new TestExecutorMetadata("random");

            Assert.AreEqual("random", metadata.ExtensionUri);
        }
    }
}
