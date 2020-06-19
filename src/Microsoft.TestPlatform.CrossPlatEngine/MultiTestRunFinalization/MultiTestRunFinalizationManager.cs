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
        private readonly IDataCollectorAttachments[] dataCollectorAttachmentsHandlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTestRunFinalizationManager"/> class.
        /// </summary>
        public MultiTestRunFinalizationManager(ITestPlatformEventSource testPlatformEventSource, params IDataCollectorAttachments[] dataCollectorAttachmentsHandlers)
        {
            this.testPlatformEventSource = testPlatformEventSource ?? throw new ArgumentNullException(nameof(testPlatformEventSource));
            this.dataCollectorAttachmentsHandlers = dataCollectorAttachmentsHandlers ?? throw new ArgumentNullException(nameof(dataCollectorAttachmentsHandlers));
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
                    Task<Collection<AttachmentSet>> task = Task.Run(() =>
                    {
                        return ProcessAttachments(new Collection<AttachmentSet>(attachments.ToList()), new ProgressReporter(eventHandler, dataCollectorAttachmentsHandlers.Length), cancellationToken);
                    });

                    var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                    if (completedTask == task)
                    {
                        return FinalizeOperation(requestData, await task, eventHandler, FinalizationCompleted);
                    }
                    else
                    {
                        eventHandler?.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Informational, "Finalization was cancelled.");
                        return FinalizeOperation(requestData, attachments, eventHandler, FinalizationCanceled);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("MultiTestRunFinalizationManager: operation was cancelled.");
                }
                return FinalizeOperation(requestData, attachments, eventHandler, FinalizationCanceled);
            }
            catch (Exception e)
            {
                EqtTrace.Error("MultiTestRunFinalizationManager: Exception in FinalizeMultiTestRunAsync: " + e);

                eventHandler?.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Error, e.Message);
                return FinalizeOperation(requestData, attachments, eventHandler, FinalizationFailed);
            }
            finally
            {
                stopwatch.Stop();
                requestData.MetricsCollection.Add(TelemetryDataConstants.TimeTakenInSecForFinalization, stopwatch.Elapsed.TotalSeconds);
            }
        }

        private Collection<AttachmentSet> ProcessAttachments(Collection<AttachmentSet> attachments, ProgressReporter progressReporter, CancellationToken cancellationToken)
        {
            if (attachments == null || !attachments.Any()) return attachments;           

            foreach (var dataCollectorAttachmentsHandler in dataCollectorAttachmentsHandlers)
            {
                Uri attachementUri = dataCollectorAttachmentsHandler.GetExtensionUri();
                if (attachementUri != null)
                {
                    var attachmentsToBeProcessed = attachments.Where(dataCollectionAttachment => attachementUri.Equals(dataCollectionAttachment.Uri)).ToArray();
                    if (attachmentsToBeProcessed.Any())
                    {
                        foreach (var attachment in attachmentsToBeProcessed)
                        {
                            attachments.Remove(attachment);
                        }

                        ICollection<AttachmentSet> processedAttachments = dataCollectorAttachmentsHandler.HandleDataCollectionAttachmentSets(new Collection<AttachmentSet>(attachmentsToBeProcessed), progressReporter, cancellationToken);
                        foreach (var attachment in processedAttachments)
                        {
                            attachments.Add(attachment);
                        }
                    }
                }
            }

            return attachments;
        }

        private Collection<AttachmentSet> FinalizeOperation(IRequestData requestData, Collection<AttachmentSet> attachments, IMultiTestRunFinalizationEventsHandler eventHandler, string finalizationState)
        {
            eventHandler?.HandleMultiTestRunFinalizationComplete(attachments);
            testPlatformEventSource.MultiTestRunFinalizationStop(attachments.Count);
            requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfAttachmentsAfterFinalization, attachments.Count);
            requestData.MetricsCollection.Add(TelemetryDataConstants.FinalizationState, finalizationState);

            return attachments;
        }

        private class ProgressReporter : IProgress<int>
        {
            private readonly IMultiTestRunFinalizationEventsHandler eventsHandler;
            private readonly int totalNumberOfHandlers;
            private int currentHandlerIndex;

            public ProgressReporter(IMultiTestRunFinalizationEventsHandler eventsHandler, int totalNumberOfHandlers)
            {
                this.eventsHandler = eventsHandler;
                this.currentHandlerIndex = 0;
                this.totalNumberOfHandlers = totalNumberOfHandlers;
            }

            public void IncremenetHandlerIndex()
            {
                currentHandlerIndex++;
            }

            public void Report(int value)
            {
                //eventsHandler.report( current, total, value)
            }
        }
    }
}
