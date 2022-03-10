// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode;

using Common.Interfaces;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

using ObjectModel.Logging;

/// <summary>
/// Registers the discovery and test run events for design mode flow
/// </summary>
public class DesignModeTestEventsRegistrar : ITestDiscoveryEventsRegistrar, ITestRunEventsRegistrar
{
    private readonly IDesignModeClient _designModeClient;

    public DesignModeTestEventsRegistrar(IDesignModeClient designModeClient)
    {
        _designModeClient = designModeClient;
    }

    #region ITestDiscoveryEventsRegistrar

    public void RegisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
    {
        discoveryRequest.OnRawMessageReceived += OnRawMessageReceived;
    }

    public void UnregisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
    {
        discoveryRequest.OnRawMessageReceived -= OnRawMessageReceived;
    }

    #endregion

    #region ITestRunEventsRegistrar

    public void RegisterTestRunEvents(ITestRunRequest testRunRequest)
    {
        testRunRequest.OnRawMessageReceived += OnRawMessageReceived;
    }

    public void UnregisterTestRunEvents(ITestRunRequest testRunRequest)
    {
        testRunRequest.OnRawMessageReceived -= OnRawMessageReceived;
    }

    #endregion

    /// <summary>
    /// RawMessage received handler for getting rawmessages directly from the host
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="rawMessage">RawMessage from the testhost</param>
    private void OnRawMessageReceived(object sender, string rawMessage)
    {
        // Directly send the data to translation layer instead of de-serializing it here
        _designModeClient.SendRawMessage(rawMessage);
    }

    public void LogWarning(string message)
    {
        _designModeClient.SendTestMessage(TestMessageLevel.Warning, message);
    }
}
