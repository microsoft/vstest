// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CommandLine;

namespace vstest.ProgrammerTests.CommandLine;

internal class VstestConsole
{
    public List<string> Sources { get; } = new();

    public List<string> Arguments { get; } = new();

    internal FakeOutput Output { get; } = new();

    internal VstestConsole WithSource(params string[] sources)
    {
        Sources.AddRange(sources);
        return this;
    }

    internal VstestConsole WithArguments(params string[] arguments)
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

internal static class StringExtensions
{
    public static string Join(this IEnumerable<string> value, string separator)
    {
        return string.Join(separator, value);
    }

    public static string JoinBySpace(this IEnumerable<string> value)
    {
        return string.Join(" ", value);
    }
}
