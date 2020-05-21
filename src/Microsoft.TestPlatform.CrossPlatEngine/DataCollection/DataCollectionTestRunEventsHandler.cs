// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// Handles DataCollection attachments by calling DataCollection Process on Test Run Complete.
    /// Existing functionality of ITestRunEventsHandler is decorated with additional call to Data Collection Process.
    /// </summary>
    internal class DataCollectionTestRunEventsHandler : ITestRunEventsHandler2
    {
        private IProxyDataCollectionManager proxyDataCollectionManager;
        private ITestRunEventsHandler testRunEventsHandler;
        private CancellationToken cancellationToken;
        private IDataSerializer dataSerializer;
        private Collection<AttachmentSet> dataCollectionAttachmentSets;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestRunEventsHandler"/> class.
        /// </summary>
        /// <param name="baseTestRunEventsHandler">
        /// The base test run events handler.
        /// </param>
        /// <param name="proxyDataCollectionManager">
        /// The proxy Data Collection Manager.
        /// </param>
        public DataCollectionTestRunEventsHandler(ITestRunEventsHandler baseTestRunEventsHandler, IProxyDataCollectionManager proxyDataCollectionManager, CancellationToken cancellationToken)
            : this(baseTestRunEventsHandler, proxyDataCollectionManager, cancellationToken, JsonDataSerializer.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionTestRunEventsHandler"/> class.
        /// </summary>
        /// <param name="baseTestRunEventsHandler">
        /// The base test run events handler.
        /// </param>
        /// <param name="proxyDataCollectionManager">
        /// The proxy Data Collection Manager.
        /// </param>
        /// <param name="dataSerializer">
        /// The data Serializer.
        /// </param>
        public DataCollectionTestRunEventsHandler(ITestRunEventsHandler baseTestRunEventsHandler, IProxyDataCollectionManager proxyDataCollectionManager, CancellationToken cancellationToken, IDataSerializer dataSerializer)
        {
            this.proxyDataCollectionManager = proxyDataCollectionManager;
            this.testRunEventsHandler = baseTestRunEventsHandler;
            this.cancellationToken = cancellationToken;
            this.dataSerializer = dataSerializer;
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
            // In case of data collection, data collection attachments should be attached to raw message for ExecutionComplete 
            var message = this.dataSerializer.DeserializeMessage(rawMessage);

            if (string.Equals(MessageType.ExecutionComplete, message.MessageType))
            {
                this.dataCollectionAttachmentSets = this.proxyDataCollectionManager?.AfterTestRunEnd(this.cancellationToken.IsCancellationRequested, this);

                if (this.dataCollectionAttachmentSets != null && this.dataCollectionAttachmentSets.Any())
                {
                    var testRunCompletePayload =
                            this.dataSerializer.DeserializePayload<TestRunCompletePayload>(message);

                    GetCombinedAttachmentSets(
                        testRunCompletePayload.TestRunCompleteArgs.AttachmentSets,
                        this.dataCollectionAttachmentSets);

                    rawMessage = this.dataSerializer.SerializePayload(
                        MessageType.ExecutionComplete,
                        testRunCompletePayload);
                }
            }

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
            if (this.dataCollectionAttachmentSets != null && this.dataCollectionAttachmentSets.Any())
            {
                runContextAttachments = DataCollectionTestRunEventsHandler.GetCombinedAttachmentSets(this.dataCollectionAttachmentSets, runContextAttachments);
            }

            this.testRunEventsHandler.HandleTestRunComplete(testRunCompleteArgs, lastChunkArgs, runContextAttachments, executorUris);
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

        /// <inheritdoc />
        public bool AttachDebuggerToProcess(int pid)
        {
            return ((ITestRunEventsHandler2)this.testRunEventsHandler).AttachDebuggerToProcess(pid);
        }

        /// <summary>
        /// The get combined attachment sets.
        /// </summary>
        /// <param name="originalAttachmentSets">
        /// The run attachments.
        /// </param>
        /// <param name="newAttachments">
        /// The run context attachments.
        /// </param>
        /// <returns>
        /// The <see cref="Collection"/>.
        /// </returns>
        internal static ICollection<AttachmentSet> GetCombinedAttachmentSets(Collection<AttachmentSet> originalAttachmentSets, ICollection<AttachmentSet> newAttachments)
        {
            if (null == newAttachments || newAttachments.Count == 0)
            {
                return originalAttachmentSets;
            }

            if (null == originalAttachmentSets)
            {
                return new Collection<AttachmentSet>(newAttachments.ToList());
            }

            foreach (var attachmentSet in newAttachments)
            {
                var attSet = originalAttachmentSets.Where(item => Uri.Equals(item.Uri, attachmentSet.Uri)).FirstOrDefault();
                if (null == attSet)
                {
                    originalAttachmentSets.Add(attachmentSet);
                }
                else
                {
                    foreach (var attachment in attachmentSet.Attachments)
                    {
                        attSet.Attachments.Add(attachment);
                    }
                }
            }

            return originalAttachmentSets;
        }
    }
}