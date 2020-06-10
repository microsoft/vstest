// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.MultiTestRunsFinalization
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using System.Collections.Generic;

    /// <summary>
    /// The multi test finalization event handler.
    /// </summary>
    public class MultiTestRunsFinalizationEventsHandler : IMultiTestRunsFinalizationEventsHandler
    {
        private ICommunicationManager communicationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTestRunsFinalizationEventsHandler"/> class.
        /// </summary>
        /// <param name="requestHandler"> The Request Handler. </param>
        public MultiTestRunsFinalizationEventsHandler(ICommunicationManager communicationManager)
        {
            this.communicationManager = communicationManager;
        }

        /// <summary>
        /// The handle discovery message.
        /// </summary>
        /// <param name="level"> Logging level. </param>
        /// <param name="message"> Logging message. </param>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            var testMessagePayload = new TestMessagePayload { MessageLevel = level, Message = message };
            this.communicationManager.SendMessage(MessageType.TestMessage, testMessagePayload);
        }

        public void HandleMultiTestRunsFinalizationComplete(ICollection<AttachmentSet> attachments)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Multi test runs finalization completed.");
            }

            var payload = new MultiTestRunsFinalizationCompletePayload()
            {
                Attachments = attachments
            };

            // Send run complete to translation layer
            this.communicationManager.SendMessage(MessageType.MultiTestRunsFinalizationComplete, payload);
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No-Op
        }
    }
}
