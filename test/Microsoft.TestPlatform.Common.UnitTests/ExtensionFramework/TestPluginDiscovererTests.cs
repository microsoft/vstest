// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.ExtensionFramework
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml;

    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
    using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework.Utilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestPluginDiscovererTests
    {
        private TestPluginDiscoverer testPluginDiscoverer;

        public TestPluginDiscovererTests()
        {
            this.testPluginDiscoverer = new TestPluginDiscoverer();
        }

        [TestMethod]
        public void GetTestExtensionsInformationShouldNotThrowOnALoadException()
        {
            var pathToExtensions = new List<string> { "foo.dll" };

            // The below should not throw an exception.
            Assert.IsNotNull(this.testPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(pathToExtensions, loadOnlyWellKnownExtensions: true));
        }

        [TestMethod]
        public void GetTestExtensionsInformationShouldNotConsiderAbstractClasses()
        {
            var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).GetTypeInfo().Assembly.Location };

            // The below should not throw an exception.
            var testExtensions = this.testPluginDiscoverer.GetTestExtensionsInformation<TestDiscovererPluginInformation, ITestDiscoverer>(pathToExtensions, loadOnlyWellKnownExtensions: true);
            var discovererPluginInformation = new TestDiscovererPluginInformation(typeof(AbstractTestDiscoverer));
            Assert.IsFalse(testExtensions.ContainsKey(discovererPluginInformation.IdentifierData));
        }

        [TestMethod]
        public void GetTestExtensionsInformationShouldReturnDiscovererExtensions()
        {
            var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).GetTypeInfo().Assembly.Location };

            // The below should not throw an exception.
            var testExtensions = this.testPluginDiscoverer.GetTestExtensionsInformation<TestDiscovererPluginInformation, ITestDiscoverer>(pathToExtensions, loadOnlyWellKnownExtensions: true);

            var discovererPluginInformation = new TestDiscovererPluginInformation(typeof(ValidDiscoverer));
            var discovererPluginInformation2 = new TestDiscovererPluginInformation(typeof(ValidDiscoverer2));

            Assert.IsTrue(testExtensions.ContainsKey(discovererPluginInformation.IdentifierData));
            Assert.IsTrue(testExtensions.ContainsKey(discovererPluginInformation2.IdentifierData));
        }

        [TestMethod]
        public void GetTestExtensionsInformationShouldReturnExecutorExtensions()
        {
            var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).GetTypeInfo().Assembly.Location };

            // The below should not throw an exception.
            var testExtensions = this.testPluginDiscoverer.GetTestExtensionsInformation<TestExecutorPluginInformation, ITestExecutor>(pathToExtensions, loadOnlyWellKnownExtensions: true);

            var pluginInformation = new TestExecutorPluginInformation(typeof(ValidExecutor));
            var pluginInformation2 = new TestExecutorPluginInformation(typeof(ValidExecutor2));

            Assert.AreEqual(2, testExtensions.Keys.Count(k => k.Contains("ValidExecutor")));
            Assert.IsTrue(testExtensions.ContainsKey(pluginInformation.IdentifierData));
            Assert.IsTrue(testExtensions.ContainsKey(pluginInformation2.IdentifierData));
        }

        [TestMethod]
        public void GetTestExtensionsInformationShouldReturnLoggerExtensions()
        {
            var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).GetTypeInfo().Assembly.Location };

            // The below should not throw an exception.
            var testExtensions = this.testPluginDiscoverer.GetTestExtensionsInformation<TestLoggerPluginInformation, ITestLogger>(pathToExtensions, loadOnlyWellKnownExtensions: true);

            var pluginInformation = new TestLoggerPluginInformation(typeof(ValidLogger));
            var pluginInformation2 = new TestLoggerPluginInformation(typeof(ValidLogger2));

            Assert.AreEqual(1, testExtensions.Keys.Where(k => k.Contains("csv")).Count());
            Assert.IsTrue(testExtensions.ContainsKey(pluginInformation.IdentifierData));
            Assert.IsTrue(testExtensions.ContainsKey(pluginInformation2.IdentifierData));
        }

        [TestMethod]
        public void GetTestExtensionsInformationShouldReturnSettingsProviderExtensions()
        {
            var pathToExtensions = new List<string> { typeof(TestPluginDiscovererTests).GetTypeInfo().Assembly.Location };

            // The below should not throw an exception.
            var testExtensions = this.testPluginDiscoverer.GetTestExtensionsInformation<TestSettingsProviderPluginInformation, ISettingsProvider>(pathToExtensions, loadOnlyWellKnownExtensions: true);

            var pluginInformation = new TestSettingsProviderPluginInformation(typeof(ValidSettingsProvider));
            var pluginInformation2 = new TestSettingsProviderPluginInformation(typeof(ValidSettingsProvider2));

            Assert.IsTrue(testExtensions.Keys.Select(k => k.Contains("ValidSettingsProvider")).Count() >= 3);
            Assert.IsTrue(testExtensions.ContainsKey(pluginInformation.IdentifierData));
            Assert.IsTrue(testExtensions.ContainsKey(pluginInformation2.IdentifierData));
        }

        [TestMethod]
        public void GetTestExtensionsInformationShouldNotAbortOnFaultyExtensions()
        {
            var pathToExtensions = new List<string>
            {
                typeof(TestPluginDiscovererTests).GetTypeInfo().Assembly.Location,
                typeof(TestPluginDiscoverer).GetTypeInfo().Assembly.Location,
            };

            // The below should not throw an exception.
            this.testPluginDiscoverer = new DummyTestPluginDiscoverer();
            var testExtensions = this.testPluginDiscoverer.GetTestExtensionsInformation<TestSettingsProviderPluginInformation, ISettingsProvider>(pathToExtensions, loadOnlyWellKnownExtensions: true);

            Assert.IsTrue(testExtensions.Keys.Select(k => k.Contains("ValidSettingsProvider")).Count() >= 3);
        }

        #region implementations

        #region Discoverers

        private abstract class AbstractTestDiscoverer : ITestDiscoverer
        {
            public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                throw new NotImplementedException();
            }
        }

        private class ValidDiscoverer : ITestDiscoverer
        {
            public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                throw new NotImplementedException();
            }
        }

        private class ValidDiscoverer2 : ITestDiscoverer
        {
            public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Executors

        [ExtensionUri("ValidExecutor")]
        private class ValidExecutor : ITestExecutor
        {
            public void Cancel()
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

        [ExtensionUri("ValidExecutor2")]
        private class ValidExecutor2 : ITestExecutor
        {
            public void Cancel()
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

        [ExtensionUri("ValidExecutor")]
        private class DuplicateExecutor : ITestExecutor
        {
            public void Cancel()
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

        #region Loggers

        [ExtensionUri("csv")]
        private class ValidLogger : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                throw new NotImplementedException();
            }
        }

        [ExtensionUri("docx")]
        private class ValidLogger2 : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                throw new NotImplementedException();
            }
        }

        [ExtensionUri("csv")]
        private class DuplicateLogger : ITestLogger
        {
            public void Initialize(TestLoggerEvents events, string testRunDirectory)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Settings Providers

        [SettingsName("ValidSettingsProvider")]
        private class ValidSettingsProvider : ISettingsProvider
        {
            public void Load(XmlReader reader)
            {
                throw new NotImplementedException();
            }
        }

        [SettingsName("ValidSettingsProvider2")]
        private class ValidSettingsProvider2 : ISettingsProvider
        {
            public void Load(XmlReader reader)
            {
                throw new NotImplementedException();
            }
        }

        [SettingsName("ValidSettingsProvider")]
        private class DuplicateSettingsProvider : ISettingsProvider
        {
            public void Load(XmlReader reader)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        private class DummyTestPluginDiscoverer : TestPluginDiscoverer
        {
            internal override void GetTestExtensionsFromAssembly<TPluginInfo, TExtension>(Assembly assembly, Dictionary<string, TPluginInfo> pluginInfos)
            {
                if (assembly.Location.Contains("Microsoft.VisualStudio.TestPlatform.Common.dll"))
                {
                    throw new Exception("Fail to load types");
                }
                else
                {
                    base.GetTestExtensionsFromAssembly<TPluginInfo, TExtension>(assembly, pluginInfos);
                }
            }
        }

        #endregion
    }
}
