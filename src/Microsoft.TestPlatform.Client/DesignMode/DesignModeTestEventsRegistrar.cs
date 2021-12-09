// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.DesignMode
{
    using Microsoft.VisualStudio.TestPlatform.Common.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Registers the discovery and test run events for design mode flow
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
            // Directly send the data to translation layer instead of de-serializing it here
            this.designModeClient.SendRawMessage(rawMessage);
        }

        public void LogWarning(string message)
        {
            this.designModeClient.SendTestMessage(TestMessageLevel.Warning, message);
        }
    }

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
            // rawMessage = rawMessage.Replace($"\"{nameof(TestRunCompleteEventArgs.TestRunId)}\":null", $"\"{nameof(TestRunCompleteEventArgs.TestRunId)}\":{($"\"{this.testRunId}\"") ?? "null"}");
            this.designModeClient.SendRawMessage(rawMessage);
        }

        public void LogWarning(string message)
        {
            this.designModeClient.SendTestMessage(TestMessageLevel.Warning, message);
        }
    }
}
