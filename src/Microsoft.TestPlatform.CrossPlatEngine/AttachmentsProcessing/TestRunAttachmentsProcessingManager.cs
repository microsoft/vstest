// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.Common.Telemetry;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.TestRunAttachmentsProcessing
{
    /// <summary>
    /// Orchestrates test run attachments processing operations.
    /// </summary>
    internal class TestRunAttachmentsProcessingManager : ITestRunAttachmentsProcessingManager
    {
        private static string AttachmentsProcessingCompleted = "Completed";
        private static string AttachmentsProcessingCanceled = "Canceled";
        private static string AttachmentsProcessingFailed = "Failed";

        private readonly ITestPlatformEventSource testPlatformEventSource;
        private readonly IDataCollectorAttachmentsProcessorsFactory dataCollectorAttachmentsProcessorsFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunAttachmentsProcessingManager"/> class.
        /// </summary>
        public TestRunAttachmentsProcessingManager(ITestPlatformEventSource testPlatformEventSource, IDataCollectorAttachmentsProcessorsFactory dataCollectorAttachmentsProcessorsFactory)
        {
            this.testPlatformEventSource = testPlatformEventSource ?? throw new ArgumentNullException(nameof(testPlatformEventSource));
            this.dataCollectorAttachmentsProcessorsFactory = dataCollectorAttachmentsProcessorsFactory ?? throw new ArgumentNullException(nameof(dataCollectorAttachmentsProcessorsFactory));
        }

        /// <inheritdoc/>
        public async Task ProcessTestRunAttachmentsAsync(string runSettingsXml, IRequestData requestData, IEnumerable<AttachmentSet> attachments, IEnumerable<InvokedDataCollector> invokedDataCollector, ITestRunAttachmentsProcessingEventsHandler eventHandler, CancellationToken cancellationToken)
        {
            await InternalProcessTestRunAttachmentsAsync(runSettingsXml, requestData, new Collection<AttachmentSet>(attachments.ToList()), invokedDataCollector, eventHandler, cancellationToken).ConfigureAwait(false);
        }
        /// <inheritdoc/>
        public Task<Collection<AttachmentSet>> ProcessTestRunAttachmentsAsync(string runSettingsXml, IRequestData requestData, IEnumerable<AttachmentSet> attachments, IEnumerable<InvokedDataCollector> invokedDataCollector, CancellationToken cancellationToken)
        {
            return InternalProcessTestRunAttachmentsAsync(runSettingsXml, requestData, new Collection<AttachmentSet>(attachments.ToList()), invokedDataCollector, null, cancellationToken);
        }

        private async Task<Collection<AttachmentSet>> InternalProcessTestRunAttachmentsAsync(string runSettingsXml, IRequestData requestData, Collection<AttachmentSet> attachments, IEnumerable<InvokedDataCollector> invokedDataCollector, ITestRunAttachmentsProcessingEventsHandler eventHandler, CancellationToken cancellationToken)
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
                    Task<Collection<AttachmentSet>> task = Task.Run(async () => await ProcessAttachmentsAsync(runSettingsXml, new Collection<AttachmentSet>(attachments.ToList()), invokedDataCollector, eventHandler, cancellationToken));

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

        private async Task<Collection<AttachmentSet>> ProcessAttachmentsAsync(string runSettingsXml, Collection<AttachmentSet> attachments, IEnumerable<InvokedDataCollector> invokedDataCollector, ITestRunAttachmentsProcessingEventsHandler eventsHandler, CancellationToken cancellationToken)
        {
            if (attachments == null || !attachments.Any()) return attachments;
            var dataCollectionRunSettings = XmlRunSettingsUtilities.GetDataCollectionRunSettings(runSettingsXml);

            var logger = CreateMessageLogger(eventsHandler);
            IReadOnlyDictionary<string, IDataCollectorAttachmentProcessor> dataCollectorAttachmentsProcessors = this.dataCollectorAttachmentsProcessorsFactory.Create(invokedDataCollector?.ToArray());
            for (int i = 0; i < dataCollectorAttachmentsProcessors.Count; i++)
            {
                // TODO: We don't want have all or nothing...if one fails we skip it
                // Add units: first failing(second merge) and all failing(no change to attachments)

                var dataCollectorAttachmentsProcessor = dataCollectorAttachmentsProcessors.ElementAt(i);
                int attachmentsHandlerIndex = i + 1;

                ICollection<Uri> attachmentProcessorUris = dataCollectorAttachmentsProcessor.Value.GetExtensionUris()?.ToList();
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
                                new TestRunAttachmentsProcessingProgressEventArgs(attachmentsHandlerIndex, attachmentProcessorUris, progress, dataCollectorAttachmentsProcessors.Count)));

                        XmlElement configuration = null;
                        var collectorConfiguration = dataCollectionRunSettings?.DataCollectorSettingsList.SingleOrDefault(c => c.FriendlyName == dataCollectorAttachmentsProcessor.Key);
                        if (collectorConfiguration != null && collectorConfiguration.IsEnabled)
                        {
                            configuration = collectorConfiguration.Configuration;
                        }

                        EqtTrace.Info($"TestRunAttachmentsProcessingManager: invocation of data collector attachment processor '{dataCollectorAttachmentsProcessor.Value.GetType().AssemblyQualifiedName}' with configuration '{(configuration == null ? "null" : configuration.OuterXml)}'");
                        ICollection<AttachmentSet> processedAttachments = await dataCollectorAttachmentsProcessor.Value.ProcessAttachmentSetsAsync(
                            configuration,
                            new Collection<AttachmentSet>(attachmentsToBeProcessed),
                            progressReporter,
                            logger,
                            cancellationToken).ConfigureAwait(false);

                        if (processedAttachments != null && processedAttachments.Any())
                        {
                            foreach (var attachment in processedAttachments)
                            {
                                attachments.Add(attachment);
                            }
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
