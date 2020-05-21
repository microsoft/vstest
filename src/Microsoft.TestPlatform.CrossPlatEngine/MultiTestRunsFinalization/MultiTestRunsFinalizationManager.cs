// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol;

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
        public void FinalizeMultiTestRuns(ICollection<AttachmentSet> attachments, IMultiTestRunsFinalizationEventsHandler eventHandler)
        {
            attachmentsHandler.HandleAttachements(attachments);
            eventHandler.HandleMultiTestRunsFinalizationComplete(attachments);
        }
    }
}
