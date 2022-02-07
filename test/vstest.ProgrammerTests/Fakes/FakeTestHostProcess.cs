// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeTestHostProcess : FakeProcess
{
    public FakeTestHostProcess(string commandLine) : base(commandLine)
    {
    }

    public CapturedRunSettings? RunSettings { get; internal set; }
}
