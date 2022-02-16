// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

using vstest.ProgrammerTests.CommandLine.Fakes;

internal class Fixture : IDisposable
{
    public FakeErrorAggregator ErrorAggregator { get; } = new();
    public FakeProcessHelper ProcessHelper { get; }
    public FakeProcess CurrentProcess { get; }
    public FakeFileHelper FileHelper { get; }

    public Fixture()
    {
        CurrentProcess = new FakeProcess(ErrorAggregator, @"X:\fake\vstest.console.exe", string.Empty, null, null, null, null, null);
        ProcessHelper = new FakeProcessHelper(ErrorAggregator, CurrentProcess);
        FileHelper = new FakeFileHelper(ErrorAggregator);

    }
    public void Dispose()
    {

    }
}
