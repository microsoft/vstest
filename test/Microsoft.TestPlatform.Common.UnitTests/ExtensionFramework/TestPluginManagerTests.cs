// Copyright (c) Microsoft. All rights reserved.

namespace TestPlatform.Common.UnitTests.ExtensionFramework
{
    using System;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;

    using Moq;
    using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
    using System.Reflection;
    using System.Linq;
    [TestClass]
    public class TestPluginManagerTests
    {
        [TestMethod]
        public void GetTestExtensionTypeShouldReturnExtensionType()
        {
            var type = TestPluginManager.GetTestExtensionType(typeof(TestPluginManagerTests).AssemblyQualifiedName);

            Assert.AreEqual(typeof(TestPluginManagerTests), type);
        }

        [TestMethod]
        public void GetTestExtensionTypeShouldThrowIfTypeNotFound()
        {
            Assert.ThrowsException<TypeLoadException>(() => TestPluginManager.GetTestExtensionType("randomassemblyname.random"));
        }

        [TestMethod]
        public void CreateTestExtensionShouldCreateExtensionTypeInstance()
        {
            var instance = TestPluginManager.CreateTestExtension<ITestDiscoverer>(typeof(DummyTestDiscoverer));

            Assert.IsNotNull(instance);
            Assert.IsTrue(instance is ITestDiscoverer);
        }

        [TestMethod]
        public void CreateTestExtensionShouldThrowIfInstanceCannotBeCreated()
        {
            Assert.ThrowsException<MissingMethodException>(() => TestPluginManager.CreateTestExtension<ITestLogger>(typeof(AbstractDummyLogger)));
        }

        [TestMethod]
        public void InstanceShouldReturnTestPluginManagerInstance()
        {
            var instance = TestPluginManager.Instance;

            Assert.IsNotNull(instance);
            Assert.IsTrue(instance is TestPluginManager);
        }

        [TestMethod]
        public void InstanceShouldReturnCachedTestPluginManagerInstance()
        {
            var instance = TestPluginManager.Instance;
            
            Assert.AreEqual(instance, TestPluginManager.Instance);
        }

        [TestMethod]
        public void GetTestExtensionsShouldReturnTestDiscovererExtensions()
        {
            TestPluginCacheTests.SetupMockExtensions();

            IEnumerable<LazyExtension<ITestDiscoverer, Dictionary<string, object>>> unfilteredTestExtensions;
            IEnumerable<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>> testExtensions;

            TestPluginManager.Instance.GetTestExtensions<ITestDiscoverer, ITestDiscovererCapabilities, TestDiscovererMetadata>(
                    out unfilteredTestExtensions,
                    out testExtensions);

            Assert.IsNotNull(unfilteredTestExtensions);
            Assert.IsNotNull(testExtensions);
            Assert.IsTrue(testExtensions.Count() > 0);
        }

        [TestMethod]
        public void GetTestExtensionsShouldDiscoverExtensionsOnlyOnce()
        {
            var discoveryCount = 0;
            TestPluginCacheTests.SetupMockExtensions(() => { discoveryCount++; });

            IEnumerable<LazyExtension<ITestDiscoverer, Dictionary<string, object>>> unfilteredTestExtensions;
            IEnumerable<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>> testExtensions;

            TestPluginManager.Instance.GetTestExtensions<ITestDiscoverer, ITestDiscovererCapabilities, TestDiscovererMetadata>(
                    out unfilteredTestExtensions,
                    out testExtensions);
            
            // Call this again to verify that discovery is not called again.
            TestPluginManager.Instance.GetTestExtensions<ITestDiscoverer, ITestDiscovererCapabilities, TestDiscovererMetadata>(
                    out unfilteredTestExtensions,
                    out testExtensions);
            
            Assert.IsNotNull(testExtensions);
            Assert.IsTrue(testExtensions.Count() > 0);
            Assert.AreEqual(1, discoveryCount);
        }

        [TestMethod]
        public void GetTestExtensionsForAnExtensionAssemblyShouldReturnExtensionsInThatAssembly()
        {
            IEnumerable<LazyExtension<ITestDiscoverer, Dictionary<string, object>>> unfilteredTestExtensions;
            IEnumerable<LazyExtension<ITestDiscoverer, ITestDiscovererCapabilities>> testExtensions;

            TestPluginManager.Instance
                .GetTestExtensions<ITestDiscoverer, ITestDiscovererCapabilities, TestDiscovererMetadata>(
                    typeof(TestPluginManagerTests).GetTypeInfo().Assembly.Location,
                    out unfilteredTestExtensions,
                    out testExtensions);

            Assert.IsNotNull(testExtensions);
            Assert.IsTrue(testExtensions.Count() > 0);
        }

        #region implementations

        private abstract class AbstractDummyLogger : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                throw new NotImplementedException();
            }
        }

        private class DummyTestDiscoverer : ITestDiscoverer, ITestExecutor
        {
            public void Cancel()
            {
                throw new NotImplementedException();
            }

            public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                throw new NotImplementedException();
            }

            public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
            {
                throw new NotImplementedException();
            }

            public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
