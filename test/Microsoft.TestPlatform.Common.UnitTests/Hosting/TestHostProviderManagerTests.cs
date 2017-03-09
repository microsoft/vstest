// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace TestPlatform.Common.UnitTests.Logging
{
    using Microsoft.VisualStudio.TestPlatform.Common;
    using Microsoft.VisualStudio.TestPlatform.Common.Hosting;
    using Microsoft.VisualStudio.TestPlatform.Common.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using TestPlatform.Common.UnitTests.ExtensionFramework;
    using TestPlatform.Common.UnitTests.Utilities;
    using ObjectModel = Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using System.Threading.Tasks;

    /// <summary>
    /// Tests the behaviors of the TestLoggerManager class.
    /// </summary>
    [TestClass]
    public class TestHostProviderManagerTests
    {
        [TestInitialize]
        public void Initialize()
        {
            TestPluginCacheTests.SetupMockExtensions();
        }

        [TestMethod]
        public void TestHostProviderManagerShouldReturnTestHostWhenAppropriateCustomURIProvided()
        {
            var manager = TestRuntimeProviderManager.Instance;
            Assert.IsNotNull(manager.GetTestHostManagerByUri("executor://CustomTestHost/"));
        }

        [TestMethod]
        public void TestHostProviderManagerShouldReturnNullWhenInvalidCustomURIProvided()
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

            RunConfiguration config = XmlRunSettingsUtilities.GetRunConfigurationNode(runSettingsXml);

            var manager = TestRuntimeProviderManager.Instance;
            Assert.IsNotNull(manager.GetTestHostManagerByRunConfiguration(config));
        }

        #region implementations

        [ExtensionUri("executor://CustomTestHost")]
        [FriendlyName("CustomHost")]
        private class CustomTestHost : ITestRuntimeProvider
        {
            public bool Shared => throw new NotImplementedException();

            public event EventHandler<HostProviderEventArgs> HostLaunched;
            public event EventHandler<HostProviderEventArgs> HostExited;

            public bool CanExecuteCurrentRunConfiguration(RunConfiguration runConfiguration)
            {
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

            public void Initialize(IMessageLogger logger)
            {
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

