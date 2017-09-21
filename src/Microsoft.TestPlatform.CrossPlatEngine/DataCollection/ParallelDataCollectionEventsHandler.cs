// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    using System;
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

        public ParallelDataCollectionEventsHandler(IRequestData requestData,
            IProxyExecutionManager proxyExecutionManager, 
            ITestRunEventsHandler actualRunEventsHandler, 
            IParallelProxyExecutionManager parallelProxyExecutionManager, 
            ParallelRunDataAggregator runDataAggregator) : 
            this(requestData, proxyExecutionManager, actualRunEventsHandler, parallelProxyExecutionManager, runDataAggregator, JsonDataSerializer.Instance)
        {
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
                // todo: use TestPluginCache to iterate over all IDataCollectorAttachments
                {
                    var coverageHandler = new CodeCoverageDataAttachmentsHandler();
                    Uri attachementUri = coverageHandler.GetExtensionUri();
                    if (attachementUri != null)
                    {
                        var coverageAttachments = runDataAggregator.RunContextAttachments
                            .Where(dataCollectionAttachment => attachementUri.Equals(dataCollectionAttachment.Uri)).ToArray();

                        foreach (var coverageAttachment in coverageAttachments)
                        {
                            runDataAggregator.RunContextAttachments.Remove(coverageAttachment);
                        }

                        ICollection<AttachmentSet> attachments = coverageHandler.HandleDataCollectionAttachmentSets(new Collection<AttachmentSet>(coverageAttachments));
                        foreach (var attachment in attachments)
                        {
                            runDataAggregator.RunContextAttachments.Add(attachment);
                        }
                    }
                }

                var completedArgs = new TestRunCompleteEventArgs(this.runDataAggregator.GetAggregatedRunStats(),
                    this.runDataAggregator.IsCanceled,
                    this.runDataAggregator.IsAborted,
                    this.runDataAggregator.GetAggregatedException(),
                    this.runDataAggregator.RunContextAttachments,
                    this.runDataAggregator.ElapsedTime);

                HandleParallelTestRunComplete(completedArgs);
            }
        }
    }
}
