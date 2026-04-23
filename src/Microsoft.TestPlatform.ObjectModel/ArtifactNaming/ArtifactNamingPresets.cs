// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.ArtifactNaming;

/// <summary>
/// Built-in artifact naming presets that define directory and file template pairs.
/// </summary>
public static class ArtifactNamingPresets
{
    /// <summary>CI preset: flat output folder, deterministic names, overwrite on rerun.</summary>
    public const string CI = "ci";

    /// <summary>Local preset: per-run timestamped subfolder, deterministic file names.</summary>
    public const string Local = "local";

    /// <summary>Detailed preset: per-run folder with run ID in file names.</summary>
    public const string Detailed = "detailed";

    /// <summary>Flat preset: minimal file names, one per assembly.</summary>
    public const string Flat = "flat";

    /// <summary>
    /// Gets the directory and file templates for a named preset.
    /// </summary>
    /// <param name="presetName">The preset name (case-insensitive).</param>
    /// <param name="directoryTemplate">The directory template for the preset.</param>
    /// <param name="fileTemplate">The file template for the preset.</param>
    /// <param name="collision">The collision behavior for the preset.</param>
    /// <returns><see langword="true"/> if the preset was found; otherwise <see langword="false"/>.</returns>
    public static bool TryGetPreset(
        string presetName,
        out string directoryTemplate,
        out string fileTemplate,
        out CollisionBehavior collision)
    {
        switch (presetName.ToLowerInvariant())
        {
            case CI:
                directoryTemplate = "{" + ArtifactNameTokens.TestResultsDirectory + "}";
                fileTemplate = "{" + ArtifactNameTokens.AssemblyName + "}_{" + ArtifactNameTokens.Tfm + "}_{" + ArtifactNameTokens.Architecture + "}";
                collision = CollisionBehavior.Overwrite;
                return true;

            case Local:
                directoryTemplate = "{" + ArtifactNameTokens.TestResultsDirectory + "}/{" + ArtifactNameTokens.Timestamp + "}";
                fileTemplate = "{" + ArtifactNameTokens.AssemblyName + "}_{" + ArtifactNameTokens.Tfm + "}_{" + ArtifactNameTokens.Architecture + "}";
                collision = CollisionBehavior.AppendCounter;
                return true;

            case Detailed:
                directoryTemplate = "{" + ArtifactNameTokens.TestResultsDirectory + "}/{" + ArtifactNameTokens.Timestamp + "}";
                fileTemplate = "{" + ArtifactNameTokens.AssemblyName + "}_{" + ArtifactNameTokens.Tfm + "}_{" + ArtifactNameTokens.Architecture + "}_{" + ArtifactNameTokens.RunId + "}";
                collision = CollisionBehavior.AppendCounter;
                return true;

            case Flat:
                directoryTemplate = "{" + ArtifactNameTokens.TestResultsDirectory + "}";
                fileTemplate = "{" + ArtifactNameTokens.AssemblyName + "}";
                collision = CollisionBehavior.AppendCounter;
                return true;

            default:
                directoryTemplate = string.Empty;
                fileTemplate = string.Empty;
                collision = CollisionBehavior.AppendCounter;
                return false;
        }
    }

    /// <summary>
    /// Gets all known preset names.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[] { CI, Local, Detailed, Flat };
}
