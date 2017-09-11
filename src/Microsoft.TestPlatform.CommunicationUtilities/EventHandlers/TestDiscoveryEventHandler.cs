// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using TestPlatform.ObjectModel.Client;

    /// <summary>
    /// The test discovery event handler.
    /// </summary>
    public class TestDiscoveryEventHandler : ITestDiscoveryEventsHandler2
    {
        private ITestRequestHandler requestHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDiscoveryEventHandler"/> class.
        /// </summary>
        /// <param name="requestHandler"> The client. </param>
        public TestDiscoveryEventHandler(ITestRequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        /// <summary>
        /// Handles discovered tests
        /// </summary>
        /// <param name="discoveredTestCases">List of test cases</param>
        public void HandleDiscoveredTests(IEnumerable<TestCase> discoveredTestCases)
        {
            EqtTrace.Info("Test Cases found ");
            this.requestHandler.SendTestCases(discoveredTestCases);
        }

        /// <summary>
        /// Handle discovery complete.
        /// </summary>
        /// <param name="discoveryCompleteEventArgs"> Discovery Compelete Events Args. </param>
        /// <param name="lastChunk"> The last chunk. </param>
        public void HandleDiscoveryComplete(DiscoveryCompleteEventArgs discoveryCompleteEventArgs, IEnumerable<TestCase> lastChunk)
        {
            if (EqtTrace.IsInfoEnabled)
            {
                EqtTrace.Info(discoveryCompleteEventArgs.IsAborted ? "Discover Aborted." : "Discover Finished.");
            }

            this.requestHandler.DiscoveryComplete(discoveryCompleteEventArgs, lastChunk);
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

        public void HandleRawMessage(string rawMessage)
        {
            // No-Op
            // TestHost at this point has no functionality where it requires rawmessage
        }
    }
}
