// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

/// <summary>
/// Interface defining contract for custom test host implementations
/// </summary>
public interface IInternalTestHostLauncher
{
    /// <summary>
    /// Gets a value indicating whether this is a debug launcher.
    /// </summary>
    bool IsDebug { get; }

    /// <summary>
    /// Launches custom test host using the default test process start info
    /// </summary>
    /// <param name="defaultTestHostStartInfo">Default TestHost Process Info</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>Process id of the launched test host</returns>
    int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo, CancellationToken cancellationToken);

    // new in this version
    bool AttachDebuggerToProcess(AttachDebuggerInfo attachDebuggerInfo, CancellationToken cancellationToken);
}



