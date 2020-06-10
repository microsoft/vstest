// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine;

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.MultiTestRunsFinalization
{
    /// <summary>
    /// Orchestrates multi test runs finalization operations for the engine communicating with the test host process.
    /// </summary>
    public class MultiTestRunsFinalizationManager : IMultiTestRunsFinalizationManager
    {
        private readonly MultiTestRunsDataCollectorAttachmentsHandler attachmentsHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTestRunsFinalizationManager"/> class.
        /// </summary>
        public MultiTestRunsFinalizationManager(MultiTestRunsDataCollectorAttachmentsHandler attachmentsHandler)
        {
            this.attachmentsHandler = attachmentsHandler;
        }

        /// <summary>
        /// Finalizes multi test runs
        /// </summary>
        /// <param name="attachments">Attachments</param>
        /// <param name="eventHandler">EventHandler for handling multi test runs finalization events from Engine</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task FinalizeMultiTestRunsAsync(ICollection<AttachmentSet> attachments, IMultiTestRunsFinalizationEventsHandler eventHandler, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var taskCompletionSource = new TaskCompletionSource<object>();
                cancellationToken.Register(() =>
                {
                    taskCompletionSource.TrySetCanceled();
                });

                Task task = Task.Run(() =>
                {
                    attachmentsHandler.HandleAttachements(attachments, cancellationToken);
                    eventHandler.HandleMultiTestRunsFinalizationComplete(attachments);
                });

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task);

                if (completedTask != task)
                {
                    eventHandler.HandleMultiTestRunsFinalizationComplete(null);
                }
            }
            catch (Exception e)
            {
                EqtTrace.Error("MultiTestRunsFinalizationManager: Exception in FinalizeMultiTestRunsAsync: " + e);

                eventHandler.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Error, e.Message);
                eventHandler.HandleMultiTestRunsFinalizationComplete(null);
            }

        }
    }
}
