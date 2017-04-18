// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
    using Microsoft.VisualStudio.TestPlatform.Utilities;


    internal class ParallelDataCollectionEventsHandler : ParallelRunEventsHandler
    {
        private readonly ParallelRunDataAggregator runDataAggregator;

        public ParallelDataCollectionEventsHandler(IProxyExecutionManager proxyExecutionManager, 
            ITestRunEventsHandler actualRunEventsHandler, 
            IParallelProxyExecutionManager parallelProxyExecutionManager, 
            ParallelRunDataAggregator runDataAggregator) : 
            this(proxyExecutionManager, actualRunEventsHandler, parallelProxyExecutionManager, runDataAggregator, JsonDataSerializer.Instance)
        {
        }

        internal ParallelDataCollectionEventsHandler(IProxyExecutionManager proxyExecutionManager,
            ITestRunEventsHandler actualRunEventsHandler,
            IParallelProxyExecutionManager parallelProxyExecutionManager,
            ParallelRunDataAggregator runDataAggregator,
            IDataSerializer dataSerializer) : 
            base(proxyExecutionManager, actualRunEventsHandler, parallelProxyExecutionManager, runDataAggregator, dataSerializer)
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
                var coverageHandler = new CodeCoverageDataAttachmentsHandler();
                var attachments = coverageHandler.HandleDataCollectionAttachmentSets(runDataAggregator.RunContextAttachments);

                var completedArgs = new TestRunCompleteEventArgs(this.runDataAggregator.GetAggregatedRunStats(),
                    this.runDataAggregator.IsCanceled,
                    this.runDataAggregator.IsAborted,
                    this.runDataAggregator.GetAggregatedException(),
                    new Collection<AttachmentSet>(attachments.ToArray()),
                    runDataAggregator.ElapsedTime);

                HandleParallelTestRunComplete(completedArgs);
            }
        }
    }
}
