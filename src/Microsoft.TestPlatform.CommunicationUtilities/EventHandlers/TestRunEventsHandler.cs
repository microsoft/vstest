// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The test run events handler.
    /// </summary>
    public class TestRunEventsHandler : ITestRunEventsHandler
    {
        private ITestRequestHandler requestHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunEventsHandler"/> class.
        /// </summary>
        /// <param name="client"> The client. </param>
        public TestRunEventsHandler(ITestRequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        /// <summary>
        /// Handle test run stats change.
        /// </summary>
        /// <param name="testRunChangedArgs"> The test run changed args. </param>
        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            EqtTrace.Info("Sending test run statistics");
            this.requestHandler.SendTestRunStatistics(testRunChangedArgs);
        }

        /// <summary>
        /// Handle test run complete.
        /// </summary>
        /// <param name="testRunCompleteArgs"> The test run complete args. </param>
        /// <param name="lastChunkArgs"> The last chunk args. </param>
        /// <param name="runContextAttachments"> The run context attachments. </param>
        /// <param name="executorUris"> The executor uris. </param>
        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
        {
            EqtTrace.Info("Sending test run complete");
            this.requestHandler.SendExecutionComplete(testRunCompleteArgs, lastChunkArgs, runContextAttachments, executorUris);
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

        /// <summary>
        /// Launches a process with a given process info under debugger
        /// Adapter get to call into this to launch any additional processes under debugger
        /// </summary>
        /// <param name="testProcessStartInfo">Process start info</param>
        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            EqtTrace.Info("Sending LaunchProcessWithDebuggerAttached on additional test process: {0}", testProcessStartInfo?.FileName);
            return this.requestHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);
        }
    }
}
