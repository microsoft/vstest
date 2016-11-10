// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework.Utilities
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Reflection;

    [TestClass]
    public class TestExtensionsTests
    {
        private TestExtensions testExtensions;

        [TestInitialize]
        public void TestInit()
        {
            this.testExtensions = new TestExtensions();
        }

        [TestMethod]
        public void AddExtensionsShouldNotThrowIfExtensionsIsNull()
        {
            this.testExtensions.AddExtensions(null);

            // Validate that the default state does not change.
            Assert.IsNull(this.testExtensions.TestDiscoverers);
        }

        [TestMethod]
        public void AddExtensionsShouldNotThrowIfExistingExtensionCollectionIsNull()
        {
            var newTestExtensions = new TestExtensions();
            newTestExtensions.TestDiscoverers =
                new System.Collections.Generic.Dictionary<string, TestDiscovererPluginInformation>();

            newTestExtensions.TestDiscoverers.Add(
                "td",
                new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));

            this.testExtensions.AddExtensions(newTestExtensions);
            
            Assert.IsNotNull(this.testExtensions.TestDiscoverers);
            CollectionAssert.AreEqual(this.testExtensions.TestDiscoverers, newTestExtensions.TestDiscoverers);

            // Validate that the others remain same.
            Assert.IsNull(this.testExtensions.TestExecutors);
            Assert.IsNull(this.testExtensions.TestSettingsProviders);
            Assert.IsNull(this.testExtensions.TestLoggers);
        }

        [TestMethod]
        public void AddExtensionsShouldAddToExistingExtensionCollection()
        {
            var newTestExtensions = new TestExtensions();
            newTestExtensions.TestDiscoverers =
                new System.Collections.Generic.Dictionary<string, TestDiscovererPluginInformation>();

            newTestExtensions.TestDiscoverers.Add(
                "td1",
                new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));
            newTestExtensions.TestDiscoverers.Add(
                "td2",
                new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));

            this.testExtensions.TestDiscoverers =
                new Dictionary<string, TestDiscovererPluginInformation>();

            this.testExtensions.TestDiscoverers.Add(
                "td",
                new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));

            // Act.
            this.testExtensions.AddExtensions(newTestExtensions);

            // Validate.
            var expectedTestExtensions = new Dictionary<string, TestDiscovererPluginInformation>();
            expectedTestExtensions.Add("td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));
            expectedTestExtensions.Add("td1", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));
            expectedTestExtensions.Add("td2", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));
            
            CollectionAssert.AreEqual(this.testExtensions.TestDiscoverers.Keys, expectedTestExtensions.Keys);

            // Validate that the others remain same.
            Assert.IsNull(this.testExtensions.TestExecutors);
            Assert.IsNull(this.testExtensions.TestSettingsProviders);
            Assert.IsNull(this.testExtensions.TestLoggers);
        }

        [TestMethod]
        public void AddExtensionsShouldNotAddAnAlreadyExistingExtensionToTheCollection()
        {
            var newTestExtensions = new TestExtensions();
            newTestExtensions.TestDiscoverers =
                new System.Collections.Generic.Dictionary<string, TestDiscovererPluginInformation>();

            newTestExtensions.TestDiscoverers.Add(
                "td",
                new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));

            this.testExtensions.TestDiscoverers =
                new System.Collections.Generic.Dictionary<string, TestDiscovererPluginInformation>();

            this.testExtensions.TestDiscoverers.Add(
                "td",
                new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));

            // Act.
            this.testExtensions.AddExtensions(newTestExtensions);
            
            // Validate.
            CollectionAssert.AreEqual(this.testExtensions.TestDiscoverers.Keys, newTestExtensions.TestDiscoverers.Keys);

            // Validate that the others remain same.
            Assert.IsNull(this.testExtensions.TestExecutors);
            Assert.IsNull(this.testExtensions.TestSettingsProviders);
            Assert.IsNull(this.testExtensions.TestLoggers);
        }

        [TestMethod]
        public void AddExtensionsShouldAddAllExtensions()
        {
            var newTestExtensions = new TestExtensions();
            newTestExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            newTestExtensions.TestExecutors = new Dictionary<string, TestExecutorPluginInformation>();
            newTestExtensions.TestSettingsProviders = new Dictionary<string, TestSettingsProviderPluginInformation>();
            newTestExtensions.TestLoggers = new Dictionary<string, TestLoggerPluginInformation>();

            newTestExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));
            newTestExtensions.TestExecutors.Add("te", new TestExecutorPluginInformation(typeof(TestExtensionsTests)));
            newTestExtensions.TestSettingsProviders.Add("tsp", new TestSettingsProviderPluginInformation(typeof(TestExtensionsTests)));
            newTestExtensions.TestLoggers.Add("tl", new TestLoggerPluginInformation(typeof(TestExtensionsTests)));
            
            // Act.
            this.testExtensions.AddExtensions(newTestExtensions);

            // Validate.
            CollectionAssert.AreEqual(this.testExtensions.TestDiscoverers.Keys, newTestExtensions.TestDiscoverers.Keys);
            CollectionAssert.AreEqual(this.testExtensions.TestExecutors.Keys, newTestExtensions.TestExecutors.Keys);
            CollectionAssert.AreEqual(this.testExtensions.TestSettingsProviders.Keys, newTestExtensions.TestSettingsProviders.Keys);
            CollectionAssert.AreEqual(this.testExtensions.TestLoggers.Keys, newTestExtensions.TestLoggers.Keys);
        }

        [TestMethod]
        public void GetExtensionsDiscoveredFromAssemblyShouldReturnNullIfNoExtensionsPresent()
        {
            var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

            Assert.IsNull(this.testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation));
        }

        [TestMethod]
        public void GetExtensionsDiscoveredFromAssemblyShouldNotThrowIfExtensionAssemblyIsNull()
        {
            this.testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            
            this.testExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));

            Assert.IsNull(this.testExtensions.GetExtensionsDiscoveredFromAssembly(null));
        }

        [TestMethod]
        public void GetExtensionsDiscoveredFromAssemblyShouldReturnTestDiscoverers()
        {
            var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

            this.testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            this.testExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));
            this.testExtensions.TestDiscoverers.Add("td1", new TestDiscovererPluginInformation(typeof(TestExtensions)));

            var extensions = this.testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation);

            var expectedExtensions = new Dictionary<string, TestDiscovererPluginInformation>();
            expectedExtensions.Add("td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));
            CollectionAssert.AreEqual(expectedExtensions.Keys, extensions.TestDiscoverers.Keys);
        }

        [TestMethod]
        public void GetExtensionsDiscoveredFromAssemblyShouldReturnTestExecutors()
        {
            var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

            this.testExtensions.TestExecutors = new Dictionary<string, TestExecutorPluginInformation>();
            this.testExtensions.TestExecutors.Add("te", new TestExecutorPluginInformation(typeof(TestExtensionsTests)));
            this.testExtensions.TestExecutors.Add("te1", new TestExecutorPluginInformation(typeof(TestExtensions)));

            var extensions = this.testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation);

            var expectedExtensions = new Dictionary<string, TestExecutorPluginInformation>();
            expectedExtensions.Add("te", new TestExecutorPluginInformation(typeof(TestExtensionsTests)));
            CollectionAssert.AreEqual(expectedExtensions.Keys, extensions.TestExecutors.Keys);
        }

        [TestMethod]
        public void GetExtensionsDiscoveredFromAssemblyShouldReturnTestSettingsProviders()
        {
            var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

            this.testExtensions.TestSettingsProviders = new Dictionary<string, TestSettingsProviderPluginInformation>();
            this.testExtensions.TestSettingsProviders.Add("tsp", new TestSettingsProviderPluginInformation(typeof(TestExtensionsTests)));
            this.testExtensions.TestSettingsProviders.Add("tsp1", new TestSettingsProviderPluginInformation(typeof(TestExtensions)));

            var extensions = this.testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation);

            var expectedExtensions = new Dictionary<string, TestSettingsProviderPluginInformation>();
            expectedExtensions.Add("tsp", new TestSettingsProviderPluginInformation(typeof(TestExtensionsTests)));
            CollectionAssert.AreEqual(expectedExtensions.Keys, extensions.TestSettingsProviders.Keys);
        }

        [TestMethod]
        public void GetExtensionsDiscoveredFromAssemblyShouldReturnTestLoggers()
        {
            var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

            this.testExtensions.TestLoggers = new Dictionary<string, TestLoggerPluginInformation>();
            this.testExtensions.TestLoggers.Add("tl", new TestLoggerPluginInformation(typeof(TestExtensionsTests)));
            this.testExtensions.TestLoggers.Add("tl1", new TestLoggerPluginInformation(typeof(TestExtensions)));

            var extensions = this.testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation);

            var expectedExtensions = new Dictionary<string, TestLoggerPluginInformation>();
            expectedExtensions.Add("tl", new TestLoggerPluginInformation(typeof(TestExtensionsTests)));
            CollectionAssert.AreEqual(expectedExtensions.Keys, extensions.TestLoggers.Keys);
        }

        [TestMethod]
        public void GetExtensionsDiscoveredFromAssemblyShouldReturnTestDiscoveresAndLoggers()
        {
            var assemblyLocation = typeof(TestExtensionsTests).GetTypeInfo().Assembly.Location;

            this.testExtensions.TestDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            this.testExtensions.TestDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));

            this.testExtensions.TestLoggers = new Dictionary<string, TestLoggerPluginInformation>();
            this.testExtensions.TestLoggers.Add("tl", new TestLoggerPluginInformation(typeof(TestExtensionsTests)));

            var extensions = this.testExtensions.GetExtensionsDiscoveredFromAssembly(assemblyLocation);

            var expectedDiscoverers = new Dictionary<string, TestDiscovererPluginInformation>();
            expectedDiscoverers.Add("td", new TestDiscovererPluginInformation(typeof(TestExtensionsTests)));
            CollectionAssert.AreEqual(expectedDiscoverers.Keys, extensions.TestDiscoverers.Keys);

            var expectedLoggers = new Dictionary<string, TestLoggerPluginInformation>();
            expectedLoggers.Add("tl", new TestLoggerPluginInformation(typeof(TestExtensionsTests)));
            CollectionAssert.AreEqual(expectedLoggers.Keys, extensions.TestLoggers.Keys);
        }
    }
}
