// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode
{
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Registers the discovery and test run events for designmode flow
    /// </summary>
    public class DesignModeTestEventsRegistrar : ITestDiscoveryEventsRegistrar, ITestRunEventsRegistrar
    {
        private IDesignModeClient designModeClient;

        public DesignModeTestEventsRegistrar(IDesignModeClient designModeClient)
        {
            this.designModeClient = designModeClient;
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
            // Directly send the data to translation layer instead of deserializing it here
            this.designModeClient.SendRawMessage(rawMessage);
        }
    }
}
