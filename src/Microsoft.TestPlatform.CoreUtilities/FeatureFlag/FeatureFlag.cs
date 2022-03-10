// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD1_0

namespace Microsoft.VisualStudio.TestPlatform.Utilities;

using System;
using System.Collections.Generic;

internal partial class FeatureFlag : IFeatureFlag
{
    private static readonly Dictionary<string, bool> FeatureFlags = new();

    private const string Prefix = "VSTEST_FEATURE_";

    public static IFeatureFlag Instance => new FeatureFlag();

    static FeatureFlag()
    {
        FeatureFlags.Add(ARTIFACTS_POSTPROCESSING, false);
        FeatureFlags.Add(ARTIFACTS_POSTPROCESSING_SDK_KEEP_OLD_UX, false);
    }

    // Added for artifact porst-processing, it enable/disable the post processing.
    // Added in 17.2-preview 7.0-preview
    public static string ARTIFACTS_POSTPROCESSING = Prefix + "ARTIFACTS_POSTPROCESSING";

    // Added for artifact porst-processing, it will show old output for dotnet sdk scenario.
    // It can be useful if we need to restore old UX in case users are parsing the console output.
    // Added in 17.2-preview 7.0-preview
    public static string ARTIFACTS_POSTPROCESSING_SDK_KEEP_OLD_UX = Prefix + "ARTIFACTS_POSTPROCESSING_SDK_KEEP_OLD_UX";

    // For now we're checking env var.
    // We could add it also to some section inside the runsettings.
    public bool IsEnabled(string featureName) =>
        int.TryParse(Environment.GetEnvironmentVariable(featureName), out int enabled) ?
        enabled == 1 :
        FeatureFlags.TryGetValue(featureName, out bool isEnabled) && isEnabled;
}

#endif
