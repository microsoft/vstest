// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.ProgrammerTests.CommandLine.Fakes;

internal class FakeTestExtensionManager : ITestExtensionManager
{
    public void ClearExtensions()
    {
        throw new NotImplementedException();
    }

    public void UseAdditionalExtensions(IEnumerable<string> pathToAdditionalExtensions, bool skipExtensionFilters)
    {
        throw new NotImplementedException();
    }
}
