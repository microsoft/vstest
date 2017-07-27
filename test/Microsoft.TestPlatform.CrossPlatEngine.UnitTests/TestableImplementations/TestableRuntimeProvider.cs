// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.CrossPlatEngine.UnitTests.TestableImplementations
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    [ExtensionUri("executor://TestableTestHost")]
    [FriendlyName("TestableTestHost")]
    public class TestableRuntimeProvider : ITestRuntimeProvider
    {
        public TestableRuntimeProvider()
        {
        }

        public TestableRuntimeProvider(bool shared)
        {
            Shared = shared;
        }

        public event EventHandler<HostProviderEventArgs> HostLaunched;

        public event EventHandler<HostProviderEventArgs> HostExited;

        public bool Shared { get; }

        public void Initialize(IMessageLogger logger, string runsettingsXml)
        {
        }

        public bool CanExecuteCurrentRunConfiguration(string runsettingsXml)
        {
            return true;
        }

        public void SetCustomLauncher(ITestHostLauncher customLauncher)
        {
        }

        public TestHostConnectionInfo GetTestHostConnectionInfo()
        {
            return new TestHostConnectionInfo { Endpoint = "127.0.0.1:0", Role = ConnectionRole.Client, Transport = Transport.Sockets };
        }

        public Task<bool> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
        {
            HostLaunched(this, null);
            return Task.FromResult(true);
        }

        public TestProcessStartInfo GetTestHostProcessStartInfo(
            IEnumerable<string> sources,
            IDictionary<string, string> environmentVariables,
            TestRunnerConnectionInfo connectionInfo)
        {
            return default(TestProcessStartInfo);
        }

        public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources, IEnumerable<string> extensions)
        {
            return extensions;
        }

        public Task CleanTestHostAsync(CancellationToken cancellationToken)
        {
            HostExited(this, null);
            return Task.FromResult(true);
        }
    }
}
