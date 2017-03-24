// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.CrossPlatEngine.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using TestPlatform.Common.UnitTests.ExtensionFramework;

    [TestClass]
    public class TestEngineTests
    {
        private ITestEngine testEngine;

        private Mock<ITestRuntimeProvider> mockTestHostManager;

        public TestEngineTests()
        {
            TestPluginCacheTests.SetupMockExtensions(new[] { typeof(TestEngineTests).GetTypeInfo().Assembly.Location }, () => { });
            this.testEngine = new TestEngine();
            this.mockTestHostManager = new Mock<ITestRuntimeProvider>();
            
            // Default setting for host manager
            this.mockTestHostManager.Setup(p => p.Shared).Returns(true);
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnANonNullInstance()
        {
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
            Assert.IsNotNull(this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnsNewInstanceOfProxyDiscoveryManagerIfTestHostIsShared()
        {
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
            var discoveryManager = this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria);

            Assert.AreNotSame(discoveryManager, this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria), typeof(ProxyDiscoveryManager));
        }

        [TestMethod]
        public void GetDiscoveryManagerShouldReturnsParallelDiscoveryManagerIfTestHostIsNotShared()
        {
            var discoveryCriteria = new DiscoveryCriteria(new List<string> { "1.dll" }, 100, null);
            this.mockTestHostManager.Setup(p => p.Shared).Returns(false);
            
            Assert.IsNotNull(this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetDiscoveryManager(this.mockTestHostManager.Object, discoveryCriteria), typeof(ParallelProxyDiscoveryManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnANonNullInstance()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnNewInstance()
        {
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100);
            var executionManager = this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria);

            Assert.AreNotSame(executionManager, this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnDefaultExecutionManagerIfParallelDisabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration></RunConfiguration ></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria), typeof(ProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerWithSingleSourceShouldReturnDefaultExecutionManagerEvenIfParallelEnabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration ></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria), typeof(ProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnParallelExecutionManagerIfParallelEnabled()
        {
            string settingXml = @"<RunSettings><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, settingXml);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria), typeof(ParallelProxyExecutionManager));
        }

        [TestMethod]
        public void GetExecutionManagerShouldReturnParallelExecutionManagerIfHostIsNotShared()
        {
            this.mockTestHostManager.Setup(p => p.Shared).Returns(false);
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll", "2.dll" }, 100, false, null);

            Assert.IsNotNull(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria));
            Assert.IsInstanceOfType(this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria), typeof(ParallelProxyExecutionManager));
        }

        [TestMethod]
        public void GetExcecutionManagerShouldReturnExectuionManagerWithDataCollectionIfDataCollectionIsEnabled()
        {
            var settingXml = @"<RunSettings><DataCollectionRunSettings><DataCollectors><DataCollector friendlyName=""Code Coverage"" uri=""datacollector://Microsoft/CodeCoverage/2.0"" assemblyQualifiedName=""Microsoft.VisualStudio.Coverage.DynamicCoverageDataCollector, Microsoft.VisualStudio.TraceCollector, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a""></DataCollector></DataCollectors></DataCollectionRunSettings></RunSettings>";
            var testRunCriteria = new TestRunCriteria(new List<string> { "1.dll" }, 100, false, settingXml);
            var result = this.testEngine.GetExecutionManager(this.mockTestHostManager.Object, testRunCriteria);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(ProxyExecutionManagerWithDataCollection));
        }

        [TestMethod]
        public void GetExtensionManagerShouldReturnANonNullInstance()
        {
            Assert.IsNotNull(this.testEngine.GetExtensionManager());
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsANonNullInstance()
        {
            this.testEngine = new TestEngine(TestRuntimeProviderManager.Instance);
            string runSettingsXml = string.Concat(
                @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings>
<RunConfiguration><MaxCpuCount>0</MaxCpuCount><TargetPlatform> x86 </TargetPlatform><TargetFrameworkVersion>",
                Framework.DefaultFramework.Name,
                "</TargetFrameworkVersion></RunConfiguration></RunSettings> ");

            Assert.IsNotNull(this.testEngine.GetDefaultTestHostManager(runSettingsXml));
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsANewInstanceEverytime()
        {
            this.testEngine = new TestEngine(TestRuntimeProviderManager.Instance);
            string runSettingsXml = string.Concat(
                @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings>
<RunConfiguration><MaxCpuCount>0</MaxCpuCount><TargetPlatform> x86 </TargetPlatform><TargetFrameworkVersion>",
                Framework.DefaultFramework.Name,
                "</TargetFrameworkVersion></RunConfiguration></RunSettings> ");

            var instance1 = this.testEngine.GetDefaultTestHostManager(runSettingsXml);
            var instance2 = this.testEngine.GetDefaultTestHostManager(runSettingsXml);

            Assert.AreNotEqual(instance1, instance2);
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsDotnetCoreHostManagerIfFrameworkIsNetCore()
        {
            this.testEngine = new TestEngine(TestRuntimeProviderManager.Instance);
            string runSettingsXml = string.Concat(
                @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings>
<RunConfiguration><MaxCpuCount>0</MaxCpuCount><TargetPlatform> x64 </TargetPlatform><TargetFrameworkVersion>",
                ".NETCoreApp,Version=v1.0",
                "</TargetFrameworkVersion></RunConfiguration></RunSettings> ");

            var testHostManager = this.testEngine.GetDefaultTestHostManager(runSettingsXml);

            Assert.AreEqual(typeof(DotnetTestHostManager), testHostManager.GetType());
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsASharedManagerIfDisableAppDomainIsFalse()
        {
            this.testEngine = new TestEngine(TestRuntimeProviderManager.Instance);
            string runSettingsXml = string.Concat(
                @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings>
<RunConfiguration><MaxCpuCount>0</MaxCpuCount><TargetPlatform> x86 </TargetPlatform><TargetFrameworkVersion>",
                ".NETFramework,Version=v4.6",
                "</TargetFrameworkVersion></RunConfiguration></RunSettings> ");

            var testHostManager = this.testEngine.GetDefaultTestHostManager(runSettingsXml);
            testHostManager.Initialize(null, runSettingsXml);
            Assert.IsNotNull(testHostManager);

            Assert.IsTrue(testHostManager.Shared, "Default TestHostManager must be shared if DisableAppDomain is false");
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsANonSharedManagerIfDisableAppDomainIsTrue()
        {
            this.testEngine = new TestEngine(TestRuntimeProviderManager.Instance);
            string runSettingsXml = string.Concat(
                @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings>
<RunConfiguration><MaxCpuCount>0</MaxCpuCount><TargetPlatform> x86 </TargetPlatform><TargetFrameworkVersion>",
                ".NETFramework,Version=v4.6",
                "</TargetFrameworkVersion><DisableAppDomain>true</DisableAppDomain></RunConfiguration></RunSettings> ");

            var testHostManager = this.testEngine.GetDefaultTestHostManager(runSettingsXml);
            testHostManager.Initialize(null, runSettingsXml);
            Assert.IsNotNull(testHostManager);

            Assert.IsFalse(testHostManager.Shared, "Default TestHostManager must NOT be shared if DisableAppDomain is true");
        }

        #region implementations

        [ExtensionUri("executor://CustomTestHost")]
        [FriendlyName("CustomHost")]
        private class CustomTestHost : ITestRuntimeProvider
        {
            public event EventHandler<HostProviderEventArgs> HostLaunched;

            public event EventHandler<HostProviderEventArgs> HostExited;

            public bool Shared { get; private set; }


            public bool CanExecuteCurrentRunConfiguration(string runsettingsXml)
            {
                var config = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
                var framework = config.TargetFrameworkVersion;
                this.Shared = !config.DisableAppDomain;

                // This is expected to be called once every run so returning a new instance every time.
                if (framework.Name.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                    || framework.Name.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }

                return true;
            }

            public void DeregisterForExitNotification()
            {
                throw new NotImplementedException();
            }

            public CancellationTokenSource GetCancellationTokenSource()
            {
                throw new NotImplementedException();
            }

            public TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources)
            {
                throw new NotImplementedException();
            }

            public void Initialize(IMessageLogger logger, string runsettingsXml)
            {
                var config = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
                this.Shared = !config.DisableAppDomain;
            }

            public Task<int> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo)
            {
                throw new NotImplementedException();
            }

            public void OnHostExited(HostProviderEventArgs e)
            {
                this.HostExited.Invoke(this, new HostProviderEventArgs("Error"));
            }

            public void OnHostLaunched(HostProviderEventArgs e)
            {
                this.HostLaunched.Invoke(this, new HostProviderEventArgs("Error"));
            }

            public void RegisterForExitNotification(Action abortCallback)
            {
                throw new NotImplementedException();
            }

            public void SetCustomLauncher(ITestHostLauncher customLauncher)
            {
                throw new NotImplementedException();
            }
        }

        [ExtensionUri("executor://DotnetTestHostManager")]
        [FriendlyName("DotnetTestHostManager")]
        private class DotnetTestHostManager : ITestRuntimeProvider
        {
            public event EventHandler<HostProviderEventArgs> HostLaunched;

            public event EventHandler<HostProviderEventArgs> HostExited;

            public bool Shared { get; private set; }


            public bool CanExecuteCurrentRunConfiguration(string runsettingsXml)
            {
                var config = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
                var framework = config.TargetFrameworkVersion;
                this.Shared = !config.DisableAppDomain;

                // This is expected to be called once every run so returning a new instance every time.
                if (framework.Name.IndexOf("netstandard", StringComparison.OrdinalIgnoreCase) >= 0
                    || framework.Name.IndexOf("netcoreapp", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                return false;
            }

            public void DeregisterForExitNotification()
            {
                throw new NotImplementedException();
            }

            public CancellationTokenSource GetCancellationTokenSource()
            {
                throw new NotImplementedException();
            }

            public TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources)
            {
                throw new NotImplementedException();
            }

            public void Initialize(IMessageLogger logger, string runsettingsXml)
            {
                var config = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
                this.Shared = !config.DisableAppDomain;
            }

            public Task<int> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo)
            {
                throw new NotImplementedException();
            }

            public void OnHostExited(HostProviderEventArgs e)
            {
                this.HostExited.Invoke(this, new HostProviderEventArgs("Error"));
            }

            public void OnHostLaunched(HostProviderEventArgs e)
            {
                this.HostLaunched.Invoke(this, new HostProviderEventArgs("Error"));
            }

            public void RegisterForExitNotification(Action abortCallback)
            {
                throw new NotImplementedException();
            }

            public void SetCustomLauncher(ITestHostLauncher customLauncher)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
