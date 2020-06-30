// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.MultiTestRunFinalization
{
    /// <summary>
    /// Orchestrates multi test run finalization operations.
    /// </summary>
    public class MultiTestRunFinalizationManager : IMultiTestRunFinalizationManager
    {
        private static string FinalizationCompleted = "Completed";
        private static string FinalizationCanceled = "Canceled";
        private static string FinalizationFailed = "Failed";

        private readonly ITestPlatformEventSource testPlatformEventSource;
        private readonly IDataCollectorAttachmentProcessor[] dataCollectorAttachmentsProcessors;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTestRunFinalizationManager"/> class.
        /// </summary>
        public MultiTestRunFinalizationManager(ITestPlatformEventSource testPlatformEventSource, params IDataCollectorAttachmentProcessor[] dataCollectorAttachmentsProcessors)
        {
            this.testPlatformEventSource = testPlatformEventSource ?? throw new ArgumentNullException(nameof(testPlatformEventSource));
            this.dataCollectorAttachmentsProcessors = dataCollectorAttachmentsProcessors ?? throw new ArgumentNullException(nameof(dataCollectorAttachmentsProcessors));
        }

        /// <inheritdoc/>
        public async Task FinalizeMultiTestRunAsync(IRequestData requestData, IEnumerable<AttachmentSet> attachments, IMultiTestRunFinalizationEventsHandler eventHandler, CancellationToken cancellationToken)
        {
            await InternalFinalizeMultiTestRunAsync(requestData, new Collection<AttachmentSet>(attachments.ToList()), eventHandler, cancellationToken).ConfigureAwait(false);
        }
        /// <inheritdoc/>
        public Task<Collection<AttachmentSet>> FinalizeMultiTestRunAsync(IRequestData requestData, IEnumerable<AttachmentSet> attachments, CancellationToken cancellationToken)
        {
            return InternalFinalizeMultiTestRunAsync(requestData, new Collection<AttachmentSet>(attachments.ToList()), null, cancellationToken);
        }

        private async Task<Collection<AttachmentSet>> InternalFinalizeMultiTestRunAsync(IRequestData requestData, Collection<AttachmentSet> attachments, IMultiTestRunFinalizationEventsHandler eventHandler, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                testPlatformEventSource.MultiTestRunFinalizationStart(attachments?.Count ?? 0);
                requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfAttachmentsSentForFinalization, attachments?.Count ?? 0);
                
                cancellationToken.ThrowIfCancellationRequested();                

                var taskCompletionSource = new TaskCompletionSource<Collection<AttachmentSet>>();
                using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled()))
                {
                    Task<Collection<AttachmentSet>> task = Task.Run(async () => await ProcessAttachmentsAsync(new Collection<AttachmentSet>(attachments.ToList()), eventHandler, cancellationToken));

                    var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                    if (completedTask == task)
                    {
                        return FinalizeOperation(requestData, new MultiTestRunFinalizationCompleteEventArgs(false, null), await task, stopwatch, eventHandler);
                    }
                    else
                    {
                        eventHandler?.HandleLogMessage(TestMessageLevel.Informational, "Finalization was cancelled.");
                        return FinalizeOperation(requestData, new MultiTestRunFinalizationCompleteEventArgs(true, null), attachments, stopwatch, eventHandler);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("MultiTestRunFinalizationManager: operation was cancelled.");
                }
                return FinalizeOperation(requestData, new MultiTestRunFinalizationCompleteEventArgs(true, null), attachments, stopwatch, eventHandler);
            }
            catch (Exception e)
            {
                EqtTrace.Error("MultiTestRunFinalizationManager: Exception in FinalizeMultiTestRunAsync: " + e);

                eventHandler?.HandleLogMessage(TestMessageLevel.Error, e.Message);
                return FinalizeOperation(requestData, new MultiTestRunFinalizationCompleteEventArgs(false, e), attachments, stopwatch, eventHandler);
            }
        }

        private async Task<Collection<AttachmentSet>> ProcessAttachmentsAsync(Collection<AttachmentSet> attachments, IMultiTestRunFinalizationEventsHandler eventsHandler, CancellationToken cancellationToken)
        {
            if (attachments == null || !attachments.Any()) return attachments;

            var logger = CreateMessageLogger(eventsHandler);

            for (int i = 0; i < dataCollectorAttachmentsProcessors.Length; i++)
            {
                var dataCollectorAttachmentsProcessor = dataCollectorAttachmentsProcessors[i];
                int attachmentsHandlerIndex = i + 1;

                ICollection<Uri> attachementProcessorUris = dataCollectorAttachmentsProcessor.GetExtensionUris()?.ToList();
                if (attachementProcessorUris != null && attachementProcessorUris.Any())
                {
                    var attachmentsToBeProcessed = attachments.Where(dataCollectionAttachment => attachementProcessorUris.Any(uri => uri.Equals(dataCollectionAttachment.Uri))).ToArray();
                    if (attachmentsToBeProcessed.Any())
                    {
                        foreach (var attachment in attachmentsToBeProcessed)
                        {
                            attachments.Remove(attachment);
                        }

                        IProgress<int> progressReporter = new Progress<int>((int progress) => 
                            eventsHandler?.HandleMultiTestRunFinalizationProgress(
                                new MultiTestRunFinalizationProgressEventArgs(attachmentsHandlerIndex, attachementProcessorUris, progress, dataCollectorAttachmentsProcessors.Length)));

                        ICollection<AttachmentSet> processedAttachments = await dataCollectorAttachmentsProcessor.ProcessAttachmentSetsAsync(new Collection<AttachmentSet>(attachmentsToBeProcessed), progressReporter, logger, cancellationToken).ConfigureAwait(false);

                        foreach (var attachment in processedAttachments)
                        {
                            attachments.Add(attachment);
                        }
                    }
                }
            }

            return attachments;
        }

        private Collection<AttachmentSet> FinalizeOperation(IRequestData requestData, MultiTestRunFinalizationCompleteEventArgs completeArgs, Collection<AttachmentSet> attachments, Stopwatch stopwatch, IMultiTestRunFinalizationEventsHandler eventHandler)
        {            
            testPlatformEventSource.MultiTestRunFinalizationStop(attachments.Count);
            requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfAttachmentsAfterFinalization, attachments.Count);
            requestData.MetricsCollection.Add(TelemetryDataConstants.FinalizationState, completeArgs.Error != null ? FinalizationFailed : completeArgs.IsCanceled ? FinalizationCanceled : FinalizationCompleted);

            stopwatch.Stop();
            requestData.MetricsCollection.Add(TelemetryDataConstants.TimeTakenInSecForFinalization, stopwatch.Elapsed.TotalSeconds);

            completeArgs.Metrics = requestData.MetricsCollection.Metrics;
            eventHandler?.HandleMultiTestRunFinalizationComplete(completeArgs, attachments);

            return attachments;
        }

        private IMessageLogger CreateMessageLogger(IMultiTestRunFinalizationEventsHandler eventsHandler)
        {
            return eventsHandler != null ? (IMessageLogger)new FinalizationMessageLogger(eventsHandler) : new NullMessageLogger();
        }

        private class FinalizationMessageLogger : IMessageLogger
        {
            private readonly IMultiTestRunFinalizationEventsHandler eventsHandler;

            public FinalizationMessageLogger(IMultiTestRunFinalizationEventsHandler eventsHandler)
            {
                this.eventsHandler = eventsHandler ?? throw new ArgumentNullException(nameof(eventsHandler));
            }

            public void SendMessage(TestMessageLevel testMessageLevel, string message)
            {
                eventsHandler.HandleLogMessage(testMessageLevel, message);
            }
        }

        private class NullMessageLogger : IMessageLogger
        {
            public void SendMessage(TestMessageLevel testMessageLevel, string message)
            {
            }
        }
    }
}
