// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


#pragma warning disable IDE1006 // Naming Styles
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Host;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;

namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeProcess
{
    public int Id { get; internal set; }
    public string Name { get; init; }
    public string Path { get; }
    public string Arguments { get; set; }

    public PlatformArchitecture Architecture { get; init; } = PlatformArchitecture.X64;
    public event EventHandler<int> ProcessExited = delegate { };

    public FakeProcess(string path, string arguments = null)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        Arguments = arguments;
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
}
