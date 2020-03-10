// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers
{
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The test run events handler.
    /// </summary>
    public class TestInitializeEventsHandler : ITestMessageEventHandler
    {
        private ITestRequestHandler requestHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestInitializeEventsHandler"/> class.
        /// </summary>
        /// <param name="requestHandler">test request handler</param>
        public TestInitializeEventsHandler(ITestRequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        /// <summary>
        /// Handles a test run message.
        /// </summary>
        /// <param name="level"> The level. </param>
        /// <param name="message"> The message. </param>
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

        public void HandleRawMessage(string rawMessage)
        {
            // No-Op
            // TestHost at this point has no functionality where it requires rawmessage
        }
    }
}
