// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Utilities.Helpers;

using Microsoft.VisualStudio.TestPlatform.Utilities.Helpers.Interfaces;

/// <summary>
/// RunSettingsHelper is used to globally share additional informations about the state of runsettings.
/// </summary>
internal class RunSettingsHelper : IRunSettingsHelper
{
    private static readonly IRunSettingsHelper RunSettings = new RunSettingsHelper();

    public static IRunSettingsHelper Instance = RunSettings;

    /// <summary>
    /// If false user updated the RunConfiguration.TargetPlatform using
    /// --arch or runsettings file or -- RunConfiguration.TargetPlatform=arch
    /// </summary>
    public bool IsDefaultTargetArchitecture { get; set; } = true;
}
