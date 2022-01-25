// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
        private readonly IProxyDataCollectionManager proxyDataCollectionManager;
        private readonly ITestRunEventsHandler testRunEventsHandler;
        private CancellationToken cancellationToken;
        private readonly IDataSerializer dataSerializer;
        private Collection<AttachmentSet> dataCollectionAttachmentSets;
        private Collection<InvokedDataCollector> invokedDataCollectors;

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
            : this(baseTestRunEventsHandler, proxyDataCollectionManager, JsonDataSerializer.Instance, cancellationToken)
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
        public DataCollectionTestRunEventsHandler(ITestRunEventsHandler baseTestRunEventsHandler, IProxyDataCollectionManager proxyDataCollectionManager, IDataSerializer dataSerializer, CancellationToken cancellationToken)
        {
            this.proxyDataCollectionManager = proxyDataCollectionManager;
            testRunEventsHandler = baseTestRunEventsHandler;
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
            testRunEventsHandler.HandleLogMessage(level, message);
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
            var message = dataSerializer.DeserializeMessage(rawMessage);

            if (string.Equals(MessageType.ExecutionComplete, message.MessageType))
            {
                var dataCollectionResult = proxyDataCollectionManager?.AfterTestRunEnd(cancellationToken.IsCancellationRequested, this);
                dataCollectionAttachmentSets = dataCollectionResult?.Attachments;

                var testRunCompletePayload =
                            dataSerializer.DeserializePayload<TestRunCompletePayload>(message);

                if (dataCollectionAttachmentSets != null && dataCollectionAttachmentSets.Any())
                {
                    GetCombinedAttachmentSets(
                        testRunCompletePayload.TestRunCompleteArgs.AttachmentSets,
                        dataCollectionAttachmentSets);
                }

                invokedDataCollectors = dataCollectionResult?.InvokedDataCollectors;
                if (invokedDataCollectors?.Count > 0)
                {
                    foreach (var dataCollector in invokedDataCollectors)
                    {
                        testRunCompletePayload.TestRunCompleteArgs.InvokedDataCollectors.Add(dataCollector);
                    }
                }

                rawMessage = dataSerializer.SerializePayload(
                    MessageType.ExecutionComplete,
                    testRunCompletePayload);
            }

            testRunEventsHandler.HandleRawMessage(rawMessage);
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
            if (dataCollectionAttachmentSets != null && dataCollectionAttachmentSets.Any())
            {
                runContextAttachments = GetCombinedAttachmentSets(dataCollectionAttachmentSets, runContextAttachments);
            }

            // At the moment, we don't support running data collectors inside testhost process, so it will always be empty inside "TestRunCompleteEventArgs testRunCompleteArgs".
            // We load invoked data collectors from data collector process inside "DataCollectionTestRunEventsHandler.HandleRawMessage" method.
            if (invokedDataCollectors != null && invokedDataCollectors.Any())
            {
                foreach (var dataCollector in invokedDataCollectors)
                {
                    testRunCompleteArgs.InvokedDataCollectors.Add(dataCollector);
                }
            }

            testRunEventsHandler.HandleTestRunComplete(testRunCompleteArgs, lastChunkArgs, runContextAttachments, executorUris);
        }

        /// <summary>
        /// The handle test run stats change.
        /// </summary>
        /// <param name="testRunChangedArgs">
        /// The test run changed args.
        /// </param>
        public void HandleTestRunStatsChange(TestRunChangedEventArgs testRunChangedArgs)
        {
            testRunEventsHandler.HandleTestRunStatsChange(testRunChangedArgs);
        }

        /// <summary>
        /// Launches a process with a given process info under debugger
        /// Adapter get to call into this to launch any additional processes under debugger
        /// </summary>
        /// <param name="testProcessStartInfo">Process start info</param>
        /// <returns>ProcessId of the launched process</returns>
        public int LaunchProcessWithDebuggerAttached(TestProcessStartInfo testProcessStartInfo)
        {
            return testRunEventsHandler.LaunchProcessWithDebuggerAttached(testProcessStartInfo);
        }

        /// <inheritdoc />
        public bool AttachDebuggerToProcess(int pid)
        {
            return ((ITestRunEventsHandler2)testRunEventsHandler).AttachDebuggerToProcess(pid);
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
            if (newAttachments == null || newAttachments.Count == 0)
            {
                return originalAttachmentSets;
            }

            if (originalAttachmentSets == null)
            {
                return new Collection<AttachmentSet>(newAttachments.ToList());
            }

            foreach (var attachmentSet in newAttachments)
            {
                var attSet = originalAttachmentSets.FirstOrDefault(item => Equals(item.Uri, attachmentSet.Uri));
                if (attSet == null)
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