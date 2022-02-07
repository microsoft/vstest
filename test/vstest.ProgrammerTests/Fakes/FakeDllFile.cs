// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using vstest.ProgrammerTests.CommandLine.Fakes;

namespace vstest.ProgrammerTests.CommandLine;

internal class FakeDllFile : FakeFile
{
    public FrameworkName FrameworkName { get; init; }
    public Architecture Architecture { get; init; }

    public FakeDllFile(string path, FrameworkName frameworkName, Architecture architecture) : base(path)
    {
        FrameworkName = frameworkName;
        Architecture = architecture;
    }
}
