// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommandLine.Processors;

using System.Runtime.Versioning;

using ObjectModel;

/// <summary>
/// Defines interface for assembly properties.
/// </summary>
internal interface IAssemblyMetadataProvider
{
    /// <summary>
    /// Determines FrameworkName from filePath.
    /// </summary>
    FrameworkName GetFrameWork(string filePath);

    /// <summary>
    /// Determines Architecture from filePath.
    /// </summary>
    Architecture GetArchitecture(string filePath);
}