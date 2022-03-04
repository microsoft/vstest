// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD1_0

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.Utilities;

// !!! FEATURES MUST BE KEPT IN SYNC WITH https://github.com/dotnet/sdk/blob/main/src/Cli/dotnet/commands/dotnet-test/VSTestFeatureFlag.cs !!!
internal sealed class FeatureFlag : IFeatureFlag
{
    private static readonly Dictionary<string, bool> FeatureFlags = new();

    private const string VSTEST_FEATURE = nameof(VSTEST_FEATURE);

    public static IFeatureFlag Instance { get; } = new FeatureFlag();

    static FeatureFlag()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        Reset();
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Obsolete("Use this only from tests, and ctor.")]
    internal static void Reset()
    {
        FeatureFlags.Clear();
        FeatureFlags.Add(ARTIFACTS_POSTPROCESSING, true);
        FeatureFlags.Add(ARTIFACTS_POSTPROCESSING_SDK_KEEP_OLD_UX, false);
        FeatureFlags.Add(FORCE_DATACOLLECTORS_ATTACHMENTPROCESSORS, false);
        FeatureFlags.Add(MULTI_TFM_RUN, true);
    }

    [Obsolete("Use this only from tests.")]
    internal static void SetFlag(string name, bool value)
    {
        if (!FeatureFlags.ContainsKey(name))
            throw new ArgumentException($"Feature flag {name} is not a known feature flag.");
        
        FeatureFlags[name] = value;
    }

    // Added for artifact post-processing, it enable/disable the post processing.
    // Added in 17.2-preview 7.0-preview
    public const string ARTIFACTS_POSTPROCESSING = VSTEST_FEATURE + "_" + nameof(ARTIFACTS_POSTPROCESSING);

    // Added for artifact post-processing, it will show old output for dotnet sdk scenario.
    // It can be useful if we need to restore old UX in case users are parsing the console output.
    // Added in 17.2-preview 7.0-preview
    public const string ARTIFACTS_POSTPROCESSING_SDK_KEEP_OLD_UX = VSTEST_FEATURE + "_" + nameof(ARTIFACTS_POSTPROCESSING_SDK_KEEP_OLD_UX);

    // Allow vstest.console to sources from multiple TFMs
    public const string MULTI_TFM_RUN = VSTEST_FEATURE + "_" + nameof(MULTI_TFM_RUN);

    // Temporary used to allow tests to work
    public const string FORCE_DATACOLLECTORS_ATTACHMENTPROCESSORS = VSTEST_FEATURE + "_" + "FORCE_DATACOLLECTORS_ATTACHMENTPROCESSORS";

    // For now we're checking env var.
    // We could add it also to some section inside the runsettings.
    public bool IsEnabled(string featureName) =>
        int.TryParse(Environment.GetEnvironmentVariable(featureName), out int enabled) ?
        enabled == 1 :
        FeatureFlags.TryGetValue(featureName, out bool isEnabled) && isEnabled;
}

#endif
