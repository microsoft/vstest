// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode
{
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    internal class IdentifiableDesignModeTestEventsRegistrar : ITestDiscoveryEventsRegistrar, ITestRunEventsRegistrar
    {
        private string testRunId;
        private IDesignModeClient designModeClient;

        public IdentifiableDesignModeTestEventsRegistrar(IDesignModeClient designModeClient, string testRunId)
        {
            this.testRunId = testRunId;
            this.designModeClient = designModeClient;
        }


        public void RegisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
        {
            discoveryRequest.OnRawMessageReceived += OnRawMessageReceived;
        }

        public void UnregisterDiscoveryEvents(IDiscoveryRequest discoveryRequest)
        {
            discoveryRequest.OnRawMessageReceived -= OnRawMessageReceived;
        }

        public void RegisterTestRunEvents(ITestRunRequest testRunRequest)
        {
            testRunRequest.OnRawMessageReceived += OnRawMessageReceived;
        }

        public void UnregisterTestRunEvents(ITestRunRequest testRunRequest)
        {
            testRunRequest.OnRawMessageReceived -= OnRawMessageReceived;
        }

        /// <summary>
        /// RawMessage received handler for getting rawmessages directly from the host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="rawMessage">RawMessage from the testhost</param>
        private void OnRawMessageReceived(object sender, string rawMessage)
        {
            // TODO: protocol version?
            this.designModeClient.SendRawMessage(rawMessage, new MessageMetadata(5, this.testRunId));
        }

        public void LogWarning(string message)
        {
            this.designModeClient.SendTestMessage(TestMessageLevel.Warning, message, new MessageMetadata(1, this.testRunId));
        }
    }
}
