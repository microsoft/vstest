// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Discovery;

using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Execution;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;

#nullable disable

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;

/// <summary>
/// The factory that provides discovery and execution managers to the test host.
/// </summary>
public class TestHostManagerFactory : ITestHostManagerFactory
{
    private IDiscoveryManager _discoveryManager;
    private IExecutionManager _executionManager;
    private readonly IRequestData _requestData;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestHostManagerFactory"/> class.
    /// </summary>
    /// <param name="requestData">
    /// Provide common services and data for a discovery/run request.
    /// </param>
    public TestHostManagerFactory(IRequestData requestData!!)
    {
        _requestData = requestData;
    }

    /// <summary>
    /// The discovery manager instance for any discovery related operations inside of the test host.
    /// </summary>
    /// <returns>The discovery manager.</returns>
    public IDiscoveryManager GetDiscoveryManager()
        => _discoveryManager ??= new DiscoveryManager(_requestData);

    /// <summary>
    /// The execution manager instance for any discovery related operations inside of the test host.
    /// </summary>
    /// <returns>The execution manager.</returns>
    public IExecutionManager GetExecutionManager()
        => _executionManager ??= new ExecutionManager(_requestData);
}
