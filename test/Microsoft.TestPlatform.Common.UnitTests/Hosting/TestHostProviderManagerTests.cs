// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.Logging
{
    using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using TestPlatform.Common.UnitTests.ExtensionFramework;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

    /// <summary>
    /// Tests the behaviors of the TestLoggerManager class.
    /// </summary>
    [TestClass]
    public class TestHostProviderManagerTests
    {
        public TestHostProviderManagerTests()
        {
            TestPluginCacheTests.SetupMockExtensions();
        }

        [TestMethod]
        public void TestHostProviderManagerShouldReturnTestHostWhenAppropriateCustomUriProvided()
        {
            var manager = TestRuntimeProviderManager.Instance;
            Assert.IsNotNull(manager.GetTestHostManagerByUri("executor://DesktopTestHost/"));
        }

        [TestMethod]
        public void TestHostProviderManagerShouldReturnNullWhenInvalidCustomUriProvided()
        {
            var manager = TestRuntimeProviderManager.Instance;
            Assert.IsNull(manager.GetTestHostManagerByUri("executor://InvalidHost/"));
        }

        [TestMethod]
        public void TestHostProviderManagerShouldReturnTestHostBasedOnRunConfiguration()
        {
            string runSettingsXml = @"<?xml version=""1.0"" encoding=""utf-8""?> 
    <RunSettings>     
      <RunConfiguration> 
        <MaxCpuCount>0</MaxCpuCount>       
        <TargetPlatform> x64 </TargetPlatform>     
        <TargetFrameworkVersion> Framework45 </TargetFrameworkVersion> 
      </RunConfiguration>     
    </RunSettings> ";

            var manager = TestRuntimeProviderManager.Instance;
            Assert.IsNotNull(manager.GetTestHostManagerByRunConfiguration(runSettingsXml));
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsANonNullInstance()
        {
            string runSettingsXml = string.Concat(
                @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings>
<RunConfiguration><MaxCpuCount>0</MaxCpuCount><TargetPlatform> x86 </TargetPlatform><TargetFrameworkVersion>",
                Framework.DefaultFramework.Name,
                "</TargetFrameworkVersion></RunConfiguration></RunSettings> ");

            Assert.IsNotNull(TestRuntimeProviderManager.Instance.GetTestHostManagerByRunConfiguration(runSettingsXml));
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsANewInstanceEverytime()
        {
            string runSettingsXml = string.Concat(
                @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings>
<RunConfiguration><MaxCpuCount>0</MaxCpuCount><TargetPlatform> x86 </TargetPlatform><TargetFrameworkVersion>",
                Framework.DefaultFramework.Name,
                "</TargetFrameworkVersion></RunConfiguration></RunSettings> ");

            var instance1 = TestRuntimeProviderManager.Instance.GetTestHostManagerByRunConfiguration(runSettingsXml);
            var instance2 = TestRuntimeProviderManager.Instance.GetTestHostManagerByRunConfiguration(runSettingsXml);

            Assert.AreNotEqual(instance1, instance2);
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsDotnetCoreHostManagerIfFrameworkIsNetCore()
        {
            string runSettingsXml = string.Concat(
                @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings>
<RunConfiguration><MaxCpuCount>0</MaxCpuCount><TargetPlatform> x64 </TargetPlatform><TargetFrameworkVersion>",
                ".NETCoreApp,Version=v1.0",
                "</TargetFrameworkVersion></RunConfiguration></RunSettings> ");

            var testHostManager = TestRuntimeProviderManager.Instance.GetTestHostManagerByRunConfiguration(runSettingsXml);

            Assert.AreEqual(typeof(TestableTestHostManager), testHostManager.GetType());
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsASharedManagerIfDisableAppDomainIsFalse()
        {
            string runSettingsXml = string.Concat(
                @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings>
<RunConfiguration><MaxCpuCount>0</MaxCpuCount><TargetPlatform> x86 </TargetPlatform><TargetFrameworkVersion>",
                ".NETFramework,Version=v4.6",
                "</TargetFrameworkVersion></RunConfiguration></RunSettings> ");

            var testHostManager = TestRuntimeProviderManager.Instance.GetTestHostManagerByRunConfiguration(runSettingsXml);
            testHostManager.Initialize(null, runSettingsXml);
            Assert.IsNotNull(testHostManager);

            Assert.IsTrue(testHostManager.Shared, "Default TestHostManager must be shared if DisableAppDomain is false");
        }

        [TestMethod]
        public void GetDefaultTestHostManagerReturnsANonSharedManagerIfDisableAppDomainIsTrue()
        {
            string runSettingsXml = string.Concat(
                @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings>
<RunConfiguration><MaxCpuCount>0</MaxCpuCount><TargetPlatform> x86 </TargetPlatform><TargetFrameworkVersion>",
                ".NETFramework,Version=v4.6",
                "</TargetFrameworkVersion><DisableAppDomain>true</DisableAppDomain></RunConfiguration></RunSettings> ");

            var testHostManager = TestRuntimeProviderManager.Instance.GetTestHostManagerByRunConfiguration(runSettingsXml);
            testHostManager.Initialize(null, runSettingsXml);
            Assert.IsNotNull(testHostManager);

            Assert.IsFalse(testHostManager.Shared, "Default TestHostManager must NOT be shared if DisableAppDomain is true");
        }

        #region implementations

        [ExtensionUri("executor://DesktopTestHost")]
        [FriendlyName("DesktopTestHost")]
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

            public TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources, IEnumerable<string> extensions)
            {
                throw new NotImplementedException();
            }

            public void Initialize(IMessageLogger logger, string runsettingsXml)
            {
                var config = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
                this.Shared = !config.DisableAppDomain;
            }

            public Task<int> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
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

            public void SetCustomLauncher(ITestHostLauncher customLauncher)
            {
                throw new NotImplementedException();
            }

            public Task TerminateAsync(int testHostId, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        [ExtensionUri("executor://NetCoreTestHost")]
        [FriendlyName("NetCoreTestHost")]
        private class TestableTestHostManager : ITestRuntimeProvider
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

            public TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources, IEnumerable<string> extensions)
            {
                throw new NotImplementedException();
            }

            public void Initialize(IMessageLogger logger, string runsettingsXml)
            {
                var config = XmlRunSettingsUtilities.GetRunConfigurationNode(runsettingsXml);
                this.Shared = !config.DisableAppDomain;
            }

            public Task<int> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
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

            public void SetCustomLauncher(ITestHostLauncher customLauncher)
            {
                throw new NotImplementedException();
            }

            public Task TerminateAsync(int testHostId, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}

