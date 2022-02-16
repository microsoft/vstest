// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace vstest.ProgrammerTests.CommandLine;

using System;

internal class FakeTestHostBuilder
{
    // TODO: this would correctly be any test holding container, but let's not get ahead of myself.
    private List<FakeTestDllFile> _dlls = new();

    public FakeTestHostBuilder()
    {
    }

    internal FakeTestHostBuilder WithTestDll(FakeTestDllFile dll)
    {
        _dlls.Add(dll);
        return this;
    }
}
