// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.VisualStudio.TestPlatform.CoreUtilities.Helpers;

/// <summary>
/// An enum representing the dotnet muxer resolution.
/// </summary>
[Flags]
public enum DotnetMuxerResolution
{
    /// <summary>
    /// Indicates if the muxer resolution process should take dotnet root into account.
    /// </summary>
    DotnetRootArchitecture = 1,

    /// <summary>
    /// Indicates if the muxer resolution process should take arch independent dotnet root into account.
    /// </summary>
    DotnetRootArchitectureLess = 2,

    /// <summary>
    /// Indicates if the muxer resolution process should look in the global installation location.
    /// </summary>
    GlobalInstallation = 4,

    /// <summary>
    /// Indicates if the muxer resolution process should look in the default installation location.
    /// </summary>
    DefaultInstallation = 8,

    /// <summary>
    /// All muxer resolution options should be taken into account.
    /// </summary>
    All = DotnetRootArchitecture | DotnetRootArchitectureLess | GlobalInstallation | DefaultInstallation,
}
