// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode;

using System.Threading;

using ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;

/// <summary>
/// DesignMode TestHost Launcher for hosting of test process
/// </summary>
internal class DesignModeTestHostLauncher : ITestHostLauncher2
{
    private readonly IDesignModeClient _designModeClient;
    private readonly string _recipient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DesignModeTestHostLauncher"/> class.
    /// </summary>
    /// <param name="designModeClient">Design mode client instance.</param>
    public DesignModeTestHostLauncher(IDesignModeClient designModeClient, string recipient)
    {
        _designModeClient = designModeClient;
        _recipient = recipient;
    }

    /// <inheritdoc/>
    public virtual bool IsDebug => false;

    /// <inheritdoc/>
    public bool AttachDebuggerToProcess(int pid)
    {
        return _designModeClient.AttachDebuggerToProcess(pid, _recipient, CancellationToken.None);
    }

    /// <inheritdoc/>
    public bool AttachDebuggerToProcess(int pid, CancellationToken cancellationToken)
    {
        return _designModeClient.AttachDebuggerToProcess(pid, _recipient, cancellationToken);
    }

    /// <inheritdoc/>
    public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo)
    {
        return _designModeClient.LaunchCustomHost(defaultTestHostStartInfo, CancellationToken.None);
    }

    /// <inheritdoc/>
    public int LaunchTestHost(TestProcessStartInfo defaultTestHostStartInfo, CancellationToken cancellationToken)
    {
        return _designModeClient.LaunchCustomHost(defaultTestHostStartInfo, cancellationToken);
    }
}

/// <summary>
/// DesignMode Debug Launcher to use if debugging enabled
/// </summary>
internal class DesignModeDebugTestHostLauncher : DesignModeTestHostLauncher
{
    /// <inheritdoc/>
    public DesignModeDebugTestHostLauncher(IDesignModeClient designModeClient, string recipient) : base(designModeClient, recipient)
    {
    }

    /// <inheritdoc/>
    public override bool IsDebug => true;
}
