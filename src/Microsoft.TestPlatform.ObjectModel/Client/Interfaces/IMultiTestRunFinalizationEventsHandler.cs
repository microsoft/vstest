// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
{
    /// <summary>
    /// Interface contract for handling multi test run finalization complete events
    /// </summary>
    public interface IMultiTestRunFinalizationEventsHandler : ITestMessageEventHandler
    {
        /// <summary>
        /// Dispatch MultiTestRunFinalizationComplete event to listeners.
        /// </summary>
        /// <param name="attachments">Attachments reprocessed.</param>
        void HandleMultiTestRunFinalizationComplete(ICollection<AttachmentSet> attachments);
    }
}