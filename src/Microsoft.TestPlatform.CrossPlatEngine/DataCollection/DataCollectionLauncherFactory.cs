﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;

using System;

using Interfaces;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;

/// <summary>
/// Factory for creating DataCollectionLauncher
/// </summary>
internal static class DataCollectionLauncherFactory
{
    /// <summary>
    /// The get data collector launcher.
    /// </summary>
    /// <returns>
    /// The <see cref="IDataCollectionLauncher"/>.
    /// </returns>
    internal static IDataCollectionLauncher GetDataCollectorLauncher(IProcessHelper processHelper, string settingsXml)
    {
        // Always launch datacollector.exe if code coverage is configured.
        if (!string.IsNullOrWhiteSpace(settingsXml))
        {
            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(settingsXml);
            foreach (var dataCollectorSettings in dataCollectionRunSettings.DataCollectorSettingsList)
            {
                if (string.Equals(dataCollectorSettings.FriendlyName, "event Log", StringComparison.OrdinalIgnoreCase) || string.Equals(dataCollectorSettings.Uri?.ToString(), @"datacollector://Microsoft/EventLog/2.0", StringComparison.OrdinalIgnoreCase))
                {
                    return new DefaultDataCollectionLauncher();
                }
            }
        }

        // Target Framework of DataCollection process and Runner should be same.
        var currentProcessPath = processHelper.GetCurrentProcessFileName();

        return currentProcessPath.EndsWith("dotnet", StringComparison.OrdinalIgnoreCase)
               || currentProcessPath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase)
            ? new DotnetDataCollectionLauncher()
            : new DefaultDataCollectionLauncher();
    }
}
