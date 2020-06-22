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
    /// The multi test finalization events handler.
    /// </summary>
    public class MultiTestRunFinalizationEventsHandler : IMultiTestRunFinalizationEventsHandler
    {
        private readonly ICommunicationManager communicationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTestRunFinalizationEventsHandler"/> class.
        /// </summary>
        /// <param name="communicationManager"> The communication manager. </param>
        public MultiTestRunFinalizationEventsHandler(ICommunicationManager communicationManager)
        {
            this.communicationManager = communicationManager;
        }

        /// <inheritdoc/>
        public void HandleMultiTestRunFinalizationComplete(MultiTestRunFinalizationCompleteEventArgs finalizationCompleteEventArgs, IEnumerable<AttachmentSet> lastChunk)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Multi test run finalization completed.");
            }

            var payload = new MultiTestRunFinalizationCompletePayload()
            {
                FinalizationCompleteEventArgs = finalizationCompleteEventArgs,
                Attachments = lastChunk
            };

            this.communicationManager.SendMessage(MessageType.MultiTestRunFinalizationComplete, payload);
        }

        /// <inheritdoc/>
        public void HandleMultiTestRunFinalizationProgress(MultiTestRunFinalizationProgressEventArgs finalizationProgressEventArgs)
        {
            var payload = new MultiTestRunFinalizationProgressPayload()
            {
                FinalizationProgressEventArgs = finalizationProgressEventArgs,
            };

            this.communicationManager.SendMessage(MessageType.MultiTestRunFinalizationProgress, payload);
        }

        /// <inheritdoc/>
        public void HandleFinalisedAttachments(IEnumerable<AttachmentSet> attachments)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            var testMessagePayload = new TestMessagePayload { MessageLevel = level, Message = message };
            this.communicationManager.SendMessage(MessageType.TestMessage, testMessagePayload);
        }

        /// <inheritdoc/>
        public void HandleRawMessage(string rawMessage)
        {
            // No-Op
        }
    }
}
