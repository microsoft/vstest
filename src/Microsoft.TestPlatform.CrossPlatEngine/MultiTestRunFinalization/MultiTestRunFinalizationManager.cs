// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public async Task FinalizeMultiTestRunAsync(ICollection<AttachmentSet> attachments, IMultiTestRunFinalizationEventsHandler eventHandler, CancellationToken cancellationToken)
        {
            await InternalFinalizeMultiTestRunAsync(new Collection<AttachmentSet>(attachments.ToList()), eventHandler, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<Collection<AttachmentSet>> FinalizeMultiTestRunAsync(ICollection<AttachmentSet> attachments, CancellationToken cancellationToken)
        {
            return InternalFinalizeMultiTestRunAsync(new Collection<AttachmentSet>(attachments.ToList()), null, cancellationToken);
        }

        private async Task<Collection<AttachmentSet>> InternalFinalizeMultiTestRunAsync(Collection<AttachmentSet> attachments, IMultiTestRunFinalizationEventsHandler eventHandler, CancellationToken cancellationToken)
        {
            try
            {
                testPlatformEventSource.MultiTestRunFinalizationStart(attachments?.Count ?? 0);

                cancellationToken.ThrowIfCancellationRequested();                

                var taskCompletionSource = new TaskCompletionSource<object>();
                using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled()))
                {
                    Task task = Task.Run(() =>
                    {
                        HandleAttachments(attachments, cancellationToken);
                    });

                    var completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);

                    if (completedTask == task)
                    {
                        await task;
                        eventHandler?.HandleMultiTestRunFinalizationComplete(attachments);
                        testPlatformEventSource.MultiTestRunFinalizationStop(attachments.Count);
                        return attachments;
                    }
                    else
                    {
                        eventHandler?.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Informational, "Finalization was cancelled.");
                        eventHandler?.HandleMultiTestRunFinalizationComplete(null);
                        testPlatformEventSource.MultiTestRunFinalizationStop(0);
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                EqtTrace.Error("MultiTestRunFinalizationManager: Exception in FinalizeMultiTestRunAsync: " + e);

                eventHandler?.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Error, e.Message);
                eventHandler?.HandleMultiTestRunFinalizationComplete(null);
                testPlatformEventSource.MultiTestRunFinalizationStop(0);
                return null;
            }
        }

        private void HandleAttachments(ICollection<AttachmentSet> attachments, CancellationToken cancellationToken)
        {
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

                        ICollection<AttachmentSet> processedAttachments = dataCollectorAttachmentsHandler.HandleDataCollectionAttachmentSets(new Collection<AttachmentSet>(attachmentsToBeProcessed), cancellationToken);
                        foreach (var attachment in processedAttachments)
                        {
                            attachments.Add(attachment);
                        }
                    }
                }
            }
        }
    }
}
