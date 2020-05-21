// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The multi test finalization event handler.
    /// </summary>
    public class MultiTestRunsFinalizationEventsHandler : IMultiTestRunsFinalizationEventsHandler
    {
        private ITestRequestHandler requestHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTestRunsFinalizationEventsHandler"/> class.
        /// </summary>
        /// <param name="requestHandler"> The Request Handler. </param>
        public MultiTestRunsFinalizationEventsHandler(ITestRequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        /// <summary>
        /// The handle discovery message.
        /// </summary>
        /// <param name="level"> Logging level. </param>
        /// <param name="message"> Logging message. </param>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            switch ((TestMessageLevel)level)
            {
                case TestMessageLevel.Informational:
                    EqtTrace.Info(message);
                    break;

                case TestMessageLevel.Warning:
                    EqtTrace.Warning(message);
                    break;

                case TestMessageLevel.Error:
                    EqtTrace.Error(message);
                    break;

                default:
                    EqtTrace.Info(message);
                    break;
            }

            this.requestHandler.SendLog(level, message);
        }

        public void HandleMultiTestRunsFinalizationComplete(ICollection<AttachmentSet> attachments)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info("Multi test runs finalization completed.");
            }

            this.requestHandler.MultiTestRunsFinalizationComplete(attachments);
        }

        public void HandleRawMessage(string rawMessage)
        {
            // No-Op
            // TestHost at this point has no functionality where it requires rawmessage
        }
    }
}
