// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.StartTestRunner
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// 
    /// </summary>
    public class StartTestRunnerEventsHandler : IStartTestRunnerEventsHandler
    {
        private readonly ICommunicationManager communicationManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="communicationManager"></param>
        public StartTestRunnerEventsHandler(ICommunicationManager communicationManager)
        {
            this.communicationManager = communicationManager;
        }

        /// <inheritdoc />
        public void HandleStartTestRunnerComplete(Session session)
        {
            var ackPayload = new StartTestRunnerAckPayload()
            {
                Session = session
            };

            this.communicationManager.SendMessage(MessageType.StartTestRunnerCallback, ackPayload);
        }

        /// <inheritdoc />
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            // No-op.
        }

        /// <inheritdoc />
        public void HandleRawMessage(string rawMessage)
        {
            // No-op.
        }
    }
}
