// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine
{
    /// <summary>
    /// Orchestrates multi test runs finalization operations.
    /// </summary>
    public interface IMultiTestRunsFinalizationManager
    {
        /// <summary>
        /// Finalizes multi test runs
        /// </summary>
        /// <param name="attachments">Attachments</param>
        /// <param name="eventHandler">EventHandler for handling multi test runs finalization events from Engine</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task FinalizeMultiTestRunsAsync(ICollection<AttachmentSet> attachments, IMultiTestRunsFinalizationEventsHandler eventHandler, CancellationToken cancellationToken);
    }
}
