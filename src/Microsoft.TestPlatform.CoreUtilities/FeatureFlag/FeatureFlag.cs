// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

#if !NETSTANDARD1_0

namespace Microsoft.VisualStudio.TestPlatform.Utilities;
// !!! FEATURES MUST BE KEPT IN SYNC WITH https://github.com/dotnet/sdk/blob/main/src/Cli/dotnet/commands/dotnet-test/VSTestFeatureFlag.cs !!!
internal partial class FeatureFlag : IFeatureFlag
{
    private static readonly Dictionary<string, bool> FeatureFlags = new();

    private const string VSTEST_FEATURE = nameof(VSTEST_FEATURE);

    public static IFeatureFlag Instance { get; } = new FeatureFlag();

    static FeatureFlag()
    {
        FeatureFlags.Add(ARTIFACTS_POSTPROCESSING, true);
        FeatureFlags.Add(ARTIFACTS_POSTPROCESSING_SDK_KEEP_OLD_UX, false);
        FeatureFlags.Add(FORCE_DATACOLLECTORS_ATTACHMENTPROCESSORS, false);
    }

    // Added for artifact porst-processing, it enable/disable the post processing.
    // Added in 17.2-preview 7.0-preview
    public static string ARTIFACTS_POSTPROCESSING = VSTEST_FEATURE + "_" + "ARTIFACTS_POSTPROCESSING";

    // Added for artifact porst-processing, it will show old output for dotnet sdk scenario.
    // It can be useful if we need to restore old UX in case users are parsing the console output.
    // Added in 17.2-preview 7.0-preview
    public static string ARTIFACTS_POSTPROCESSING_SDK_KEEP_OLD_UX = VSTEST_FEATURE + "_" + "ARTIFACTS_POSTPROCESSING_SDK_KEEP_OLD_UX";

    // Temporary used to allow tests to work
    public static string FORCE_DATACOLLECTORS_ATTACHMENTPROCESSORS = VSTEST_FEATURE + "_" + "FORCE_DATACOLLECTORS_ATTACHMENTPROCESSORS";

    // For now we're checking env var.
    // We could add it also to some section inside the runsettings.
    public bool IsEnabled(string featureName) =>
        int.TryParse(Environment.GetEnvironmentVariable(featureName), out int enabled) ?
        enabled == 1 :
        FeatureFlags.TryGetValue(featureName, out bool isEnabled) && isEnabled;
}

#endif
