// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeTestRuntimeProvider : ITestRuntimeProvider
{
    public FakeProcessHelper FakeProcessHelper { get; }
    public FakeCommunicationEndpoint FakeCommunicationEndpoint { get; }
    public FakeProcess? TestHostProcess { get; private set; }

    // TODO: make this configurable?
    public bool Shared => false;

    public event EventHandler<HostProviderEventArgs> HostLaunched;
    public event EventHandler<HostProviderEventArgs> HostExited;

    public FakeTestRuntimeProvider(FakeProcessHelper fakeProcessHelper, FakeCommunicationEndpoint fakeCommunicationEndpoint)
    {
        FakeProcessHelper = fakeProcessHelper;
        FakeCommunicationEndpoint = fakeCommunicationEndpoint;
    }

    public bool CanExecuteCurrentRunConfiguration(string runsettingsXml)
    {
        return true;
    }

    public Task CleanTestHostAsync(CancellationToken cancellationToken)
    {
        if (TestHostProcess == null)
            throw new InvalidOperationException("Cannot clean testhost, no testhost process was started");
        FakeProcessHelper.TerminateProcess(TestHostProcess);
        return Task.CompletedTask;
    }

    public TestHostConnectionInfo GetTestHostConnectionInfo()
    {
        // TODO: Makes this configurable?
        return new TestHostConnectionInfo
        {
            // using 9090 for no particular reason, apart from knowing that port 0 is ignored somewhere in our codebase
            Endpoint = "127.0.0.1:9090",
            Role = ConnectionRole.Client,
            Transport = Transport.Sockets,
        };
    }

    public TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo)
    {
        // TODO: do we need to do more here? How to link testhost to the fake one we "start"?
        return new TestProcessStartInfo();
    }

    public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources, IEnumerable<string> extensions)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> GetTestSources(IEnumerable<string> sources)
    {
        throw new NotImplementedException();
    }

    public void Initialize(IMessageLogger logger, string runsettingsXml)
    {
        // do nothing
    }

    public Task<bool> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
    {
        // todo: Add fake process for testhost
        // TestHostProcess = FakeProcessHelper.LaunchProcess(testHostStartInfo);
        //HostLaunched(this, new HostProviderEventArgs("Fake testhost launched", 0, TestHostProcess.Id));
        //TestHostProcess.ProcessExited += (s, e) => HostExited(s, new HostProviderEventArgs($"Fake testhost {TestHostProcess.Id} exited", -99999, e));
        return Task.FromResult(true);
    }

    public void SetCustomLauncher(ITestHostLauncher customLauncher)
    {
        throw new NotImplementedException();
    }
}
