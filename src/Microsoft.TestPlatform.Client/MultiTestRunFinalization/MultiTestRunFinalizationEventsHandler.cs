// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.Client.MultiTestRunFinalization
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
    public class MultiTestRunFinalizationEventsHandler : IMultiTestRunFinalizationEventsHandler
    {
        private readonly ICommunicationManager communicationManager;
        private bool finalizationCompleteSent;
        private readonly object syncObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTestRunFinalizationEventsHandler"/> class.
        /// </summary>
        /// <param name="requestHandler"> The Request Handler. </param>
        public MultiTestRunFinalizationEventsHandler(ICommunicationManager communicationManager)
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

        public void HandleMultiTestRunFinalizationComplete(ICollection<AttachmentSet> attachments)
        {
            lock(this.syncObject)
            {
                if(!finalizationCompleteSent)
                {
                    if (EqtTrace.IsInfoEnabled)
                    {
                        EqtTrace.Info("Multi test run finalization completed.");
                    }

                    var payload = new MultiTestRunFinalizationCompletePayload()
                    {
                        Attachments = attachments
                    };

                    // Send run complete to translation layer
                    this.communicationManager.SendMessage(MessageType.MultiTestRunFinalizationComplete, payload);

                    finalizationCompleteSent = true;
                }
            }
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No-Op
        }
    }
}
