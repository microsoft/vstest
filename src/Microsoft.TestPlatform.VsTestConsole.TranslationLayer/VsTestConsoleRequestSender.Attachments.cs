// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Tracing;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using TranslationLayerResources = Microsoft.VisualStudio.TestPlatform.VsTestConsole.TranslationLayer.Resources.Resources;

namespace Microsoft.TestPlatform.VsTestConsole.TranslationLayer;

/// <summary>
/// Attachments post-processing protocol handling for <see cref="VsTestConsoleRequestSender"/>.
/// </summary>
internal partial class VsTestConsoleRequestSender
{
    /// <inheritdoc/>
    public Task ProcessTestRunAttachmentsAsync(
        IEnumerable<AttachmentSet> attachments,
        IEnumerable<InvokedDataCollector>? invokedDataCollectors,
        string? runSettings,
        bool collectMetrics,
        ITestRunAttachmentsProcessingEventsHandler testSessionEventsHandler,
        CancellationToken cancellationToken)
    {
        return SendMessageAndListenAndReportAttachmentsProcessingResultAsync(
            attachments,
            invokedDataCollectors,
            runSettings,
            collectMetrics,
            testSessionEventsHandler,
            cancellationToken);
    }

    private async Task SendMessageAndListenAndReportAttachmentsProcessingResultAsync(
        IEnumerable<AttachmentSet> attachments,
        IEnumerable<InvokedDataCollector>? invokedDataCollectors,
        string? runSettings,
        bool collectMetrics,
        ITestRunAttachmentsProcessingEventsHandler eventHandler,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = new TestRunAttachmentsProcessingPayload
            {
                Attachments = attachments,
                InvokedDataCollectors = invokedDataCollectors,
                RunSettings = runSettings,
                CollectMetrics = collectMetrics
            };

            _communicationManager.SendMessage(
                MessageType.TestRunAttachmentsProcessingStart,
                payload);
            var isTestRunAttachmentsProcessingComplete = false;

            using (cancellationToken.Register(() =>
                       _communicationManager.SendMessage(MessageType.TestRunAttachmentsProcessingCancel)))
            {
                // Cycle through the messages that vstest.console sends.
                // Currently each operation is not a separate task since it should not take that
                // much time to complete.
                //
                // This is just a notification.
                while (!isTestRunAttachmentsProcessingComplete)
                {
                    var message = await TryReceiveMessageAsync().ConfigureAwait(false);

                    if (string.Equals(
                            MessageType.TestRunAttachmentsProcessingComplete,
                            message.MessageType))
                    {
                        EqtTrace.Info(
                            "VsTestConsoleRequestSender.SendMessageAndListenAndReportAttachments: Process complete.");

                        var testRunAttachmentsProcessingCompletePayload = _dataSerializer
                            .DeserializePayload<TestRunAttachmentsProcessingCompletePayload>(message);
                        TPDebug.Assert(testRunAttachmentsProcessingCompletePayload is not null, "testRunAttachmentsProcessingCompletePayload is null");

                        eventHandler.HandleTestRunAttachmentsProcessingComplete(
                            testRunAttachmentsProcessingCompletePayload.AttachmentsProcessingCompleteEventArgs!,
                            testRunAttachmentsProcessingCompletePayload.Attachments);
                        isTestRunAttachmentsProcessingComplete = true;
                    }
                    else if (string.Equals(
                                 MessageType.TestRunAttachmentsProcessingProgress,
                                 message.MessageType))
                    {
                        var testRunAttachmentsProcessingProgressPayload = _dataSerializer
                            .DeserializePayload<TestRunAttachmentsProcessingProgressPayload>(message);
                        TPDebug.Assert(testRunAttachmentsProcessingProgressPayload is not null, "testRunAttachmentsProcessingProgressPayload is null");

                        eventHandler.HandleTestRunAttachmentsProcessingProgress(
                            testRunAttachmentsProcessingProgressPayload.AttachmentsProcessingProgressEventArgs!);
                    }
                    else if (string.Equals(MessageType.TestMessage, message.MessageType))
                    {
                        var testMessagePayload = _dataSerializer.DeserializePayload<TestMessagePayload>(message);
                        TPDebug.Assert(testMessagePayload is not null, "testMessagePayload is null");

                        eventHandler.HandleLogMessage(
                            testMessagePayload.MessageLevel,
                            testMessagePayload.Message);
                    }
                    else
                    {
                        EqtTrace.Warning(
                            $"VsTestConsoleRequestSender.SendMessageAndListenAndReportAttachments: Unexpected message received {message.MessageType}.");
                    }
                }
            }
        }
        catch (Exception exception)
        {
            EqtTrace.Error("Aborting Test Session End Operation: {0}", exception);
            eventHandler.HandleLogMessage(
                TestMessageLevel.Error,
                TranslationLayerResources.AbortedTestRunAttachmentsProcessing);
            eventHandler.HandleTestRunAttachmentsProcessingComplete(
                new TestRunAttachmentsProcessingCompleteEventArgs(false, exception),
                null);

            // Earlier we were closing the connection with vstest.console in case of exceptions.
            // Removing that code because vstest.console might be in a healthy state and letting
            // the client know of the error, so that the TL can wait for the next instruction
            // from the client itself.
            // Also, connection termination might not kill the process which could result in
            // files being locked by testhost.
        }
        finally
        {
            _testPlatformEventSource.TranslationLayerTestRunAttachmentsProcessingStop();
        }
    }
}
