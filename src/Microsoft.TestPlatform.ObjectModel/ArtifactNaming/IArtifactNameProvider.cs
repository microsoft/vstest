// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.ArtifactNaming;

/// <summary>
/// Resolves artifact name templates to concrete, sanitized file paths.
/// All artifacts are files.
/// </summary>
public interface IArtifactNameProvider
{
    /// <summary>
    /// Resolves a template to a concrete, sanitized, collision-safe file path.
    /// The result includes metadata about whether the file already existed (overwrite)
    /// and whether this process owns the output directory.
    /// </summary>
    /// <param name="request">The artifact name request containing template, extension, context, and collision behavior.</param>
    /// <returns>The resolution result containing the file path and ownership metadata.</returns>
    ArtifactNameResult Resolve(ArtifactNameRequest request);

    /// <summary>
    /// Expands a template string by replacing <c>{TokenName}</c> placeholders with values
    /// from the provided context. Does not perform sanitization or collision handling.
    /// </summary>
    /// <param name="template">The template string with <c>{TokenName}</c> placeholders.</param>
    /// <param name="context">Token name-value pairs for expansion.</param>
    /// <returns>The expanded string. Unknown tokens are kept literally (e.g., <c>{Unknown}</c>).</returns>
    string ExpandTemplate(string template, IReadOnlyDictionary<string, string> context);
}
