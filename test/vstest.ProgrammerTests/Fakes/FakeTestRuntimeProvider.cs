﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine.Fakes;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

internal class FakeTestRuntimeProvider : ITestRuntimeProvider
{
    public FakeProcessHelper FakeProcessHelper { get; }
    public FakeCommunicationEndpoint FakeCommunicationEndpoint { get; }
    public FakeProcess? TestHostProcess { get; private set; }

    // TODO: make this configurable?
    public bool Shared => false;

    public event EventHandler<HostProviderEventArgs> HostLaunched;
    public event EventHandler<HostProviderEventArgs> HostExited;

    public FakeTestRuntimeProvider(FakeProcessHelper fakeProcessHelper, FakeCommunicationEndpoint fakeCommunicationEndpoint, FakeErrorAggregator fakeErrorAggregator)
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
        return FakeCommunicationEndpoint.TestHostConnectionInfo;
    }

    public TestProcessStartInfo GetTestHostProcessStartInfo(IEnumerable<string> sources, IDictionary<string, string> environmentVariables, TestRunnerConnectionInfo connectionInfo)
    {
        // TODO: do we need to do more here? How to link testhost to the fake one we "start"?
        return new TestProcessStartInfo();
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
        TestHostProcess = (FakeProcess)FakeProcessHelper.LaunchProcess(
            testHostStartInfo.FileName,
            testHostStartInfo.Arguments,
            testHostStartInfo.WorkingDirectory,
            testHostStartInfo.EnvironmentVariables,
            errorCallback: (_, _) => { },
            exitCallBack: p => {
                var process = (FakeProcess)p;
                if (HostExited != null)
                {
                    // TODO: When we exit, eventually there are no subscribers, maybe we should review if we don't lose the error output sometimes, in unnecessary way
                    HostExited(this, new HostProviderEventArgs(process.ErrorOutput, process.ExitCode, process.Id));
                }
            },
            outputCallback: (_, _) => { }
            );
        HostLaunched(this, new HostProviderEventArgs("Fake testhost launched", 0, TestHostProcess.Id));
        return Task.FromResult(true);
    }

    public void SetCustomLauncher(ITestHostLauncher customLauncher)
    {
        throw new NotImplementedException();
    }
}
