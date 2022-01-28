﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLineUtilities;

using System.Collections.Generic;
using System.Linq;

using CommandLine;

using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

public static class GenerateFakesUtilities
{
    internal static void GenerateFakesSettings(CommandLineOptions commandLineOptions, IEnumerable<string> sources, ref string runSettingsXml)
    {
        // dynamically compute the fakes datacollector settings
        // This runs with or without design mode.
        if (commandLineOptions == null || !commandLineOptions.DisableAutoFakes)
        {
            runSettingsXml = FakesUtilities.GenerateFakesSettingsForRunConfiguration(sources.ToArray(), runSettingsXml);
        }
    }
}