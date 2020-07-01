// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

    internal class ParallelDataCollectionEventsHandler : ParallelRunEventsHandler
    {
        private readonly ParallelRunDataAggregator runDataAggregator;
        private readonly ITestRunAttachmentsProcessingManager attachmentsProcessingManager;
        private readonly CancellationToken cancellationToken;

        public ParallelDataCollectionEventsHandler(IRequestData requestData,
            IProxyExecutionManager proxyExecutionManager,
            ITestRunEventsHandler actualRunEventsHandler,
            IParallelProxyExecutionManager parallelProxyExecutionManager,
            ParallelRunDataAggregator runDataAggregator,
            ITestRunAttachmentsProcessingManager attachmentsProcessingManager,
            CancellationToken cancellationToken) :
            this(requestData, proxyExecutionManager, actualRunEventsHandler, parallelProxyExecutionManager, runDataAggregator, JsonDataSerializer.Instance)
        {
            this.attachmentsProcessingManager = attachmentsProcessingManager;
            this.cancellationToken = cancellationToken;
        }

        internal ParallelDataCollectionEventsHandler(IRequestData requestData,
            IProxyExecutionManager proxyExecutionManager,
            ITestRunEventsHandler actualRunEventsHandler,
            IParallelProxyExecutionManager parallelProxyExecutionManager,
            ParallelRunDataAggregator runDataAggregator,
            IDataSerializer dataSerializer) :
            base(requestData, proxyExecutionManager, actualRunEventsHandler, parallelProxyExecutionManager, runDataAggregator, dataSerializer)
        {
            this.runDataAggregator = runDataAggregator;
        }

        /// <summary>
        /// Handles the Run Complete event from a parallel proxy manager
        /// </summary>
        public override void HandleTestRunComplete(
            TestRunCompleteEventArgs testRunCompleteArgs,
            TestRunChangedEventArgs lastChunkArgs,
            ICollection<AttachmentSet> runContextAttachments,
            ICollection<string> executorUris)
        {
            var parallelRunComplete = HandleSingleTestRunComplete(testRunCompleteArgs, lastChunkArgs, runContextAttachments, executorUris);

            if (parallelRunComplete)
            {
                runDataAggregator.RunContextAttachments = attachmentsProcessingManager.ProcessTestRunAttachmentsAsync(requestData, runDataAggregator.RunContextAttachments, cancellationToken).Result ?? runDataAggregator.RunContextAttachments;

                var completedArgs = new TestRunCompleteEventArgs(this.runDataAggregator.GetAggregatedRunStats(),
                    this.runDataAggregator.IsCanceled,
                    this.runDataAggregator.IsAborted,
                    this.runDataAggregator.GetAggregatedException(),
                    runDataAggregator.RunContextAttachments,
                    this.runDataAggregator.ElapsedTime);

                // Add Metrics from Test Host
                completedArgs.Metrics = this.runDataAggregator.GetAggregatedRunDataMetrics();

                HandleParallelTestRunComplete(completedArgs);
            }
        }
    }
}
