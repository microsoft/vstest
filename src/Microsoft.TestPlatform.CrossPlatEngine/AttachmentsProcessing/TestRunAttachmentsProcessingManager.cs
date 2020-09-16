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

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing
{
    /// <summary>
    /// Orchestrates test run attachments processing operations.
    /// </summary>
    public class TestRunAttachmentsProcessingManager : ITestRunAttachmentsProcessingManager
    {
        private static string AttachmentsProcessingCompleted = "Completed";
        private static string AttachmentsProcessingCanceled = "Canceled";
        private static string AttachmentsProcessingFailed = "Failed";

        private readonly ITestPlatformEventSource testPlatformEventSource;
        private readonly IDataCollectorAttachmentProcessor[] dataCollectorAttachmentsProcessors;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunAttachmentsProcessingManager"/> class.
        /// </summary>
        public TestRunAttachmentsProcessingManager(ITestPlatformEventSource testPlatformEventSource, params IDataCollectorAttachmentProcessor[] dataCollectorAttachmentsProcessors)
        {
            this.testPlatformEventSource = testPlatformEventSource ?? throw new ArgumentNullException(nameof(testPlatformEventSource));
            this.dataCollectorAttachmentsProcessors = dataCollectorAttachmentsProcessors ?? throw new ArgumentNullException(nameof(dataCollectorAttachmentsProcessors));
        }

        /// <inheritdoc/>
        public async Task ProcessTestRunAttachmentsAsync(IRequestData requestData, IEnumerable<AttachmentSet> attachments, ITestRunAttachmentsProcessingEventsHandler eventHandler, CancellationToken cancellationToken)
        {
            await InternalProcessTestRunAttachmentsAsync(requestData, new Collection<AttachmentSet>(attachments.ToList()), eventHandler, cancellationToken).ConfigureAwait(false);
        }
        /// <inheritdoc/>
        public Task<Collection<AttachmentSet>> ProcessTestRunAttachmentsAsync(IRequestData requestData, IEnumerable<AttachmentSet> attachments, CancellationToken cancellationToken)
        {
            return InternalProcessTestRunAttachmentsAsync(requestData, new Collection<AttachmentSet>(attachments.ToList()), null, cancellationToken);
        }

        private async Task<Collection<AttachmentSet>> InternalProcessTestRunAttachmentsAsync(IRequestData requestData, Collection<AttachmentSet> attachments, ITestRunAttachmentsProcessingEventsHandler eventHandler, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                testPlatformEventSource.TestRunAttachmentsProcessingStart(attachments?.Count ?? 0);
                requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfAttachmentsSentForProcessing, attachments?.Count ?? 0);
                
                cancellationToken.ThrowIfCancellationRequested();                

                var taskCompletionSource = new TaskCompletionSource<Collection<AttachmentSet>>();
                using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled()))
                {
                    Task<Collection<AttachmentSet>> task = Task.Run(async () => await ProcessAttachmentsAsync(new Collection<AttachmentSet>(attachments.ToList()), eventHandler, cancellationToken));

                    var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                    if (completedTask == task)
                    {
                        return FinalizeOperation(requestData, new TestRunAttachmentsProcessingCompleteEventArgs(false, null), await task, stopwatch, eventHandler);
                    }
                    else
                    {
                        eventHandler?.HandleLogMessage(TestMessageLevel.Informational, "Attachments processing was cancelled.");
                        return FinalizeOperation(requestData, new TestRunAttachmentsProcessingCompleteEventArgs(true, null), attachments, stopwatch, eventHandler);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (EqtTrace.IsWarningEnabled)
                {
                    EqtTrace.Warning("TestRunAttachmentsProcessingManager: operation was cancelled.");
                }
                return FinalizeOperation(requestData, new TestRunAttachmentsProcessingCompleteEventArgs(true, null), attachments, stopwatch, eventHandler);
            }
            catch (Exception e)
            {
                EqtTrace.Error("TestRunAttachmentsProcessingManager: Exception in ProcessTestRunAttachmentsAsync: " + e);

                eventHandler?.HandleLogMessage(TestMessageLevel.Error, e.Message);
                return FinalizeOperation(requestData, new TestRunAttachmentsProcessingCompleteEventArgs(false, e), attachments, stopwatch, eventHandler);
            }
        }

        private async Task<Collection<AttachmentSet>> ProcessAttachmentsAsync(Collection<AttachmentSet> attachments, ITestRunAttachmentsProcessingEventsHandler eventsHandler, CancellationToken cancellationToken)
        {
            if (attachments == null || !attachments.Any()) return attachments;

            var logger = CreateMessageLogger(eventsHandler);

            for (int i = 0; i < dataCollectorAttachmentsProcessors.Length; i++)
            {
                var dataCollectorAttachmentsProcessor = dataCollectorAttachmentsProcessors[i];
                int attachmentsHandlerIndex = i + 1;

                ICollection<Uri> attachmentProcessorUris = dataCollectorAttachmentsProcessor.GetExtensionUris()?.ToList();
                if (attachmentProcessorUris != null && attachmentProcessorUris.Any())
                {
                    var attachmentsToBeProcessed = attachments.Where(dataCollectionAttachment => attachmentProcessorUris.Any(uri => uri.Equals(dataCollectionAttachment.Uri))).ToArray();
                    if (attachmentsToBeProcessed.Any())
                    {
                        foreach (var attachment in attachmentsToBeProcessed)
                        {
                            attachments.Remove(attachment);
                        }

                        IProgress<int> progressReporter = new Progress<int>((int progress) => 
                            eventsHandler?.HandleTestRunAttachmentsProcessingProgress(
                                new TestRunAttachmentsProcessingProgressEventArgs(attachmentsHandlerIndex, attachmentProcessorUris, progress, dataCollectorAttachmentsProcessors.Length)));

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

        private Collection<AttachmentSet> FinalizeOperation(IRequestData requestData, TestRunAttachmentsProcessingCompleteEventArgs completeArgs, Collection<AttachmentSet> attachments, Stopwatch stopwatch, ITestRunAttachmentsProcessingEventsHandler eventHandler)
        {            
            testPlatformEventSource.TestRunAttachmentsProcessingStop(attachments.Count);
            requestData.MetricsCollection.Add(TelemetryDataConstants.NumberOfAttachmentsAfterProcessing, attachments.Count);
            requestData.MetricsCollection.Add(TelemetryDataConstants.AttachmentsProcessingState, completeArgs.Error != null ? AttachmentsProcessingFailed : completeArgs.IsCanceled ? AttachmentsProcessingCanceled : AttachmentsProcessingCompleted);

            stopwatch.Stop();
            requestData.MetricsCollection.Add(TelemetryDataConstants.TimeTakenInSecForAttachmentsProcessing, stopwatch.Elapsed.TotalSeconds);

            completeArgs.Metrics = requestData.MetricsCollection.Metrics;
            eventHandler?.HandleTestRunAttachmentsProcessingComplete(completeArgs, attachments);

            return attachments;
        }

        private IMessageLogger CreateMessageLogger(ITestRunAttachmentsProcessingEventsHandler eventsHandler)
        {
            return eventsHandler != null ? (IMessageLogger)new AttachmentsProcessingMessageLogger(eventsHandler) : new NullMessageLogger();
        }

        private class AttachmentsProcessingMessageLogger : IMessageLogger
        {
            private readonly ITestRunAttachmentsProcessingEventsHandler eventsHandler;

            public AttachmentsProcessingMessageLogger(ITestRunAttachmentsProcessingEventsHandler eventsHandler)
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
