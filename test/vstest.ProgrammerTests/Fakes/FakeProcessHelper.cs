﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeProcessHelper : IProcessHelper
{
    // starting from 100 for no particular reason
    // I want to avoid processId 0 and 1 as they are
    // "reserved" on Windows (0) and Linux (both 0 and 1)
    private int _lastProcessId = 100;

    public FakeProcess CurrentProcess { get; }
    public List<FakeProcess> Processes { get; } = new();
    public int LastProcessId => _lastProcessId;

    public FakeErrorAggregator FakeErrorAggregator { get; }

    public FakeProcessHelper(FakeProcess currentProcess, FakeErrorAggregator fakeErrorAggregator)
    {
        CurrentProcess = currentProcess;
        AddProcess(currentProcess);
        FakeErrorAggregator = fakeErrorAggregator;
    }

    private void AddProcess(FakeProcess currentProcess)
    {
        var id = Interlocked.Increment(ref _lastProcessId);
        currentProcess.SetId(id);
        Processes.Add(currentProcess);
    }

    public PlatformArchitecture GetCurrentProcessArchitecture()
    {
        return CurrentProcess.Architecture;
    }

    public string GetCurrentProcessFileName()
    {
        return CurrentProcess.Path;
    }

    public int GetCurrentProcessId()
    {
        return CurrentProcess.Id;
    }

    public string GetCurrentProcessLocation()
    {
        // TODO: how is this different from Path
        throw new NotImplementedException();
    }

    public string GetNativeDllDirectory()
    {
        throw new NotImplementedException();
    }

    public IntPtr GetProcessHandle(int processId)
    {
        throw new NotImplementedException();
    }

    public int GetProcessId(object process)
    {
        var fakeProcess = FakeProcess.EnsureFakeProcess(process);
        return fakeProcess.Id;
    }

    public string GetProcessName(int processId)
    {
        var process = Processes.Single(p => p.Id == processId);
        return process.Name;
    }

    public string GetTestEngineDirectory()
    {
        throw new NotImplementedException();
    }

    public object LaunchProcess(string processPath, string arguments, string workingDirectory, IDictionary<string, string> environmentVariables, Action<object, string> errorCallback, Action<object> exitCallBack, Action<object, string> outputCallback)
    {
        throw new NotImplementedException();
    }

    public void SetExitCallback(int processId, Action<object> callbackAction)
    {
        throw new NotImplementedException();
    }

    public void TerminateProcess(object process)
    {
        throw new NotImplementedException();
    }

    public bool TryGetExitCode(object process, out int exitCode)
    {
        throw new NotImplementedException();
    }

    public void WaitForProcessExit(object process)
    {
        throw new NotImplementedException();
    }
}
