// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE1006 // Naming Styles
namespace vstest.console.UnitTests.Processors;
#pragma warning restore IDE1006 // Naming Styles

using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;

internal class TestableRunSettingsProvider : IRunSettingsProvider
{
    public RunSettings ActiveRunSettings
    {
        get;
        set;
    }

    public void SetActiveRunSettings(RunSettings runSettings)
    {
        ActiveRunSettings = runSettings;
    }
}