// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System.Collections.Generic;
    using System.Linq;


    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
    using System.Collections.ObjectModel;
    using System;
    /// <summary>
    /// Handles DataCollection attachments by calling DataCollection Process on Test Run Complete. 
    /// Existing functionality of ITestRunEventsHandler is decorated with aditional call to Data Collection Process.
    /// </summary>
    internal class DataCollectionTestRunEventsHandler : ITestRunEventsHandler
    {
        private IProxyDataCollectionManager proxyDataCollectionManager;
        private ITestRunEventsHandler testRunEventsHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestRunEventsHandler"/> class.
        /// </summary>
        /// <param name="baseTestRunEventsHandler">
        /// The base test run events handler.
        /// </param>
        /// <param name="proxyExecutionManager">
        /// The proxy execution manager.
        /// </param>
        public DataCollectionTestRunEventsHandler(ITestRunEventsHandler baseTestRunEventsHandler, IProxyDataCollectionManager proxyDataCollectionManager)
        {
            this.proxyDataCollectionManager = proxyDataCollectionManager;
            this.testRunEventsHandler = baseTestRunEventsHandler;
        }

        /// <summary>
        /// The handle log message.
        /// </summary>
        /// <param name="level">
        /// The level.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public void HandleLogMessage(TestMessageLevel level, string message)
        {
            this.testRunEventsHandler.HandleLogMessage(level, message);
        }

        /// <summary>
        /// The handle raw message.
        /// </summary>
        /// <param name="rawMessage">
        /// The raw message.
        /// </param>
        public void HandleRawMessage(string rawMessage)
        {
            this.testRunEventsHandler.HandleRawMessage(rawMessage);
        }

        /// <summary>
        /// The handle test run complete.
        /// </summary>
        /// <param name="testRunCompleteArgs">
        /// The test run complete args.
        /// </param>
        /// <param name="lastChunkArgs">
        /// The last chunk args.
        /// </param>
        /// <param name="runContextAttachments">
        /// The run context attachments.
        /// </param>
        /// <param name="executorUris">
        /// The executor uris.
        /// </param>
        public void HandleTestRunComplete(TestRunCompleteEventArgs testRunCompleteArgs, TestRunChangedEventArgs lastChunkArgs, ICollection<AttachmentSet> runContextAttachments, ICollection<string> executorUris)
        {
            var attachmentSet = this.proxyDataCollectionManager?.AfterTestRunEnd(false, this);
            attachmentSet = DataCollectionTestRunEventsHandler.GetCombinedAttachmentSets(attachmentSet, runContextAttachments);
            this.testRunEventsHandler.HandleTestRunComplete(testRunCompleteArgs, lastChunkArgs, attachmentSet, executorUris);
        }

        /// <summary>
        /// The handle test run stats change.
        /// </summary>
        /// <param name="testRunChangedArgs">
        /// The test run changed args.
        /// </param>
        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            this.testRunEventsHandler.HandleTestRunStatsChange(testRunChangedArgs);
        }

        /// <summary>
        /// Launches a process with a given process info under debugger
        /// Adapter get to call into this to launch any additional processes under debugger
        /// </summary>
        /// <param name="testProcessStartInfo">Process start info</param>
        /// <returns>ProcessId of the launched process</returns>
        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            return this.testRunEventsHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);
        }

        /// <summary>
        /// The get combined attachment sets.
        /// </summary>
        /// <param name="runAttachments">
        /// The run attachments.
        /// </param>
        /// <param name="runcontextAttachments">
        /// The runcontext attachments.
        /// </param>
        /// <returns>
        /// The <see cref="Collection"/>.
        /// </returns>
        internal static Collection<AttachmentSet> GetCombinedAttachmentSets(Collection<AttachmentSet> runAttachments, ICollection<AttachmentSet> runcontextAttachments)
        {
            if (null == runcontextAttachments || runcontextAttachments.Count == 0)
            {
                return runAttachments;
            }

            if (null == runAttachments)
            {
                return new Collection<AttachmentSet>(runcontextAttachments.ToList());
            }

            foreach (var attachmentSet in runcontextAttachments)
            {
                var attSet = runAttachments.Where(item => Uri.Equals(item.Uri, attachmentSet.Uri)).FirstOrDefault();
                if (null == attSet)
                {
                    runAttachments.Add(attachmentSet);
                }
                else
                {
                    foreach (var attachment in attachmentSet.Attachments)
                    {
                        attSet.Attachments.Add(attachment);
                    }
                }
            }

            return runAttachments;
        }
    }
}