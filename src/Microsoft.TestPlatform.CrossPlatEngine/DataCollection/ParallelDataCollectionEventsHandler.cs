using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
#if !NET46
using System.Runtime.Loader;
#endif
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.Client.Parallel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection
{
    internal class ParallelDataCollectionEventsHandler : ParallelRunEventsHandler
    {
        private readonly ParallelRunDataAggregator runDataAggregator;

        private readonly IDataSerializer dataSerializer;

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
            this.dataSerializer = dataSerializer;
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
                // todo : Merge Code Coverage files here
                // todo: Iterate over avaible datacollecter attachement handlers get final list of attachments
                // ICollection<AttachmentSet>  attachments = new List<AttachmentSet>();
                var completedArgs = new TestRunCompleteEventArgs(this.runDataAggregator.GetAggregatedRunStats(),
                    this.runDataAggregator.IsCanceled,
                    this.runDataAggregator.IsAborted,
                    this.runDataAggregator.GetAggregatedException(),
                    new Collection<AttachmentSet>(new List<AttachmentSet>().ToArray()),
                    runDataAggregator.ElapsedTime);

                HandleParallelTestRunComplete(completedArgs);
            }
        }

        public override void HandleRawMessage(string rawMessage)
        {
            // In case of parallel - we can send everything but handle complete
            // HandleComplete is not true-end of the overall execution as we only get completion of one executor here
            // Always aggregate data, deserialize and raw for complete events
            var message = this.dataSerializer.DeserializeMessage(rawMessage);

            // Do not deserialize further - just send if not execution complete
            if (!string.Equals(MessageType.ExecutionComplete, message.MessageType))
            {
                base.HandleRawMessage(rawMessage);
            }
        }
    }
}
