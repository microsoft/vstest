// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeProcess
{
    public int Id { get; internal set; }
    public string Name { get; init; }
    public string Path { get; }
    public string Arguments { get; set; }
    public string WorkingDirectory { get; }
    public IDictionary<string, string> EnvironmentVariables { get; }
    public Action<object, string> ErrorCallback { get; }
    public Action<object> ExitCallback { get; }
    public Action<object, string> OutputCallback { get; }
    public PlatformArchitecture Architecture { get; init; } = PlatformArchitecture.X64;
    public FakeErrorAggregator FakeErrorAggregator { get; }
    public string? ErrorOutput { get; init; }
    public int ExitCode { get; init; } = -1;
    public bool Exited { get; private set; }

    public FakeProcess(string path, string arguments, string workingDirectory, IDictionary<string, string> environmentVariables, Action<object, string> errorCallback, Action<object> exitCallBack, Action<object, string> outputCallback, FakeErrorAggregator fakeErrorAggregator)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        Arguments = arguments;
        WorkingDirectory = workingDirectory;
        EnvironmentVariables = environmentVariables;
        ErrorCallback = errorCallback;
        ExitCallback = exitCallBack;
        OutputCallback = outputCallback;
        FakeErrorAggregator = fakeErrorAggregator;
    }

    internal static FakeProcess EnsureFakeProcess(object process)
    {
        return (FakeProcess)process;
    }

    internal void SetId(int id)
    {
        if (Id != 0)
            throw new InvalidOperationException($"Cannot set Id to {id} for fake process {Name}, {Id}, because it was already set.");

        Id = id;
    }

    internal void Exit()
    {
        // We want to call the exit callback just once. This is behavior inherent to being a real process,
        // that also exits only once.
        var exited = Exited;
        Exited = true;
        if (!exited && ExitCallback != null)
        {
            ExitCallback(this);
        }
    }
}
