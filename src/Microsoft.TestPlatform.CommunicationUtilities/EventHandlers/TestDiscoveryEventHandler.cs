// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using TestPlatform.ObjectModel.Client;

    /// <summary>
    /// The test discovery event handler.
    /// </summary>
    public class TestDiscoveryEventHandler : ITestDiscoveryEventsHandler
    {
        private ITestRequestHandler requestHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDiscoveryEventHandler"/> class.
        /// </summary>
        /// <param name="client"> The client. </param>
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
        /// <param name="totalTests"> The total tests. </param>
        /// <param name="lastChunk"> The last chunk. </param>
        /// <param name="isAborted"> The is aborted. </param>
        public void HandleDiscoveryComplete(long totalTests, IEnumerable<TestCase> lastChunk, bool isAborted)
        {
            EqtTrace.Info(isAborted ? "Discover Aborted." : "Discover Finished.");
            this.requestHandler.DiscoveryComplete(totalTests, lastChunk, isAborted);
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
