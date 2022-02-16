// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

using Microsoft.VisualStudio.TestPlatform.CommandLine;
using vstest.ProgrammerTests.CommandLine.Fakes;

internal class VstestConsoleBuilder
{
    public List<string> Sources { get; } = new();

    public List<string> Arguments { get; } = new();

    internal FakeOutput Output { get; } = new();

    internal VstestConsoleBuilder WithSource(params string[] sources)
    {
        Sources.AddRange(sources);
        return this;
    }

    internal VstestConsoleBuilder WithArguments(params string[] arguments)
    {
        Arguments.AddRange(arguments);
        return this;
    }

    internal void Execute()
    {
        var commandLine = new[] { Sources, Arguments }.SelectMany(s => s).JoinBySpace();
        // vstest.console
        var console = new Executor(Output).Execute(commandLine);
    }
}
