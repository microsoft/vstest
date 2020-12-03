// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Defines the way in which test session events should be handled.
    /// </summary>
    internal class TestSessionEventsHandler : ITestSessionEventsHandler
    {
        private readonly ICommunicationManager communicationManager;

        /// <summary>
        /// Creates an instance of the current class.
        /// </summary>
        /// 
        /// <param name="communicationManager">
        /// The communication manager used for passing messages around.
        /// </param>
        public TestSessionEventsHandler(ICommunicationManager communicationManager)
        {
            this.communicationManager = communicationManager;
        }

        /// <inheritdoc />
        public void HandleStartTestSessionComplete(TestSessionInfo testSessionInfo)
        {
            var ackPayload = new StartTestSessionAckPayload()
            {
                TestSessionInfo = testSessionInfo
            };

            this.communicationManager.SendMessage(MessageType.StartTestSessionCallback, ackPayload);
        }

        /// <inheritdoc />
        public void HandleStopTestSessionComplete(TestSessionInfo testSessionInfo, bool stopped)
        {
            var ackPayload = new StopTestSessionAckPayload()
            {
                TestSessionInfo = testSessionInfo,
                IsStopped = stopped
            };

            this.communicationManager.SendMessage(MessageType.StopTestSessionCallback, ackPayload);
        }

        /// <inheritdoc />
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            var messagePayload = new TestMessagePayload()
            {
                MessageLevel = level,
                Message = message
            };

            this.communicationManager.SendMessage(MessageType.TestMessage, messagePayload);
        }

        /// <inheritdoc />
        public void HandleRawMessage(string rawMessage)
        {
            // No-op.
        }
    }
}
