// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.CommandLine;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;

public static class GenerateFakesUtilities
{
    internal static void GenerateFakesSettings(CommandLineOptions? commandLineOptions, IDictionary<string, Framework> sourceToFrameworkMap, ref string runSettingsXml)
    {
        // dynamically compute the fakes datacollector settings
        // This runs with or without design mode.
        if (commandLineOptions == null || !commandLineOptions.DisableAutoFakes)
        {
            runSettingsXml = FakesUtilities.GenerateFakesSettingsForRunConfiguration(sourceToFrameworkMap, runSettingsXml);
        }
    }
}
