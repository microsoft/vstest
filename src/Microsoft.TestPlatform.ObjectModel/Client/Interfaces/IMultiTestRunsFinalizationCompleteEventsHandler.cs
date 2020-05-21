// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface contract for handling multi test runs finalization complete events
    /// </summary>
    public interface IMultiTestRunsFinalizationCompleteEventsHandler : ITestMessageEventHandler
    {
        /// <summary>
        /// Dispatch SessionComplete event to listeners.
        /// </summary>
        /// <param name="attachments">Attachments reprocessed.</param>
        void HandleMultiTestRunsFinalizationComplete(IEnumerable<AttachmentSet> attachments);
    }
}