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

namespace Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.MultiTestRunFinalization
{
    /// <summary>
    /// Orchestrates multi test run finalization operations for the engine communicating with the test host process.
    /// </summary>
    public class MultiTestRunFinalizationManager : IMultiTestRunFinalizationManager
    {
        private readonly DataCollectorAttachmentsHandler attachmentsHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiTestRunFinalizationManager"/> class.
        /// </summary>
        public MultiTestRunFinalizationManager(DataCollectorAttachmentsHandler attachmentsHandler)
        {
            this.attachmentsHandler = attachmentsHandler;
        }

        /// <summary>
        /// Finalizes multi test run
        /// </summary>
        /// <param name="attachments">Attachments</param>
        /// <param name="eventHandler">EventHandler for handling multi test run finalization events from Engine</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task FinalizeMultiTestRunAsync(ICollection<AttachmentSet> attachments, IMultiTestRunFinalizationEventsHandler eventHandler, CancellationToken cancellationToken)
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
                    eventHandler.HandleMultiTestRunFinalizationComplete(attachments);
                });

                var completedTask = await Task.WhenAny(task, taskCompletionSource.Task);

                if (completedTask != task)
                {
                    eventHandler.HandleMultiTestRunFinalizationComplete(null);
                }
            }
            catch (Exception e)
            {
                EqtTrace.Error("MultiTestRunFinalizationManager: Exception in FinalizeMultiTestRunAsync: " + e);

                eventHandler.HandleLogMessage(ObjectModel.Logging.TestMessageLevel.Error, e.Message);
                eventHandler.HandleMultiTestRunFinalizationComplete(null);
            }

        }
    }
}
