// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.ArtifactNaming;

/// <summary>
/// Describes a request to resolve an artifact file name from a template.
/// </summary>
public sealed class ArtifactNameRequest
{
    /// <summary>
    /// Gets or sets the file name template (without directory, without extension).
    /// Uses <c>{TokenName}</c> placeholders for token expansion.
    /// </summary>
    /// <example><c>{AssemblyName}_{Tfm}_{Architecture}</c></example>
    public string FileTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file extension including the leading dot.
    /// </summary>
    /// <example><c>.trx</c>, <c>.xml</c>, <c>.coverage</c></example>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token values available for template expansion.
    /// </summary>
    public IReadOnlyDictionary<string, string> Context { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the collision behavior when the resolved path already exists.
    /// Defaults to <see cref="CollisionBehavior.AppendCounter"/>.
    /// </summary>
    public CollisionBehavior Collision { get; set; } = CollisionBehavior.AppendCounter;

    /// <summary>
    /// Gets or sets the optional directory template. When <see langword="null"/>, the value of
    /// <see cref="ArtifactNameTokens.TestResultsDirectory"/> from <see cref="Context"/> is used.
    /// </summary>
    public string? DirectoryTemplate { get; set; }

    /// <summary>
    /// Gets or sets an optional artifact kind tag for diagnostics (e.g., "trx", "coverage", "blame").
    /// </summary>
    public string? ArtifactKind { get; set; }

    /// <summary>
    /// Gets or sets an optional producer name for diagnostics (e.g., "TrxLogger", "BlameCollector").
    /// </summary>
    public string? ProducerName { get; set; }
}
