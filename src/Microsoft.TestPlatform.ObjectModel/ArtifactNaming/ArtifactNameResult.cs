// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.ArtifactNaming;

/// <summary>
/// Result of resolving an artifact name. Contains the resolved path and metadata
/// about the resolution process (e.g., whether an existing file was detected).
/// </summary>
public sealed class ArtifactNameResult
{
    /// <summary>
    /// Gets the fully resolved, sanitized file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets a value indicating whether the resolved path already existed before resolution.
    /// When <see langword="true"/> with <see cref="CollisionBehavior.Overwrite"/>, the caller
    /// should log a warning before writing.
    /// </summary>
    public bool IsOverwrite { get; }

    /// <summary>
    /// Gets a value indicating whether this process created (and therefore owns) the
    /// output directory. When <see langword="false"/>, the directory was already present
    /// (possibly created by another test host in the same run, or a previous run).
    /// </summary>
    public bool IsDirectoryOwner { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtifactNameResult"/> class.
    /// </summary>
    public ArtifactNameResult(string filePath, bool isOverwrite, bool isDirectoryOwner)
    {
        FilePath = filePath;
        IsOverwrite = isOverwrite;
        IsDirectoryOwner = isDirectoryOwner;
    }

    /// <summary>
    /// Returns the resolved file path.
    /// </summary>
    public override string ToString() => FilePath;
}
