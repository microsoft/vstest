﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeTestHostLauncher : ITestHostLauncher
{
    private readonly FakeProcessHelper _fakeProcessHelper;

    public FakeTestHostLauncher(FakeProcessHelper fakeProcessHelper, bool isDebug = false)
    {
        IsDebug = isDebug;
        _fakeProcessHelper = fakeProcessHelper;
    }

    public bool IsDebug { get; }

    public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo)
    {
        throw new NotImplementedException();
    }

    public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
