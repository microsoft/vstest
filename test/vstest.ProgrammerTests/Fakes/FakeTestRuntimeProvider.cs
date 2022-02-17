// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine.Fakes;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

internal class FakeTestRuntimeProvider : ITestRuntimeProvider
{
    public FakeProcessHelper FakeProcessHelper { get; }
    public FakeCommunicationEndpoint FakeCommunicationEndpoint { get; }
    public FakeErrorAggregator FakeErrorAggregator { get; }
    public FakeProcess TestHostProcess { get; private set; }

    // TODO: make this configurable?
    public bool Shared => false;

    public event EventHandler<HostProviderEventArgs>? HostLaunched;
    public event EventHandler<HostProviderEventArgs>? HostExited;

    public FakeTestRuntimeProvider(FakeProcessHelper fakeProcessHelper, FakeProcess fakeTestHostProcess, FakeCommunicationEndpoint fakeCommunicationEndpoint, FakeErrorAggregator fakeErrorAggregator)
    {
        FakeProcessHelper = fakeProcessHelper;
        TestHostProcess = fakeTestHostProcess;
        TestHostProcess.ExitCallback = p =>
        {
            // TODO: Validate the process we are passed is actually the same as TestHostProcess
            // TODO: Validate we already started the process.
            var process = (FakeProcess)p;
            if (HostExited != null)
            {
                // TODO: When we exit, eventually there are no subscribers, maybe we should review if we don't lose the error output sometimes, in unnecessary way
                HostExited(this, new HostProviderEventArgs(process.ErrorOutput, process.ExitCode, process.Id));
            }
        };

        FakeCommunicationEndpoint = fakeCommunicationEndpoint;
        FakeErrorAggregator = fakeErrorAggregator;
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
        return FakeCommunicationEndpoint.TestHostConnectionInfo;
    }

    public TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo)
    {
        // TODO: do we need to do more here? How to link testhost to the fake one we "start"?
        return TestHostProcess.TestProcessStartInfo;
    }

    public IEnumerable<string> GetTestPlatformExtensions(IEnumerable<string> sources, IEnumerable<string> extensions)
    {
        // send extensions so we send InitializeExecutionMessage
        return new[] { @"c:\temp\extension.dll" };
    }

    public IEnumerable<string> GetTestSources(IEnumerable<string> sources)
    {
        // gives testhost opportunity to translate sources to something else,
        // e.g. in uwp the main exe is returned, rather than the dlls that dlls that are tested
        return sources;
    }

    public void Initialize(IMessageLogger logger, string runsettingsXml)
    {
        // TODO: this is called twice, is that okay?
        // TODO: and also by HandlePartialRunComplete after the test run has completed and we aborted because the client disconnected

        // do nothing
    }

    public Task<bool> LaunchTestHostAsync(TestProcessStartInfo testHostStartInfo, CancellationToken cancellationToken)
    {
        if (TestHostProcess.TestProcessStartInfo.FileName != testHostStartInfo.FileName)
            throw new InvalidOperationException($"Tried to start a different process than the one associated with this provider: File name is {testHostStartInfo.FileName} is not the same as the fake process associated with this provider {TestHostProcess.TestProcessStartInfo.FileName}.");

        FakeProcessHelper.StartFakeProcess(TestHostProcess);

        if (HostLaunched != null)
        {
            HostLaunched(this, new HostProviderEventArgs("Fake testhost launched", 0, TestHostProcess.Id));
        }
        return Task.FromResult(true);
    }

    public void SetCustomLauncher(ITestHostLauncher customLauncher)
    {
        throw new NotImplementedException();
    }
}
