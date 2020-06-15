// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    /// <summary>
    /// Orchestrates multi test run finalization operations.
    /// </summary>
    internal interface IMultiTestRunFinalizationManager
    {
        /// <summary>
        /// Finalizes multi test run and provides results through handler
        /// </summary>
        /// <param name="attachments">Attachments</param>
        /// <param name="eventHandler">EventHandler for handling multi test run finalization events from Engine</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task FinalizeMultiTestRunAsync(ICollection<AttachmentSet> attachments, IMultiTestRunFinalizationEventsHandler eventHandler, CancellationToken cancellationToken);

        /// <summary>
        /// Finalizes multi test
        /// </summary>
        /// <param name="attachments">Attachments</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<Collection<AttachmentSet>> FinalizeMultiTestRunAsync(ICollection<AttachmentSet> attachments, CancellationToken cancellationToken);
    }
}
