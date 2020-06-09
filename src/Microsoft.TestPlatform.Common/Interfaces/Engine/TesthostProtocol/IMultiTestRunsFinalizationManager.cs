// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.TesthostProtocol
{
    /// <summary>
    /// Orchestrates multi test runs finalization operations for the engine communicating with the test host process.
    /// </summary>
    public interface IMultiTestRunsFinalizationManager
    {
        /// <summary>
        /// Finalizes multi test runs
        /// </summary>
        /// <param name="attachments">Attachments</param>
        /// <param name="eventHandler">EventHandler for handling multi test runs finalization events from Engine</param>
        void FinalizeMultiTestRuns(ICollection<AttachmentSet> attachments, IMultiTestRunsFinalizationEventsHandler eventHandler);

        /// <summary>
        /// Aborts multi test runs finalization
        /// </summary>
        void Abort();
    }
}
