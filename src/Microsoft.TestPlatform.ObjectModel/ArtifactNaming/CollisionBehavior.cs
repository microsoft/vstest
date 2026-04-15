// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.ArtifactNaming;

/// <summary>
/// Defines how to handle file name collisions when the resolved artifact path already exists.
/// </summary>
public enum CollisionBehavior
{
    /// <summary>
    /// Append an incrementing counter suffix: file_2.trx, file_3.trx, etc.
    /// </summary>
    AppendCounter,

    /// <summary>
    /// Overwrite the existing file.
    /// </summary>
    Overwrite,

    /// <summary>
    /// Fail with an error if the path already exists.
    /// </summary>
    Fail,

    /// <summary>
    /// Append a timestamp suffix to make the name unique.
    /// </summary>
    AppendTimestamp,
}
